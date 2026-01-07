using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PDVNow.Data;
using PDVNow.Dtos.Products;
using PDVNow.Entities;

namespace PDVNow.Controllers;

[ApiController]
[Route("api/v1/products")]
[Authorize(Policy = "PdvAccess")]
public sealed class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> List(
        [FromQuery] string? query,
        [FromQuery] string? sku,
        [FromQuery] string? barcode,
        [FromQuery] bool? active,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (skip < 0) skip = 0;
        if (take <= 0) take = 50;
        if (take > 200) take = 200;

        IQueryable<Product> products = _db.Products.AsNoTracking();

        if (active.HasValue)
            products = products.Where(p => p.IsActive == active.Value);

        if (!string.IsNullOrWhiteSpace(sku))
        {
            var s = sku.Trim();
            products = products.Where(p => p.Sku == s);
        }

        if (!string.IsNullOrWhiteSpace(barcode))
        {
            var b = barcode.Trim();
            products = products.Where(p => p.Barcode == b);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            products = products.Where(p =>
                p.Name.Contains(q) ||
                (p.Sku != null && p.Sku.Contains(q)) ||
                (p.Barcode != null && p.Barcode.Contains(q)));
        }

        var result = await products
            .OrderBy(p => p.Code)
            .Skip(skip)
            .Take(take)
            .Select(p => new ProductResponse(
                p.Id,
                p.Code,
                p.Name,
                p.Description,
                p.Sku,
                p.Barcode,
                p.Unit,
                p.CostPrice,
                p.SalePrice,
                p.StockQuantity,
                p.MinStockQuantity,
                p.IsActive,
                p.SupplierId,
                p.CreatedAtUtc,
                p.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
            return NotFound();

        return Ok(new ProductResponse(
            product.Id,
            product.Code,
            product.Name,
            product.Description,
            product.Sku,
            product.Barcode,
            product.Unit,
            product.CostPrice,
            product.SalePrice,
            product.StockQuantity,
            product.MinStockQuantity,
            product.IsActive,
            product.SupplierId,
            product.CreatedAtUtc,
            product.UpdatedAtUtc));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ProductResponse>> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest();

        if (request.CostPrice < 0 || request.SalePrice < 0 || request.StockQuantity < 0)
            return BadRequest();

        if (request.SupplierId.HasValue)
        {
            var supplierExists = await _db.Suppliers.AnyAsync(s => s.Id == request.SupplierId.Value, cancellationToken);
            if (!supplierExists)
                return BadRequest();
        }

        var nowUtc = DateTimeOffset.UtcNow;

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Sku = request.Sku?.Trim(),
            Barcode = request.Barcode?.Trim(),
            Unit = request.Unit,
            CostPrice = request.CostPrice,
            SalePrice = request.SalePrice,
            StockQuantity = request.StockQuantity,
            MinStockQuantity = request.MinStockQuantity,
            IsActive = true,
            SupplierId = request.SupplierId,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = null
        };

        _db.Products.Add(product);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict();
        }

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, new ProductResponse(
            product.Id,
            product.Code,
            product.Name,
            product.Description,
            product.Sku,
            product.Barcode,
            product.Unit,
            product.CostPrice,
            product.SalePrice,
            product.StockQuantity,
            product.MinStockQuantity,
            product.IsActive,
            product.SupplierId,
            product.CreatedAtUtc,
            product.UpdatedAtUtc));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ProductResponse>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest();

        if (request.CostPrice < 0 || request.SalePrice < 0 || request.StockQuantity < 0)
            return BadRequest();

        if (request.SupplierId.HasValue)
        {
            var supplierExists = await _db.Suppliers.AnyAsync(s => s.Id == request.SupplierId.Value, cancellationToken);
            if (!supplierExists)
                return BadRequest();
        }

        var product = await _db.Products.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
            return NotFound();

        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.Sku = request.Sku?.Trim();
        product.Barcode = request.Barcode?.Trim();
        product.Unit = request.Unit;
        product.CostPrice = request.CostPrice;
        product.SalePrice = request.SalePrice;
        product.StockQuantity = request.StockQuantity;
        product.MinStockQuantity = request.MinStockQuantity;
        product.IsActive = request.IsActive;
        product.SupplierId = request.SupplierId;
        product.UpdatedAtUtc = DateTimeOffset.UtcNow;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict();
        }

        return Ok(new ProductResponse(
            product.Id,
            product.Code,
            product.Name,
            product.Description,
            product.Sku,
            product.Barcode,
            product.Unit,
            product.CostPrice,
            product.SalePrice,
            product.StockQuantity,
            product.MinStockQuantity,
            product.IsActive,
            product.SupplierId,
            product.CreatedAtUtc,
            product.UpdatedAtUtc));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var product = await _db.Products.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
            return NotFound();

        product.Excluded = true;
        product.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
