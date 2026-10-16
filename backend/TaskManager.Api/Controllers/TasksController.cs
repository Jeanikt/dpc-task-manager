using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Common;
using TaskManager.Application.Tasks;

namespace TaskManager.Api.Controllers;

/// <summary>
/// CRUD de tarefas e controle de status. Usuários comuns acessam apenas as próprias tarefas;
/// administradores acessam todas.
/// </summary>
[ApiController]
[Route("api/tasks")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class TasksController(TaskService taskService) : ControllerBase
{
    /// <summary>Lista tarefas com pesquisa, filtros, ordenação e paginação.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<TaskResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<PagedResult<TaskResponse>> List([FromQuery] TaskQueryParameters query, CancellationToken ct) =>
        taskService.SearchAsync(query, ct);

    /// <summary>Totais por status e quantidade de tarefas atrasadas.</summary>
    [HttpGet("summary")]
    [ProducesResponseType<TaskSummaryResponse>(StatusCodes.Status200OK)]
    public Task<TaskSummaryResponse> Summary(CancellationToken ct) =>
        taskService.GetSummaryAsync(ct);

    /// <summary>Detalhe de uma tarefa.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<TaskResponse> Get(Guid id, CancellationToken ct) =>
        taskService.GetAsync(id, ct);

    /// <summary>Cria uma tarefa. Toda tarefa nasce com status Pending.</summary>
    [HttpPost]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskResponse>> Create(CreateTaskRequest request, CancellationToken ct)
    {
        var task = await taskService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }

    /// <summary>Atualiza título, descrição, prioridade e prazo. Tarefas concluídas ou canceladas não podem ser editadas.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<TaskResponse> Update(Guid id, UpdateTaskRequest request, CancellationToken ct) =>
        taskService.UpdateAsync(id, request, ct);

    /// <summary>
    /// Altera o status da tarefa. Transições permitidas:
    /// Pending → InProgress | Completed | Cancelled;
    /// InProgress → Pending | Completed | Cancelled;
    /// Completed → Pending | InProgress (reabrir);
    /// Cancelled → Pending (reativar).
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<TaskResponse> ChangeStatus(Guid id, ChangeTaskStatusRequest request, CancellationToken ct) =>
        taskService.ChangeStatusAsync(id, request, ct);

    /// <summary>Exclui uma tarefa.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await taskService.DeleteAsync(id, ct);
        return NoContent();
    }
}
