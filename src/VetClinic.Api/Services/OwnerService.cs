using Microsoft.EntityFrameworkCore;
using VetClinic.Api.Data;
using VetClinic.Api.DTOs;
using VetClinic.Api.Entities;
using VetClinic.Api.Exceptions;

namespace VetClinic.Api.Services;

public class OwnerService(VetClinicDbContext db)
{
    public async Task<OwnerResponse> CreateAsync(CreateOwnerRequest req)
    {
        var owner = new Owner
        {
            FirstName = req.FirstName,
            LastName  = req.LastName,
            Phone     = req.Phone,
            Email     = req.Email
        };
        db.Owners.Add(owner);
        await db.SaveChangesAsync();
        return ToResponse(owner);
    }

    public async Task<OwnerResponse?> GetByIdAsync(Guid id)
    {
        var owner = await db.Owners.FindAsync(id);
        return owner is null ? null : ToResponse(owner);
    }

    public async Task<List<OwnerResponse>> GetAllAsync() =>
        await db.Owners.Select(o => ToResponse(o)).ToListAsync();

    public async Task DeleteAsync(Guid id)
    {
        var owner = await db.Owners.FindAsync(id)
            ?? throw new NotFoundException($"Власника Id={id} не знайдено.");
        db.Owners.Remove(owner);
        await db.SaveChangesAsync();
    }

    private static OwnerResponse ToResponse(Owner o) =>
        new(o.Id, o.FirstName, o.LastName, o.Phone, o.Email);
}