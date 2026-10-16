using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Common;
using TaskManager.Application.Tasks;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Infrastructure.Persistence.Repositories;

public class TaskRepository(AppDbContext db) : ITaskRepository
{
    public Task<TaskItem?> GetByIdAsync(Guid id, Guid? ownerId, CancellationToken ct) =>
        Scoped(ownerId).Include(t => t.Owner).FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<PagedResult<TaskItem>> SearchAsync(TaskQueryParameters query, Guid? ownerId, DateOnly today, CancellationToken ct)
    {
        var tasks = Scoped(ownerId).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLike(query.Search.Trim())}%";
            tasks = tasks.Where(t =>
                EF.Functions.Like(t.Title, pattern, "\\") ||
                (t.Description != null && EF.Functions.Like(t.Description, pattern, "\\")));
        }

        if (query.Status is { } status)
            tasks = tasks.Where(t => t.Status == status);

        if (query.Priority is { } priority)
            tasks = tasks.Where(t => t.Priority == priority);

        if (query.DueFrom is { } dueFrom)
            tasks = tasks.Where(t => t.DueDate >= dueFrom);

        if (query.DueTo is { } dueTo)
            tasks = tasks.Where(t => t.DueDate <= dueTo);

        if (query.Overdue == true)
            tasks = tasks.Where(OverduePredicate(today));

        var total = await tasks.CountAsync(ct);
        var items = await Sort(tasks, query)
            .Include(t => t.Owner)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<TaskItem>(items, query.Page, query.PageSize, total);
    }

    public Task<Dictionary<TaskItemStatus, int>> CountByStatusAsync(Guid? ownerId, CancellationToken ct) =>
        Scoped(ownerId)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);

    public Task<int> CountOverdueAsync(Guid? ownerId, DateOnly today, CancellationToken ct) =>
        Scoped(ownerId).CountAsync(OverduePredicate(today), ct);

    public void Add(TaskItem task) => db.Tasks.Add(task);

    public void Remove(TaskItem task) => db.Tasks.Remove(task);

    private IQueryable<TaskItem> Scoped(Guid? ownerId) =>
        ownerId is { } id ? db.Tasks.Where(t => t.OwnerId == id) : db.Tasks;

    private static System.Linq.Expressions.Expression<Func<TaskItem, bool>> OverduePredicate(DateOnly today) =>
        t => t.Status != TaskItemStatus.Completed
             && t.Status != TaskItemStatus.Cancelled
             && t.DueDate != null
             && t.DueDate < today;

    private static IQueryable<TaskItem> Sort(IQueryable<TaskItem> tasks, TaskQueryParameters query)
    {
        var asc = query.SortDirection == SortDirection.Asc;

        IOrderedQueryable<TaskItem> ordered = query.SortBy switch
        {
            TaskSortField.DueDate => asc
                ? tasks.OrderBy(t => t.DueDate == null).ThenBy(t => t.DueDate)
                : tasks.OrderBy(t => t.DueDate == null).ThenByDescending(t => t.DueDate),
            TaskSortField.Priority => asc ? tasks.OrderBy(t => t.Priority) : tasks.OrderByDescending(t => t.Priority),
            TaskSortField.Title => asc ? tasks.OrderBy(t => t.Title) : tasks.OrderByDescending(t => t.Title),
            TaskSortField.Status => asc ? tasks.OrderBy(t => t.Status) : tasks.OrderByDescending(t => t.Status),
            TaskSortField.UpdatedAt => asc ? tasks.OrderBy(t => t.UpdatedAt) : tasks.OrderByDescending(t => t.UpdatedAt),
            _ => asc ? tasks.OrderBy(t => t.CreatedAt) : tasks.OrderByDescending(t => t.CreatedAt)
        };

        return ordered.ThenByDescending(t => t.CreatedAt).ThenBy(t => t.Id);
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
