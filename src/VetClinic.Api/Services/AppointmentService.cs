using Microsoft.EntityFrameworkCore;
using VetClinic.Api.Data;
using VetClinic.Api.DTOs;
using VetClinic.Api.Entities;
using VetClinic.Api.Exceptions;

namespace VetClinic.Api.Services;

public class AppointmentService(VetClinicDbContext db)
{
    public static void ValidateFutureDate(DateOnly date)
    {
        if (date <= DateOnly.FromDateTime(DateTime.UtcNow))
            throw new BusinessException(
                "Дата прийому повинна бути у майбутньому.");
    }


    public static void ValidateNotCancelled(Appointment appointment)
    {
        if (appointment.Status == AppointmentStatus.Cancelled)
            throw new BusinessException(
                "Скасований запис не підлягає зміні. Створіть новий запис.");
    }


    public async Task<AppointmentResponse> CreateAsync(CreateAppointmentRequest req)
    {
        ValidateFutureDate(req.Date);

        var petExists = await db.Pets.AnyAsync(p => p.Id == req.PetId);
        if (!petExists)
            throw new NotFoundException($"Тварину Id={req.PetId} не знайдено.");

        var conflict = await db.Appointments
            .AnyAsync(a => a.PetId == req.PetId && a.Date == req.Date);
        if (conflict)
            throw new BusinessException(
                $"Тварина вже має запис на {req.Date:dd.MM.yyyy}.");

        var appointment = new Appointment
        {
            PetId   = req.PetId,
            VetName = req.VetName,
            Date    = req.Date,
            Time    = req.Time,
            Reason  = req.Reason,
            Status  = AppointmentStatus.Scheduled
        };

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();
        return ToResponse(appointment);
    }


    public async Task<List<AppointmentResponse>> GetByDateAsync(DateOnly date) =>
        await db.Appointments
            .Where(a => a.Date == date)
            .Select(a => ToResponse(a))
            .ToListAsync();


    public async Task<AppointmentResponse?> GetByIdAsync(Guid id)
    {
        var appointment = await db.Appointments.FindAsync(id);
        return appointment is null ? null : ToResponse(appointment);
    }


    public async Task CancelAsync(Guid id)
    {
        var appointment = await db.Appointments.FindAsync(id)
            ?? throw new NotFoundException($"Запис Id={id} не знайдено.");

        ValidateNotCancelled(appointment);

        appointment.Status = AppointmentStatus.Cancelled;
        await db.SaveChangesAsync();
    }


    private static AppointmentResponse ToResponse(Appointment a) =>
        new(a.Id, a.PetId, a.VetName, a.Date, a.Time, a.Reason, a.Status);
}