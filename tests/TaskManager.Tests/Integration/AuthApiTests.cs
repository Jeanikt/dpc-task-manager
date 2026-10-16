using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Auth;
using TaskManager.Domain.Enums;

namespace TaskManager.Tests.Integration;

public class AuthApiTests(TaskManagerApiFactory factory) : IClassFixture<TaskManagerApiFactory>
{
    private static readonly System.Text.Json.JsonSerializerOptions Json = TaskManagerApiFactory.Json;

    [Fact]
    public async Task Register_then_login_returns_token_for_regular_user()
    {
        var client = factory.CreateClient();
        var email = $"novo-{Guid.NewGuid():N}@teste.com";

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Maria", email, "Senha@123"), Json);
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email.ToUpperInvariant(), "Senha@123"), Json);
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>(Json);
        auth!.AccessToken.Should().NotBeNullOrWhiteSpace();
        auth.User.Email.Should().Be(email);
        auth.User.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public async Task Register_with_existing_email_returns_409()
    {
        var client = factory.CreateClient();
        var request = new RegisterRequest("João", $"dup-{Guid.NewGuid():N}@teste.com", "Senha@123");

        (await client.PostAsJsonAsync("/api/auth/register", request, Json)).EnsureSuccessStatusCode();
        var duplicate = await client.PostAsJsonAsync("/api/auth/register", request, Json);

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_with_weak_password_returns_validation_errors()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("", "email-invalido", "123"), Json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        problem!.Errors.Keys.Should().BeEquivalentTo("name", "email", "password");
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var client = factory.CreateClient();
        var email = $"login-{Guid.NewGuid():N}@teste.com";
        (await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Ana", email, "Senha@123"), Json)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "SenhaErrada1"), Json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_returns_current_user()
    {
        var email = $"me-{Guid.NewGuid():N}@teste.com";
        var client = await factory.CreateAuthenticatedClientAsync(email);

        var me = await client.GetFromJsonAsync<UserResponse>("/api/auth/me", Json);

        me!.Email.Should().Be(email);
    }

    [Fact]
    public async Task Swagger_document_is_available()
    {
        var response = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
