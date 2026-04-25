using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using FluentAssertions;
using VetClinic.Api.Data;
using VetClinic.Api.Entities;
using Xunit;

namespace VetClinic.Tests.Database;

public class DatabaseConstraintTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .Build();

    private VetClinicDbContext _db = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _db = new VetClinicDbContext(
            new DbContextOptionsBuilder<VetClinicDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options);
        await _db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.StopAsync();
    }

    [Fact]
    public async Task UniqueIndex_SamePetSameDate_ThrowsPostgresException()
    {
        var owner = new Owner
        {
            FirstName = "Тест", LastName = "Тестов",
            Phone = "099", Email = "t@t.com"
        };
        _db.Owners.Add(owner);
        var pet = new Pet
        {
            Name = "Рекс", Species = Species.Dog,
            Breed = "Лабрадор",
            BirthDate = new DateOnly(2020, 1, 1),
            OwnerId = owner.Id
        };
        _db.Pets.Add(pet);
        await _db.SaveChangesAsync();

        var date = new DateOnly(2099, 12, 1);
        _db.Appointments.Add(new Appointment
        {
            PetId = pet.Id, VetName = "Dr.A",
            Date = date, Time = new TimeOnly(10, 0),
            Reason = "Огляд", Status = AppointmentStatus.Scheduled
        });
        await _db.SaveChangesAsync();

        _db.Appointments.Add(new Appointment
        {
            PetId = pet.Id, VetName = "Dr.B",
            Date = date, Time = new TimeOnly(11, 0),
            Reason = "Ще огляд", Status = AppointmentStatus.Scheduled
        });

        await FluentActions
            .Awaiting(() => _db.SaveChangesAsync())
            .Should().ThrowAsync<DbUpdateException>()
            .Where(ex => ex.InnerException != null && ex.InnerException.GetType() == typeof(PostgresException) && ((PostgresException)ex.InnerException).SqlState == "23505");
    }

    [Fact]
    public async Task CascadeDelete_Owner_RemovesPetsAndAppointments()
    {
        var owner = new Owner
        {
            FirstName = "Іван", LastName = "Франко",
            Phone = "099", Email = "i@f.com"
        };
        _db.Owners.Add(owner);
        var pet = new Pet
        {
            Name = "Мурчик", Species = Species.Cat,
            Breed = "Сіамська",
            BirthDate = new DateOnly(2021, 5, 10),
            OwnerId = owner.Id
        };
        _db.Pets.Add(pet);
        _db.Appointments.Add(new Appointment
        {
            PetId = pet.Id, VetName = "Dr.C",
            Date = new DateOnly(2099, 11, 1),
            Time = new TimeOnly(9, 0),
            Reason = "Щеплення", Status = AppointmentStatus.Scheduled
        });
        await _db.SaveChangesAsync();

        _db.Owners.Remove(owner);
        await _db.SaveChangesAsync();

        (await _db.Pets.AnyAsync(p => p.Id == pet.Id))
            .Should().BeFalse("Pet має бути видалений каскадом");
        (await _db.Appointments.AnyAsync(a => a.PetId == pet.Id))
            .Should().BeFalse("Appointments мають бути видалені каскадом");
    }
}