using System;
using SecureGate.Domain.Entities;
using Xunit;

namespace SecureGate.Tests.Domain;

public class UserTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesUserWithGeneratedIdAndUtcTimestamp()
    {
        var before = DateTime.UtcNow;

        var user = new User("Ana Souza", "ana@example.com", "hashed-password");

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("Ana Souza", user.Name);
        Assert.Equal("ana@example.com", user.Email);
        Assert.Equal("hashed-password", user.PasswordHash);
        Assert.InRange(user.CreatedAt, before, DateTime.UtcNow);
        Assert.Equal(DateTimeKind.Utc, user.CreatedAt.Kind);
    }

    [Fact]
    public void Constructor_TrimsNameAndEmail()
    {
        var user = new User("  Ana Souza  ", "  ana@example.com  ", "hashed-password");

        Assert.Equal("Ana Souza", user.Name);
        Assert.Equal("ana@example.com", user.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string invalidName)
    {
        Assert.Throws<ArgumentException>(() => new User(invalidName, "ana@example.com", "hashed-password"));
    }

    [Fact]
    public void Constructor_WithNameLongerThan100Characters_ThrowsArgumentException()
    {
        var tooLong = new string('a', 101);

        Assert.Throws<ArgumentException>(() => new User(tooLong, "ana@example.com", "hashed-password"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    [InlineData("@missing-local.com")]
    public void Constructor_WithInvalidEmail_ThrowsArgumentException(string invalidEmail)
    {
        Assert.Throws<ArgumentException>(() => new User("Ana Souza", invalidEmail, "hashed-password"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingPasswordHash_ThrowsArgumentException(string invalidPasswordHash)
    {
        Assert.Throws<ArgumentException>(() => new User("Ana Souza", "ana@example.com", invalidPasswordHash));
    }
}
