using PDVNow.Entities;

namespace PDVNow.Auth.Dtos;

public sealed record GenerateOverrideCodeRequest(
    AdminOverridePurpose Purpose,
    string? Justification);
