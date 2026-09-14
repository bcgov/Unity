using System;

namespace Unity.GrantManager.Applications;

/// <summary>
/// Domain input for editing an address or creating a missing address when Id is empty.
/// </summary>
public record ApplicantAddressInput(
    Guid Id,
    string? Street,
    string? Street2,
    string? Unit,
    string? City,
    string? Province,
    string? PostalCode);
