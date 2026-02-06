namespace Mediconnet_Backend.Core.Constants;

/// <summary>
/// Constantes métier centralisées pour l'application MediConnect
/// </summary>
public static class BusinessRules
{
    /// <summary>
    /// Durée de validité d'un paiement de consultation (en jours)
    /// Un patient ayant payé une consultation peut revenir voir un médecin 
    /// de la même spécialité/service pendant cette période sans repayer
    /// </summary>
    public const int PaymentValidityDays = 14;

    /// <summary>
    /// Durée par défaut d'une consultation (en minutes)
    /// </summary>
    public const int DefaultConsultationDurationMinutes = 30;

    /// <summary>
    /// Prix par défaut d'une consultation (en FCFA)
    /// </summary>
    public const decimal DefaultConsultationPrice = 5000m;

    /// <summary>
    /// Nombre maximum de consultations à afficher dans l'historique
    /// </summary>
    public const int MaxHistoriqueConsultations = 20;

    /// <summary>
    /// Nombre maximum d'ordonnances à afficher dans l'historique
    /// </summary>
    public const int MaxHistoriqueOrdonnances = 10;

    /// <summary>
    /// Nombre maximum d'examens à afficher dans l'historique
    /// </summary>
    public const int MaxHistoriqueExamens = 20;

    /// <summary>
    /// Délai avant échéance d'une facture de consultation (en jours)
    /// </summary>
    public const int FactureConsultationEcheanceDays = 1;

    /// <summary>
    /// Délai avant échéance d'une facture d'hospitalisation (en jours)
    /// </summary>
    public const int FactureHospitalisationEcheanceDays = 30;
}

/// <summary>
/// Types de factures
/// </summary>
public static class FactureTypes
{
    public const string Consultation = "consultation";
    public const string Hospitalisation = "hospitalisation";
    public const string Examen = "examen";
    public const string Medicament = "medicament";
}

/// <summary>
/// Statuts de factures
/// </summary>
public static class FactureStatuts
{
    public const string EnAttente = "en_attente";
    public const string Payee = "payee";
    public const string Annulee = "annulee";
    public const string Partielle = "partielle";
}

/// <summary>
/// Types de rendez-vous
/// </summary>
public static class RendezVousTypes
{
    public const string Consultation = "consultation";
    public const string Suivi = "suivi";
    public const string Urgence = "urgence";
    public const string Controle = "controle";
}

/// <summary>
/// Types de consultations
/// </summary>
public static class ConsultationTypes
{
    public const string Normale = "normale";
    public const string Urgence = "urgence";
    public const string Suivi = "suivi";
    public const string Premiere = "premiere";
}

/// <summary>
/// Types de soins d'hospitalisation
/// </summary>
public static class SoinTypes
{
    public const string SoinsInfirmiers = "soins_infirmiers";
    public const string Surveillance = "surveillance";
    public const string Reeducation = "reeducation";
    public const string Nutrition = "nutrition";
    public const string Autre = "autre";
}

/// <summary>
/// Statuts de lits
/// </summary>
public static class LitStatuts
{
    public const string Libre = "libre";
    public const string Occupe = "occupe";
    public const string Maintenance = "maintenance";
    public const string Reserve = "reserve";
}
