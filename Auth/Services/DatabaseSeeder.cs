using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PDVNow.Auth.Entities;
using PDVNow.Data;

namespace PDVNow.Auth.Services;

public sealed class DatabaseSeeder
{
    private readonly AppDbContext _db;
    private readonly SeedAdminOptions _seedAdmin;

    public DatabaseSeeder(AppDbContext db, IOptions<SeedAdminOptions> seedAdmin)
    {
        _db = db;
        _seedAdmin = seedAdmin.Value;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!_seedAdmin.Enabled)
            return;

        var anyUsers = await _db.Users.AnyAsync(cancellationToken);
        if (anyUsers)
            return;

        if (string.IsNullOrWhiteSpace(_seedAdmin.Password))
            throw new InvalidOperationException("SeedAdmin:Password não configurado. Defina via User Secrets ou variável de ambiente (SeedAdmin__Password).");

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = _seedAdmin.Username.Trim(),
            UserType = UserType.Admin,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var hasher = new PasswordHasher<AppUser>();
        user.PasswordHash = hasher.HashPassword(user, _seedAdmin.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
