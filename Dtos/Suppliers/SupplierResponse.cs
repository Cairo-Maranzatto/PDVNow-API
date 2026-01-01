namespace PDVNow.Dtos.Suppliers;

public sealed record SupplierResponse(
    Guid Id,
    string Name,
    string? TradeName,
    string? Cnpj,
    string? StateRegistration,
    string? Email,
    string? Phone,
    string? AddressLine1,
    string? City,
    string? State,
    string? PostalCode,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
