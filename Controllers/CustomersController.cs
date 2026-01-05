using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PDVNow.Data;
using PDVNow.Dtos.Customers;
using PDVNow.Entities;

namespace PDVNow.Controllers;

[ApiController]
[Route("api/v1/customers")]
[Authorize(Policy = "PdvAccess")]
public sealed class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;

    public CustomersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerResponse>>> List(
        [FromQuery] string? query,
        [FromQuery] string? document,
        [FromQuery] string? email,
        [FromQuery] string? phone,
        [FromQuery] bool? active,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (skip < 0) skip = 0;
        if (take <= 0) take = 50;
        if (take > 200) take = 200;

        IQueryable<Customer> customers = _db.Customers.AsNoTracking();

        if (active.HasValue)
            customers = customers.Where(c => c.IsActive == active.Value);

        if (!string.IsNullOrWhiteSpace(document))
        {
            var d = document.Trim();
            customers = customers.Where(c => c.Document == d);
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var e = email.Trim();
            customers = customers.Where(c => c.Email != null && c.Email == e);
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var p = phone.Trim();
            customers = customers.Where(c => (c.Phone != null && c.Phone == p) || (c.Mobile != null && c.Mobile == p));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            customers = customers.Where(c =>
                c.Name.Contains(q) ||
                (c.TradeName != null && c.TradeName.Contains(q)) ||
                (c.Document != null && c.Document.Contains(q)));
        }

        var result = await customers
            .OrderBy(c => c.Name)
            .Skip(skip)
            .Take(take)
            .Select(c => new CustomerResponse(
                c.Id,
                c.Code,
                c.PersonType,
                c.Name,
                c.TradeName,
                c.Document,
                c.Email,
                c.Phone,
                c.Mobile,
                c.BirthDate,
                c.AddressLine1,
                c.AddressLine2,
                c.City,
                c.State,
                c.PostalCode,
                c.Notes,
                c.CreditLimit,
                c.CurrentBalance,
                c.IsActive,
                c.CreatedAtUtc,
                c.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer is null)
            return NotFound();

        return Ok(new CustomerResponse(
            customer.Id,
            customer.Code,
            customer.PersonType,
            customer.Name,
            customer.TradeName,
            customer.Document,
            customer.Email,
            customer.Phone,
            customer.Mobile,
            customer.BirthDate,
            customer.AddressLine1,
            customer.AddressLine2,
            customer.City,
            customer.State,
            customer.PostalCode,
            customer.Notes,
            customer.CreditLimit,
            customer.CurrentBalance,
            customer.IsActive,
            customer.CreatedAtUtc,
            customer.UpdatedAtUtc));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CustomerResponse>> Create([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest();

        if (request.CreditLimit < 0)
            return BadRequest();

        var nowUtc = DateTimeOffset.UtcNow;
        var document = string.IsNullOrWhiteSpace(request.Document) ? null : request.Document.Trim();

        if (document is not null)
        {
            var documentExists = await _db.Customers
                .IgnoreQueryFilters()
                .AnyAsync(c => c.Document != null && c.Document == document, cancellationToken);

            if (documentExists)
                return Conflict("Documento já existe.");
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            PersonType = request.PersonType,
            Name = request.Name.Trim(),
            TradeName = request.TradeName?.Trim(),
            Document = document,
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            Mobile = request.Mobile?.Trim(),
            BirthDate = request.BirthDate,
            AddressLine1 = request.AddressLine1?.Trim(),
            AddressLine2 = request.AddressLine2?.Trim(),
            City = request.City?.Trim(),
            State = request.State?.Trim(),
            PostalCode = request.PostalCode?.Trim(),
            Notes = request.Notes?.Trim(),
            CreditLimit = request.CreditLimit,
            CurrentBalance = 0m,
            IsActive = request.IsActive,
            Excluded = false,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = null
        };

        _db.Customers.Add(customer);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict();
        }

        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, new CustomerResponse(
            customer.Id,
            customer.Code,
            customer.PersonType,
            customer.Name,
            customer.TradeName,
            customer.Document,
            customer.Email,
            customer.Phone,
            customer.Mobile,
            customer.BirthDate,
            customer.AddressLine1,
            customer.AddressLine2,
            customer.City,
            customer.State,
            customer.PostalCode,
            customer.Notes,
            customer.CreditLimit,
            customer.CurrentBalance,
            customer.IsActive,
            customer.CreatedAtUtc,
            customer.UpdatedAtUtc));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CustomerResponse>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest();

        if (request.CreditLimit < 0)
            return BadRequest();

        var customer = await _db.Customers.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (customer is null)
            return NotFound();

        var document = string.IsNullOrWhiteSpace(request.Document) ? null : request.Document.Trim();
        if (document is not null)
        {
            var documentExists = await _db.Customers
                .IgnoreQueryFilters()
                .AnyAsync(c => c.Id != id && c.Document != null && c.Document == document, cancellationToken);

            if (documentExists)
                return Conflict("Documento já existe.");
        }

        customer.PersonType = request.PersonType;
        customer.Name = request.Name.Trim();
        customer.TradeName = request.TradeName?.Trim();
        customer.Document = document;
        customer.Email = request.Email?.Trim();
        customer.Phone = request.Phone?.Trim();
        customer.Mobile = request.Mobile?.Trim();
        customer.BirthDate = request.BirthDate;
        customer.AddressLine1 = request.AddressLine1?.Trim();
        customer.AddressLine2 = request.AddressLine2?.Trim();
        customer.City = request.City?.Trim();
        customer.State = request.State?.Trim();
        customer.PostalCode = request.PostalCode?.Trim();
        customer.Notes = request.Notes?.Trim();
        customer.CreditLimit = request.CreditLimit;
        customer.IsActive = request.IsActive;
        customer.UpdatedAtUtc = DateTimeOffset.UtcNow;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict();
        }

        return Ok(new CustomerResponse(
            customer.Id,
            customer.Code,
            customer.PersonType,
            customer.Name,
            customer.TradeName,
            customer.Document,
            customer.Email,
            customer.Phone,
            customer.Mobile,
            customer.BirthDate,
            customer.AddressLine1,
            customer.AddressLine2,
            customer.City,
            customer.State,
            customer.PostalCode,
            customer.Notes,
            customer.CreditLimit,
            customer.CurrentBalance,
            customer.IsActive,
            customer.CreatedAtUtc,
            customer.UpdatedAtUtc));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var customer = await _db.Customers.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (customer is null)
            return NotFound();

        customer.Excluded = true;
        customer.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
