using FluentAssertions;
using TaskManager.Infrastructure.Security;

namespace TaskManager.Tests.Security;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Verify_accepts_correct_password()
    {
        var hash = _hasher.Hash("Senha@123");

        _hasher.Verify("Senha@123", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_rejects_wrong_password()
    {
        var hash = _hasher.Hash("Senha@123");

        _hasher.Verify("senha@123", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_uses_random_salt()
    {
        _hasher.Hash("Senha@123").Should().NotBe(_hasher.Hash("Senha@123"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("texto-qualquer")]
    [InlineData("abc.%%%.###")]
    public void Verify_returns_false_for_malformed_hash(string hash)
    {
        _hasher.Verify("Senha@123", hash).Should().BeFalse();
    }
}
