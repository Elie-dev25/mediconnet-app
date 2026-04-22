using Mediconnet_Backend.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace Mediconnet_Backend.Tests.Infrastructure;

public class MemoryCacheServiceTests
{
    private static MemoryCacheService CreateSut()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new MemoryCacheService(cache);
    }

    [Fact]
    public async Task GetAsync_MissingKey_ReturnsDefault()
    {
        var sut = CreateSut();

        var value = await sut.GetAsync<string>("missing");

        value.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ThenGet_ReturnsStoredValue()
    {
        var sut = CreateSut();

        await sut.SetAsync("key1", "hello");
        var value = await sut.GetAsync<string>("key1");

        value.Should().Be("hello");
    }

    [Fact]
    public async Task SetAsync_WithExpiration_Works()
    {
        var sut = CreateSut();

        await sut.SetAsync("k", 42, TimeSpan.FromMinutes(1));

        (await sut.GetAsync<int>("k")).Should().Be(42);
    }

    [Fact]
    public async Task RemoveAsync_ExistingKey_RemovesIt()
    {
        var sut = CreateSut();
        await sut.SetAsync("k", "v");

        await sut.RemoveAsync("k");

        (await sut.GetAsync<string>("k")).Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_MissingKey_DoesNotThrow()
    {
        var sut = CreateSut();

        var act = async () => await sut.RemoveAsync("unknown");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExistsAsync_KeyPresent_ReturnsTrue()
    {
        var sut = CreateSut();
        await sut.SetAsync("k", "v");

        (await sut.ExistsAsync("k")).Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_KeyMissing_ReturnsFalse()
    {
        var sut = CreateSut();

        (await sut.ExistsAsync("nope")).Should().BeFalse();
    }

    [Fact]
    public async Task RemoveByPatternAsync_RemovesMatchingKeys()
    {
        var sut = CreateSut();
        await sut.SetAsync("user:1:profile", "a");
        await sut.SetAsync("user:2:profile", "b");
        await sut.SetAsync("role:admin", "c");

        await sut.RemoveByPatternAsync("user:*");

        (await sut.ExistsAsync("user:1:profile")).Should().BeFalse();
        (await sut.ExistsAsync("user:2:profile")).Should().BeFalse();
        (await sut.ExistsAsync("role:admin")).Should().BeTrue();
    }

    [Fact]
    public async Task RemoveByPatternAsync_NoMatches_RemovesNothing()
    {
        var sut = CreateSut();
        await sut.SetAsync("a", 1);
        await sut.SetAsync("b", 2);

        await sut.RemoveByPatternAsync("z:*");

        (await sut.ExistsAsync("a")).Should().BeTrue();
        (await sut.ExistsAsync("b")).Should().BeTrue();
    }

    [Fact]
    public async Task GetOrCreateAsync_Missing_CallsFactoryAndStores()
    {
        var sut = CreateSut();
        var calls = 0;

        var value = await sut.GetOrCreateAsync("k", () =>
        {
            calls++;
            return Task.FromResult("computed");
        });

        value.Should().Be("computed");
        calls.Should().Be(1);
        (await sut.GetAsync<string>("k")).Should().Be("computed");
    }

    [Fact]
    public async Task GetOrCreateAsync_Existing_DoesNotCallFactory()
    {
        var sut = CreateSut();
        await sut.SetAsync("k", "cached");
        var calls = 0;

        var value = await sut.GetOrCreateAsync("k", () =>
        {
            calls++;
            return Task.FromResult("factory");
        });

        value.Should().Be("cached");
        calls.Should().Be(0);
    }

    [Fact]
    public async Task GetOrCreateAsync_WithExpiration_StoresValue()
    {
        var sut = CreateSut();

        var v = await sut.GetOrCreateAsync("k", () => Task.FromResult(123), TimeSpan.FromMinutes(2));

        v.Should().Be(123);
    }
}
