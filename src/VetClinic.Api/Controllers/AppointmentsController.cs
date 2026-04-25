using Microsoft.AspNetCore.Mvc;
using VetClinic.Api.DTOs;
using VetClinic.Api.Services;

namespace VetClinic.Api.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController(AppointmentService svc) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest req)
    {
        var result = await svc.CreateAsync(req);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetByDate([FromQuery] string date)
    {
        if (!DateOnly.TryParse(date, out var parsedDate))
            return BadRequest("Невірний формат дати. Використовуй YYYY-MM-DD");

        return Ok(await svc.GetByDateAsync(parsedDate));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await svc.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await svc.CancelAsync(id);
        return NoContent();
    }
}