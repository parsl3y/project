namespace VetClinic.Api.Entities;

public enum Species { Dog, Cat, Bird, Other }

public class Pet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Species Species { get; set; }
    public string Breed { get; set; } = string.Empty;
    public DateOnly BirthDate { get; set; }
    public Guid OwnerId { get; set; }

    public Owner Owner { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}