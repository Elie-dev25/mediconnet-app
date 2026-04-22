using Mediconnet_Backend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mediconnet_Backend.Tests.Services;

public class JwtTokenServiceTests
{
    private const string TestSecret = "ThisIsATestSecretKeyWithAtLeast32Characters!!";

    private static JwtTokenService CreateService(Dictionary<string, string?>? extraConfig = null)
    {
        var config = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = TestSecret,
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience",
            ["Jwt:ExpirationMinutes"] = "60"
        };
        if (extraConfig != null)
        {
            foreach (var kv in extraConfig) config[kv.Key] = kv.Value;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();

        return new JwtTokenService(configuration, NullLogger<JwtTokenService>.Instance);
    }

    [Fact]
    public async Task GenerateTokenAsync_WithValidConfig_ReturnsNonEmptyToken()
    {
        var service = CreateService();

        var token = await service.GenerateTokenAsync(42, "medecin");

        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3); // Header.Payload.Signature
    }

    [Fact]
    public async Task GenerateTokenAsync_MissingSecret_Throws()
    {
        var service = CreateService(new() { ["Jwt:Secret"] = null });

        var act = async () => await service.GenerateTokenAsync(1, "medecin");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*JWT secret is not configured*");
    }

    [Fact]
    public async Task GenerateTokenAsync_EmptySecret_Throws()
    {
        var service = CreateService(new() { ["Jwt:Secret"] = "   " });

        var act = async () => await service.GenerateTokenAsync(1, "medecin");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetUserIdFromToken_ValidToken_ReturnsUserId()
    {
        var service = CreateService();
        var token = await service.GenerateTokenAsync(123, "patient");

        var userId = service.GetUserIdFromToken(token);

        userId.Should().Be("123");
    }

    [Fact]
    public void GetUserIdFromToken_GarbageToken_ReturnsNull()
    {
        var service = CreateService();

        service.GetUserIdFromToken("not.a.valid.jwt").Should().BeNull();
    }

    [Fact]
    public void GetUserIdFromToken_EmptyToken_ReturnsNull()
    {
        var service = CreateService();

        service.GetUserIdFromToken("").Should().BeNull();
    }

    [Fact]
    public async Task GetUserIdFromToken_TokenSignedWithDifferentSecret_ReturnsNull()
    {
        var serviceA = CreateService();
        var tokenA = await serviceA.GenerateTokenAsync(7, "admin");

        var serviceB = CreateService(new()
        {
            ["Jwt:Secret"] = "CompletelyDifferentSecretKeyOfSufficientLength!!"
        });

        serviceB.GetUserIdFromToken(tokenA).Should().BeNull();
    }

    [Fact]
    public async Task GenerateTokenAsync_DifferentUsers_ProduceDifferentTokens()
    {
        var service = CreateService();

        var token1 = await service.GenerateTokenAsync(1, "medecin");
        var token2 = await service.GenerateTokenAsync(2, "medecin");

        token1.Should().NotBe(token2);
    }
}
