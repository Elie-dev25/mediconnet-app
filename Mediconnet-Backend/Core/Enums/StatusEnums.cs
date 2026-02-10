namespace Mediconnet_Backend.Core.Enums;

/// <summary>
/// Statuts possibles pour une consultation médicale
/// </summary>
public enum ConsultationStatut
{
    /// <summary>Consultation planifiée, en attente</summary>
    Planifie,
    /// <summary>Consultation en cours avec le médecin</summary>
    EnCours,
    /// <summary>Consultation mise en pause temporairement</summary>
    EnPause,
    /// <summary>Consultation terminée</summary>
    Terminee,
    /// <summary>Consultation annulée</summary>
    Annulee
}

/// <summary>
/// Statuts possibles pour une hospitalisation
/// </summary>
public enum HospitalisationStatut
{
    /// <summary>Hospitalisation ordonnée, en attente d'attribution de lit par le Major</summary>
    EnAttente,
    /// <summary>Lit attribué, patient hospitalisé</summary>
    EnCours,
    /// <summary>Hospitalisation terminée, patient sorti</summary>
    Termine
}

/// <summary>
/// Statuts possibles pour un rendez-vous
/// </summary>
public enum RendezVousStatut
{
    /// <summary>RDV planifié mais pas encore confirmé (en attente de paiement)</summary>
    EnAttente,
    /// <summary>RDV confirmé (paiement effectué ou valide)</summary>
    Confirme,
    /// <summary>RDV planifié</summary>
    Planifie,
    /// <summary>RDV terminé</summary>
    Termine,
    /// <summary>RDV annulé</summary>
    Annule
}

/// <summary>
/// Statuts possibles pour un soin d'hospitalisation
/// </summary>
public enum SoinStatut
{
    /// <summary>Soin prescrit, en attente d'exécution</summary>
    Prescrit,
    /// <summary>Soin en cours d'exécution</summary>
    EnCours,
    /// <summary>Soin terminé/réalisé</summary>
    Termine,
    /// <summary>Soin annulé</summary>
    Annule
}

/// <summary>
/// Statuts possibles pour un bulletin d'examen
/// </summary>
public enum ExamenStatut
{
    /// <summary>Examen prescrit, en attente de réalisation</summary>
    Prescrit,
    /// <summary>Examen en cours de réalisation</summary>
    EnCours,
    /// <summary>Examen réalisé, résultats disponibles</summary>
    Realise,
    /// <summary>Examen annulé</summary>
    Annule
}

/// <summary>
/// Niveaux d'urgence pour une hospitalisation
/// </summary>
public enum NiveauUrgence
{
    /// <summary>Urgence normale</summary>
    Normale,
    /// <summary>Urgence élevée</summary>
    Urgente,
    /// <summary>Urgence critique</summary>
    Critique
}

/// <summary>
/// Priorités pour les soins
/// </summary>
public enum SoinPriorite
{
    /// <summary>Priorité basse</summary>
    Basse,
    /// <summary>Priorité normale</summary>
    Normale,
    /// <summary>Priorité haute</summary>
    Haute,
    /// <summary>Priorité urgente</summary>
    Urgente
}

/// <summary>
/// Extensions pour convertir les enums en chaînes de caractères compatibles avec la base de données
/// </summary>
public static class StatusEnumExtensions
{
    public static string ToDbString(this ConsultationStatut statut) => statut switch
    {
        ConsultationStatut.Planifie => "planifie",
        ConsultationStatut.EnCours => "en_cours",
        ConsultationStatut.EnPause => "en_pause",
        ConsultationStatut.Terminee => "terminee",
        ConsultationStatut.Annulee => "annulee",
        _ => "planifie"
    };

    public static ConsultationStatut ToConsultationStatut(this string? statut) => statut?.ToLower() switch
    {
        "planifie" => ConsultationStatut.Planifie,
        "en_cours" => ConsultationStatut.EnCours,
        "en_pause" => ConsultationStatut.EnPause,
        "terminee" or "termine" => ConsultationStatut.Terminee,
        "annulee" or "annule" => ConsultationStatut.Annulee,
        _ => ConsultationStatut.Planifie
    };

    public static string ToDbString(this HospitalisationStatut statut) => statut switch
    {
        HospitalisationStatut.EnAttente => "en_attente",
        HospitalisationStatut.EnCours => "en_cours",
        HospitalisationStatut.Termine => "termine",
        _ => "en_attente"
    };

    public static HospitalisationStatut ToHospitalisationStatut(this string? statut) => statut?.ToLower() switch
    {
        "en_attente" or "en_attente_lit" or "EN_ATTENTE" or "EN_ATTENTE_LIT" => HospitalisationStatut.EnAttente,
        "en_cours" or "actif" or "EN_COURS" or "ACTIF" => HospitalisationStatut.EnCours,
        "termine" or "terminee" or "TERMINE" or "TERMINEE" => HospitalisationStatut.Termine,
        _ => HospitalisationStatut.EnAttente
    };

    public static string ToDbString(this RendezVousStatut statut) => statut switch
    {
        RendezVousStatut.EnAttente => "en_attente",
        RendezVousStatut.Confirme => "confirme",
        RendezVousStatut.Planifie => "planifie",
        RendezVousStatut.Termine => "termine",
        RendezVousStatut.Annule => "annule",
        _ => "en_attente"
    };

    public static RendezVousStatut ToRendezVousStatut(this string? statut) => statut?.ToLower() switch
    {
        "en_attente" => RendezVousStatut.EnAttente,
        "confirme" => RendezVousStatut.Confirme,
        "planifie" => RendezVousStatut.Planifie,
        "termine" or "terminee" => RendezVousStatut.Termine,
        "annule" or "annulee" => RendezVousStatut.Annule,
        _ => RendezVousStatut.EnAttente
    };

    public static string ToDbString(this SoinStatut statut) => statut switch
    {
        SoinStatut.Prescrit => "prescrit",
        SoinStatut.EnCours => "en_cours",
        SoinStatut.Termine => "termine",
        SoinStatut.Annule => "annule",
        _ => "prescrit"
    };

    public static SoinStatut ToSoinStatut(this string? statut) => statut?.ToLower() switch
    {
        "prescrit" => SoinStatut.Prescrit,
        "en_cours" => SoinStatut.EnCours,
        "termine" or "terminee" => SoinStatut.Termine,
        "annule" or "annulee" => SoinStatut.Annule,
        _ => SoinStatut.Prescrit
    };

    public static string ToDbString(this ExamenStatut statut) => statut switch
    {
        ExamenStatut.Prescrit => "prescrit",
        ExamenStatut.EnCours => "en_cours",
        ExamenStatut.Realise => "termine",
        ExamenStatut.Annule => "annule",
        _ => "prescrit"
    };

    public static ExamenStatut ToExamenStatut(this string? statut) => statut?.ToLower() switch
    {
        "prescrit" => ExamenStatut.Prescrit,
        "en_cours" => ExamenStatut.EnCours,
        "termine" or "realise" => ExamenStatut.Realise,
        "annule" or "annulee" => ExamenStatut.Annule,
        _ => ExamenStatut.Prescrit
    };

    public static string ToDbString(this NiveauUrgence urgence) => urgence switch
    {
        NiveauUrgence.Normale => "normale",
        NiveauUrgence.Urgente => "urgente",
        NiveauUrgence.Critique => "critique",
        _ => "normale"
    };

    public static NiveauUrgence ToNiveauUrgence(this string? urgence) => urgence?.ToLower() switch
    {
        "normale" => NiveauUrgence.Normale,
        "urgente" => NiveauUrgence.Urgente,
        "critique" => NiveauUrgence.Critique,
        _ => NiveauUrgence.Normale
    };

    public static string ToDbString(this SoinPriorite priorite) => priorite switch
    {
        SoinPriorite.Basse => "basse",
        SoinPriorite.Normale => "normale",
        SoinPriorite.Haute => "haute",
        SoinPriorite.Urgente => "urgente",
        _ => "normale"
    };

    public static SoinPriorite ToSoinPriorite(this string? priorite) => priorite?.ToLower() switch
    {
        "basse" => SoinPriorite.Basse,
        "normale" => SoinPriorite.Normale,
        "haute" => SoinPriorite.Haute,
        "urgente" => SoinPriorite.Urgente,
        _ => SoinPriorite.Normale
    };
}
