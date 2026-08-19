namespace ECommerce.Application.DTOs;

public sealed record UserDto(
    int Id,
    string Username,
    string Email,
    string Role);
