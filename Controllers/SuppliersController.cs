using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PDVNow.Data;
using PDVNow.Dtos.Suppliers;
using PDVNow.Entities;

namespace PDVNow.Controllers;

[ApiController]
[Route("api/v1/suppliers")]
[Authorize(Policy = "PdvAccess")]
public sealed class SuppliersController : ControllerBase
{
    private readonly AppDbContext _db;

    public SuppliersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SupplierResponse>>> List(
        [FromQuery] string? query,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (skip < 0) skip = 0;
        if (take <= 0) take = 50;
        if (take > 200) take = 200;

        IQueryable<Supplier> suppliers = _db.Suppliers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            suppliers = suppliers.Where(s =>
                s.Name.Contains(q) ||
                (s.TradeName != null && s.TradeName.Contains(q)) ||
                (s.Cnpj != null && s.Cnpj.Contains(q)));
        }

        var result = await suppliers
            .OrderBy(s => s.Name)
            .Skip(skip)
            .Take(take)
            .Select(s => new SupplierResponse(
                s.Id,
                s.Name,
                s.TradeName,
                s.Cnpj,
                s.StateRegistration,
                s.Email,
                s.Phone,
                s.AddressLine1,
                s.City,
                s.State,
                s.PostalCode,
                s.IsActive,
                s.CreatedAtUtc,
                s.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierResponse>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var supplier = await _db.Suppliers
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (supplier is null)
            return NotFound();

        return Ok(new SupplierResponse(
            supplier.Id,
            supplier.Name,
            supplier.TradeName,
            supplier.Cnpj,
            supplier.StateRegistration,
            supplier.Email,
            supplier.Phone,
            supplier.AddressLine1,
            supplier.City,
            supplier.State,
            supplier.PostalCode,
            supplier.IsActive,
            supplier.CreatedAtUtc,
            supplier.UpdatedAtUtc));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<SupplierResponse>> Create([FromBody] CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest();

        var nowUtc = DateTimeOffset.UtcNow;

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            TradeName = request.TradeName?.Trim(),
            Cnpj = request.Cnpj?.Trim(),
            StateRegistration = request.StateRegistration?.Trim(),
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            AddressLine1 = request.AddressLine1?.Trim(),
            City = request.City?.Trim(),
            State = request.State?.Trim(),
            PostalCode = request.PostalCode?.Trim(),
            IsActive = true,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = null
        };

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync(cancellationToken);

        var response = new SupplierResponse(
            supplier.Id,
            supplier.Name,
            supplier.TradeName,
            supplier.Cnpj,
            supplier.StateRegistration,
            supplier.Email,
            supplier.Phone,
            supplier.AddressLine1,
            supplier.City,
            supplier.State,
            supplier.PostalCode,
            supplier.IsActive,
            supplier.CreatedAtUtc,
            supplier.UpdatedAtUtc);

        return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<SupplierResponse>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateSupplierRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest();

        var supplier = await _db.Suppliers.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (supplier is null)
            return NotFound();

        supplier.Name = request.Name.Trim();
        supplier.TradeName = request.TradeName?.Trim();
        supplier.Cnpj = request.Cnpj?.Trim();
        supplier.StateRegistration = request.StateRegistration?.Trim();
        supplier.Email = request.Email?.Trim();
        supplier.Phone = request.Phone?.Trim();
        supplier.AddressLine1 = request.AddressLine1?.Trim();
        supplier.City = request.City?.Trim();
        supplier.State = request.State?.Trim();
        supplier.PostalCode = request.PostalCode?.Trim();
        supplier.IsActive = request.IsActive;
        supplier.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new SupplierResponse(
            supplier.Id,
            supplier.Name,
            supplier.TradeName,
            supplier.Cnpj,
            supplier.StateRegistration,
            supplier.Email,
            supplier.Phone,
            supplier.AddressLine1,
            supplier.City,
            supplier.State,
            supplier.PostalCode,
            supplier.IsActive,
            supplier.CreatedAtUtc,
            supplier.UpdatedAtUtc));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var supplier = await _db.Suppliers.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (supplier is null)
            return NotFound();

        supplier.IsActive = false;
        supplier.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
