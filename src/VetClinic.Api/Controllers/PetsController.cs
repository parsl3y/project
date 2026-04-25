using Microsoft.AspNetCore.Mvc;
using VetClinic.Api.DTOs;
using VetClinic.Api.Services;

namespace VetClinic.Api.Controllers;

[ApiController]
[Route("api/pets")]
public class PetsController(PetService svc) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await svc.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("by-owner/{ownerId:guid}")]
    public async Task<IActionResult> GetByOwner(Guid ownerId) =>
        Ok(await svc.GetByOwnerAsync(ownerId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePetRequest req)
    {
        var result = await svc.CreateAsync(req);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await svc.DeleteAsync(id);
        return NoContent();
    }
}