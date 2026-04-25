namespace VetClinic.Api.Entities;

public enum AppointmentStatus { Scheduled, Completed, Cancelled }

public class Appointment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PetId { get; set; }
    public string VetName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public string Reason { get; set; } = string.Empty;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    public Pet Pet { get; set; } = null!;
}