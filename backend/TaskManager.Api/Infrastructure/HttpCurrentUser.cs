using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using TaskManager.Application.Abstractions;
using TaskManager.Domain.Enums;

namespace TaskManager.Api.Infrastructure;

public class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal =>
        accessor.HttpContext?.User ?? throw new InvalidOperationException("Não há requisição HTTP ativa.");

    public Guid UserId =>
        Guid.TryParse(Principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : throw new UnauthorizedAccessException("Usuário não autenticado.");

    public bool IsAdmin => Principal.IsInRole(nameof(UserRole.Admin));
}
