using CampusConnect.Application.Common.Security;
using System.Security.Cryptography;
using System.Text;

namespace CampusConnect.Application.Tests.Common.Security;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_ShouldUseUniqueSaltForEachPassword()
    {
        var firstHash = PasswordHasher.Hash("secure-password");
        var secondHash = PasswordHasher.Hash("secure-password");

        Assert.NotEqual(firstHash, secondHash);
        Assert.True(PasswordHasher.Verify("secure-password", firstHash));
        Assert.True(PasswordHasher.Verify("secure-password", secondHash));
    }

    [Fact]
    public void Verify_ShouldRejectWrongPassword()
    {
        var hash = PasswordHasher.Hash("secure-password");

        Assert.False(PasswordHasher.Verify("wrong-password", hash));
    }

    [Fact]
    public void Verify_ShouldAcceptLegacySha256Hashes()
    {
        var legacyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("legacy-password")));

        Assert.True(PasswordHasher.Verify("legacy-password", legacyHash));
        Assert.False(PasswordHasher.Verify("other-password", legacyHash));
    }
}