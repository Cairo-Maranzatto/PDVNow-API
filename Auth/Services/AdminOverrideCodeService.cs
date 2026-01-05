using Microsoft.EntityFrameworkCore;
using PDVNow.Auth.Services;
using PDVNow.Data;
using PDVNow.Entities;

namespace PDVNow.Auth.Services;

public sealed class AdminOverrideCodeService
{
    private readonly AppDbContext _db;

    public AdminOverrideCodeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdminOverrideCode?> ValidateAndConsumeAsync(
        string code,
        AdminOverridePurpose purpose,
        Guid usedByUserId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var normalized = code.Trim();
        if (normalized.Length != 6 || !normalized.All(char.IsDigit))
            return null;

        var hash = TokenHasher.Sha256Base64(normalized);

        var entity = await _db.AdminOverrideCodes
            .SingleOrDefaultAsync(x =>
                x.CodeHash == hash &&
                x.UsedAtUtc == null,
                cancellationToken);

        if (entity is null)
            return null;

        if (entity.ExpiresAtUtc <= nowUtc)
            return null;

        entity.UsedAtUtc = nowUtc;
        entity.UsedByUserId = usedByUserId;

        await _db.SaveChangesAsync(cancellationToken);

        return entity;
    }
}
