using FluentValidation;
using TaskManager.Application.Abstractions;

namespace TaskManager.Application.Tasks;

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator(IBusinessClock clock)
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(150).WithMessage("O título deve ter no máximo 150 caracteres.");
        RuleFor(x => x.Description).MaximumLength(2000).WithMessage("A descrição deve ter no máximo 2000 caracteres.");
        RuleFor(x => x.Priority).IsInEnum().WithMessage("Prioridade inválida.");
        RuleFor(x => x.DueDate)
            .Must(d => d is null || d >= clock.Today)
            .WithMessage("O prazo não pode estar no passado.");
    }
}

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(150).WithMessage("O título deve ter no máximo 150 caracteres.");
        RuleFor(x => x.Description).MaximumLength(2000).WithMessage("A descrição deve ter no máximo 2000 caracteres.");
        RuleFor(x => x.Priority).IsInEnum().WithMessage("Prioridade inválida.");
    }
}

public class ChangeTaskStatusRequestValidator : AbstractValidator<ChangeTaskStatusRequest>
{
    public ChangeTaskStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("Status inválido.");
    }
}

public class TaskQueryParametersValidator : AbstractValidator<TaskQueryParameters>
{
    public TaskQueryParametersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("A página deve ser maior ou igual a 1.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, TaskQueryParameters.MaxPageSize)
            .WithMessage($"O tamanho da página deve estar entre 1 e {TaskQueryParameters.MaxPageSize}.");
        RuleFor(x => x.Search).MaximumLength(100).WithMessage("A pesquisa deve ter no máximo 100 caracteres.");
        RuleFor(x => x.DueTo)
            .Must((query, dueTo) => query.DueFrom is null || dueTo is null || query.DueFrom <= dueTo)
            .WithMessage("A data final do prazo deve ser maior ou igual à inicial.");
    }
}
