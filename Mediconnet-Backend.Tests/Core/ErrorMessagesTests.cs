using Mediconnet_Backend.Core.Services;

namespace Mediconnet_Backend.Tests.Core;

public class ErrorMessagesTests
{
    // ==================== ChampObligatoireNomme ====================

    [Theory]
    [InlineData("Email", "Le champ 'Email' est obligatoire")]
    [InlineData("Nom", "Le champ 'Nom' est obligatoire")]
    [InlineData("DateNaissance", "Le champ 'DateNaissance' est obligatoire")]
    public void ChampObligatoireNomme_ReturnsFormattedMessage(string fieldName, string expected)
    {
        ErrorMessages.ChampObligatoireNomme(fieldName).Should().Be(expected);
    }

    // ==================== FormatInvalideNomme ====================

    [Theory]
    [InlineData("Email", "Le format du champ 'Email' est invalide")]
    [InlineData("Telephone", "Le format du champ 'Telephone' est invalide")]
    public void FormatInvalideNomme_ReturnsFormattedMessage(string fieldName, string expected)
    {
        ErrorMessages.FormatInvalideNomme(fieldName).Should().Be(expected);
    }

    // ==================== LongueurMaxDepassee ====================

    [Theory]
    [InlineData("Nom", 50, "Le champ 'Nom' ne doit pas dépasser 50 caractères")]
    [InlineData("Description", 500, "Le champ 'Description' ne doit pas dépasser 500 caractères")]
    public void LongueurMaxDepassee_ReturnsFormattedMessage(string fieldName, int max, string expected)
    {
        ErrorMessages.LongueurMaxDepassee(fieldName, max).Should().Be(expected);
    }

    // ==================== ValeurHorsLimites ====================

    [Theory]
    [InlineData("Age", 0, 150, "La valeur de 'Age' doit être entre 0 et 150")]
    [InlineData("Quantite", 1, 100, "La valeur de 'Quantite' doit être entre 1 et 100")]
    public void ValeurHorsLimites_ReturnsFormattedMessage(string fieldName, int min, int max, string expected)
    {
        ErrorMessages.ValeurHorsLimites(fieldName, min, max).Should().Be(expected);
    }

    // ==================== FichierTropVolumineuxAvecLimite ====================

    [Theory]
    [InlineData(10, "Le fichier ne doit pas dépasser 10 Mo")]
    [InlineData(50, "Le fichier ne doit pas dépasser 50 Mo")]
    public void FichierTropVolumineuxAvecLimite_ReturnsFormattedMessage(long maxMo, string expected)
    {
        ErrorMessages.FichierTropVolumineuxAvecLimite(maxMo).Should().Be(expected);
    }

    // ==================== TypesAutorises ====================

    [Fact]
    public void TypesAutorises_ReturnsFormattedMessage()
    {
        ErrorMessages.TypesAutorises("pdf, jpg, png").Should().Be("Types de fichiers autorisés: pdf, jpg, png");
    }

    // ==================== RessourceNonTrouvee ====================

    [Fact]
    public void RessourceNonTrouvee_WithId_ReturnsFormattedMessage()
    {
        ErrorMessages.RessourceNonTrouvee("Patient", 123).Should().Be("Patient avec l'identifiant '123' non trouvé(e)");
    }

    [Fact]
    public void RessourceNonTrouvee_WithoutId_ReturnsSimpleMessage()
    {
        ErrorMessages.RessourceNonTrouvee("Patient").Should().Be("Patient non trouvé(e)");
    }

    [Fact]
    public void RessourceNonTrouvee_WithNullId_ReturnsSimpleMessage()
    {
        ErrorMessages.RessourceNonTrouvee("Consultation", null).Should().Be("Consultation non trouvé(e)");
    }

    // ==================== ActionNonAutorisee ====================

    [Fact]
    public void ActionNonAutorisee_WithReason_ReturnsFormattedMessage()
    {
        ErrorMessages.ActionNonAutorisee("modifier ce dossier", "vous n'êtes pas le médecin traitant")
            .Should().Be("Impossible de modifier ce dossier: vous n'êtes pas le médecin traitant");
    }

    [Fact]
    public void ActionNonAutorisee_WithoutReason_ReturnsSimpleMessage()
    {
        ErrorMessages.ActionNonAutorisee("supprimer ce patient")
            .Should().Be("Vous n'êtes pas autorisé à supprimer ce patient");
    }

    [Fact]
    public void ActionNonAutorisee_WithNullReason_ReturnsSimpleMessage()
    {
        ErrorMessages.ActionNonAutorisee("accéder à ce dossier", null)
            .Should().Be("Vous n'êtes pas autorisé à accéder à ce dossier");
    }

    // ==================== TransitionStatutInvalide ====================

    [Fact]
    public void TransitionStatutInvalide_ReturnsFormattedMessage()
    {
        ErrorMessages.TransitionStatutInvalide("consultation", "en_cours", "planifie")
            .Should().Be("Impossible de passer le statut de consultation de 'en_cours' à 'planifie'");
    }

    [Fact]
    public void TransitionStatutInvalide_WithNullStatuts_HandlesGracefully()
    {
        ErrorMessages.TransitionStatutInvalide("examen", null, null)
            .Should().Be("Impossible de passer le statut de examen de 'inconnu' à 'inconnu'");
    }

    // ==================== Constants exist ====================

    [Fact]
    public void Constants_AreNotEmpty()
    {
        ErrorMessages.NonAuthentifie.Should().NotBeNullOrEmpty();
        ErrorMessages.PatientNonTrouve.Should().NotBeNullOrEmpty();
        ErrorMessages.ConsultationDejaTerminee.Should().NotBeNullOrEmpty();
        ErrorMessages.ErreurServeur.Should().NotBeNullOrEmpty();
    }
}
