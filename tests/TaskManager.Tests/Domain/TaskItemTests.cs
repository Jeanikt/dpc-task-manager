using FluentAssertions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;

namespace TaskManager.Tests.Domain;

public class TaskItemTests
{
    private static readonly DateTime Now = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now);

    private static TaskItem NewTask(DateOnly? dueDate = null) =>
        TaskItem.Create(Guid.NewGuid(), "  Fechamento mensal  ", "  ", TaskPriority.High, dueDate, Now);

    [Fact]
    public void Create_starts_pending_and_normalizes_text()
    {
        var task = NewTask();

        task.Status.Should().Be(TaskItemStatus.Pending);
        task.Title.Should().Be("Fechamento mensal");
        task.Description.Should().BeNull();
        task.CreatedAt.Should().Be(Now);
        task.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Create_without_title_throws()
    {
        var act = () => TaskItem.Create(Guid.NewGuid(), " ", null, TaskPriority.Low, null, Now);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(TaskItemStatus.Pending, TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.Pending, TaskItemStatus.Completed)]
    [InlineData(TaskItemStatus.Pending, TaskItemStatus.Cancelled)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Pending)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Completed)]
    [InlineData(TaskItemStatus.Completed, TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.Cancelled, TaskItemStatus.Pending)]
    public void ChangeStatus_accepts_allowed_transitions(TaskItemStatus from, TaskItemStatus to)
    {
        var task = TaskIn(from);

        task.ChangeStatus(to, Now.AddHours(1));

        task.Status.Should().Be(to);
        task.UpdatedAt.Should().Be(Now.AddHours(1));
    }

    [Theory]
    [InlineData(TaskItemStatus.Cancelled, TaskItemStatus.Completed)]
    [InlineData(TaskItemStatus.Cancelled, TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.Completed, TaskItemStatus.Cancelled)]
    public void ChangeStatus_rejects_forbidden_transitions(TaskItemStatus from, TaskItemStatus to)
    {
        var task = TaskIn(from);

        var act = () => task.ChangeStatus(to, Now);

        act.Should().Throw<DomainException>();
        task.Status.Should().Be(from);
    }

    [Fact]
    public void ChangeStatus_to_same_status_throws()
    {
        var act = () => NewTask().ChangeStatus(TaskItemStatus.Pending, Now);

        act.Should().Throw<DomainException>().WithMessage("*já está*");
    }

    [Fact]
    public void Completing_sets_CompletedAt_and_reopening_clears_it()
    {
        var task = NewTask();

        task.ChangeStatus(TaskItemStatus.Completed, Now);
        task.CompletedAt.Should().Be(Now);

        task.ChangeStatus(TaskItemStatus.InProgress, Now.AddDays(1));
        task.CompletedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(TaskItemStatus.Completed)]
    [InlineData(TaskItemStatus.Cancelled)]
    public void Update_on_closed_task_throws(TaskItemStatus closedStatus)
    {
        var task = TaskIn(closedStatus);

        var act = () => task.Update("Novo título", null, TaskPriority.Low, null, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void IsOverdue_only_for_open_tasks_past_due_date()
    {
        NewTask(Today.AddDays(-1)).IsOverdue(Today).Should().BeTrue();
        NewTask(Today).IsOverdue(Today).Should().BeFalse();
        NewTask().IsOverdue(Today).Should().BeFalse();

        var completed = NewTask(Today.AddDays(-1));
        completed.ChangeStatus(TaskItemStatus.Completed, Now);
        completed.IsOverdue(Today).Should().BeFalse();
    }

    private static TaskItem TaskIn(TaskItemStatus status)
    {
        var task = NewTask();
        if (status != TaskItemStatus.Pending)
            task.ChangeStatus(status, Now);
        return task;
    }
}
