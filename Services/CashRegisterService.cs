using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PDVNow.Auth;
using PDVNow.Auth.Services;
using PDVNow.Data;
using PDVNow.Entities;

namespace PDVNow.Services;

public sealed class CashRegisterService
{
    private readonly AppDbContext _db;
    private readonly AdminOverrideCodeService _overrideCodes;
    private readonly CashRegisterOptions _options;

    public CashRegisterService(
        AppDbContext db,
        AdminOverrideCodeService overrideCodes,
        IOptions<CashRegisterOptions> options)
    {
        _db = db;
        _overrideCodes = overrideCodes;
        _options = options.Value;
    }

    public async Task<CashRegister> GetOrCreateRegisterAsync(
        string name,
        string? location,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();

        var existing = await _db.CashRegisters
            .SingleOrDefaultAsync(r => r.Name == normalizedName, cancellationToken);

        if (existing is not null)
        {
            if (!string.IsNullOrWhiteSpace(location) && existing.Location != location.Trim())
            {
                existing.Location = location.Trim();
                existing.UpdatedAtUtc = nowUtc;
                await _db.SaveChangesAsync(cancellationToken);
            }

            return existing;
        }

        var created = new CashRegister
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim(),
            IsActive = true,
            CreatedAtUtc = nowUtc
        };

        _db.CashRegisters.Add(created);
        await _db.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<CashSession> OpenSessionAsync(
        Guid userId,
        bool isAdmin,
        string cashRegisterName,
        string? location,
        decimal openingFloatAmount,
        string? overrideCode,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cashRegisterName))
            throw new InvalidOperationException("CashRegisterName é obrigatório.");

        if (openingFloatAmount < 0)
            throw new InvalidOperationException("OpeningFloatAmount inválido.");

        if (!isAdmin)
        {
            var ok = await _overrideCodes.ValidateAndConsumeAsync(
                overrideCode ?? string.Empty,
                AdminOverridePurpose.OpenSession,
                userId,
                nowUtc,
                cancellationToken);

            if (ok is null)
                throw new InvalidOperationException("Código de liberação inválido ou expirado.");
        }

        var register = await GetOrCreateRegisterAsync(cashRegisterName, location, nowUtc, cancellationToken);

        var alreadyOpen = await _db.CashSessions
            .AnyAsync(s => s.CashRegisterId == register.Id && s.ClosedAtUtc == null, cancellationToken);

        if (alreadyOpen)
            throw new InvalidOperationException("Já existe uma sessão aberta para este caixa.");

        var session = new CashSession
        {
            Id = Guid.NewGuid(),
            CashRegisterId = register.Id,
            OpenedByUserId = userId,
            OpenedAtUtc = nowUtc,
            OpeningFloatAmount = openingFloatAmount
        };

        _db.CashSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        return session;
    }

    public async Task<CashSession> CloseSessionAsync(
        Guid userId,
        bool isAdmin,
        Guid cashRegisterId,
        IReadOnlyList<(decimal denomination, int quantity)> denominations,
        string? notes,
        string? overrideCode,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (!isAdmin)
        {
            var ok = await _overrideCodes.ValidateAndConsumeAsync(
                overrideCode ?? string.Empty,
                AdminOverridePurpose.CloseSession,
                userId,
                nowUtc,
                cancellationToken);

            if (ok is null)
                throw new InvalidOperationException("Código de liberação inválido ou expirado.");
        }

        var session = await _db.CashSessions
            .SingleOrDefaultAsync(s => s.CashRegisterId == cashRegisterId && s.ClosedAtUtc == null, cancellationToken);

        if (session is null)
            throw new InvalidOperationException("Não existe sessão aberta para este caixa.");

        var total = 0m;
        foreach (var (denomination, quantity) in denominations)
        {
            if (denomination <= 0)
                throw new InvalidOperationException("Denomination inválida.");
            if (quantity < 0)
                throw new InvalidOperationException("Quantity inválida.");

            total += denomination * quantity;
        }

        session.ClosedAtUtc = nowUtc;
        session.ClosedByUserId = userId;
        session.ClosingCountedAmount = total;
        session.ClosingNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        var denomEntities = denominations
            .Where(x => x.quantity > 0)
            .Select(x => new CashSessionDenomination
            {
                Id = Guid.NewGuid(),
                CashSessionId = session.Id,
                Denomination = x.denomination,
                Quantity = x.quantity
            });

        _db.CashSessionDenominations.AddRange(denomEntities);

        await _db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<CashMovement> CreateMovementAsync(
        Guid userId,
        bool isAdmin,
        Guid cashRegisterId,
        CashMovementType type,
        decimal amount,
        string? notes,
        string? overrideCode,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Amount inválido.");

        var session = await _db.CashSessions
            .SingleOrDefaultAsync(s => s.CashRegisterId == cashRegisterId && s.ClosedAtUtc == null, cancellationToken);

        if (session is null)
            throw new InvalidOperationException("Não existe sessão aberta para este caixa.");

        AdminOverrideCode? consumed = null;

        if (!isAdmin)
        {
            var requires = type switch
            {
                CashMovementType.Supply => _options.RequireOverrideForSupply,
                CashMovementType.Withdrawal => _options.RequireOverrideForWithdrawal,
                _ => false
            };

            if (requires)
            {
                consumed = await _overrideCodes.ValidateAndConsumeAsync(
                    overrideCode ?? string.Empty,
                    AdminOverridePurpose.CashMovement,
                    userId,
                    nowUtc,
                    cancellationToken);

                if (consumed is null)
                    throw new InvalidOperationException("Código de liberação inválido ou expirado.");
            }
        }

        var movement = new CashMovement
        {
            Id = Guid.NewGuid(),
            CashSessionId = session.Id,
            Type = type,
            Amount = amount,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedByUserId = userId,
            CreatedAtUtc = nowUtc,
            AdminOverrideCodeId = consumed?.Id
        };

        _db.CashMovements.Add(movement);
        await _db.SaveChangesAsync(cancellationToken);
        return movement;
    }

    public async Task<CashSessionReopenEvent> ReopenSessionAsync(
        Guid adminUserId,
        Guid cashSessionId,
        string justification,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var session = await _db.CashSessions
            .SingleOrDefaultAsync(s => s.Id == cashSessionId, cancellationToken);

        if (session is null)
            throw new InvalidOperationException("Sessão não encontrada.");

        if (session.ClosedAtUtc is null)
            throw new InvalidOperationException("Sessão já está aberta.");

        var existsOpen = await _db.CashSessions
            .AnyAsync(s => s.CashRegisterId == session.CashRegisterId && s.ClosedAtUtc == null, cancellationToken);

        if (existsOpen)
            throw new InvalidOperationException("Já existe uma sessão aberta para este caixa.");

        if (string.IsNullOrWhiteSpace(justification))
            throw new InvalidOperationException("Justificativa é obrigatória.");

        session.ClosedAtUtc = null;
        session.ClosedByUserId = null;

        var evt = new CashSessionReopenEvent
        {
            Id = Guid.NewGuid(),
            CashSessionId = session.Id,
            ReopenedByAdminUserId = adminUserId,
            ReopenedAtUtc = nowUtc,
            Justification = justification.Trim()
        };

        _db.CashSessionReopenEvents.Add(evt);
        await _db.SaveChangesAsync(cancellationToken);

        return evt;
    }
}
