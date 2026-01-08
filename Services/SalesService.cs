using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PDVNow.Auth;
using PDVNow.Auth.Services;
using PDVNow.Data;
using PDVNow.Entities;

namespace PDVNow.Services;

public sealed class SalesService
{
    private readonly AppDbContext _db;
    private readonly AdminOverrideCodeService _overrideCodes;
    private readonly CashRegisterOptions _cashOptions;

    public SalesService(
        AppDbContext db,
        AdminOverrideCodeService overrideCodes,
        IOptions<CashRegisterOptions> cashOptions)
    {
        _db = db;
        _overrideCodes = overrideCodes;
        _cashOptions = cashOptions.Value;
    }

    public async Task<Sale> CreateSaleAsync(
        Guid userId,
        Guid cashRegisterId,
        Guid customerId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var customerExists = await _db.Customers.AnyAsync(c => c.Id == customerId, cancellationToken);
        if (!customerExists)
            throw new InvalidOperationException("CustomerId inválido.");

        var openSession = await _db.CashSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.CashRegisterId == cashRegisterId && s.ClosedAtUtc == null, cancellationToken);

        if (openSession is null)
            throw new InvalidOperationException("Não existe sessão aberta para este caixa.");

        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            Status = SaleStatus.Draft,
            CashRegisterId = cashRegisterId,
            CashSessionId = openSession.Id,
            CustomerId = customerId,
            SubtotalAmount = 0m,
            ItemDiscountTotalAmount = 0m,
            SaleDiscountAmount = 0m,
            TotalAmount = 0m,
            PaidAmount = 0m,
            CreatedByUserId = userId,
            CreatedAtUtc = nowUtc
        };

        _db.Sales.Add(sale);
        _db.SaleEvents.Add(new SaleEvent
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            Type = SaleEventType.Created,
            Details = null,
            PerformedByUserId = userId,
            PerformedAtUtc = nowUtc
        });

        await _db.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task<SaleItem> AddItemAsync(
        Guid userId,
        bool isAdmin,
        Guid saleId,
        Guid productId,
        decimal quantity,
        decimal? unitPriceFinal,
        decimal? discountAmount,
        string? overrideCode,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Quantity inválida.");

        var sale = await GetSaleForMutationAsync(saleId, cancellationToken);
        EnsureSaleEditable(sale);

        var product = await _db.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null)
            throw new InvalidOperationException("Produto não encontrado.");

        var original = product.SalePrice;
        var final = unitPriceFinal ?? original;
        var discount = discountAmount ?? 0m;

        if (final < 0 || discount < 0)
            throw new InvalidOperationException("Valores inválidos.");

        var changedPrice = final != original;
        var changedDiscount = discount > 0;

        if (!isAdmin && (changedPrice || changedDiscount))
        {
            var ok = await _overrideCodes.ValidateAndConsumeAsync(
                overrideCode ?? string.Empty,
                AdminOverridePurpose.CashMovement,
                userId,
                nowUtc,
                cancellationToken);

            if (ok is null)
                throw new InvalidOperationException("Código de liberação inválido ou expirado.");
        }

        var lineTotal = (final * quantity) - discount;
        if (lineTotal < 0)
            throw new InvalidOperationException("Total do item inválido.");

        var existing = await _db.SaleItems
            .SingleOrDefaultAsync(i => i.SaleId == saleId && i.ProductId == productId, cancellationToken);

        if (existing is not null)
            throw new InvalidOperationException("Produto já existe na venda. Use update.");

        var item = new SaleItem
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            ProductId = productId,
            Quantity = quantity,
            UnitPriceOriginal = original,
            UnitPriceFinal = final,
            DiscountAmount = discount,
            LineTotalAmount = lineTotal,
            CreatedByUserId = userId,
            CreatedAtUtc = nowUtc
        };

        _db.SaleItems.Add(item);

        await RecalculateSaleAsync(sale.Id, userId, nowUtc, cancellationToken);

        _db.SaleEvents.Add(new SaleEvent
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            Type = SaleEventType.ItemAdded,
            Details = $"ProductId={productId};Qty={quantity};Original={original};Final={final};Discount={discount}",
            PerformedByUserId = userId,
            PerformedAtUtc = nowUtc
        });

        await _db.SaveChangesAsync(cancellationToken);

        return item;
    }

    public async Task<SaleItem> UpdateItemAsync(
        Guid userId,
        bool isAdmin,
        Guid saleId,
        Guid itemId,
        decimal quantity,
        decimal? unitPriceFinal,
        decimal? discountAmount,
        string? overrideCode,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Quantity inválida.");

        var sale = await GetSaleForMutationAsync(saleId, cancellationToken);
        EnsureSaleEditable(sale);

        var item = await _db.SaleItems
            .SingleOrDefaultAsync(i => i.Id == itemId && i.SaleId == saleId, cancellationToken);

        if (item is null)
            throw new InvalidOperationException("Item não encontrado.");

        var final = unitPriceFinal ?? item.UnitPriceOriginal;
        var discount = discountAmount ?? 0m;

        if (final < 0 || discount < 0)
            throw new InvalidOperationException("Valores inválidos.");

        var changedPrice = final != item.UnitPriceOriginal;
        var changedDiscount = discount > 0;

        if (!isAdmin && (changedPrice || changedDiscount))
        {
            var ok = await _overrideCodes.ValidateAndConsumeAsync(
                overrideCode ?? string.Empty,
                AdminOverridePurpose.CashMovement,
                userId,
                nowUtc,
                cancellationToken);

            if (ok is null)
                throw new InvalidOperationException("Código de liberação inválido ou expirado.");
        }

        var lineTotal = (final * quantity) - discount;
        if (lineTotal < 0)
            throw new InvalidOperationException("Total do item inválido.");

        item.Quantity = quantity;
        item.UnitPriceFinal = final;
        item.DiscountAmount = discount;
        item.LineTotalAmount = lineTotal;
        item.UpdatedByUserId = userId;
        item.UpdatedAtUtc = nowUtc;

        await RecalculateSaleAsync(sale.Id, userId, nowUtc, cancellationToken);

        _db.SaleEvents.Add(new SaleEvent
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            Type = SaleEventType.ItemUpdated,
            Details = $"ItemId={itemId};Qty={quantity};Final={final};Discount={discount}",
            PerformedByUserId = userId,
            PerformedAtUtc = nowUtc
        });

        await _db.SaveChangesAsync(cancellationToken);

        return item;
    }

    public async Task RemoveItemAsync(
        Guid userId,
        Guid saleId,
        Guid itemId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var sale = await GetSaleForMutationAsync(saleId, cancellationToken);
        EnsureSaleEditable(sale);

        var item = await _db.SaleItems
            .SingleOrDefaultAsync(i => i.Id == itemId && i.SaleId == saleId, cancellationToken);

        if (item is null)
            throw new InvalidOperationException("Item não encontrado.");

        _db.SaleItems.Remove(item);

        await RecalculateSaleAsync(sale.Id, userId, nowUtc, cancellationToken);

        _db.SaleEvents.Add(new SaleEvent
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            Type = SaleEventType.ItemRemoved,
            Details = $"ItemId={itemId};ProductId={item.ProductId}",
            PerformedByUserId = userId,
            PerformedAtUtc = nowUtc
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SalePayment> AddPaymentAsync(
        Guid userId,
        Guid saleId,
        SalePaymentMethod method,
        decimal amount,
        decimal? amountReceived,
        string? authorizationCode,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Amount inválido.");

        var sale = await GetSaleForMutationAsync(saleId, cancellationToken);
        EnsureSaleEditable(sale);

        if (sale.TotalAmount <= 0)
            throw new InvalidOperationException("Venda sem itens.");

        decimal? changeGiven = null;

        if (method == SalePaymentMethod.Cash)
        {
            if (!amountReceived.HasValue)
                throw new InvalidOperationException("AmountReceived é obrigatório para dinheiro.");

            if (amountReceived.Value < amount)
                throw new InvalidOperationException("AmountReceived não pode ser menor que Amount.");

            // Troco calculado por pagamento em dinheiro (mais simples para o comércio):
            // Troco = valor recebido - valor pago em dinheiro (desta parcela)
            changeGiven = amountReceived.Value - amount;
        }

        var payment = new SalePayment
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            Method = method,
            Amount = amount,
            AmountReceived = amountReceived,
            ChangeGiven = changeGiven,
            AuthorizationCode = string.IsNullOrWhiteSpace(authorizationCode) ? null : authorizationCode.Trim(),
            CreatedByUserId = userId,
            CreatedAtUtc = nowUtc
        };

        _db.SalePayments.Add(payment);

        // Atualiza pago
        var paid = await _db.SalePayments
            .Where(p => p.SaleId == sale.Id)
            .SumAsync(p => p.Amount, cancellationToken);

        sale.PaidAmount = paid + amount;
        sale.Status = sale.PaidAmount >= sale.TotalAmount ? SaleStatus.Paid : SaleStatus.PendingPayment;
        sale.UpdatedByUserId = userId;
        sale.UpdatedAtUtc = nowUtc;

        _db.SaleEvents.Add(new SaleEvent
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            Type = SaleEventType.PaymentAdded,
            Details = $"Method={method};Amount={amount};Received={amountReceived};Change={changeGiven}",
            PerformedByUserId = userId,
            PerformedAtUtc = nowUtc
        });

        await _db.SaveChangesAsync(cancellationToken);

        return payment;
    }

    public async Task<Sale> FinalizeAsync(
        Guid userId,
        bool isAdmin,
        Guid saleId,
        decimal? saleDiscountAmount,
        string? overrideCode,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var sale = await GetSaleForMutationAsync(saleId, cancellationToken);

        if (sale.Status == SaleStatus.Cancelled)
            throw new InvalidOperationException("Venda cancelada.");

        if (sale.FinalizedAtUtc is not null)
            throw new InvalidOperationException("Venda já finalizada.");

        if (saleDiscountAmount.HasValue)
        {
            if (saleDiscountAmount.Value < 0)
                throw new InvalidOperationException("Desconto inválido.");

            if (!isAdmin && saleDiscountAmount.Value > 0)
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

            sale.SaleDiscountAmount = saleDiscountAmount.Value;
        }

        await RecalculateSaleAsync(sale.Id, userId, nowUtc, cancellationToken);

        // Exige pagamento total
        if (sale.TotalAmount <= 0)
            throw new InvalidOperationException("Venda sem itens.");

        if (sale.PaidAmount < sale.TotalAmount)
            throw new InvalidOperationException("Pagamento insuficiente.");

        // Confirma que ainda existe sessão aberta
        var openSession = await _db.CashSessions
            .AsNoTracking()
            .AnyAsync(s => s.Id == sale.CashSessionId && s.ClosedAtUtc == null, cancellationToken);

        if (!openSession)
            throw new InvalidOperationException("Sessão do caixa não está aberta.");

        sale.FinalizedAtUtc = nowUtc;
        sale.FinalizedByUserId = userId;
        sale.UpdatedByUserId = userId;
        sale.UpdatedAtUtc = nowUtc;

        // Movimentação no caixa: entrada SOMENTE para dinheiro
        var cashAmount = await _db.SalePayments
            .Where(p => p.SaleId == sale.Id && p.Method == SalePaymentMethod.Cash)
            .SumAsync(p => p.Amount, cancellationToken);

        if (cashAmount > 0)
        {
            _db.CashMovements.Add(new CashMovement
            {
                Id = Guid.NewGuid(),
                CashSessionId = sale.CashSessionId,
                Type = CashMovementType.Supply,
                Amount = cashAmount,
                Notes = $"Entrada venda {sale.Code}",
                CreatedByUserId = userId,
                CreatedAtUtc = nowUtc
            });
        }

        _db.SaleEvents.Add(new SaleEvent
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            Type = SaleEventType.Finalized,
            Details = null,
            PerformedByUserId = userId,
            PerformedAtUtc = nowUtc
        });

        await _db.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task CancelAsync(
        Guid userId,
        bool isAdmin,
        Guid saleId,
        string reason,
        string? overrideCode,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var sale = await GetSaleForMutationAsync(saleId, cancellationToken);

        if (sale.Status == SaleStatus.Cancelled)
            throw new InvalidOperationException("Venda já cancelada.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Motivo obrigatório.");

        // cancelamento é ação sensível: para PdvUser exige liberação
        if (!isAdmin)
        {
            var ok = await _overrideCodes.ValidateAndConsumeAsync(
                overrideCode ?? string.Empty,
                AdminOverridePurpose.ReopenSession,
                userId,
                nowUtc,
                cancellationToken);

            if (ok is null)
                throw new InvalidOperationException("Código de liberação inválido ou expirado.");
        }

        // Estorno no caixa (somente para entradas em dinheiro)
        var cashAmount = await _db.SalePayments
            .Where(p => p.SaleId == sale.Id && p.Method == SalePaymentMethod.Cash)
            .SumAsync(p => p.Amount, cancellationToken);

        if (cashAmount > 0)
        {
            _db.CashMovements.Add(new CashMovement
            {
                Id = Guid.NewGuid(),
                CashSessionId = sale.CashSessionId,
                Type = CashMovementType.Withdrawal,
                Amount = cashAmount,
                Notes = $"Estorno venda {sale.Code}",
                CreatedByUserId = userId,
                CreatedAtUtc = nowUtc
            });
        }

        sale.Status = SaleStatus.Cancelled;
        sale.CancelReason = reason.Trim();
        sale.CancelledAtUtc = nowUtc;
        sale.CancelledByUserId = userId;
        sale.UpdatedAtUtc = nowUtc;
        sale.UpdatedByUserId = userId;

        _db.SaleEvents.Add(new SaleEvent
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            Type = SaleEventType.Cancelled,
            Details = sale.CancelReason,
            PerformedByUserId = userId,
            PerformedAtUtc = nowUtc
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(Sale sale, IReadOnlyList<SaleItem> items, IReadOnlyList<SalePayment> payments)> GetSaleDetailsAsync(
        Guid saleId,
        CancellationToken cancellationToken)
    {
        var sale = await _db.Sales
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == saleId, cancellationToken);

        if (sale is null)
            throw new InvalidOperationException("Venda não encontrada.");

        var items = await _db.SaleItems
            .AsNoTracking()
            .Where(i => i.SaleId == saleId)
            .OrderBy(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var payments = await _db.SalePayments
            .AsNoTracking()
            .Where(p => p.SaleId == saleId)
            .OrderBy(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return (sale, items, payments);
    }

    public async Task<(decimal total, decimal paid, decimal remaining)> GetBalanceAsync(Guid saleId, CancellationToken cancellationToken)
    {
        var sale = await _db.Sales
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == saleId, cancellationToken);

        if (sale is null)
            throw new InvalidOperationException("Venda não encontrada.");

        var remaining = sale.TotalAmount - sale.PaidAmount;
        if (remaining < 0) remaining = 0;

        return (sale.TotalAmount, sale.PaidAmount, remaining);
    }

    private async Task<Sale> GetSaleForMutationAsync(Guid saleId, CancellationToken cancellationToken)
    {
        var sale = await _db.Sales
            .SingleOrDefaultAsync(s => s.Id == saleId, cancellationToken);

        if (sale is null)
            throw new InvalidOperationException("Venda não encontrada.");

        return sale;
    }

    private static void EnsureSaleEditable(Sale sale)
    {
        if (sale.Status == SaleStatus.Cancelled)
            throw new InvalidOperationException("Venda cancelada.");

        if (sale.FinalizedAtUtc is not null)
            throw new InvalidOperationException("Venda já finalizada.");
    }

    private async Task RecalculateSaleAsync(Guid saleId, Guid userId, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        var sale = await _db.Sales.SingleAsync(s => s.Id == saleId, cancellationToken);

        var items = await _db.SaleItems
            .AsNoTracking()
            .Where(i => i.SaleId == saleId)
            .ToListAsync(cancellationToken);

        var subtotal = items.Sum(i => i.UnitPriceFinal * i.Quantity);
        var itemDiscount = items.Sum(i => i.DiscountAmount);

        var total = subtotal - itemDiscount - sale.SaleDiscountAmount;
        if (total < 0) total = 0;

        sale.SubtotalAmount = subtotal;
        sale.ItemDiscountTotalAmount = itemDiscount;
        sale.TotalAmount = total;
        sale.UpdatedByUserId = userId;
        sale.UpdatedAtUtc = nowUtc;

        // Status
        sale.Status = sale.PaidAmount >= sale.TotalAmount && sale.TotalAmount > 0
            ? SaleStatus.Paid
            : (sale.TotalAmount > 0 ? SaleStatus.PendingPayment : SaleStatus.Draft);
    }
}
