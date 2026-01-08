using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PDVNow.Auth;
using PDVNow.Data;
using PDVNow.Dtos.Sales;
using PDVNow.Entities;
using PDVNow.Services;

namespace PDVNow.Controllers;

[ApiController]
[Route("api/v1/sales")]
[Authorize(Policy = "PdvAccess")]
public sealed class SalesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SalesService _sales;

    public SalesController(AppDbContext db, SalesService sales)
    {
        _db = db;
        _sales = sales;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SaleResponse>>> List(
        [FromQuery] Guid? cashRegisterId,
        [FromQuery] Guid? cashSessionId,
        [FromQuery] Guid? customerId,
        [FromQuery] SaleStatus? status,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (skip < 0) skip = 0;
        if (take <= 0) take = 50;
        if (take > 200) take = 200;

        IQueryable<Sale> query = _db.Sales.AsNoTracking();

        if (cashRegisterId.HasValue)
            query = query.Where(s => s.CashRegisterId == cashRegisterId.Value);

        if (cashSessionId.HasValue)
            query = query.Where(s => s.CashSessionId == cashSessionId.Value);

        if (customerId.HasValue)
            query = query.Where(s => s.CustomerId == customerId.Value);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        var result = await query
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(s => new SaleResponse(
                s.Id,
                s.Code,
                s.Status,
                s.CashRegisterId,
                s.CashSessionId,
                s.CustomerId,
                s.SubtotalAmount,
                s.ItemDiscountTotalAmount,
                s.SaleDiscountAmount,
                s.TotalAmount,
                s.PaidAmount,
                s.CreatedByUserId,
                s.CreatedAtUtc,
                s.UpdatedByUserId,
                s.UpdatedAtUtc,
                s.FinalizedByUserId,
                s.FinalizedAtUtc,
                s.CancelledByUserId,
                s.CancelledAtUtc,
                s.CancelReason))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SaleDetailsResponse>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var (sale, items, payments) = await _sales.GetSaleDetailsAsync(id, cancellationToken);
        var (total, paid, remaining) = await _sales.GetBalanceAsync(id, cancellationToken);

        var response = new SaleDetailsResponse(
            new SaleResponse(
                sale.Id,
                sale.Code,
                sale.Status,
                sale.CashRegisterId,
                sale.CashSessionId,
                sale.CustomerId,
                sale.SubtotalAmount,
                sale.ItemDiscountTotalAmount,
                sale.SaleDiscountAmount,
                sale.TotalAmount,
                sale.PaidAmount,
                sale.CreatedByUserId,
                sale.CreatedAtUtc,
                sale.UpdatedByUserId,
                sale.UpdatedAtUtc,
                sale.FinalizedByUserId,
                sale.FinalizedAtUtc,
                sale.CancelledByUserId,
                sale.CancelledAtUtc,
                sale.CancelReason),
            items.Select(i => new SaleItemResponse(
                i.Id,
                i.ProductId,
                i.Quantity,
                i.UnitPriceOriginal,
                i.UnitPriceFinal,
                i.DiscountAmount,
                i.LineTotalAmount,
                i.CreatedByUserId,
                i.CreatedAtUtc,
                i.UpdatedByUserId,
                i.UpdatedAtUtc)).ToList(),
            payments.Select(p => new SalePaymentResponse(
                p.Id,
                p.Method,
                p.Amount,
                p.AmountReceived,
                p.ChangeGiven,
                p.AuthorizationCode,
                p.CreatedByUserId,
                p.CreatedAtUtc)).ToList(),
            new SaleBalanceResponse(total, paid, remaining));

        return Ok(response);
    }

    [HttpGet("{id:guid}/balance")]
    public async Task<ActionResult<SaleBalanceResponse>> GetBalance(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var (total, paid, remaining) = await _sales.GetBalanceAsync(id, cancellationToken);
        return Ok(new SaleBalanceResponse(total, paid, remaining));
    }

    [HttpPost]
    public async Task<ActionResult<SaleResponse>> Create(
        [FromBody] CreateSaleRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var nowUtc = DateTimeOffset.UtcNow;

        var sale = await _sales.CreateSaleAsync(
            userId,
            request.CashRegisterId,
            request.CustomerId,
            nowUtc,
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = sale.Id }, new SaleResponse(
            sale.Id,
            sale.Code,
            sale.Status,
            sale.CashRegisterId,
            sale.CashSessionId,
            sale.CustomerId,
            sale.SubtotalAmount,
            sale.ItemDiscountTotalAmount,
            sale.SaleDiscountAmount,
            sale.TotalAmount,
            sale.PaidAmount,
            sale.CreatedByUserId,
            sale.CreatedAtUtc,
            sale.UpdatedByUserId,
            sale.UpdatedAtUtc,
            sale.FinalizedByUserId,
            sale.FinalizedAtUtc,
            sale.CancelledByUserId,
            sale.CancelledAtUtc,
            sale.CancelReason));
    }

    [HttpPost("{saleId:guid}/items")]
    public async Task<ActionResult<SaleItemResponse>> AddItem(
        [FromRoute] Guid saleId,
        [FromBody] AddSaleItemRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole(UserType.Admin.ToString());

        var item = await _sales.AddItemAsync(
            userId,
            isAdmin,
            saleId,
            request.ProductId,
            request.Quantity,
            request.UnitPriceFinal,
            request.DiscountAmount,
            request.OverrideCode,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return Ok(new SaleItemResponse(
            item.Id,
            item.ProductId,
            item.Quantity,
            item.UnitPriceOriginal,
            item.UnitPriceFinal,
            item.DiscountAmount,
            item.LineTotalAmount,
            item.CreatedByUserId,
            item.CreatedAtUtc,
            item.UpdatedByUserId,
            item.UpdatedAtUtc));
    }

    [HttpPut("{saleId:guid}/items/{itemId:guid}")]
    public async Task<ActionResult<SaleItemResponse>> UpdateItem(
        [FromRoute] Guid saleId,
        [FromRoute] Guid itemId,
        [FromBody] UpdateSaleItemRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole(UserType.Admin.ToString());

        var item = await _sales.UpdateItemAsync(
            userId,
            isAdmin,
            saleId,
            itemId,
            request.Quantity,
            request.UnitPriceFinal,
            request.DiscountAmount,
            request.OverrideCode,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return Ok(new SaleItemResponse(
            item.Id,
            item.ProductId,
            item.Quantity,
            item.UnitPriceOriginal,
            item.UnitPriceFinal,
            item.DiscountAmount,
            item.LineTotalAmount,
            item.CreatedByUserId,
            item.CreatedAtUtc,
            item.UpdatedByUserId,
            item.UpdatedAtUtc));
    }

    [HttpDelete("{saleId:guid}/items/{itemId:guid}")]
    public async Task<ActionResult> RemoveItem(
        [FromRoute] Guid saleId,
        [FromRoute] Guid itemId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        await _sales.RemoveItemAsync(
            userId,
            saleId,
            itemId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{saleId:guid}/payments")]
    public async Task<ActionResult<SalePaymentResponse>> AddPayment(
        [FromRoute] Guid saleId,
        [FromBody] AddSalePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var payment = await _sales.AddPaymentAsync(
            userId,
            saleId,
            request.Method,
            request.Amount,
            request.AmountReceived,
            request.AuthorizationCode,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return Ok(new SalePaymentResponse(
            payment.Id,
            payment.Method,
            payment.Amount,
            payment.AmountReceived,
            payment.ChangeGiven,
            payment.AuthorizationCode,
            payment.CreatedByUserId,
            payment.CreatedAtUtc));
    }

    [HttpPost("{saleId:guid}/finalize")]
    public async Task<ActionResult<SaleResponse>> Finalize(
        [FromRoute] Guid saleId,
        [FromBody] FinalizeSaleRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole(UserType.Admin.ToString());

        var sale = await _sales.FinalizeAsync(
            userId,
            isAdmin,
            saleId,
            request.SaleDiscountAmount,
            request.OverrideCode,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return Ok(new SaleResponse(
            sale.Id,
            sale.Code,
            sale.Status,
            sale.CashRegisterId,
            sale.CashSessionId,
            sale.CustomerId,
            sale.SubtotalAmount,
            sale.ItemDiscountTotalAmount,
            sale.SaleDiscountAmount,
            sale.TotalAmount,
            sale.PaidAmount,
            sale.CreatedByUserId,
            sale.CreatedAtUtc,
            sale.UpdatedByUserId,
            sale.UpdatedAtUtc,
            sale.FinalizedByUserId,
            sale.FinalizedAtUtc,
            sale.CancelledByUserId,
            sale.CancelledAtUtc,
            sale.CancelReason));
    }

    [HttpPost("{saleId:guid}/cancel")]
    public async Task<ActionResult> Cancel(
        [FromRoute] Guid saleId,
        [FromBody] CancelSaleRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole(UserType.Admin.ToString());

        await _sales.CancelAsync(
            userId,
            isAdmin,
            saleId,
            request.Reason,
            request.OverrideCode,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return NoContent();
    }

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            throw new InvalidOperationException("Usuário não autenticado.");
        return userId;
    }
}
