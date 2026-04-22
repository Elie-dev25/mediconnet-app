using Mediconnet_Backend.Core.Constants;

namespace Mediconnet_Backend.Tests.Core;

public class BusinessRulesTests
{
    // ==================== BusinessRules Constants ====================

    [Fact]
    public void PaymentValidityDays_HasReasonableValue()
    {
        BusinessRules.PaymentValidityDays.Should().BeGreaterThan(0);
        BusinessRules.PaymentValidityDays.Should().BeLessThanOrEqualTo(30);
    }

    [Fact]
    public void DefaultConsultationDurationMinutes_HasReasonableValue()
    {
        BusinessRules.DefaultConsultationDurationMinutes.Should().BeGreaterThan(0);
        BusinessRules.DefaultConsultationDurationMinutes.Should().BeLessThanOrEqualTo(120);
    }

    [Fact]
    public void DefaultConsultationPrice_IsPositive()
    {
        BusinessRules.DefaultConsultationPrice.Should().BeGreaterThan(0);
    }

    // ==================== FactureTypes Constants ====================

    [Fact]
    public void FactureTypes_HasExpectedValues()
    {
        FactureTypes.Consultation.Should().Be("consultation");
        FactureTypes.Hospitalisation.Should().Be("hospitalisation");
        FactureTypes.Examen.Should().Be("examen");
        FactureTypes.Medicament.Should().Be("medicament");
        FactureTypes.Pharmacie.Should().Be("pharmacie");
    }

    // ==================== PrestationTypes Constants ====================

    [Fact]
    public void PrestationTypes_HasExpectedValues()
    {
        PrestationTypes.Consultation.Should().Be("consultation");
        PrestationTypes.Hospitalisation.Should().Be("hospitalisation");
        PrestationTypes.Examen.Should().Be("examen");
        PrestationTypes.Pharmacie.Should().Be("pharmacie");
    }

    // ==================== FactureStatuts Constants ====================

    [Fact]
    public void FactureStatuts_HasExpectedValues()
    {
        FactureStatuts.EnAttente.Should().Be("en_attente");
        FactureStatuts.Payee.Should().Be("payee");
        FactureStatuts.Annulee.Should().Be("annulee");
        FactureStatuts.Partielle.Should().Be("partielle");
    }

    // ==================== RendezVousTypes Constants ====================

    [Fact]
    public void RendezVousTypes_HasExpectedValues()
    {
        RendezVousTypes.Consultation.Should().Be("consultation");
        RendezVousTypes.Suivi.Should().Be("suivi");
        RendezVousTypes.Urgence.Should().Be("urgence");
        RendezVousTypes.Controle.Should().Be("controle");
    }

    // ==================== ConsultationTypes Constants ====================

    [Fact]
    public void ConsultationTypes_HasExpectedValues()
    {
        ConsultationTypes.Normale.Should().Be("normale");
        ConsultationTypes.Urgence.Should().Be("urgence");
        ConsultationTypes.Suivi.Should().Be("suivi");
        ConsultationTypes.Premiere.Should().Be("premiere");
    }

    // ==================== SoinTypes Constants ====================

    [Fact]
    public void SoinTypes_HasExpectedValues()
    {
        SoinTypes.SoinsInfirmiers.Should().Be("soins_infirmiers");
        SoinTypes.Surveillance.Should().Be("surveillance");
        SoinTypes.Reeducation.Should().Be("reeducation");
        SoinTypes.Nutrition.Should().Be("nutrition");
        SoinTypes.Autre.Should().Be("autre");
    }

    // ==================== SpecialiteIds Constants ====================

    [Fact]
    public void SpecialiteIds_GynecologieObstetrique_HasExpectedValue()
    {
        SpecialiteIds.GynecologieObstetrique.Should().Be(23);
    }

    // ==================== LitStatuts Constants ====================

    [Fact]
    public void LitStatuts_HasExpectedValues()
    {
        LitStatuts.Libre.Should().Be("libre");
        LitStatuts.Occupe.Should().Be("occupe");
        LitStatuts.Maintenance.Should().Be("maintenance");
        LitStatuts.Reserve.Should().Be("reserve");
    }
}
