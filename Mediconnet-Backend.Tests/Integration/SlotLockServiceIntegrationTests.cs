using Microsoft.Extensions.Logging;
using Moq;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Services;

namespace Mediconnet_Backend.Tests.Integration;

public class SlotLockServiceIntegrationTests : IDisposable
{
    private readonly Data.ApplicationDbContext _context;
    private readonly SlotLockService _slotLockService;

    public SlotLockServiceIntegrationTests()
    {
        _context = TestDbContextFactory.Create();
        var logger = new Mock<ILogger<SlotLockService>>();
        _slotLockService = new SlotLockService(_context, logger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task AcquireLockAsync_WhenSlotFree_ReturnsSuccess()
    {
        // Arrange
        var medecinId = 1;
        var dateHeure = DateTime.UtcNow.AddHours(1);
        var duree = 30;
        var userId = 100;

        // Act
        var result = await _slotLockService.AcquireLockAsync(medecinId, dateHeure, duree, userId);

        // Assert
        result.Success.Should().BeTrue();
        result.LockToken.Should().NotBeNullOrEmpty();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task AcquireLockAsync_WhenSlotAlreadyLocked_ReturnsFalse()
    {
        // Arrange
        var medecinId = 1;
        var dateHeure = DateTime.UtcNow.AddHours(1);
        var duree = 30;
        var userId1 = 100;
        var userId2 = 200;

        // First user acquires lock
        await _slotLockService.AcquireLockAsync(medecinId, dateHeure, duree, userId1);

        // Act - Second user tries to acquire same slot
        var result = await _slotLockService.AcquireLockAsync(medecinId, dateHeure, duree, userId2);

        // Assert
        result.Success.Should().BeFalse();
        result.LockedByUserId.Should().Be(userId1);
    }

    [Fact]
    public async Task AcquireLockAsync_SameUserExtends_ReturnsSuccess()
    {
        // Arrange
        var medecinId = 1;
        var dateHeure = DateTime.UtcNow.AddHours(1);
        var duree = 30;
        var userId = 100;

        // First acquisition
        var firstResult = await _slotLockService.AcquireLockAsync(medecinId, dateHeure, duree, userId);

        // Act - Same user acquires again
        var result = await _slotLockService.AcquireLockAsync(medecinId, dateHeure, duree, userId);

        // Assert
        result.Success.Should().BeTrue();
        result.LockToken.Should().Be(firstResult.LockToken);
        result.Message.Should().Contain("prolongé");
    }

    [Fact]
    public async Task ValidateLockAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var medecinId = 1;
        var dateHeure = DateTime.UtcNow.AddHours(1);
        var duree = 30;
        var userId = 100;

        var lockResult = await _slotLockService.AcquireLockAsync(medecinId, dateHeure, duree, userId);

        // Act
        var isValid = await _slotLockService.ValidateLockAsync(lockResult.LockToken!, userId);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateLockAsync_WithInvalidToken_ReturnsFalse()
    {
        // Act
        var isValid = await _slotLockService.ValidateLockAsync("invalid-token", 100);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateLockAsync_WithNullToken_ReturnsFalse()
    {
        // Act
        var isValid = await _slotLockService.ValidateLockAsync(null!, 100);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ReleaseLockAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var medecinId = 1;
        var dateHeure = DateTime.UtcNow.AddHours(1);
        var duree = 30;
        var userId = 100;

        var lockResult = await _slotLockService.AcquireLockAsync(medecinId, dateHeure, duree, userId);

        // Act
        var released = await _slotLockService.ReleaseLockAsync(lockResult.LockToken!, userId);

        // Assert
        released.Should().BeTrue();

        // Verify lock is gone
        var isValid = await _slotLockService.ValidateLockAsync(lockResult.LockToken!, userId);
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ReleaseLockAsync_WithInvalidToken_ReturnsFalse()
    {
        // Act
        var released = await _slotLockService.ReleaseLockAsync("invalid-token", 100);

        // Assert
        released.Should().BeFalse();
    }

    [Fact]
    public async Task ExtendLockAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var medecinId = 1;
        var dateHeure = DateTime.UtcNow.AddHours(1);
        var duree = 30;
        var userId = 100;

        var lockResult = await _slotLockService.AcquireLockAsync(medecinId, dateHeure, duree, userId);
        var originalExpiry = lockResult.ExpiresAt;

        // Act
        var extended = await _slotLockService.ExtendLockAsync(lockResult.LockToken!, userId, 10);

        // Assert
        extended.Should().BeTrue();
    }

    [Fact]
    public async Task ExtendLockAsync_WithInvalidToken_ReturnsFalse()
    {
        // Act
        var extended = await _slotLockService.ExtendLockAsync("invalid-token", 100);

        // Assert
        extended.Should().BeFalse();
    }

    [Fact]
    public async Task CleanupExpiredLocksAsync_RemovesExpiredLocks()
    {
        // Arrange - Add expired lock directly
        var expiredLock = new SlotLock
        {
            IdMedecin = 1,
            DateHeure = DateTime.UtcNow.AddHours(-1),
            Duree = 30,
            IdUser = 100,
            LockToken = "expired-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10), // Already expired
            CreatedAt = DateTime.UtcNow.AddMinutes(-15)
        };
        _context.SlotLocks.Add(expiredLock);
        await _context.SaveChangesAsync();

        // Act
        var count = await _slotLockService.CleanupExpiredLocksAsync();

        // Assert
        count.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task IsSlotLockedAsync_WhenLocked_ReturnsTrue()
    {
        // Arrange
        var medecinId = 1;
        var dateHeure = DateTime.UtcNow.AddHours(1);
        var duree = 30;
        var userId = 100;

        await _slotLockService.AcquireLockAsync(medecinId, dateHeure, duree, userId);

        // Act
        var isLocked = await _slotLockService.IsSlotLockedAsync(medecinId, dateHeure, duree);

        // Assert
        isLocked.Should().BeTrue();
    }

    [Fact]
    public async Task IsSlotLockedAsync_WhenNotLocked_ReturnsFalse()
    {
        // Act
        var isLocked = await _slotLockService.IsSlotLockedAsync(1, DateTime.UtcNow.AddHours(2), 30);

        // Assert
        isLocked.Should().BeFalse();
    }

    [Fact]
    public async Task IsSlotLockedAsync_ExcludesUser_ReturnsFalse()
    {
        // Arrange
        var medecinId = 1;
        var dateHeure = DateTime.UtcNow.AddHours(1);
        var duree = 30;
        var userId = 100;

        await _slotLockService.AcquireLockAsync(medecinId, dateHeure, duree, userId);

        // Act - Check if locked excluding the same user
        var isLocked = await _slotLockService.IsSlotLockedAsync(medecinId, dateHeure, duree, excludeUserId: userId);

        // Assert
        isLocked.Should().BeFalse();
    }
}
