using FluentValidation;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Exceptions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Auth;

public class AuthService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    TimeProvider clock,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        await registerValidator.ValidateAndThrowAsync(request, ct);

        var email = User.NormalizeEmail(request.Email);
        if (await users.EmailExistsAsync(email, ct))
            throw new ConflictException("Já existe um usuário cadastrado com esse e-mail.");

        var user = User.Create(request.Name, email, passwordHasher.Hash(request.Password), UserRole.User, clock.GetUtcNow().UtcDateTime);
        users.Add(user);
        await unitOfWork.SaveChangesAsync(ct);

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        await loginValidator.ValidateAndThrowAsync(request, ct);

        var user = await users.GetByEmailAsync(User.NormalizeEmail(request.Email), ct);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        return CreateAuthResponse(user);
    }

    public async Task<UserResponse> GetProfileAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("Usuário não encontrado.");
        return ToResponse(user);
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var (token, expiresAt) = tokenService.CreateToken(user);
        return new AuthResponse(token, expiresAt, ToResponse(user));
    }

    private static UserResponse ToResponse(User user) => new(user.Id, user.Name, user.Email, user.Role);
}
