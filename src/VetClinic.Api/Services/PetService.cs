using Microsoft.EntityFrameworkCore;
using VetClinic.Api.Data;
using VetClinic.Api.DTOs;
using VetClinic.Api.Entities;
using VetClinic.Api.Exceptions;

namespace VetClinic.Api.Services;

public class PetService(VetClinicDbContext db)
{
    public async Task<PetResponse> CreateAsync(CreatePetRequest req)
    {
        // Перевірка: власник повинен існувати
        var ownerExists = await db.Owners.AnyAsync(o => o.Id == req.OwnerId);
        if (!ownerExists)
            throw new NotFoundException($"Власника Id={req.OwnerId} не знайдено.");

        var pet = new Pet
        {
            Name      = req.Name,
            Species   = req.Species,
            Breed     = req.Breed,
            BirthDate = req.BirthDate,
            OwnerId   = req.OwnerId
        };
        db.Pets.Add(pet);
        await db.SaveChangesAsync();
        return ToResponse(pet);
    }

    public async Task<PetResponse?> GetByIdAsync(Guid id)
    {
        var pet = await db.Pets.FindAsync(id);
        return pet is null ? null : ToResponse(pet);
    }

    public async Task<List<PetResponse>> GetByOwnerAsync(Guid ownerId) =>
        await db.Pets
            .Where(p => p.OwnerId == ownerId)
            .Select(p => ToResponse(p))
            .ToListAsync();

    public async Task<PetResponse> UpdateAsync(Guid id, UpdatePetRequest req)
    {
        var pet = await db.Pets.FindAsync(id)
            ?? throw new NotFoundException($"Тварину Id={id} не знайдено.");

        // Перевірка: власник повинен існувати
        var ownerExists = await db.Owners.AnyAsync(o => o.Id == req.OwnerId);
        if (!ownerExists)
            throw new NotFoundException($"Власника Id={req.OwnerId} не знайдено.");

        pet.Name = req.Name;
        pet.Species = req.Species;
        pet.Breed = req.Breed;
        pet.BirthDate = req.BirthDate;
        pet.OwnerId = req.OwnerId;

        await db.SaveChangesAsync();
        return ToResponse(pet);
    }

    public async Task DeleteAsync(Guid id)
    {
        var pet = await db.Pets.FindAsync(id)
            ?? throw new NotFoundException($"Тварину Id={id} не знайдено.");
        db.Pets.Remove(pet);
        await db.SaveChangesAsync();
    }

    private static PetResponse ToResponse(Pet p) =>
        new(p.Id, p.Name, p.Species, p.Breed, p.BirthDate, p.OwnerId);
}