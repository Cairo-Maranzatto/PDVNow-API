using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PDVNow.Auth;
using PDVNow.Auth.Services;
using PDVNow.Data;
using PDVNow.Dtos.Cash;
using PDVNow.Entities;
using PDVNow.Services;

namespace PDVNow.Controllers;

[ApiController]
[Route("api/v1/cash")]
[Authorize(Policy = "PdvAccess")]
public sealed class CashController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CashRegisterService _cash;

    public CashController(AppDbContext db, CashRegisterService cash)
    {
        _db = db;
        _cash = cash;
    }

    [HttpGet("registers")]
    public async Task<ActionResult<IReadOnlyList<CashRegisterResponse>>> ListRegisters(CancellationToken cancellationToken)
    {
        var registers = await _db.CashRegisters
            .AsNoTracking()
            .OrderBy(r => r.Code)
            .Select(r => new CashRegisterResponse(
                r.Id,
                r.Code,
                r.Name,
                r.Location,
                r.IsActive,
                r.CreatedAtUtc,
                r.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(registers);
    }

    [HttpGet("registers/{cashRegisterId:guid}/session")]
    public async Task<ActionResult<CashSessionResponse>> GetOpenSession(
        [FromRoute] Guid cashRegisterId,
        CancellationToken cancellationToken)
    {
        var session = await _db.CashSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.CashRegisterId == cashRegisterId && s.ClosedAtUtc == null, cancellationToken);

        if (session is null)
            return NotFound();

        return Ok(new CashSessionResponse(
            session.Id,
            session.CashRegisterId,
            session.OpenedByUserId,
            session.ClosedByUserId,
            session.OpenedAtUtc,
            session.ClosedAtUtc,
            session.OpeningFloatAmount,
            session.ClosingCountedAmount,
            session.ClosingNotes));
    }

    [HttpPost("open")]
    public async Task<ActionResult<CashSessionResponse>> Open(
        [FromBody] OpenCashSessionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole(UserType.Admin.ToString());

        var session = await _cash.OpenSessionAsync(
            userId,
            isAdmin,
            request.CashRegisterName,
            request.Location,
            request.OpeningFloatAmount,
            request.OverrideCode,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return Ok(new CashSessionResponse(
            session.Id,
            session.CashRegisterId,
            session.OpenedByUserId,
            session.ClosedByUserId,
            session.OpenedAtUtc,
            session.ClosedAtUtc,
            session.OpeningFloatAmount,
            session.ClosingCountedAmount,
            session.ClosingNotes));
    }

    [HttpPost("close")]
    public async Task<ActionResult<CashSessionResponse>> Close(
        [FromBody] CloseCashSessionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole(UserType.Admin.ToString());

        var denoms = request.Denominations
            .Select(x => (x.Denomination, x.Quantity))
            .ToList();

        var session = await _cash.CloseSessionAsync(
            userId,
            isAdmin,
            request.CashRegisterId,
            denoms,
            request.Notes,
            request.OverrideCode,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return Ok(new CashSessionResponse(
            session.Id,
            session.CashRegisterId,
            session.OpenedByUserId,
            session.ClosedByUserId,
            session.OpenedAtUtc,
            session.ClosedAtUtc,
            session.OpeningFloatAmount,
            session.ClosingCountedAmount,
            session.ClosingNotes));
    }

    [HttpPost("movements")]
    public async Task<ActionResult> CreateMovement(
        [FromBody] CreateCashMovementRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole(UserType.Admin.ToString());

        var movement = await _cash.CreateMovementAsync(
            userId,
            isAdmin,
            request.CashRegisterId,
            request.Type,
            request.Amount,
            request.Notes,
            request.OverrideCode,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return Ok(new
        {
            movement.Id,
            movement.CashSessionId,
            movement.Type,
            movement.Amount,
            movement.Notes,
            movement.CreatedByUserId,
            movement.CreatedAtUtc
        });
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("reopen/{cashSessionId:guid}")]
    public async Task<ActionResult> Reopen(
        [FromRoute] Guid cashSessionId,
        [FromBody] string justification,
        CancellationToken cancellationToken)
    {
        var adminId = GetUserId();

        var evt = await _cash.ReopenSessionAsync(
            adminId,
            cashSessionId,
            justification,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return Ok(new
        {
            evt.Id,
            evt.CashSessionId,
            evt.ReopenedByAdminUserId,
            evt.ReopenedAtUtc,
            evt.Justification
        });
    }

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            throw new InvalidOperationException("Usuário não autenticado.");
        return userId;
    }
}
