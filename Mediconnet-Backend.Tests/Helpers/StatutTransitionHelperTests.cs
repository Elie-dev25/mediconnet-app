using Mediconnet_Backend.Helpers;

namespace Mediconnet_Backend.Tests.Helpers;

public class StatutTransitionHelperTests
{
    // ==================== Consultation Transitions ====================

    [Theory]
    [InlineData("planifiee", "en_cours", true)]
    [InlineData("planifiee", "annulee", true)]
    [InlineData("planifiee", "terminee", false)]
    [InlineData("en_cours", "en_pause", true)]
    [InlineData("en_cours", "terminee", true)]
    [InlineData("en_cours", "annulee", true)]
    [InlineData("en_cours", "planifiee", false)]
    [InlineData("en_pause", "en_cours", true)]
    [InlineData("en_pause", "terminee", true)]
    [InlineData("terminee", "en_cours", false)]
    [InlineData("terminee", "annulee", false)]
    [InlineData("annulee", "planifiee", false)]
    public void IsValidConsultationTransition_ReturnsExpectedResult(string current, string next, bool expected)
    {
        StatutTransitionHelper.IsValidConsultationTransition(current, next).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "planifiee", true)]
    [InlineData(null, "en_cours", true)]
    [InlineData("", "planifiee", true)]
    [InlineData(null, "terminee", false)]
    public void IsValidConsultationTransition_WithNullOrEmpty_ReturnsExpectedResult(string? current, string next, bool expected)
    {
        StatutTransitionHelper.IsValidConsultationTransition(current, next).Should().Be(expected);
    }

    // ==================== Hospitalisation Transitions ====================

    [Theory]
    [InlineData("en_attente", "en_attente_lit", true)]
    [InlineData("en_attente", "admis", true)]
    [InlineData("en_attente", "annulee", true)]
    [InlineData("en_attente_lit", "admis", true)]
    [InlineData("admis", "en_cours", true)]
    [InlineData("admis", "sortie", true)]
    [InlineData("en_cours", "sortie", true)]
    [InlineData("sortie", "en_cours", false)]
    [InlineData("sortie", "admis", false)]
    public void IsValidHospitalisationTransition_ReturnsExpectedResult(string current, string next, bool expected)
    {
        StatutTransitionHelper.IsValidHospitalisationTransition(current, next).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "en_attente", true)]
    [InlineData(null, "admis", false)]
    public void IsValidHospitalisationTransition_WithNull_ReturnsExpectedResult(string? current, string next, bool expected)
    {
        StatutTransitionHelper.IsValidHospitalisationTransition(current, next).Should().Be(expected);
    }

    // ==================== Reservation Bloc Transitions ====================

    [Theory]
    [InlineData("planifiee", "confirmee", true)]
    [InlineData("planifiee", "annulee", true)]
    [InlineData("confirmee", "en_cours", true)]
    [InlineData("confirmee", "annulee", true)]
    [InlineData("en_cours", "terminee", true)]
    [InlineData("terminee", "en_cours", false)]
    public void IsValidReservationBlocTransition_ReturnsExpectedResult(string current, string next, bool expected)
    {
        StatutTransitionHelper.IsValidReservationBlocTransition(current, next).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "planifiee", true)]
    [InlineData(null, "confirmee", false)]
    public void IsValidReservationBlocTransition_WithNull_ReturnsExpectedResult(string? current, string next, bool expected)
    {
        StatutTransitionHelper.IsValidReservationBlocTransition(current, next).Should().Be(expected);
    }

    // ==================== Coordination Transitions ====================

    [Theory]
    [InlineData("en_attente", "acceptee", true)]
    [InlineData("en_attente", "refusee", true)]
    [InlineData("en_attente", "contre_proposition", true)]
    [InlineData("contre_proposition", "acceptee", true)]
    [InlineData("contre_proposition", "en_attente", true)]
    [InlineData("acceptee", "programmee", true)]
    [InlineData("acceptee", "annulee", true)]
    [InlineData("refusee", "acceptee", false)]
    [InlineData("programmee", "terminee", true)]
    [InlineData("terminee", "programmee", false)]
    public void IsValidCoordinationTransition_ReturnsExpectedResult(string current, string next, bool expected)
    {
        StatutTransitionHelper.IsValidCoordinationTransition(current, next).Should().Be(expected);
    }

    // ==================== GetValidNextStatuts ====================

    [Fact]
    public void GetValidNextStatuts_ForConsultation_ReturnsCorrectStatuts()
    {
        var result = StatutTransitionHelper.GetValidNextStatuts("consultation", "planifiee");
        result.Should().Contain("en_cours");
        result.Should().Contain("annulee");
        result.Should().HaveCount(2);
    }

    [Fact]
    public void GetValidNextStatuts_ForTerminee_ReturnsEmpty()
    {
        var result = StatutTransitionHelper.GetValidNextStatuts("consultation", "terminee");
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetValidNextStatuts_ForUnknownStatut_ReturnsEmpty()
    {
        var result = StatutTransitionHelper.GetValidNextStatuts("consultation", "unknown");
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetValidNextStatuts_ForNullStatut_ReturnsEmpty()
    {
        var result = StatutTransitionHelper.GetValidNextStatuts("consultation", null);
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetValidNextStatuts_ForUnknownEntityType_ThrowsArgumentException()
    {
        var act = () => StatutTransitionHelper.GetValidNextStatuts("unknown_type", "planifiee");
        act.Should().Throw<ArgumentException>().WithParameterName("entityType");
    }

    // ==================== IsFinalStatut ====================

    [Theory]
    [InlineData("consultation", "terminee", true)]
    [InlineData("consultation", "annulee", true)]
    [InlineData("consultation", "en_cours", false)]
    [InlineData("hospitalisation", "sortie", true)]
    [InlineData("hospitalisation", "admis", false)]
    [InlineData("reservation_bloc", "terminee", true)]
    [InlineData("coordination", "refusee", true)]
    [InlineData("coordination", "terminee", true)]
    public void IsFinalStatut_ReturnsExpectedResult(string entityType, string statut, bool expected)
    {
        StatutTransitionHelper.IsFinalStatut(entityType, statut).Should().Be(expected);
    }
}
