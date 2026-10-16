using TaskManager.Application.Common;
using TaskManager.Application.Tasks;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Abstractions;

public interface ICurrentUser
{
    Guid UserId { get; }
    bool IsAdmin { get; }
}

public interface IBusinessClock
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(User user);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct);
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct);
    void Add(User user);
}

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, Guid? ownerId, CancellationToken ct);
    Task<PagedResult<TaskItem>> SearchAsync(TaskQueryParameters query, Guid? ownerId, DateOnly today, CancellationToken ct);
    Task<Dictionary<TaskItemStatus, int>> CountByStatusAsync(Guid? ownerId, CancellationToken ct);
    Task<int> CountOverdueAsync(Guid? ownerId, DateOnly today, CancellationToken ct);
    void Add(TaskItem task);
    void Remove(TaskItem task);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}
