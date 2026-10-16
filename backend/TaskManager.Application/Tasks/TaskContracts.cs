using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks;

public record CreateTaskRequest(string Title, string? Description, TaskPriority Priority, DateOnly? DueDate);

public record UpdateTaskRequest(string Title, string? Description, TaskPriority Priority, DateOnly? DueDate);

public record ChangeTaskStatusRequest(TaskItemStatus Status);

public record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    DateOnly? DueDate,
    bool IsOverdue,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt,
    Guid OwnerId,
    string? OwnerName,
    IReadOnlyCollection<TaskItemStatus> AllowedTransitions);

public record TaskSummaryResponse(int Total, int Pending, int InProgress, int Completed, int Cancelled, int Overdue);

public enum TaskSortField
{
    CreatedAt,
    UpdatedAt,
    DueDate,
    Priority,
    Title,
    Status
}

public enum SortDirection
{
    Asc,
    Desc
}

/// <summary>
/// Filtros, pesquisa, ordenação e paginação da listagem de tarefas.
/// </summary>
public class TaskQueryParameters
{
    public const int MaxPageSize = 100;

    /// <summary>Pesquisa por texto no título ou na descrição.</summary>
    public string? Search { get; set; }
    public TaskItemStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    /// <summary>Somente tarefas abertas com prazo vencido.</summary>
    public bool? Overdue { get; set; }
    public DateOnly? DueFrom { get; set; }
    public DateOnly? DueTo { get; set; }
    public TaskSortField SortBy { get; set; } = TaskSortField.CreatedAt;
    public SortDirection SortDirection { get; set; } = SortDirection.Desc;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
