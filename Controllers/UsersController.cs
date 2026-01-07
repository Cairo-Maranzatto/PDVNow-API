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

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> List(
        [FromQuery] string? query,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (skip < 0) skip = 0;
        if (take <= 0) take = 50;
        if (take > 200) take = 200;

        IQueryable<AppUser> users = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            users = users.Where(u =>
                u.Username.Contains(q) ||
                (u.Email != null && u.Email.Contains(q)));
        }

        var result = await users
            .OrderBy(u => u.Username)
            .Skip(skip)
            .Take(take)
            .Select(u => new UserResponse(
                u.Id,
                u.Username,
                u.Email,
                u.UserType,
                u.IsActive,
                u.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest();

        var username = request.Username.Trim().ToLowerInvariant();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

        var usernameExists = await _db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Username.ToLower() == username, cancellationToken);
        if (usernameExists)
            return Conflict("Username já existe.");

        if (email is not null)
        {
            var emailExists = await _db.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower(), cancellationToken);
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
            Excluded = false,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new UserResponse(
            user.Id,
            user.Username,
            user.Email,
            user.UserType,
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
            user.UserType,
            user.IsActive,
            user.CreatedAtUtc));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserResponse>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
            return NotFound();

        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

        if (email is not null && email != user.Email)
        {
            var emailExists = await _db.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Id != id && u.Email != null && u.Email.ToLower() == email.ToLower(), cancellationToken);
            if (emailExists)
                return Conflict("Email já existe.");
        }

        user.Email = email;
        user.UserType = request.UserType;
        user.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new UserResponse(
            user.Id,
            user.Username,
            user.Email,
            user.UserType,
            user.IsActive,
            user.CreatedAtUtc));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
            return NotFound();

        user.Excluded = true;
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
