using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using FluentAssertions;
using VetClinic.Api.DTOs;
using VetClinic.Api.Entities;
using Xunit;

namespace VetClinic.Tests.Integration;

public class PetIntegrationTests : IClassFixture<VetClinicWebAppFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly VetClinicWebAppFactory _factory;
    private readonly Fixture _fixture = new();

    public PetIntegrationTests(VetClinicWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() =>
        await _factory.ResetPetsAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<OwnerResponse> CreateOwnerAsync()
    {
        var req = new CreateOwnerRequest(
            _fixture.Create<string>()[..8],
            _fixture.Create<string>()[..8],
            "+380991234567",
            $"{_fixture.Create<string>()[..6]}@test.com");

        var resp = await _client.PostAsJsonAsync("/api/owners", req);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await resp.Content.ReadFromJsonAsync<OwnerResponse>())!;
    }

    private async Task<PetResponse> CreatePetAsync(Guid ownerId)
    {
        var req = new CreatePetRequest(
            _fixture.Create<string>()[..10],
            Species.Dog,
            "Labrador",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)),
            ownerId);

        var resp = await _client.PostAsJsonAsync("/api/pets", req);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await resp.Content.ReadFromJsonAsync<PetResponse>())!;
    }

    [Fact]
    public async Task Put_UpdatePet_WithValidData_ReturnsUpdatedPet()
    {
        // Arrange
        var owner = await CreateOwnerAsync();
        var pet = await CreatePetAsync(owner.Id);

        var updateReq = new UpdatePetRequest(
            "UpdatedName",
            Species.Cat,
            "Persian",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-3)),
            owner.Id);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/pets/{pet.Id}", updateReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedPet = await response.Content.ReadFromJsonAsync<PetResponse>();
        
        updatedPet.Should().NotBeNull();
        updatedPet!.Id.Should().Be(pet.Id);
        updatedPet.Name.Should().Be("UpdatedName");
        updatedPet.Species.Should().Be(Species.Cat);
        updatedPet.Breed.Should().Be("Persian");
        updatedPet.BirthDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-3)));
        updatedPet.OwnerId.Should().Be(owner.Id);
    }

    [Fact]
    public async Task Put_UpdatePet_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var owner = await CreateOwnerAsync();
        var nonExistentPetId = Guid.NewGuid();

        var updateReq = new UpdatePetRequest(
            "UpdatedName",
            Species.Cat,
            "Persian",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-3)),
            owner.Id);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/pets/{nonExistentPetId}", updateReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_UpdatePet_WithNonExistentOwner_ReturnsNotFound()
    {
        // Arrange
        var owner = await CreateOwnerAsync();
        var pet = await CreatePetAsync(owner.Id);
        var nonExistentOwnerId = Guid.NewGuid();

        var updateReq = new UpdatePetRequest(
            "UpdatedName",
            Species.Cat,
            "Persian",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-3)),
            nonExistentOwnerId);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/pets/{pet.Id}", updateReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_UpdatePet_WithValidData_UpdatesSuccessfully()
    {
        // Arrange
        var owner = await CreateOwnerAsync();
        var pet = await CreatePetAsync(owner.Id);

        var updateReq = new UpdatePetRequest(
            "ValidPetName",
            Species.Bird,
            "Parrot",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            owner.Id);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/pets/{pet.Id}", updateReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedPet = await response.Content.ReadFromJsonAsync<PetResponse>();
        
        updatedPet.Should().NotBeNull();
        updatedPet!.Name.Should().Be("ValidPetName");
        updatedPet.Species.Should().Be(Species.Bird);
        updatedPet.Breed.Should().Be("Parrot");
    }

    [Fact]
    public async Task Get_OwnerPets_ReturnsOwnerPets()
    {
        // Arrange
        var owner = await CreateOwnerAsync();
        var pet1 = await CreatePetAsync(owner.Id);
        var pet2 = await CreatePetAsync(owner.Id);

        // Act
        var response = await _client.GetAsync($"/api/owners/{owner.Id}/pets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pets = await response.Content.ReadFromJsonAsync<List<PetResponse>>();
        
        pets.Should().NotBeNull();
        pets!.Should().HaveCount(2);
        pets.Should().Contain(p => p.Id == pet1.Id);
        pets.Should().Contain(p => p.Id == pet2.Id);
    }

    [Fact]
    public async Task Get_OwnerPets_WithNonExistentOwner_ReturnsNotFound()
    {
        // Arrange
        var nonExistentOwnerId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/owners/{nonExistentOwnerId}/pets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_OwnerPets_WithNoPets_ReturnsEmptyList()
    {
        // Arrange
        var owner = await CreateOwnerAsync();

        // Act
        var response = await _client.GetAsync($"/api/owners/{owner.Id}/pets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pets = await response.Content.ReadFromJsonAsync<List<PetResponse>>();
        
        pets.Should().NotBeNull();
        pets!.Should().BeEmpty();
    }
}
