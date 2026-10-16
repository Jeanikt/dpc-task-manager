using Microsoft.AspNetCore.Mvc;

namespace TaskManager.Api.Infrastructure;

public static class InvalidModelStateResponse
{
    private const string InvalidValueMessage = "Valor inválido ou em formato incorreto.";

    public static IActionResult Create(ActionContext context)
    {
        var errors = context.ModelState
            .Where(e => e.Value is { Errors.Count: > 0 })
            .Select(e => NormalizeKey(e.Key))
            .Where(key => key.Length > 0 && key != "request")
            .Distinct()
            .ToDictionary(key => key, _ => new[] { InvalidValueMessage });

        if (errors.Count == 0)
            errors["body"] = ["O corpo da requisição é obrigatório ou está malformado."];

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Dados inválidos",
            Detail = "Um ou mais campos não passaram na validação."
        };
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        return new BadRequestObjectResult(problem);
    }

    private static string NormalizeKey(string key)
    {
        var name = key.StartsWith("$.") ? key[2..] : key.TrimStart('$');
        return name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
    }
}
