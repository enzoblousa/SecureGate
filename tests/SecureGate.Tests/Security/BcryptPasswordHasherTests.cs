using SecureGate.Api.Infrastructure.Security;
using Xunit;

namespace SecureGate.Tests.Security;

public class BcryptPasswordHasherTests
{
    [Fact]
    public void Hash_ReturnsValueDifferentFromThePlainPassword()
    {
        var hasher = new BcryptPasswordHasher();

        var hash = hasher.Hash("senha1234");

        Assert.NotEqual("senha1234", hash);
    }

    [Fact]
    public void Hash_ProducesAHashThatBCryptCanVerifyAgainstTheOriginalPassword()
    {
        var hasher = new BcryptPasswordHasher();

        var hash = hasher.Hash("senha1234");

        Assert.True(BCrypt.Net.BCrypt.Verify("senha1234", hash));
    }

    [Fact]
    public void Hash_ProducesDifferentHashesForTheSamePasswordOnDifferentCalls()
    {
        var hasher = new BcryptPasswordHasher();

        var hash1 = hasher.Hash("senha1234");
        var hash2 = hasher.Hash("senha1234");

        Assert.NotEqual(hash1, hash2);
    }
}
