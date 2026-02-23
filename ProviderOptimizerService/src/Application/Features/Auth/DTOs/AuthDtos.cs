namespace ProviderOptimizerService.Application.Features.Auth.DTOs;

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Username, string Email, string Password);
public record AuthResultDto(string Token, DateTime ExpiresAt, string Username, string Email, string Role);
