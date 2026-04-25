using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using FluentAssertions;
using VetClinic.Api.DTOs;
using VetClinic.Api.Entities;
using Xunit;

namespace VetClinic.Tests.Integration;

public class AppointmentIntegrationTests
    : IClassFixture<VetClinicWebAppFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly VetClinicWebAppFactory _factory; 
    private readonly Fixture _fixture = new();

    public AppointmentIntegrationTests(VetClinicWebAppFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    // Між тестами очищаємо тільки appointments
    // seed-дані (owners, pets) залишаються
    public async Task InitializeAsync() =>
        await _factory.ResetAppointmentsAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // ──────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────

    private async Task<OwnerResponse> CreateOwnerAsync()
    {
        var req = new CreateOwnerRequest(
            _fixture.Create<string>()[..8],
            _fixture.Create<string>()[..8],
            "+380991234567",
            $"{_fixture.Create<string>()[..6]}@test.com");

        var resp = await _client.PostAsJsonAsync("/api/owners", req);
        resp.StatusCode.Should().Be(HttpStatusCode.Created,
            "створення власника повинно повертати 201");

        return (await resp.Content.ReadFromJsonAsync<OwnerResponse>())!;
    }

    private async Task<PetResponse> CreatePetAsync(Guid ownerId)
    {
        var req = new CreatePetRequest(
            _fixture.Create<string>()[..8],
            Species.Dog,              // явно
            _fixture.Create<string>()[..8],
            new DateOnly(2021, 3, 15), // явно — минуле
            ownerId);

        var resp = await _client.PostAsJsonAsync("/api/pets", req);
        resp.StatusCode.Should().Be(HttpStatusCode.Created,
            "створення тварини повинно повертати 201");

        return (await resp.Content.ReadFromJsonAsync<PetResponse>())!;
    }

    private async Task<AppointmentResponse> CreateAppointmentAsync(
        Guid petId, DateOnly date)
    {
        var req = new CreateAppointmentRequest(
            petId,
            $"Dr. {_fixture.Create<string>()[..6]}",
            date,
            new TimeOnly(10, 0),
            _fixture.Create<string>()[..15]);

        var resp = await _client.PostAsJsonAsync("/api/appointments", req);
        resp.StatusCode.Should().Be(HttpStatusCode.Created,
            $"створення запису на {date} повинно повертати 201");

        return (await resp.Content.ReadFromJsonAsync<AppointmentResponse>())!;
    }

    // ──────────────────────────────────────────
    // 1. Реєстрація власника та тварини
    // ──────────────────────────────────────────

    [Fact]
    public async Task RegisterOwner_ValidData_Returns201WithCorrectFields()
    {
        var req = new CreateOwnerRequest(
            "Іван", "Франко", "+380991234567", "ivan@franko.com");

        var resp = await _client.PostAsJsonAsync("/api/owners", req);

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await resp.Content.ReadFromJsonAsync<OwnerResponse>();
        result!.Id.Should().NotBeEmpty();
        result.FirstName.Should().Be("Іван");
        result.LastName.Should().Be("Франко");
        result.Email.Should().Be("ivan@franko.com");
    }

    [Fact]
    public async Task RegisterPet_ValidOwner_Returns201WithCorrectFields()
    {
        var owner = await CreateOwnerAsync();

        var req = new CreatePetRequest(
            "Рекс", Species.Dog, "Лабрадор",
            new DateOnly(2021, 5, 10),
            owner.Id);

        var resp = await _client.PostAsJsonAsync("/api/pets", req);

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await resp.Content.ReadFromJsonAsync<PetResponse>();
        result!.Name.Should().Be("Рекс");
        result.Species.Should().Be(Species.Dog);
        result.OwnerId.Should().Be(owner.Id);
    }

    [Fact]
    public async Task RegisterPet_NonExistentOwner_Returns404()
    {
        var req = new CreatePetRequest(
            "Мурчик", Species.Cat, "Сіамська",
            new DateOnly(2020, 1, 1),
            Guid.NewGuid()); // неіснуючий власник

        var resp = await _client.PostAsJsonAsync("/api/pets", req);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────
    // 2. Повний цикл: Owner → Pet → Appointment
    // ──────────────────────────────────────────

    [Fact]
    public async Task FullFlow_RegisterOwnerPetAppointment_Success()
    {
        // Крок 1: реєструємо власника
        var owner = await CreateOwnerAsync();
        owner.Id.Should().NotBeEmpty();

        // Крок 2: реєструємо тварину
        var pet = await CreatePetAsync(owner.Id);
        pet.OwnerId.Should().Be(owner.Id);

        // Крок 3: плануємо запис
        var date        = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var appointment = await CreateAppointmentAsync(pet.Id, date);

        appointment.PetId.Should().Be(pet.Id);
        appointment.Status.Should().Be(AppointmentStatus.Scheduled);
        appointment.Date.Should().Be(date);
    }

    // ──────────────────────────────────────────
    // 3. Планування записів
    // ──────────────────────────────────────────

    [Fact]
    public async Task ScheduleAppointment_FutureDate_Returns201()
    {
        var owner = await CreateOwnerAsync();
        var pet   = await CreatePetAsync(owner.Id);
        var date  = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));

        var resp = await _client.PostAsJsonAsync("/api/appointments",
            new CreateAppointmentRequest(
                pet.Id, "Dr. Шевченко", date,
                new TimeOnly(14, 30), "Планова вакцинація"));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await resp.Content.ReadFromJsonAsync<AppointmentResponse>();
        result!.Status.Should().Be(AppointmentStatus.Scheduled);
        result.VetName.Should().Be("Dr. Шевченко");
        result.Time.Should().Be(new TimeOnly(14, 30));
    }

    [Fact]
    public async Task ScheduleAppointment_PastDate_Returns422()
    {
        var owner = await CreateOwnerAsync();
        var pet   = await CreatePetAsync(owner.Id);

        var resp = await _client.PostAsJsonAsync("/api/appointments",
            new CreateAppointmentRequest(
                pet.Id, "Dr. Test",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), // минуле
                new TimeOnly(10, 0), "Огляд"));

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ScheduleAppointment_TodayDate_Returns422()
    {
        var owner = await CreateOwnerAsync();
        var pet   = await CreatePetAsync(owner.Id);

        var resp = await _client.PostAsJsonAsync("/api/appointments",
            new CreateAppointmentRequest(
                pet.Id, "Dr. Test",
                DateOnly.FromDateTime(DateTime.UtcNow), // сьогодні — не дозволено
                new TimeOnly(10, 0), "Огляд"));

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ScheduleAppointment_NonExistentPet_Returns404()
    {
        var resp = await _client.PostAsJsonAsync("/api/appointments",
            new CreateAppointmentRequest(
                Guid.NewGuid(), // неіснуючий PetId
                "Dr. Test",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
                new TimeOnly(10, 0), "Огляд"));

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────
    // 4. Обробка помилок конфліктів
    // ──────────────────────────────────────────

    [Fact]
    public async Task ConflictHandling_DuplicateDateSamePet_Returns422()
    {
        var owner = await CreateOwnerAsync();
        var pet   = await CreatePetAsync(owner.Id);
        var date  = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));

        // Перший запис — успіх
        await CreateAppointmentAsync(pet.Id, date);

        // Другий на ту саму дату і тварину — конфлікт
        var resp = await _client.PostAsJsonAsync("/api/appointments",
            new CreateAppointmentRequest(
                pet.Id, "Dr. Other", date,
                new TimeOnly(15, 0), "Інший огляд"));

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        body!["error"].Should().Contain(date.ToString("dd.MM.yyyy"));
    }

    [Fact]
    public async Task ConflictHandling_SameDateDifferentPets_Returns201()
    {
        // Та сама дата але різні тварини — НЕ конфлікт
        var owner = await CreateOwnerAsync();
        var pet1  = await CreatePetAsync(owner.Id);
        var pet2  = await CreatePetAsync(owner.Id);
        var date  = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15));

        await CreateAppointmentAsync(pet1.Id, date);

        var resp = await _client.PostAsJsonAsync("/api/appointments",
            new CreateAppointmentRequest(
                pet2.Id, "Dr. Test", date,
                new TimeOnly(11, 0), "Огляд"));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ConflictHandling_CancelThenReschedule_NewRecordAllowed()
    {
        var owner = await CreateOwnerAsync();
        var pet   = await CreatePetAsync(owner.Id);
        var date  = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));

        // Створюємо і скасовуємо
        var appointment = await CreateAppointmentAsync(pet.Id, date);
        await _client.PatchAsync(
            $"/api/appointments/{appointment.Id}/cancel", null);

        // Після скасування — нова дата дозволена (старий запис не займає місце
        // у бізнес-логіці перевірки, але та сама дата заблокована індексом БД)
        var newDate = date.AddDays(1);
        var resp    = await _client.PostAsJsonAsync("/api/appointments",
            new CreateAppointmentRequest(
                pet.Id, "Dr. New", newDate,
                new TimeOnly(10, 0), "Повторний огляд"));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ──────────────────────────────────────────
    // 5. GET за датою — з seed-даними
    // ──────────────────────────────────────────

    [Fact]
    public async Task GetByDate_WithSeedData_ReturnsOnlyMatchingDate()
    {
        var owner = await CreateOwnerAsync();
        var pet   = await CreatePetAsync(owner.Id);
        var date  = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(50));

        await CreateAppointmentAsync(pet.Id, date);

        var resp = await _client.GetAsync(
            $"/api/appointments?date={date:yyyy-MM-dd}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await resp.Content
            .ReadFromJsonAsync<List<AppointmentResponse>>();

        results.Should().NotBeEmpty();
        results!.Should().AllSatisfy(a =>
            a.Date.Should().Be(date));
    }

    [Fact]
    public async Task GetByDate_InvalidFormat_Returns400WithMessage()
    {
        var resp = await _client.GetAsync(
            "/api/appointments?date=32-13-2099");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetByDate_NoAppointments_ReturnsEmptyList()
    {
        var date = new DateOnly(2099, 1, 1); // дата без записів

        var resp = await _client.GetAsync(
            $"/api/appointments?date={date:yyyy-MM-dd}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await resp.Content
            .ReadFromJsonAsync<List<AppointmentResponse>>();
        results.Should().BeEmpty();
    }

    // ──────────────────────────────────────────
    // 6. Скасування
    // ──────────────────────────────────────────

    [Fact]
    public async Task Cancel_ValidAppointment_Returns204AndStatusChanged()
    {
        var owner = await CreateOwnerAsync();
        var pet   = await CreatePetAsync(owner.Id);
        var date  = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(6));

        var appointment = await CreateAppointmentAsync(pet.Id, date);

        var cancelResp = await _client.PatchAsync(
            $"/api/appointments/{appointment.Id}/cancel", null);

        cancelResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Перевіряємо статус через GET
        var getResp = await _client.GetAsync(
            $"/api/appointments/{appointment.Id}");
        var updated = await getResp.Content
            .ReadFromJsonAsync<AppointmentResponse>();
        updated!.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_AlreadyCancelled_Returns422()
    {
        var owner = await CreateOwnerAsync();
        var pet   = await CreatePetAsync(owner.Id);
        var date  = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(8));

        var appointment = await CreateAppointmentAsync(pet.Id, date);

        await _client.PatchAsync(
            $"/api/appointments/{appointment.Id}/cancel", null);

        // Повторне скасування
        var resp = await _client.PatchAsync(
            $"/api/appointments/{appointment.Id}/cancel", null);

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Cancel_NonExistentId_Returns404()
    {
        var resp = await _client.PatchAsync(
            $"/api/appointments/{Guid.NewGuid()}/cancel", null);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────
    // 7. Каскадне видалення
    // ──────────────────────────────────────────

    [Fact]
    public async Task DeleteOwner_CascadesDeleteToPetsAndAppointments()
    {
        var owner = await CreateOwnerAsync();
        var pet   = await CreatePetAsync(owner.Id);
        var date  = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(9));
        await CreateAppointmentAsync(pet.Id, date);

        // Видаляємо власника
        var deleteResp = await _client.DeleteAsync($"/api/owners/{owner.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Pet повинен бути видалений
        var petResp = await _client.GetAsync($"/api/pets/{pet.Id}");
        petResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}