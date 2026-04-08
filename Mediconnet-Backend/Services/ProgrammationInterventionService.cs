using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Consultation;
using Mediconnet_Backend.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service pour la gestion des programmations d'interventions chirurgicales
/// Gère les notifications, le blocage des créneaux et l'intégration avec l'agenda
/// </summary>
public class ProgrammationInterventionService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ProgrammationInterventionService> _logger;

    // IDs des spécialités chirurgicales autorisées
    private static readonly int[] CHIRURGIE_SPECIALITE_IDS = { 5, 6, 12, 21, 26, 31, 39, 41 };

    public ProgrammationInterventionService(
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<ProgrammationInterventionService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Créer une nouvelle programmation d'intervention avec notifications et blocage de créneaux
    /// </summary>
    public async Task<(bool Success, string Message, int? IdProgrammation)> CreateProgrammationAsync(
        int medecinId, CreateProgrammationInterventionRequest request)
    {
        // Vérifier que le médecin est chirurgien
        var medecin = await _context.Medecins
            .Include(m => m.Specialite)
            .Include(m => m.Utilisateur)
            .FirstOrDefaultAsync(m => m.IdUser == medecinId);

        if (medecin == null)
            return (false, "Médecin non trouvé", null);

        if (!CHIRURGIE_SPECIALITE_IDS.Contains(medecin.IdSpecialite ?? 0))
            return (false, "Seuls les chirurgiens peuvent créer des programmations d'intervention", null);

        // Vérifier la consultation
        var consultation = await _context.Consultations
            .Include(c => c.Patient)
                .ThenInclude(p => p.Utilisateur)
            .FirstOrDefaultAsync(c => c.IdConsultation == request.IdConsultation);

        if (consultation == null)
            return (false, "Consultation non trouvée", null);

        // Vérifier qu'il n'y a pas déjà une programmation pour cette consultation
        var existingProgrammation = await _context.ProgrammationsInterventions
            .FirstOrDefaultAsync(p => p.IdConsultation == request.IdConsultation);

        if (existingProgrammation != null)
            return (false, "Une programmation existe déjà pour cette consultation", null);

        // Créer l'indisponibilité pour bloquer le créneau si date et heure sont fournies
        IndisponibiliteMedecin? indisponibilite = null;
        if (request.DatePrevue.HasValue && !string.IsNullOrEmpty(request.HeureDebut) && request.DureeEstimee.HasValue)
        {
            var resultIndispo = await CreerIndisponibiliteInterventionAsync(
                medecinId, 
                request.DatePrevue.Value, 
                request.HeureDebut, 
                request.DureeEstimee.Value,
                consultation.Patient?.Utilisateur?.Nom,
                consultation.Patient?.Utilisateur?.Prenom,
                request.TechniquePrevue);

            if (resultIndispo.Success && resultIndispo.Indisponibilite != null)
            {
                indisponibilite = resultIndispo.Indisponibilite;
            }
            else if (!resultIndispo.Success)
            {
                return (false, resultIndispo.Message, null);
            }
        }

        // Créer la programmation
        var programmation = new ProgrammationIntervention
        {
            IdConsultation = request.IdConsultation,
            IdPatient = consultation.IdPatient,
            IdChirurgien = medecinId,
            TypeIntervention = request.TypeIntervention,
            ClassificationAsa = request.ClassificationAsa,
            RisqueOperatoire = request.RisqueOperatoire,
            ConsentementEclaire = request.ConsentementEclaire,
            DateConsentement = request.DateConsentement,
            IndicationOperatoire = request.IndicationOperatoire,
            TechniquePrevue = request.TechniquePrevue,
            DatePrevue = request.DatePrevue,
            HeureDebut = request.HeureDebut,
            DureeEstimee = request.DureeEstimee,
            NotesAnesthesie = request.NotesAnesthesie,
            BilanPreoperatoire = request.BilanPreoperatoire,
            InstructionsPatient = request.InstructionsPatient,
            Notes = request.Notes,
            Statut = "en_attente",
            IdIndisponibilite = indisponibilite?.IdIndisponibilite,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProgrammationsInterventions.Add(programmation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Programmation d'intervention créée: {Id} pour consultation {ConsultationId}", 
            programmation.IdProgrammation, request.IdConsultation);

        // Envoyer les notifications email
        await EnvoyerNotificationsAsync(programmation, medecin, consultation.Patient);

        return (true, "Programmation créée avec succès", programmation.IdProgrammation);
    }

    /// <summary>
    /// Mettre à jour une programmation avec mise à jour du blocage de créneaux
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateProgrammationAsync(
        int medecinId, int idProgrammation, UpdateProgrammationInterventionRequest request)
    {
        var programmation = await _context.ProgrammationsInterventions
            .Include(p => p.Patient).ThenInclude(pat => pat.Utilisateur)
            .Include(p => p.Chirurgien).ThenInclude(m => m.Utilisateur)
            .Include(p => p.Indisponibilite)
            .FirstOrDefaultAsync(p => p.IdProgrammation == idProgrammation);

        if (programmation == null)
            return (false, "Programmation non trouvée");

        if (programmation.IdChirurgien != medecinId)
            return (false, "Non autorisé");

        // Vérifier si la date/heure/durée a changé
        bool dateHeureChanged = (request.DatePrevue.HasValue && request.DatePrevue != programmation.DatePrevue) ||
                                (!string.IsNullOrEmpty(request.HeureDebut) && request.HeureDebut != programmation.HeureDebut) ||
                                (request.DureeEstimee.HasValue && request.DureeEstimee != programmation.DureeEstimee);

        // Mise à jour des champs
        if (request.TypeIntervention != null)
            programmation.TypeIntervention = request.TypeIntervention;
        if (request.ClassificationAsa != null)
            programmation.ClassificationAsa = request.ClassificationAsa;
        if (request.RisqueOperatoire != null)
            programmation.RisqueOperatoire = request.RisqueOperatoire;
        if (request.ConsentementEclaire.HasValue)
            programmation.ConsentementEclaire = request.ConsentementEclaire.Value;
        if (request.DateConsentement.HasValue)
            programmation.DateConsentement = request.DateConsentement;
        if (request.IndicationOperatoire != null)
            programmation.IndicationOperatoire = request.IndicationOperatoire;
        if (request.TechniquePrevue != null)
            programmation.TechniquePrevue = request.TechniquePrevue;
        if (request.DatePrevue.HasValue)
            programmation.DatePrevue = request.DatePrevue;
        if (!string.IsNullOrEmpty(request.HeureDebut))
            programmation.HeureDebut = request.HeureDebut;
        if (request.DureeEstimee.HasValue)
            programmation.DureeEstimee = request.DureeEstimee;
        if (request.NotesAnesthesie != null)
            programmation.NotesAnesthesie = request.NotesAnesthesie;
        if (request.BilanPreoperatoire != null)
            programmation.BilanPreoperatoire = request.BilanPreoperatoire;
        if (request.InstructionsPatient != null)
            programmation.InstructionsPatient = request.InstructionsPatient;
        if (request.Statut != null)
            programmation.Statut = request.Statut;
        if (request.MotifAnnulation != null)
            programmation.MotifAnnulation = request.MotifAnnulation;
        if (request.Notes != null)
            programmation.Notes = request.Notes;

        programmation.UpdatedAt = DateTime.UtcNow;

        // Si la date/heure a changé, mettre à jour l'indisponibilité
        if (dateHeureChanged && programmation.DatePrevue.HasValue && 
            !string.IsNullOrEmpty(programmation.HeureDebut) && programmation.DureeEstimee.HasValue)
        {
            // Récupérer l'ID de l'ancienne indisponibilité pour l'exclure de la vérification
            var oldIndispoId = programmation.IdIndisponibilite;

            // Créer la nouvelle indisponibilité (en excluant l'ancienne de la vérification)
            var resultIndispo = await CreerIndisponibiliteInterventionAsync(
                medecinId,
                programmation.DatePrevue.Value,
                programmation.HeureDebut,
                programmation.DureeEstimee.Value,
                programmation.Patient?.Utilisateur?.Nom,
                programmation.Patient?.Utilisateur?.Prenom,
                programmation.TechniquePrevue,
                oldIndispoId);

            if (resultIndispo.Success && resultIndispo.Indisponibilite != null)
            {
                // Supprimer l'ancienne indisponibilité après création de la nouvelle
                if (programmation.Indisponibilite != null)
                {
                    _context.IndisponibilitesMedecin.Remove(programmation.Indisponibilite);
                }
                programmation.IdIndisponibilite = resultIndispo.Indisponibilite.IdIndisponibilite;
            }
            else if (!resultIndispo.Success)
            {
                return (false, resultIndispo.Message);
            }
        }

        await _context.SaveChangesAsync();

        // Envoyer notification de modification si date/heure a changé
        if (dateHeureChanged)
        {
            await EnvoyerNotificationModificationAsync(programmation);
        }

        return (true, "Programmation mise à jour avec succès");
    }

    /// <summary>
    /// Annuler une programmation et libérer le créneau
    /// </summary>
    public async Task<(bool Success, string Message)> AnnulerProgrammationAsync(
        int medecinId, int idProgrammation, string? motif)
    {
        var programmation = await _context.ProgrammationsInterventions
            .Include(p => p.Patient).ThenInclude(pat => pat.Utilisateur)
            .Include(p => p.Chirurgien).ThenInclude(m => m.Utilisateur)
            .Include(p => p.Indisponibilite)
            .FirstOrDefaultAsync(p => p.IdProgrammation == idProgrammation);

        if (programmation == null)
            return (false, "Programmation non trouvée");

        if (programmation.IdChirurgien != medecinId)
            return (false, "Non autorisé");

        if (programmation.Statut == "realisee")
            return (false, "Impossible d'annuler une intervention déjà réalisée");

        // Supprimer l'indisponibilité pour libérer le créneau
        if (programmation.Indisponibilite != null)
        {
            _context.IndisponibilitesMedecin.Remove(programmation.Indisponibilite);
            programmation.IdIndisponibilite = null;
        }

        programmation.Statut = "annulee";
        programmation.MotifAnnulation = motif;
        programmation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Envoyer notification d'annulation
        await EnvoyerNotificationAnnulationAsync(programmation, motif);

        return (true, "Programmation annulée");
    }

    /// <summary>
    /// Créer une indisponibilité pour bloquer le créneau de l'intervention
    /// </summary>
    /// <param name="excludeIndispoId">ID de l'indisponibilité à exclure de la vérification (pour les mises à jour)</param>
    private async Task<(bool Success, string Message, IndisponibiliteMedecin? Indisponibilite)> CreerIndisponibiliteInterventionAsync(
        int medecinId, DateTime datePrevue, string heureDebut, int dureeMinutes,
        string? patientNom, string? patientPrenom, string? technique, int? excludeIndispoId = null)
    {
        // Parser l'heure de début
        if (!TimeSpan.TryParse(heureDebut, out var heureDebutTs))
            return (false, "Format d'heure invalide", null);

        // Calculer l'heure de fin
        var heureFinTs = heureDebutTs.Add(TimeSpan.FromMinutes(dureeMinutes));

        // Créer les dates de début et fin
        var dateDebut = datePrevue.Date.Add(heureDebutTs);
        var dateFin = datePrevue.Date.Add(heureFinTs);

        // Vérifier les conflits avec d'autres RDV ou indisponibilités
        var conflitRdv = await _context.RendezVous
            .Where(r => r.IdMedecin == medecinId &&
                       r.Statut != "annule" &&
                       r.DateHeure.Date == datePrevue.Date &&
                       r.DateHeure < dateFin &&
                       r.DateHeure.AddMinutes(r.Duree) > dateDebut)
            .AnyAsync();

        if (conflitRdv)
            return (false, "Un rendez-vous existe déjà sur ce créneau horaire", null);

        var conflitIndispo = await _context.IndisponibilitesMedecin
            .Where(i => i.IdMedecin == medecinId &&
                       i.DateDebut < dateFin &&
                       i.DateFin > dateDebut &&
                       (excludeIndispoId == null || i.IdIndisponibilite != excludeIndispoId))
            .AnyAsync();

        if (conflitIndispo)
            return (false, "Une indisponibilité existe déjà sur ce créneau horaire", null);

        // Créer l'indisponibilité
        var motif = $"Intervention chirurgicale - {patientPrenom} {patientNom}";
        if (!string.IsNullOrEmpty(technique))
            motif += $" ({technique})";

        var indispo = new IndisponibiliteMedecin
        {
            IdMedecin = medecinId,
            DateDebut = dateDebut,
            DateFin = dateFin,
            Type = "intervention",
            Motif = motif,
            JourneeComplete = false
        };

        _context.IndisponibilitesMedecin.Add(indispo);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Indisponibilité créée pour intervention: {Id}, {DateDebut} - {DateFin}",
            indispo.IdIndisponibilite, dateDebut, dateFin);

        return (true, "Créneau bloqué avec succès", indispo);
    }

    /// <summary>
    /// Envoyer les notifications email au médecin et au patient
    /// </summary>
    private async Task EnvoyerNotificationsAsync(ProgrammationIntervention programmation, Medecin medecin, Patient patient)
    {
        var patientNom = $"{patient.Utilisateur?.Prenom} {patient.Utilisateur?.Nom}";
        var medecinNom = $"Dr. {medecin.Utilisateur?.Prenom} {medecin.Utilisateur?.Nom}";
        var dateStr = programmation.DatePrevue?.ToString("dd/MM/yyyy") ?? "À définir";
        var heureStr = programmation.HeureDebut ?? "À définir";
        var dureeStr = programmation.DureeEstimee.HasValue ? $"{programmation.DureeEstimee} minutes" : "À définir";

        // Email au patient
        if (!string.IsNullOrEmpty(patient.Utilisateur?.Email))
        {
            var subjectPatient = "Programmation d'intervention chirurgicale - MediConnect";
            var bodyPatient = GetEmailTemplatePatient(
                patientNom, medecinNom, medecin.Specialite?.NomSpecialite ?? "",
                programmation.IndicationOperatoire ?? "", programmation.TechniquePrevue ?? "",
                dateStr, heureStr, dureeStr, programmation.InstructionsPatient ?? "",
                programmation.BilanPreoperatoire ?? "");

            await _emailService.SendEmailAsync(patient.Utilisateur.Email, subjectPatient, bodyPatient);
            _logger.LogInformation("Email de programmation envoyé au patient: {Email}", patient.Utilisateur.Email);
        }

        // Email au médecin
        if (!string.IsNullOrEmpty(medecin.Utilisateur?.Email))
        {
            var subjectMedecin = $"Nouvelle programmation d'intervention - {patientNom}";
            var bodyMedecin = GetEmailTemplateMedecin(
                medecinNom, patientNom, programmation.IndicationOperatoire ?? "",
                programmation.TechniquePrevue ?? "", dateStr, heureStr, dureeStr,
                programmation.ClassificationAsa ?? "", programmation.RisqueOperatoire ?? "",
                programmation.NotesAnesthesie ?? "");

            await _emailService.SendEmailAsync(medecin.Utilisateur.Email, subjectMedecin, bodyMedecin);
            _logger.LogInformation("Email de programmation envoyé au médecin: {Email}", medecin.Utilisateur.Email);
        }
    }

    /// <summary>
    /// Envoyer notification de modification
    /// </summary>
    private async Task EnvoyerNotificationModificationAsync(ProgrammationIntervention programmation)
    {
        var patientEmail = programmation.Patient?.Utilisateur?.Email;
        var patientNom = $"{programmation.Patient?.Utilisateur?.Prenom} {programmation.Patient?.Utilisateur?.Nom}";
        var dateStr = programmation.DatePrevue?.ToString("dd/MM/yyyy") ?? "À définir";
        var heureStr = programmation.HeureDebut ?? "À définir";

        if (!string.IsNullOrEmpty(patientEmail))
        {
            var subject = "Modification de votre intervention chirurgicale - MediConnect";
            var body = GetEmailTemplateModification(patientNom, dateStr, heureStr);
            await _emailService.SendEmailAsync(patientEmail, subject, body);
        }
    }

    /// <summary>
    /// Envoyer notification d'annulation
    /// </summary>
    private async Task EnvoyerNotificationAnnulationAsync(ProgrammationIntervention programmation, string? motif)
    {
        var patientEmail = programmation.Patient?.Utilisateur?.Email;
        var patientNom = $"{programmation.Patient?.Utilisateur?.Prenom} {programmation.Patient?.Utilisateur?.Nom}";
        var medecinNom = $"Dr. {programmation.Chirurgien?.Utilisateur?.Prenom} {programmation.Chirurgien?.Utilisateur?.Nom}";

        if (!string.IsNullOrEmpty(patientEmail))
        {
            var subject = "Annulation de votre intervention chirurgicale - MediConnect";
            var body = GetEmailTemplateAnnulation(patientNom, medecinNom, motif ?? "Non précisé");
            await _emailService.SendEmailAsync(patientEmail, subject, body);
        }
    }

    #region Email Templates

    private string GetEmailTemplatePatient(string patientNom, string medecinNom, string specialite,
        string indication, string technique, string date, string heure, string duree,
        string instructions, string bilanPreop)
    {
        return $@"
<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #7c3aed 0%, #8b5cf6 100%); padding: 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 24px;"">🏥 Programmation d'Intervention</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #1f2937; margin: 0 0 20px;"">Bonjour {patientNom},</h2>
                            <p style=""color: #4b5563; font-size: 16px; line-height: 1.6;"">
                                Une intervention chirurgicale a été programmée pour vous par <strong>{medecinNom}</strong> ({specialite}).
                            </p>
                            
                            <div style=""background-color: #f3f4f6; padding: 20px; border-radius: 8px; margin: 25px 0;"">
                                <h3 style=""color: #1f2937; margin: 0 0 15px;"">📋 Détails de l'intervention</h3>
                                <table style=""width: 100%; border-collapse: collapse;"">
                                    <tr><td style=""padding: 8px 0; color: #6b7280;"">Indication :</td><td style=""padding: 8px 0; color: #1f2937; font-weight: 500;"">{indication}</td></tr>
                                    <tr><td style=""padding: 8px 0; color: #6b7280;"">Technique :</td><td style=""padding: 8px 0; color: #1f2937; font-weight: 500;"">{technique}</td></tr>
                                    <tr><td style=""padding: 8px 0; color: #6b7280;"">Date :</td><td style=""padding: 8px 0; color: #1f2937; font-weight: 500;"">{date}</td></tr>
                                    <tr><td style=""padding: 8px 0; color: #6b7280;"">Heure :</td><td style=""padding: 8px 0; color: #1f2937; font-weight: 500;"">{heure}</td></tr>
                                    <tr><td style=""padding: 8px 0; color: #6b7280;"">Durée estimée :</td><td style=""padding: 8px 0; color: #1f2937; font-weight: 500;"">{duree}</td></tr>
                                </table>
                            </div>

                            {(string.IsNullOrEmpty(bilanPreop) ? "" : $@"
                            <div style=""background-color: #fef3c7; padding: 20px; border-radius: 8px; margin: 25px 0; border-left: 4px solid #d97706;"">
                                <h3 style=""color: #92400e; margin: 0 0 10px;"">📝 Bilan pré-opératoire requis</h3>
                                <p style=""color: #78350f; margin: 0;"">{bilanPreop}</p>
                            </div>
                            ")}

                            {(string.IsNullOrEmpty(instructions) ? "" : $@"
                            <div style=""background-color: #dbeafe; padding: 20px; border-radius: 8px; margin: 25px 0; border-left: 4px solid #2563eb;"">
                                <h3 style=""color: #1e40af; margin: 0 0 10px;"">📌 Instructions pré-opératoires</h3>
                                <p style=""color: #1e3a8a; margin: 0;"">{instructions}</p>
                            </div>
                            ")}

                            <p style=""color: #6b7280; font-size: 14px; margin-top: 30px;"">
                                Pour toute question, n'hésitez pas à contacter votre chirurgien ou notre équipe médicale.
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f9fafb; padding: 25px 30px; text-align: center; border-radius: 0 0 12px 12px;"">
                            <p style=""color: #9ca3af; font-size: 13px; margin: 0;"">© {DateTime.Now.Year} MediConnect</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string GetEmailTemplateMedecin(string medecinNom, string patientNom, string indication,
        string technique, string date, string heure, string duree, string asa, string risque, string notesAnesthesie)
    {
        return $@"
<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""UTF-8"">
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #0e7490 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 24px;"">🔔 Nouvelle Programmation</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #1f2937; margin: 0 0 20px;"">Bonjour {medecinNom},</h2>
                            <p style=""color: #4b5563; font-size: 16px; line-height: 1.6;"">
                                Une intervention chirurgicale a été programmée et ajoutée à votre agenda.
                            </p>
                            
                            <div style=""background-color: #f3f4f6; padding: 20px; border-radius: 8px; margin: 25px 0;"">
                                <h3 style=""color: #1f2937; margin: 0 0 15px;"">📋 Récapitulatif</h3>
                                <table style=""width: 100%; border-collapse: collapse;"">
                                    <tr><td style=""padding: 8px 0; color: #6b7280;"">Patient :</td><td style=""padding: 8px 0; color: #1f2937; font-weight: 500;"">{patientNom}</td></tr>
                                    <tr><td style=""padding: 8px 0; color: #6b7280;"">Indication :</td><td style=""padding: 8px 0; color: #1f2937; font-weight: 500;"">{indication}</td></tr>
                                    <tr><td style=""padding: 8px 0; color: #6b7280;"">Technique :</td><td style=""padding: 8px 0; color: #1f2937; font-weight: 500;"">{technique}</td></tr>
                                    <tr><td style=""padding: 8px 0; color: #6b7280;"">Date :</td><td style=""padding: 8px 0; color: #1f2937; font-weight: 500;"">{date}</td></tr>
                                    <tr><td style=""padding: 8px 0; color: #6b7280;"">Heure :</td><td style=""padding: 8px 0; color: #1f2937; font-weight: 500;"">{heure}</td></tr>
                                    <tr><td style=""padding: 8px 0; color: #6b7280;"">Durée estimée :</td><td style=""padding: 8px 0; color: #1f2937; font-weight: 500;"">{duree}</td></tr>
                                    <tr><td style=""padding: 8px 0; color: #6b7280;"">Classification ASA :</td><td style=""padding: 8px 0; color: #1f2937; font-weight: 500;"">{asa}</td></tr>
                                    <tr><td style=""padding: 8px 0; color: #6b7280;"">Risque opératoire :</td><td style=""padding: 8px 0; color: #1f2937; font-weight: 500;"">{risque}</td></tr>
                                </table>
                            </div>

                            {(string.IsNullOrEmpty(notesAnesthesie) ? "" : $@"
                            <div style=""background-color: #fef3c7; padding: 20px; border-radius: 8px; margin: 25px 0;"">
                                <h3 style=""color: #92400e; margin: 0 0 10px;"">💉 Notes anesthésie</h3>
                                <p style=""color: #78350f; margin: 0;"">{notesAnesthesie}</p>
                            </div>
                            ")}

                            <p style=""color: #059669; font-size: 14px; margin-top: 20px; padding: 15px; background-color: #d1fae5; border-radius: 8px;"">
                                ✅ Le créneau a été automatiquement bloqué dans votre agenda.
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f9fafb; padding: 25px 30px; text-align: center; border-radius: 0 0 12px 12px;"">
                            <p style=""color: #9ca3af; font-size: 13px; margin: 0;"">© {DateTime.Now.Year} MediConnect</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string GetEmailTemplateModification(string patientNom, string date, string heure)
    {
        return $@"
<!DOCTYPE html>
<html lang=""fr"">
<head><meta charset=""UTF-8""></head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', sans-serif; background-color: #f4f7fa;"">
    <table style=""width: 100%;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table style=""width: 600px; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #d97706 0%, #f59e0b 100%); padding: 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0;"">⚠️ Modification d'Intervention</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #1f2937;"">Bonjour {patientNom},</h2>
                            <p style=""color: #4b5563; font-size: 16px;"">
                                La date ou l'heure de votre intervention chirurgicale a été modifiée.
                            </p>
                            <div style=""background-color: #fef3c7; padding: 20px; border-radius: 8px; margin: 25px 0;"">
                                <p style=""margin: 0;""><strong>Nouvelle date :</strong> {date}</p>
                                <p style=""margin: 10px 0 0;""><strong>Nouvelle heure :</strong> {heure}</p>
                            </div>
                            <p style=""color: #6b7280;"">Veuillez prendre note de ce changement. Contactez-nous si vous avez des questions.</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f9fafb; padding: 25px; text-align: center; border-radius: 0 0 12px 12px;"">
                            <p style=""color: #9ca3af; font-size: 13px; margin: 0;"">© {DateTime.Now.Year} MediConnect</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string GetEmailTemplateAnnulation(string patientNom, string medecinNom, string motif)
    {
        return $@"
<!DOCTYPE html>
<html lang=""fr"">
<head><meta charset=""UTF-8""></head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', sans-serif; background-color: #f4f7fa;"">
    <table style=""width: 100%;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table style=""width: 600px; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #dc2626 0%, #ef4444 100%); padding: 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0;"">❌ Annulation d'Intervention</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #1f2937;"">Bonjour {patientNom},</h2>
                            <p style=""color: #4b5563; font-size: 16px;"">
                                Nous vous informons que votre intervention chirurgicale programmée avec <strong>{medecinNom}</strong> a été annulée.
                            </p>
                            <div style=""background-color: #fee2e2; padding: 20px; border-radius: 8px; margin: 25px 0;"">
                                <p style=""margin: 0;""><strong>Motif :</strong> {motif}</p>
                            </div>
                            <p style=""color: #6b7280;"">
                                Veuillez contacter votre chirurgien ou notre équipe médicale pour reprogrammer si nécessaire.
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f9fafb; padding: 25px; text-align: center; border-radius: 0 0 12px 12px;"">
                            <p style=""color: #9ca3af; font-size: 13px; margin: 0;"">© {DateTime.Now.Year} MediConnect</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    #endregion
}
