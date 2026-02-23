using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProviderOptimizerService.Application.Features.Providers.Commands.CreateProvider;
using ProviderOptimizerService.Application.Features.Providers.Commands.DeleteProvider;
using ProviderOptimizerService.Application.Features.Providers.Commands.UpdateProvider;
using ProviderOptimizerService.Application.Features.Providers.Queries.GetAvailableProviders;
using ProviderOptimizerService.Application.Features.Providers.Queries.GetProviderById;
using ProviderOptimizerService.Application.Features.Providers.Queries.GetProviders;
using ProviderOptimizerService.Domain.Entities;

namespace ProviderOptimizerService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProvidersController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProvidersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Get all providers.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _mediator.Send(new GetProvidersQuery(), ct));

    /// <summary>
    /// GET /providers/available — Returns only available providers,
    /// optionally filtered by type. Criterion #26.
    /// </summary>
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable(
        [FromQuery] ProviderType? type,
        CancellationToken ct)
        => Ok(await _mediator.Send(new GetAvailableProvidersQuery(type), ct));

    /// <summary>Get provider by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetProviderByIdQuery(id), ct));

    /// <summary>Create a new provider.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProviderCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Update a provider.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProviderCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("ID mismatch.");
        return Ok(await _mediator.Send(command, ct));
    }

    /// <summary>Delete a provider.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteProviderCommand(id), ct);
        return NoContent();
    }
}
