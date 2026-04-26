using Microsoft.AspNetCore.Mvc;
using VetClinic.Api.DTOs;
using VetClinic.Api.Services;

namespace VetClinic.Api.Controllers;

[ApiController]
[Route("api/owners")]
public class OwnersController(OwnerService svc) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await svc.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await svc.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOwnerRequest req)
    {
        var result = await svc.CreateAsync(req);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}/pets")]
    public async Task<IActionResult> GetOwnerPets(Guid id)
    {
        var pets = await svc.GetOwnerPetsAsync(id);
        return Ok(pets);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await svc.DeleteAsync(id);
        return NoContent();
    }
}
