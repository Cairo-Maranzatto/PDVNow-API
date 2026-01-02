using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PDVNow.Auth.Entities;
using PDVNow.Data;
using PDVNow.Dtos.Users;

namespace PDVNow.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = "AdminOnly")]
public sealed class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<AppUser> _passwordHasher;

    public UsersController(AppDbContext db)
    {
        _db = db;
        _passwordHasher = new PasswordHasher<AppUser>();
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest();

        var username = request.Username.Trim().ToLowerInvariant();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

        var usernameExists = await _db.Users.AnyAsync(u => u.Username.ToLower() == username, cancellationToken);
        if (usernameExists)
            return Conflict("Username já existe.");

        if (email is not null)
        {
            var emailExists = await _db.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower(), cancellationToken);
            if (emailExists)
                return Conflict("Email já existe.");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            UserType = request.UserType,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new UserResponse(
            user.Id,
            user.Username,
            user.Email,
            user.UserType.ToString(),
            user.IsActive,
            user.CreatedAtUtc));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
            return NotFound();

        return Ok(new UserResponse(
            user.Id,
            user.Username,
            user.Email,
            user.UserType.ToString(),
            user.IsActive,
            user.CreatedAtUtc));
    }
}
