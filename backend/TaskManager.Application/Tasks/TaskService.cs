using FluentValidation;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Common;
using TaskManager.Application.Exceptions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks;

public class TaskService(
    ITaskRepository tasks,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IBusinessClock clock,
    IValidator<CreateTaskRequest> createValidator,
    IValidator<UpdateTaskRequest> updateValidator,
    IValidator<ChangeTaskStatusRequest> statusValidator,
    IValidator<TaskQueryParameters> queryValidator)
{
    private Guid? OwnerScope => currentUser.IsAdmin ? null : currentUser.UserId;

    private DateTime Now => clock.UtcNow;
    private DateOnly Today => clock.Today;

    public async Task<PagedResult<TaskResponse>> SearchAsync(TaskQueryParameters query, CancellationToken ct)
    {
        await queryValidator.ValidateAndThrowAsync(query, ct);
        var today = Today;
        var page = await tasks.SearchAsync(query, OwnerScope, today, ct);
        return page.Map(t => ToResponse(t, today));
    }

    public async Task<TaskResponse> GetAsync(Guid id, CancellationToken ct) =>
        ToResponse(await LoadAsync(id, ct), Today);

    public async Task<TaskSummaryResponse> GetSummaryAsync(CancellationToken ct)
    {
        var counts = await tasks.CountByStatusAsync(OwnerScope, ct);
        var overdue = await tasks.CountOverdueAsync(OwnerScope, Today, ct);
        int Count(TaskItemStatus s) => counts.GetValueOrDefault(s);

        return new TaskSummaryResponse(
            Total: counts.Values.Sum(),
            Pending: Count(TaskItemStatus.Pending),
            InProgress: Count(TaskItemStatus.InProgress),
            Completed: Count(TaskItemStatus.Completed),
            Cancelled: Count(TaskItemStatus.Cancelled),
            Overdue: overdue);
    }

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);

        var task = TaskItem.Create(currentUser.UserId, request.Title, request.Description, request.Priority, request.DueDate, Now);
        tasks.Add(task);
        await unitOfWork.SaveChangesAsync(ct);

        return ToResponse(task, Today);
    }

    public async Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct)
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);

        var task = await LoadAsync(id, ct);
        task.Update(request.Title, request.Description, request.Priority, request.DueDate, Now);
        await unitOfWork.SaveChangesAsync(ct);

        return ToResponse(task, Today);
    }

    public async Task<TaskResponse> ChangeStatusAsync(Guid id, ChangeTaskStatusRequest request, CancellationToken ct)
    {
        await statusValidator.ValidateAndThrowAsync(request, ct);

        var task = await LoadAsync(id, ct);
        task.ChangeStatus(request.Status, Now);
        await unitOfWork.SaveChangesAsync(ct);

        return ToResponse(task, Today);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var task = await LoadAsync(id, ct);
        tasks.Remove(task);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<TaskItem> LoadAsync(Guid id, CancellationToken ct) =>
        await tasks.GetByIdAsync(id, OwnerScope, ct)
        ?? throw new NotFoundException("Tarefa não encontrada.");

    private static TaskResponse ToResponse(TaskItem t, DateOnly today) => new(
        t.Id,
        t.Title,
        t.Description,
        t.Status,
        t.Priority,
        t.DueDate,
        t.IsOverdue(today),
        t.CreatedAt,
        t.UpdatedAt,
        t.CompletedAt,
        t.OwnerId,
        t.Owner?.Name,
        TaskItem.TransitionsFrom(t.Status));
}
