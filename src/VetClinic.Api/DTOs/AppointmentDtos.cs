using VetClinic.Api.Entities;

namespace VetClinic.Api.DTOs;

public record CreateAppointmentRequest(
    Guid PetId,
    string VetName,
    DateOnly Date,
    TimeOnly Time,
    string Reason);

public record UpdateAppointmentRequest(
    string? VetName,
    DateOnly? Date,
    TimeOnly? Time,
    string? Reason,
    AppointmentStatus? Status);

public record AppointmentResponse(
    Guid Id,
    Guid PetId,
    string VetName,
    DateOnly Date,
    TimeOnly Time,
    string Reason,
    AppointmentStatus Status);