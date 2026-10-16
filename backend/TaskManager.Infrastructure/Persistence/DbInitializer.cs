using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskManager.Application.Abstractions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Infrastructure.Persistence;

public class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; set; }
    public string AdminEmail { get; set; } = "admin@dpc.local";
    public string AdminPassword { get; set; } = string.Empty;
    public string UserEmail { get; set; } = "usuario@dpc.local";
    public string UserPassword { get; set; } = string.Empty;
}

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<AppDbContext>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbInitializer));

        if (db.Database.IsRelational())
            await db.Database.MigrateAsync(ct);

        var seed = provider.GetRequiredService<IOptions<SeedOptions>>().Value;
        if (!seed.Enabled || await db.Users.AnyAsync(ct))
            return;

        var hasher = provider.GetRequiredService<IPasswordHasher>();
        var clock = provider.GetRequiredService<IBusinessClock>();
        var now = clock.UtcNow;
        var today = clock.Today;

        var admin = User.Create("Administrador", seed.AdminEmail, hasher.Hash(seed.AdminPassword), UserRole.Admin, now);
        var user = User.Create("Usuário Demo", seed.UserEmail, hasher.Hash(seed.UserPassword), UserRole.User, now);
        db.Users.AddRange(admin, user);

        TaskItem New(User owner, string title, string? description, TaskPriority priority, int? dueInDays, TaskItemStatus? status = null)
        {
            var task = TaskItem.Create(owner.Id, title, description, priority, dueInDays is { } d ? today.AddDays(d) : null, now);
            if (status is { } s)
                task.ChangeStatus(s, now);
            return task;
        }

        db.Tasks.AddRange(
            New(user, "Fechamento contábil de setembro", "Conciliar contas e conferir lançamentos do mês.", TaskPriority.High, 3, TaskItemStatus.InProgress),
            New(user, "Enviar DCTFWeb do cliente Alfa", "Conferir débitos e transmitir a declaração.", TaskPriority.High, -2),
            New(user, "Atualizar cadastro de fornecedores", null, TaskPriority.Low, 15),
            New(user, "Revisar folha de pagamento", "Validar horas extras e descontos antes do envio.", TaskPriority.Medium, 5),
            New(user, "Conciliação bancária - Banco do Brasil", null, TaskPriority.Medium, -5, TaskItemStatus.Completed),
            New(user, "Reunião de alinhamento com cliente Beta", "Pauta: regime tributário 2027.", TaskPriority.Low, 7, TaskItemStatus.Cancelled),
            New(admin, "Revisar permissões de acesso", "Auditoria trimestral de usuários do sistema.", TaskPriority.Medium, 10),
            New(admin, "Backup mensal dos arquivos fiscais", null, TaskPriority.High, 1, TaskItemStatus.InProgress));

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Banco populado com dados de demonstração.");
    }
}
