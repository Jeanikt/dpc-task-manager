using TaskManager.Domain.Enums;

namespace TaskManager.Application.Auth;

public record RegisterRequest(string Name, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record UserResponse(Guid Id, string Name, string Email, UserRole Role);

public record AuthResponse(string AccessToken, DateTime ExpiresAt, UserResponse User);
