using Microsoft.Extensions.Logging;

namespace Mediconnet_Backend.Core.Services;

/// <summary>
/// Extensions de logging pour enrichir les logs avec du contexte métier
/// </summary>
public static class LoggingExtensions
{
    #region Consultation

    public static void LogConsultationDemarree(this ILogger logger, int idConsultation, int idMedecin, int idPatient)
    {
        logger.LogInformation(
            "Consultation démarrée - IdConsultation: {IdConsultation}, IdMedecin: {IdMedecin}, IdPatient: {IdPatient}",
            idConsultation, idMedecin, idPatient);
    }

    public static void LogConsultationTerminee(this ILogger logger, int idConsultation, int idMedecin, int idPatient, TimeSpan? duree = null)
    {
        if (duree.HasValue)
        {
            logger.LogInformation(
                "Consultation terminée - IdConsultation: {IdConsultation}, IdMedecin: {IdMedecin}, IdPatient: {IdPatient}, Durée: {Duree}",
                idConsultation, idMedecin, idPatient, duree.Value);
        }
        else
        {
            logger.LogInformation(
                "Consultation terminée - IdConsultation: {IdConsultation}, IdMedecin: {IdMedecin}, IdPatient: {IdPatient}",
                idConsultation, idMedecin, idPatient);
        }
    }

    public static void LogConsultationErreur(this ILogger logger, int idConsultation, string operation, Exception ex)
    {
        logger.LogError(ex,
            "Erreur consultation - IdConsultation: {IdConsultation}, Opération: {Operation}, Message: {Message}",
            idConsultation, operation, ex.Message);
    }

    #endregion

    #region Examen

    public static void LogExamenDemarre(this ILogger logger, int idBulletin, int? idLaborantin, string? nomExamen = null)
    {
        logger.LogInformation(
            "Examen démarré - IdBulletin: {IdBulletin}, IdLaborantin: {IdLaborantin}, NomExamen: {NomExamen}",
            idBulletin, idLaborantin, nomExamen ?? "N/A");
    }

    public static void LogExamenResultatEnregistre(this ILogger logger, int idBulletin, int idLaborantin, bool avecFichiers)
    {
        logger.LogInformation(
            "Résultat examen enregistré - IdBulletin: {IdBulletin}, IdLaborantin: {IdLaborantin}, AvecFichiers: {AvecFichiers}",
            idBulletin, idLaborantin, avecFichiers);
    }

    public static void LogExamenErreur(this ILogger logger, int idBulletin, string operation, Exception ex)
    {
        logger.LogError(ex,
            "Erreur examen - IdBulletin: {IdBulletin}, Opération: {Operation}, Message: {Message}",
            idBulletin, operation, ex.Message);
    }

    #endregion

    #region Hospitalisation

    public static void LogHospitalisationCreee(this ILogger logger, int idHospitalisation, int idPatient, int idMedecin, int? idLit = null)
    {
        logger.LogInformation(
            "Hospitalisation créée - IdHospitalisation: {IdHospitalisation}, IdPatient: {IdPatient}, IdMedecin: {IdMedecin}, IdLit: {IdLit}",
            idHospitalisation, idPatient, idMedecin, idLit?.ToString() ?? "Non attribué");
    }

    public static void LogHospitalisationLitAttribue(this ILogger logger, int idHospitalisation, int idLit, string? numeroChambre = null)
    {
        logger.LogInformation(
            "Lit attribué - IdHospitalisation: {IdHospitalisation}, IdLit: {IdLit}, Chambre: {Chambre}",
            idHospitalisation, idLit, numeroChambre ?? "N/A");
    }

    public static void LogHospitalisationTerminee(this ILogger logger, int idHospitalisation, int idPatient, DateTime dateEntree, DateTime dateSortie)
    {
        var duree = dateSortie - dateEntree;
        logger.LogInformation(
            "Hospitalisation terminée - IdHospitalisation: {IdHospitalisation}, IdPatient: {IdPatient}, DuréeJours: {DureeJours}",
            idHospitalisation, idPatient, duree.TotalDays.ToString("F1"));
    }

    #endregion

    #region Rendez-vous

    public static void LogRendezVousCree(this ILogger logger, int idRendezVous, int idPatient, int idMedecin, DateTime dateHeure)
    {
        logger.LogInformation(
            "Rendez-vous créé - IdRendezVous: {IdRendezVous}, IdPatient: {IdPatient}, IdMedecin: {IdMedecin}, DateHeure: {DateHeure}",
            idRendezVous, idPatient, idMedecin, dateHeure);
    }

    public static void LogRendezVousConfirme(this ILogger logger, int idRendezVous, int? idAgent = null)
    {
        logger.LogInformation(
            "Rendez-vous confirmé - IdRendezVous: {IdRendezVous}, IdAgent: {IdAgent}",
            idRendezVous, idAgent?.ToString() ?? "Système");
    }

    public static void LogRendezVousAnnule(this ILogger logger, int idRendezVous, string? raison = null)
    {
        logger.LogInformation(
            "Rendez-vous annulé - IdRendezVous: {IdRendezVous}, Raison: {Raison}",
            idRendezVous, raison ?? "Non spécifiée");
    }

    #endregion

    #region Document

    public static void LogDocumentCree(this ILogger logger, string uuid, int idPatient, string typeDocument, int? idCreateur = null)
    {
        logger.LogInformation(
            "Document créé - UUID: {UUID}, IdPatient: {IdPatient}, Type: {TypeDocument}, IdCreateur: {IdCreateur}",
            uuid, idPatient, typeDocument, idCreateur?.ToString() ?? "Système");
    }

    public static void LogDocumentAccede(this ILogger logger, string uuid, int idUtilisateur, string role)
    {
        logger.LogInformation(
            "Document accédé - UUID: {UUID}, IdUtilisateur: {IdUtilisateur}, Role: {Role}",
            uuid, idUtilisateur, role);
    }

    public static void LogDocumentErreur(this ILogger logger, string uuid, string operation, Exception ex)
    {
        logger.LogError(ex,
            "Erreur document - UUID: {UUID}, Opération: {Operation}, Message: {Message}",
            uuid, operation, ex.Message);
    }

    #endregion

    #region Sécurité et accès

    public static void LogAccesNonAutorise(this ILogger logger, int? idUtilisateur, string ressource, string action)
    {
        logger.LogWarning(
            "Accès non autorisé - IdUtilisateur: {IdUtilisateur}, Ressource: {Ressource}, Action: {Action}",
            idUtilisateur?.ToString() ?? "Anonyme", ressource, action);
    }

    public static void LogConnexionReussie(this ILogger logger, int idUtilisateur, string role, string? ip = null)
    {
        logger.LogInformation(
            "Connexion réussie - IdUtilisateur: {IdUtilisateur}, Role: {Role}, IP: {IP}",
            idUtilisateur, role, ip ?? "N/A");
    }

    public static void LogConnexionEchouee(this ILogger logger, string email, string? raison = null, string? ip = null)
    {
        logger.LogWarning(
            "Connexion échouée - Email: {Email}, Raison: {Raison}, IP: {IP}",
            email, raison ?? "Identifiants invalides", ip ?? "N/A");
    }

    #endregion

    #region Performance

    public static void LogOperationLente(this ILogger logger, string operation, TimeSpan duree, int? seuilMs = 1000)
    {
        if (duree.TotalMilliseconds > (seuilMs ?? 1000))
        {
            logger.LogWarning(
                "Opération lente détectée - Opération: {Operation}, Durée: {DureeMs}ms",
                operation, duree.TotalMilliseconds);
        }
    }

    #endregion
}
