using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PDVNow.Auth;
using PDVNow.Auth.Dtos;
using PDVNow.Auth.Entities;
using PDVNow.Auth.Services;
using PDVNow.Data;
using PDVNow.Entities;

namespace PDVNow.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<AppUser> _passwordHasher;
    private readonly JwtTokenService _jwtTokenService;
    private readonly JwtOptions _jwtOptions;
    private readonly CashRegisterOptions _cashRegisterOptions;

    public AuthController(
        AppDbContext db,
        JwtTokenService jwtTokenService,
        Microsoft.Extensions.Options.IOptions<JwtOptions> jwtOptions,
        Microsoft.Extensions.Options.IOptions<CashRegisterOptions> cashRegisterOptions)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
        _jwtOptions = jwtOptions.Value;
        _cashRegisterOptions = cashRegisterOptions.Value;
        _passwordHasher = new PasswordHasher<AppUser>();
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
            return BadRequest();

        var normalized = request.Username.Trim().ToLowerInvariant();

        var user = await _db.Users
            .SingleOrDefaultAsync(u => u.Username.ToLower() == normalized, cancellationToken);

        if (user is null || !user.IsActive)
            return Unauthorized();

        var verify = _passwordHasher.VerifyHashedPassword(
            user, user.PasswordHash, request.Password);

        if (verify == PasswordVerificationResult.Failed)
            return Unauthorized();

        var nowUtc = DateTimeOffset.UtcNow;

        var (accessToken, accessExpiresAtUtc) =
            _jwtTokenService.CreateAccessToken(user, nowUtc);

        var refreshToken = RefreshTokenGenerator.GenerateOpaqueToken();
        var refreshExpiresAtUtc = nowUtc.AddDays(_jwtOptions.RefreshTokenDays);

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = TokenHasher.Sha256Base64(refreshToken),
            ExpiresAtUtc = refreshExpiresAtUtc,
            CreatedAtUtc = nowUtc
        });

        await _db.SaveChangesAsync(cancellationToken);

        SetAuthCookies(accessToken, refreshToken, accessExpiresAtUtc, refreshExpiresAtUtc);

        return Ok(new AuthResponseDto(
            user.Id,
            user.Username,
            user.UserType));
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("generate-code")]
    public async Task<ActionResult<GenerateOverrideCodeResponse>> GenerateCode(
        [FromBody] GenerateOverrideCodeRequest request,
        CancellationToken cancellationToken)
    {
        var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(adminIdStr, out var adminId))
            return Unauthorized();

        var nowUtc = DateTimeOffset.UtcNow;

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var expiresAtUtc = nowUtc.AddSeconds(_cashRegisterOptions.OverrideCodeExpirationSeconds);

        var entity = new AdminOverrideCode
        {
            Id = Guid.NewGuid(),
            CodeHash = TokenHasher.Sha256Base64(code),
            Purpose = request.Purpose,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = expiresAtUtc,
            CreatedByAdminUserId = adminId,
            Justification = string.IsNullOrWhiteSpace(request.Justification) ? null : request.Justification.Trim()
        };

        _db.AdminOverrideCodes.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new GenerateOverrideCodeResponse(code, expiresAtUtc));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh(
        CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue("refresh_token", out var refreshToken) ||
            string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized();

        var tokenHash = TokenHasher.Sha256Base64(refreshToken);

        var stored = await _db.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (stored is null || stored.IsRevoked || stored.IsExpired)
            return Unauthorized();

        var user = stored.User;
        if (!user.IsActive)
            return Unauthorized();

        var nowUtc = DateTimeOffset.UtcNow;

        // Revoga o refresh antigo
        stored.RevokedAtUtc = nowUtc;

        var (newAccessToken, accessExpiresAtUtc) =
            _jwtTokenService.CreateAccessToken(user, nowUtc);

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

        SetAuthCookies(
            newAccessToken,
            newRefreshToken,
            accessExpiresAtUtc,
            refreshExpiresAtUtc);

        return Ok(new AuthResponseDto(
            user.Id,
            user.Username,
            user.UserType));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
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

        Response.Cookies.Delete("access_token");
        Response.Cookies.Delete("refresh_token");

        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthResponseDto>> Me(
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var id))
            return Unauthorized();

        var user = await _db.Users
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null || !user.IsActive)
            return Unauthorized();

        return Ok(new AuthResponseDto(
            user.Id,
            user.Username,
            user.UserType));
    }

    private void SetAuthCookies(
        string accessToken,
        string refreshToken,
        DateTimeOffset accessExpires,
        DateTimeOffset refreshExpires)
    {
        Response.Cookies.Append("access_token", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = accessExpires,
            Path = "/"
        });

        Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = refreshExpires,
            Path = "/api/v1/auth"
        });
    }
}
