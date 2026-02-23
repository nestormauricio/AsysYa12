using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProviderOptimizerService.Application.Features.Auth.Commands.Login;
using ProviderOptimizerService.Application.Features.Auth.Commands.Register;
using ProviderOptimizerService.Application.Features.Auth.DTOs;

namespace ProviderOptimizerService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RegisterCommand(request.Username, request.Email, request.Password), ct);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password), ct);
        return Ok(result);
    }
}
