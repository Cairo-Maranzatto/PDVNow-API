using PDVNow.Entities;

namespace PDVNow.Dtos.Customers;

public sealed record CustomerResponse(
    Guid Id,
    int Code,
    CustomerPersonType PersonType,
    string Name,
    string? TradeName,
    string? Document,
    string? Email,
    string? Phone,
    string? Mobile,
    DateOnly? BirthDate,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? PostalCode,
    string? Notes,
    decimal CreditLimit,
    decimal CurrentBalance,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
