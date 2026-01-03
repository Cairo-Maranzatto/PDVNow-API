using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PDVNow.Auth;
using PDVNow.Auth.Dtos;
using PDVNow.Auth.Entities;
using PDVNow.Auth.Services;
using PDVNow.Data;

namespace PDVNow.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<AppUser> _passwordHasher;
    private readonly JwtTokenService _jwtTokenService;
    private readonly JwtOptions _jwtOptions;

    public AuthController(
        AppDbContext db,
        JwtTokenService jwtTokenService,
        Microsoft.Extensions.Options.IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
        _jwtOptions = jwtOptions.Value;
        _passwordHasher = new PasswordHasher<AppUser>();
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest();

        var normalized = request.Username.Trim().ToLowerInvariant();

        var user = await _db.Users
            .SingleOrDefaultAsync(u => u.Username.ToLower() == normalized, cancellationToken);

        if (user is null || !user.IsActive)
            return Unauthorized();

        var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verify == PasswordVerificationResult.Failed)
            return Unauthorized();

        var nowUtc = DateTimeOffset.UtcNow;
        var (accessToken, accessExpiresAtUtc) = _jwtTokenService.CreateAccessToken(user, nowUtc);

        var refreshToken = RefreshTokenGenerator.GenerateOpaqueToken();
        var refreshExpiresAtUtc = nowUtc.AddDays(_jwtOptions.RefreshTokenDays);

        var refreshEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = TokenHasher.Sha256Base64(refreshToken),
            ExpiresAtUtc = refreshExpiresAtUtc,
            CreatedAtUtc = nowUtc
        };

        _db.RefreshTokens.Add(refreshEntity);
        await _db.SaveChangesAsync(cancellationToken);

        // Armazena tokens em cookies HttpOnly
        SetAuthCookies(accessToken, refreshToken, accessExpiresAtUtc, refreshExpiresAtUtc);

        // Retorna apenas dados não-sensíveis
        return Ok(new AuthResponseDto(
            user.Id,
            user.Username,
            user.UserType.ToString()));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh(CancellationToken cancellationToken)
    {
        // Lê refresh token do cookie
        if (!Request.Cookies.TryGetValue("refresh_token", out var refreshToken) ||
            string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized();

        // Extrai userId do access token atual (mesmo expirado)
        var userId = GetUserIdFromToken();
        if (userId == Guid.Empty)
            return Unauthorized();

        var tokenHash = TokenHasher.Sha256Base64(refreshToken);

        var stored = await _db.RefreshTokens
            .SingleOrDefaultAsync(t => t.UserId == userId && t.TokenHash == tokenHash, cancellationToken);

        if (stored is null || stored.IsRevoked || stored.IsExpired)
            return Unauthorized();

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || !user.IsActive)
            return Unauthorized();

        var nowUtc = DateTimeOffset.UtcNow;
        stored.RevokedAtUtc = nowUtc;

        var (accessToken, accessExpiresAtUtc) = _jwtTokenService.CreateAccessToken(user, nowUtc);

        var newRefreshToken = RefreshTokenGenerator.GenerateOpaqueToken();
        var refreshExpiresAtUtc = nowUtc.AddDays(_jwtOptions.RefreshTokenDays);

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = TokenHasher.Sha256Base64(newRefreshToken),
            ExpiresAtUtc = refreshExpiresAtUtc,
            CreatedAtUtc = nowUtc
        });

        await _db.SaveChangesAsync(cancellationToken);

        // Atualiza cookies com novos tokens
        SetAuthCookies(accessToken, newRefreshToken, accessExpiresAtUtc, refreshExpiresAtUtc);

        return Ok(new AuthResponseDto(
            user.Id,
            user.Username,
            user.UserType.ToString()));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        // Revoga refresh token se existir
        if (Request.Cookies.TryGetValue("refresh_token", out var refreshToken))
        {
            var tokenHash = TokenHasher.Sha256Base64(refreshToken);
            var stored = await _db.RefreshTokens
                .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

            if (stored is not null)
            {
                stored.RevokedAtUtc = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        // Remove cookies
        Response.Cookies.Delete("access_token");
        Response.Cookies.Delete("refresh_token");

        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthResponseDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromToken();
        if (userId == Guid.Empty)
            return Unauthorized();

        var user = await _db.Users
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null || !user.IsActive)
            return Unauthorized();

        return Ok(new AuthResponseDto(
            user.Id,
            user.Username,
            user.UserType.ToString()));
    }

    private void SetAuthCookies(
        string accessToken,
        string refreshToken,
        DateTimeOffset accessExpires,
        DateTimeOffset refreshExpires)
    {
        // Access Token Cookie
        Response.Cookies.Append("access_token", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = accessExpires,
            Path = "/"
        });

        // Refresh Token Cookie (mais restritivo)
        Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = refreshExpires,
            Path = "/api/v1/auth" // Apenas acessível nos endpoints de auth
        });
    }

    private Guid GetUserIdFromToken()
    {
        if (!Request.Cookies.TryGetValue("access_token", out var token))
            return Guid.Empty;

        var handler = new JwtSecurityTokenHandler();

        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtOptions.Audience,
                ValidateLifetime = false, // Permite tokens expirados (útil para refresh)
                ClockSkew = TimeSpan.Zero
            };

            var principal = handler.ValidateToken(token, validationParameters, out _);
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);

            return Guid.TryParse(userIdClaim?.Value, out var userId) ? userId : Guid.Empty;
        }
        catch
        {
            return Guid.Empty;
        }
    }
}