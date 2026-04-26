using VetClinic.Api.Entities;

namespace VetClinic.Api.DTOs;

public record CreatePetRequest(
    string Name,
    Species Species,
    string Breed,
    DateOnly BirthDate,
    Guid OwnerId);

public record UpdatePetRequest(
    string Name,
    Species Species,
    string Breed,
    DateOnly BirthDate,
    Guid OwnerId);

public record PetResponse(
    Guid Id,
    string Name,
    Species Species,
    string Breed,
    DateOnly BirthDate,
    Guid OwnerId);