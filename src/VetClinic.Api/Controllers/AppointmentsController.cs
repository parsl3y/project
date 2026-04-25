using Microsoft.AspNetCore.Mvc;
using VetClinic.Api.DTOs;
using VetClinic.Api.Services;

namespace VetClinic.Api.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController(AppointmentService svc) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetByDate([FromQuery] DateOnly date) =>
        Ok(await svc.GetByDateAsync(date));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await svc.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest req)
    {
        var result = await svc.CreateAsync(req);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id,
        [FromBody] UpdateAppointmentRequest req)
    {
        var result = await svc.UpdateAsync(id, req);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await svc.DeleteAsync(id);
        return NoContent();
    }
}