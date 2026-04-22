using Mediconnet_Backend.Infrastructure.Caching;

namespace Mediconnet_Backend.Tests.Infrastructure;

public class CacheKeysTests
{
    // ==================== MedecinById ====================

    [Theory]
    [InlineData(1, "medecins:1")]
    [InlineData(42, "medecins:42")]
    [InlineData(999, "medecins:999")]
    public void MedecinById_ReturnsCorrectKey(int id, string expected)
    {
        CacheKeys.MedecinById(id).Should().Be(expected);
    }

    // ==================== MedecinsByService ====================

    [Theory]
    [InlineData(1, "medecins:service:1")]
    [InlineData(5, "medecins:service:5")]
    public void MedecinsByService_ReturnsCorrectKey(int serviceId, string expected)
    {
        CacheKeys.MedecinsByService(serviceId).Should().Be(expected);
    }

    // ==================== MedecinsBySpecialite ====================

    [Theory]
    [InlineData(1, "medecins:specialite:1")]
    [InlineData(10, "medecins:specialite:10")]
    public void MedecinsBySpecialite_ReturnsCorrectKey(int specialiteId, string expected)
    {
        CacheKeys.MedecinsBySpecialite(specialiteId).Should().Be(expected);
    }

    // ==================== PatientById ====================

    [Theory]
    [InlineData(1, "patients:1")]
    [InlineData(100, "patients:100")]
    public void PatientById_ReturnsCorrectKey(int id, string expected)
    {
        CacheKeys.PatientById(id).Should().Be(expected);
    }

    // ==================== PatientByUserId ====================

    [Theory]
    [InlineData(1, "patients:user:1")]
    [InlineData(50, "patients:user:50")]
    public void PatientByUserId_ReturnsCorrectKey(int userId, string expected)
    {
        CacheKeys.PatientByUserId(userId).Should().Be(expected);
    }

    // ==================== MedecinPlanning ====================

    [Fact]
    public void MedecinPlanning_ReturnsCorrectKey()
    {
        var date = new DateTime(2026, 4, 22);
        CacheKeys.MedecinPlanning(5, date).Should().Be("planning:5:2026-04-22");
    }

    [Fact]
    public void MedecinPlanning_FormatsDateCorrectly()
    {
        var date = new DateTime(2026, 1, 5);
        CacheKeys.MedecinPlanning(1, date).Should().Be("planning:1:2026-01-05");
    }

    // ==================== CreneauxDisponibles ====================

    [Fact]
    public void CreneauxDisponibles_ReturnsCorrectKey()
    {
        var date = new DateTime(2026, 12, 31);
        CacheKeys.CreneauxDisponibles(10, date).Should().Be("creneaux:10:2026-12-31");
    }

    // ==================== DashboardStatsByService ====================

    [Theory]
    [InlineData(1, "dashboard:stats:service:1")]
    [InlineData(7, "dashboard:stats:service:7")]
    public void DashboardStatsByService_ReturnsCorrectKey(int serviceId, string expected)
    {
        CacheKeys.DashboardStatsByService(serviceId).Should().Be(expected);
    }

    // ==================== Constants ====================

    [Fact]
    public void Constants_HaveExpectedValues()
    {
        CacheKeys.AllServices.Should().Be("ref:services:all");
        CacheKeys.AllSpecialites.Should().Be("ref:specialites:all");
        CacheKeys.AllAssurances.Should().Be("ref:assurances:all");
        CacheKeys.AllMedecins.Should().Be("medecins:all");
        CacheKeys.DashboardStats.Should().Be("dashboard:stats");
    }

    // ==================== Expiration ====================

    [Fact]
    public void Expiration_HasReasonableValues()
    {
        CacheKeys.Expiration.Reference.Should().Be(TimeSpan.FromHours(24));
        CacheKeys.Expiration.Medecins.Should().Be(TimeSpan.FromHours(1));
        CacheKeys.Expiration.Planning.Should().Be(TimeSpan.FromMinutes(5));
        CacheKeys.Expiration.Dashboard.Should().Be(TimeSpan.FromMinutes(10));
        CacheKeys.Expiration.Short.Should().Be(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void Expiration_ReferenceIsLongerThanMedecins()
    {
        CacheKeys.Expiration.Reference.Should().BeGreaterThan(CacheKeys.Expiration.Medecins);
    }

    [Fact]
    public void Expiration_ShortIsShortest()
    {
        CacheKeys.Expiration.Short.Should().BeLessThanOrEqualTo(CacheKeys.Expiration.Planning);
        CacheKeys.Expiration.Short.Should().BeLessThanOrEqualTo(CacheKeys.Expiration.Dashboard);
    }
}
