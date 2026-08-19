using ECommerce.Application.DTOs;

namespace ECommerce.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(UserDto user);
}
