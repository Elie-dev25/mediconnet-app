using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Tests.Services;

public class MedecinHelperServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    // ==================== IsStatutTermine ====================

    [Theory]
    [InlineData("termine", true)]
    [InlineData("terminee", true)]
    [InlineData("TERMINE", true)]
    [InlineData("Terminee", true)]
    public void IsStatutTermine_FinishedStatus_ReturnsTrue(string statut, bool expected)
    {
        var sut = new MedecinHelperService(CreateContext());

        sut.IsStatutTermine(statut).Should().Be(expected);
    }

    [Theory]
    [InlineData("en_cours")]
    [InlineData("annule")]
    [InlineData("")]
    [InlineData("fini")]
    public void IsStatutTermine_OtherStatus_ReturnsFalse(string statut)
    {
        var sut = new MedecinHelperService(CreateContext());

        sut.IsStatutTermine(statut).Should().BeFalse();
    }

    [Fact]
    public void IsStatutTermine_Null_ReturnsFalse()
    {
        var sut = new MedecinHelperService(CreateContext());

        sut.IsStatutTermine(null).Should().BeFalse();
    }

    // ==================== GetMedecinSpecialiteIdAsync ====================

    [Fact]
    public async Task GetMedecinSpecialiteIdAsync_KnownMedecin_ReturnsSpecialiteId()
    {
        using var ctx = CreateContext();
        ctx.Medecins.Add(new Medecin { IdUser = 10, IdSpecialite = 42 });
        await ctx.SaveChangesAsync();

        var sut = new MedecinHelperService(ctx);

        var result = await sut.GetMedecinSpecialiteIdAsync(10);

        result.Should().Be(42);
    }

    [Fact]
    public async Task GetMedecinSpecialiteIdAsync_UnknownMedecin_ReturnsNull()
    {
        using var ctx = CreateContext();
        var sut = new MedecinHelperService(ctx);

        var result = await sut.GetMedecinSpecialiteIdAsync(999);

        result.Should().BeNull();
    }

    // ==================== IsPremiereConsultationAsync ====================

    [Fact]
    public async Task IsPremiereConsultationAsync_DossierCloture_ReturnsTrue()
    {
        using var ctx = CreateContext();
        ctx.Patients.Add(new Patient { IdUser = 1, DossierCloture = true });
        // Meme si une consultation terminee existe
        ctx.Consultations.Add(new Consultation { IdPatient = 1, IdMedecin = 5, Statut = "termine" });
        await ctx.SaveChangesAsync();

        var sut = new MedecinHelperService(ctx);

        (await sut.IsPremiereConsultationAsync(1, 5)).Should().BeTrue();
    }

    [Fact]
    public async Task IsPremiereConsultationAsync_NoPriorConsultation_ReturnsTrue()
    {
        using var ctx = CreateContext();
        ctx.Patients.Add(new Patient { IdUser = 2, DossierCloture = false });
        await ctx.SaveChangesAsync();

        var sut = new MedecinHelperService(ctx);

        (await sut.IsPremiereConsultationAsync(2, 5)).Should().BeTrue();
    }

    [Fact]
    public async Task IsPremiereConsultationAsync_HasFinishedConsultation_ReturnsFalse()
    {
        using var ctx = CreateContext();
        ctx.Patients.Add(new Patient { IdUser = 3, DossierCloture = false });
        ctx.Consultations.Add(new Consultation { IdPatient = 3, IdMedecin = 5, Statut = "terminee" });
        await ctx.SaveChangesAsync();

        var sut = new MedecinHelperService(ctx);

        (await sut.IsPremiereConsultationAsync(3, 5)).Should().BeFalse();
    }

    [Fact]
    public async Task IsPremiereConsultationAsync_HasPendingConsultation_ReturnsTrue()
    {
        using var ctx = CreateContext();
        ctx.Patients.Add(new Patient { IdUser = 4, DossierCloture = false });
        ctx.Consultations.Add(new Consultation { IdPatient = 4, IdMedecin = 5, Statut = "en_cours" });
        await ctx.SaveChangesAsync();

        var sut = new MedecinHelperService(ctx);

        (await sut.IsPremiereConsultationAsync(4, 5)).Should().BeTrue();
    }
}
