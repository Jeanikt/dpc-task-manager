using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Common;
using TaskManager.Application.Tasks;
using TaskManager.Domain.Enums;

namespace TaskManager.Tests.Integration;

public class TasksApiTests(TaskManagerApiFactory factory) : IClassFixture<TaskManagerApiFactory>
{
    private static readonly System.Text.Json.JsonSerializerOptions Json = TaskManagerApiFactory.Json;
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Requests_without_token_return_401()
    {
        var response = await factory.CreateClient().GetAsync("/api/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Full_crud_flow()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var create = await client.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest("Enviar SPED", "Prazo da Receita", TaskPriority.High, Today.AddDays(2)), Json);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        create.Headers.Location.Should().NotBeNull();
        var created = await create.Content.ReadFromJsonAsync<TaskResponse>(Json);
        created!.Status.Should().Be(TaskItemStatus.Pending);
        created.AllowedTransitions.Should().Contain(TaskItemStatus.InProgress);

        var fetched = await client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{created.Id}", Json);
        fetched!.Title.Should().Be("Enviar SPED");

        var update = await client.PutAsJsonAsync($"/api/tasks/{created.Id}",
            new UpdateTaskRequest("Enviar SPED Fiscal", null, TaskPriority.Medium, null), Json);
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await update.Content.ReadFromJsonAsync<TaskResponse>(Json);
        updated!.Title.Should().Be("Enviar SPED Fiscal");
        updated.Description.Should().BeNull();
        updated.DueDate.Should().BeNull();

        var status = await client.PatchAsJsonAsync($"/api/tasks/{created.Id}/status",
            new ChangeTaskStatusRequest(TaskItemStatus.Completed), Json);
        status.StatusCode.Should().Be(HttpStatusCode.OK);
        var completed = await status.Content.ReadFromJsonAsync<TaskResponse>(Json);
        completed!.Status.Should().Be(TaskItemStatus.Completed);
        completed.CompletedAt.Should().NotBeNull();

        (await client.DeleteAsync($"/api/tasks/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/tasks/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_with_invalid_data_returns_validation_problem()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest("", null, TaskPriority.Low, Today.AddDays(-3)), Json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        problem!.Errors.Keys.Should().BeEquivalentTo("title", "dueDate");
    }

    [Fact]
    public async Task Create_with_unknown_enum_returns_400_without_internal_details()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var body = new StringContent("""{"title":"Teste","priority":"Urgentissima"}""", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/tasks", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("priority").And.NotContain("TaskManager.");
    }

    [Fact]
    public async Task Forbidden_status_transition_returns_422()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var task = await CreateTaskAsync(client, "Tarefa cancelada");
        await ChangeStatusAsync(client, task.Id, TaskItemStatus.Cancelled);

        var response = await client.PatchAsJsonAsync($"/api/tasks/{task.Id}/status",
            new ChangeTaskStatusRequest(TaskItemStatus.Completed), Json);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        problem!.Detail.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Editing_a_completed_task_returns_422()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var task = await CreateTaskAsync(client, "Tarefa concluída");
        await ChangeStatusAsync(client, task.Id, TaskItemStatus.Completed);

        var response = await client.PutAsJsonAsync($"/api/tasks/{task.Id}",
            new UpdateTaskRequest("Outro título", null, TaskPriority.Low, null), Json);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Users_cannot_access_tasks_from_other_users()
    {
        var owner = await factory.CreateAuthenticatedClientAsync();
        var intruder = await factory.CreateAuthenticatedClientAsync();
        var task = await CreateTaskAsync(owner, "Tarefa privada");

        (await intruder.GetAsync($"/api/tasks/{task.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await intruder.DeleteAsync($"/api/tasks/{task.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var list = await intruder.GetFromJsonAsync<PagedResult<TaskResponse>>("/api/tasks", Json);
        list!.Items.Should().NotContain(t => t.Id == task.Id);
    }

    [Fact]
    public async Task List_supports_search_filters_and_pagination()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        await CreateTaskAsync(client, "Conciliação bancária", TaskPriority.High);
        await CreateTaskAsync(client, "Folha de pagamento", TaskPriority.Low);
        var inProgress = await CreateTaskAsync(client, "Balancete trimestral", TaskPriority.High);
        await ChangeStatusAsync(client, inProgress.Id, TaskItemStatus.InProgress);

        var search = await client.GetFromJsonAsync<PagedResult<TaskResponse>>("/api/tasks?search=folha", Json);
        search!.Items.Should().ContainSingle(t => t.Title == "Folha de pagamento");

        var byStatus = await client.GetFromJsonAsync<PagedResult<TaskResponse>>("/api/tasks?status=InProgress", Json);
        byStatus!.Items.Should().ContainSingle(t => t.Id == inProgress.Id);

        var byPriority = await client.GetFromJsonAsync<PagedResult<TaskResponse>>("/api/tasks?priority=High&sortBy=Title&sortDirection=Asc", Json);
        byPriority!.Items.Select(t => t.Title).Should().Equal("Balancete trimestral", "Conciliação bancária");

        var paged = await client.GetFromJsonAsync<PagedResult<TaskResponse>>("/api/tasks?pageSize=2&page=2", Json);
        paged!.TotalCount.Should().Be(3);
        paged.TotalPages.Should().Be(2);
        paged.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task List_with_invalid_page_size_returns_400()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/tasks?pageSize=500");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Summary_counts_tasks_by_status()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        await CreateTaskAsync(client, "A");
        var b = await CreateTaskAsync(client, "B");
        await ChangeStatusAsync(client, b.Id, TaskItemStatus.Completed);

        var summary = await client.GetFromJsonAsync<TaskSummaryResponse>("/api/tasks/summary", Json);

        summary.Should().BeEquivalentTo(new TaskSummaryResponse(Total: 2, Pending: 1, InProgress: 0, Completed: 1, Cancelled: 0, Overdue: 0));
    }

    private static async Task<TaskResponse> CreateTaskAsync(HttpClient client, string title, TaskPriority priority = TaskPriority.Medium)
    {
        var response = await client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest(title, null, priority, null), Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskResponse>(Json))!;
    }

    private static async Task ChangeStatusAsync(HttpClient client, Guid id, TaskItemStatus status)
    {
        var response = await client.PatchAsJsonAsync($"/api/tasks/{id}/status", new ChangeTaskStatusRequest(status), Json);
        response.EnsureSuccessStatusCode();
    }
}
