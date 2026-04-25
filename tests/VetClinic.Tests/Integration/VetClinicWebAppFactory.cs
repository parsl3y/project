using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using VetClinic.Api.Data;
using VetClinic.Api.Entities;
using AutoFixture;

namespace VetClinic.Tests.Integration;

public class VetClinicWebAppFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("vetclinic_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly Fixture _fixture = new();
    private static readonly Random Rng = new(42);
    private static readonly Species[] AllSpecies = Enum.GetValues<Species>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Видаляємо ВСІ реєстрації DbContext
            var descriptors = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<VetClinicDbContext>) ||
                    d.ServiceType == typeof(VetClinicDbContext))
                .ToList();

            foreach (var d in descriptors)
                services.Remove(d);

            // Підключаємо тестовий PostgreSQL
            services.AddDbContext<VetClinicDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        // 1. Стартуємо контейнер
        await _postgres.StartAsync();

        // 2. Створюємо схему
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VetClinicDbContext>();
        await db.Database.EnsureCreatedAsync();

        // 3. Наповнюємо даними
        await SeedDatabaseAsync(db);
    }

    public new async Task DisposeAsync()
    {
        await _postgres.StopAsync();
        await base.DisposeAsync();
    }

    // Між тестами — очищаємо тільки Appointments
    public async Task ResetAppointmentsAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VetClinicDbContext>();
        db.Appointments.RemoveRange(db.Appointments);
        await db.SaveChangesAsync();
    }

    // Повне скидання з перестворенням схеми
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VetClinicDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        await SeedDatabaseAsync(db);
    }

    // Seeding: 10,000+ записів
    private async Task SeedDatabaseAsync(VetClinicDbContext db)
    {
        if (await db.Owners.AnyAsync()) return;

        // 1. Власники — 200
        var owners = Enumerable.Range(0, 200).Select(_ => new Owner
        {
            Id        = Guid.NewGuid(),
            FirstName = _fixture.Create<string>()[..8],
            LastName  = _fixture.Create<string>()[..8],
            Phone     = $"+38099{Rng.Next(1000000, 9999999)}",
            Email     = $"{_fixture.Create<string>()[..6]}@test.com"
        }).ToList();

        await db.Owners.AddRangeAsync(owners);
        await db.SaveChangesAsync();

        // 2. Тварини — 10-25 на власника (~3,000)
        var pets = new List<Pet>();
        foreach (var owner in owners)
        {
            for (var i = 0; i < Rng.Next(10, 26); i++)
            {
                pets.Add(new Pet
                {
                    Id        = Guid.NewGuid(),
                    Name      = _fixture.Create<string>()[..8],
                    Species   = AllSpecies[Rng.Next(AllSpecies.Length)],
                    Breed     = _fixture.Create<string>()[..10],
                    BirthDate = DateOnly.FromDateTime(
                        DateTime.UtcNow.AddDays(-Rng.Next(365, 3650))),
                    OwnerId   = owner.Id
                });
            }
        }

        foreach (var batch in pets.Chunk(500))
        {
            await db.Pets.AddRangeAsync(batch);
            await db.SaveChangesAsync();
        }

        // 3. Записи — 2-5 на тварину (~10,000+)
        var appointments = new List<Appointment>();
        foreach (var pet in pets)
        {
            var usedDates = new HashSet<DateOnly>();
            for (var i = 0; i < Rng.Next(2, 6); i++)
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

                var isPast = date < DateOnly.FromDateTime(DateTime.UtcNow);
                var status = isPast
                    ? (Rng.NextDouble() > 0.2
                        ? AppointmentStatus.Completed
                        : AppointmentStatus.Cancelled)
                    : AppointmentStatus.Scheduled;

                appointments.Add(new Appointment
                {
                    Id      = Guid.NewGuid(),
                    PetId   = pet.Id,
                    VetName = $"Dr. {_fixture.Create<string>()[..6]}",
                    Date    = date,
                    Time    = new TimeOnly(Rng.Next(8, 18), 0),
                    Reason  = _fixture.Create<string>()[..15],
                    Status  = status
                });
            }
        }

        foreach (var batch in appointments.Chunk(500))
        {
            await db.Appointments.AddRangeAsync(batch);
            await db.SaveChangesAsync();
        }
    }

    // Хелпери для тестів
    public async Task<(Owner owner, Pet pet)> GetExistingOwnerWithPetAsync()
    {
        using var scope = Services.CreateScope();
        var db    = scope.ServiceProvider.GetRequiredService<VetClinicDbContext>();
        var owner = await db.Owners
            .Include(o => o.Pets)
            .FirstAsync(o => o.Pets.Any());
        return (owner, owner.Pets.First());
    }

    public async Task<DateOnly> GetFreeDateForPetAsync(Guid petId)
    {
        using var scope = Services.CreateScope();
        var db   = scope.ServiceProvider.GetRequiredService<VetClinicDbContext>();
        var used = await db.Appointments
            .Where(a => a.PetId == petId)
            .Select(a => a.Date)
            .ToListAsync();

        var candidate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        while (used.Contains(candidate))
            candidate = candidate.AddDays(1);

        return candidate;
    }
}