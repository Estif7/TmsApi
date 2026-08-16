namespace TmsApi.Application.DTOs;

public record LoginRequestDto(string Email, string Password);

public record AuthResponseDto(
    string Token,
    string Email,
    string Role,
    DateTime ExpiresAt);