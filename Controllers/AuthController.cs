using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
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

        return Ok(new AuthResponse(
            user.Id,
            user.Username,
            user.UserType.ToString(),
            accessToken,
            accessExpiresAtUtc,
            refreshToken,
            refreshExpiresAtUtc));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest();

        var tokenHash = TokenHasher.Sha256Base64(request.RefreshToken);

        var stored = await _db.RefreshTokens
            .SingleOrDefaultAsync(t => t.UserId == request.UserId && t.TokenHash == tokenHash, cancellationToken);

        if (stored is null || stored.IsRevoked || stored.IsExpired)
            return Unauthorized();

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
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

        return Ok(new AuthResponse(
            user.Id,
            user.Username,
            user.UserType.ToString(),
            accessToken,
            accessExpiresAtUtc,
            newRefreshToken,
            refreshExpiresAtUtc));
    }
}
