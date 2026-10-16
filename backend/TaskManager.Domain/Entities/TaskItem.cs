using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;

namespace TaskManager.Domain.Entities;

public class TaskItem
{
    private static readonly Dictionary<TaskItemStatus, TaskItemStatus[]> AllowedTransitions = new()
    {
        [TaskItemStatus.Pending] = [TaskItemStatus.InProgress, TaskItemStatus.Completed, TaskItemStatus.Cancelled],
        [TaskItemStatus.InProgress] = [TaskItemStatus.Pending, TaskItemStatus.Completed, TaskItemStatus.Cancelled],
        [TaskItemStatus.Completed] = [TaskItemStatus.Pending, TaskItemStatus.InProgress],
        [TaskItemStatus.Cancelled] = [TaskItemStatus.Pending]
    };

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public TaskPriority Priority { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public Guid OwnerId { get; private set; }
    public User? Owner { get; private set; }

    private TaskItem() { }

    public static TaskItem Create(Guid ownerId, string title, string? description, TaskPriority priority, DateOnly? dueDate, DateTime now)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Status = TaskItemStatus.Pending,
            CreatedAt = now
        };
        task.SetDetails(title, description, priority, dueDate, now);
        return task;
    }

    public bool IsClosed => Status is TaskItemStatus.Completed or TaskItemStatus.Cancelled;

    public bool IsOverdue(DateOnly today) => !IsClosed && DueDate is { } due && due < today;

    public void Update(string title, string? description, TaskPriority priority, DateOnly? dueDate, DateTime now)
    {
        if (IsClosed)
            throw new DomainException("Tarefas concluídas ou canceladas não podem ser editadas. Reabra a tarefa antes de alterá-la.");

        SetDetails(title, description, priority, dueDate, now);
    }

    public void ChangeStatus(TaskItemStatus newStatus, DateTime now)
    {
        if (newStatus == Status)
            throw new DomainException("A tarefa já está nesse status.");

        if (!CanTransitionTo(newStatus))
            throw new DomainException($"Não é permitido mudar o status de '{Status}' para '{newStatus}'.");

        Status = newStatus;
        CompletedAt = newStatus == TaskItemStatus.Completed ? now : null;
        UpdatedAt = now;
    }

    public bool CanTransitionTo(TaskItemStatus newStatus) =>
        AllowedTransitions.TryGetValue(Status, out var allowed) && allowed.Contains(newStatus);

    public static IReadOnlyCollection<TaskItemStatus> TransitionsFrom(TaskItemStatus status) =>
        AllowedTransitions.TryGetValue(status, out var allowed) ? allowed : [];

    private void SetDetails(string title, string? description, TaskPriority priority, DateOnly? dueDate, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("O título é obrigatório.");

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Priority = priority;
        DueDate = dueDate;
        UpdatedAt = now;
    }
}
