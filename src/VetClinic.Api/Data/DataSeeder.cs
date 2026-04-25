using Bogus;
using Microsoft.EntityFrameworkCore;
using VetClinic.Api.Entities;

namespace VetClinic.Api.Data;

public static class DataSeeder
{
    private static readonly Species[] AllSpecies = Enum.GetValues<Species>();
    private static readonly Random Rng = new(42);

    public static async Task SeedAsync(VetClinicDbContext db, int ownerCount = 500)
    {
        if (await db.Owners.AnyAsync()) return;

        var faker = new Faker("uk");

        // 1. Власники
        var owners = Enumerable.Range(0, ownerCount).Select(_ => new Owner
        {
            FirstName = faker.Name.FirstName(),
            LastName  = faker.Name.LastName(),
            Phone     = faker.Phone.PhoneNumber("+38(0##)###-##-##"),
            Email     = faker.Internet.Email()
        }).ToList();

        await db.Owners.AddRangeAsync(owners);
        await db.SaveChangesAsync();

        // 2. Тварини (10–25 на власника)
        var pets = new List<Pet>();
        foreach (var owner in owners)
        {
            for (var i = 0; i < Rng.Next(10, 26); i++)
            {
                pets.Add(new Pet
                {
                    Name      = faker.Name.FirstName(),
                    Species   = AllSpecies[Rng.Next(AllSpecies.Length)], // явно
                    Breed     = faker.Commerce.ProductAdjective() + " Breed",
                    BirthDate = DateOnly.FromDateTime(                    // явно
                        DateTime.UtcNow.AddDays(-Rng.Next(365, 3650))),
                    OwnerId   = owner.Id
                });
            }
        }

        foreach (var batch in pets.Chunk(1000))
        {
            await db.Pets.AddRangeAsync(batch);
            await db.SaveChangesAsync();
        }

        // 3. Записи (1–5 на тварину, без конфліктів дат)
        var appointments = new List<Appointment>();
        foreach (var pet in pets)
        {
            var usedDates = new HashSet<DateOnly>();
            for (var i = 0; i < Rng.Next(1, 6); i++)
            {
                DateOnly date;
                var tries = 0;
                do
                {
                    date = DateOnly.FromDateTime(
                        DateTime.UtcNow.AddDays(Rng.Next(-180, 365)));
                    tries++;
                }
                while (usedDates.Contains(date) && tries < 20);

                if (usedDates.Contains(date)) continue;
                usedDates.Add(date);

                // Статус: явно, залежно від дати
                var isPast = date < DateOnly.FromDateTime(DateTime.UtcNow);
                var status = isPast
                    ? (Rng.NextDouble() > 0.2
                        ? AppointmentStatus.Completed
                        : AppointmentStatus.Cancelled)
                    : AppointmentStatus.Scheduled;

                appointments.Add(new Appointment
                {
                    PetId   = pet.Id,
                    VetName = $"Dr. {faker.Name.LastName()}",
                    Date    = date,
                    Time    = new TimeOnly(Rng.Next(8, 18), Rng.Next(0, 4) * 15),
                    Reason  = faker.Lorem.Sentence(3),
                    Status  = status
                });
            }
        }

        foreach (var batch in appointments.Chunk(1000))
        {
            await db.Appointments.AddRangeAsync(batch);
            await db.SaveChangesAsync();
        }
    }
}