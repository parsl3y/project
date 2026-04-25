namespace VetClinic.Api.DTOs;

public record CreateOwnerRequest(
    string FirstName,
    string LastName,
    string Phone,
    string Email);

public record OwnerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Phone,
    string Email);