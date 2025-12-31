using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Controllers.Base;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Patient;

namespace Mediconnet_Backend.Controllers;

/// <summary>
/// [OBSOLÈTE] Contrôleur pour la gestion du profil utilisateur et la complétion du profil patient.
/// Ce contrôleur n'est plus utilisé depuis que le profil est complété lors de l'inscription.
/// La route /complete-profile du frontend redirige maintenant vers /register.
/// Conservé pour référence et rétrocompatibilité éventuelle.
/// Pour les opérations de profil patient, utiliser PatientController.
/// </summary>
[Obsolete("Ce contrôleur est obsolète. Le profil est maintenant complété lors de l'inscription.")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(ApplicationDbContext context, ILogger<ProfileController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Récupère le statut de complétion du profil
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<ProfileStatusResponse>> GetProfileStatus()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var user = await _context.Utilisateurs.FindAsync(userId.Value);
        if (user == null)
            return NotFound(new { message = "Utilisateur non trouvé" });

        var response = new ProfileStatusResponse
        {
            ProfileCompleted = user.ProfileCompleted,
            ProfileCompletedAt = user.ProfileCompletedAt,
            EmailConfirmed = user.EmailConfirmed
        };

        // Déterminer la redirection
        if (!user.EmailConfirmed)
        {
            response.RedirectTo = "/auth/email-verified";
        }
        else if (!user.ProfileCompleted && user.Role == "patient")
        {
            response.RedirectTo = "/complete-profile";
        }
        else
        {
            response.RedirectTo = $"/{user.Role}";
        }

        return Ok(response);
    }

    /// <summary>
    /// Récupère les options pour les formulaires (régions, groupes sanguins, etc.)
    /// </summary>
    [HttpGet("form-options")]
    [AllowAnonymous]
    public ActionResult<ProfileFormOptionsDto> GetFormOptions()
    {
        var options = new ProfileFormOptionsDto
        {
            Regions = new List<string>
            {
                "Adamaoua",
                "Centre",
                "Est",
                "Extrême-Nord",
                "Littoral",
                "Nord",
                "Nord-Ouest",
                "Ouest",
                "Sud",
                "Sud-Ouest",
                "Autres"
            },
            GroupesSanguins = new List<string>
            {
                "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"
            },
            SituationsMatrimoniales = new List<string>
            {
                "Célibataire",
                "Marié(e)",
                "Divorcé(e)",
                "Veuf/Veuve",
                "En couple"
            },
            MaladiesChroniquesOptions = new List<string>
            {
                "Diabète",
                "Hypertension",
                "Asthme",
                "Insuffisance cardiaque",
                "Insuffisance rénale",
                "Épilepsie",
                "VIH/SIDA",
                "Hépatite",
                "Drépanocytose",
                "Cancer",
                "Aucune",
                "Autres"
            },
            FrequencesAlcool = new List<string>
            {
                "Occasionnel",
                "Régulier",
                "Quotidien"
            }
        };

        return Ok(options);
    }

    /// <summary>
    /// Complète le profil patient (toutes les étapes en une fois)
    /// </summary>
    [HttpPost("complete")]
    public async Task<ActionResult<CompleteProfileResponse>> CompleteProfile([FromBody] CompleteProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            var user = await _context.Utilisateurs
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.IdUser == userId.Value);

            if (user == null)
                return NotFound(new CompleteProfileResponse
                {
                    Success = false,
                    Message = "Utilisateur non trouvé",
                    ErrorCode = "USER_NOT_FOUND"
                });

            // Vérifier que l'email est confirmé
            if (!user.EmailConfirmed)
            {
                return BadRequest(new CompleteProfileResponse
                {
                    Success = false,
                    Message = "Veuillez d'abord confirmer votre email",
                    ErrorCode = "EMAIL_NOT_CONFIRMED"
                });
            }

            // Vérifier que c'est un patient
            if (user.Role != "patient")
            {
                return BadRequest(new CompleteProfileResponse
                {
                    Success = false,
                    Message = "Seuls les patients peuvent compléter ce profil",
                    ErrorCode = "NOT_A_PATIENT"
                });
            }

            // Vérifier que la déclaration sur l'honneur est acceptée
            if (!request.DeclarationHonneur.Acceptee)
            {
                return BadRequest(new CompleteProfileResponse
                {
                    Success = false,
                    Message = "Vous devez accepter la déclaration sur l'honneur",
                    ErrorCode = "DECLARATION_NOT_ACCEPTED"
                });
            }

            // Mettre à jour les informations utilisateur
            user.Naissance = request.PersonalInfo.DateNaissance;
            user.Nationalite = request.PersonalInfo.Nationalite;
            user.RegionOrigine = request.PersonalInfo.RegionOrigine;
            user.Sexe = request.PersonalInfo.Sexe;
            user.SituationMatrimoniale = request.PersonalInfo.SituationMatrimoniale;
            user.Adresse = request.PersonalInfo.Adresse;
            user.ProfileCompleted = true;
            user.ProfileCompletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            // Créer ou mettre à jour le profil patient
            if (user.Patient == null)
            {
                user.Patient = new Core.Entities.Patient
                {
                    IdUser = user.IdUser,
                    NumeroDossier = GenerateNumeroDossier(),
                    DateCreation = DateTime.UtcNow
                };
                _context.Patients.Add(user.Patient);
            }

            var patient = user.Patient;

            // Informations personnelles
            patient.Ethnie = request.PersonalInfo.Ethnie;
            patient.NbEnfants = request.PersonalInfo.NbEnfants;

            // Informations médicales
            patient.GroupeSanguin = request.MedicalInfo.GroupeSanguin;
            patient.Profession = request.MedicalInfo.Profession;

            // Construire la liste des maladies chroniques
            var maladies = request.MedicalInfo.MaladiesChroniques
                .Where(m => m != "Aucune" && m != "Autres")
                .ToList();
            if (!string.IsNullOrWhiteSpace(request.MedicalInfo.AutreMaladieChronique))
            {
                maladies.Add(request.MedicalInfo.AutreMaladieChronique);
            }
            patient.MaladiesChroniques = maladies.Count > 0 ? string.Join(", ", maladies) : null;

            patient.OperationsChirurgicales = request.MedicalInfo.OperationsChirurgicales;
            patient.OperationsDetails = request.MedicalInfo.OperationsChirurgicales 
                ? request.MedicalInfo.OperationsDetails 
                : null;

            patient.AllergiesConnues = request.MedicalInfo.AllergiesConnues;
            patient.AllergiesDetails = request.MedicalInfo.AllergiesConnues 
                ? request.MedicalInfo.AllergiesDetails 
                : null;

            patient.AntecedentsFamiliaux = request.MedicalInfo.AntecedentsFamiliaux;
            patient.AntecedentsFamiliauxDetails = request.MedicalInfo.AntecedentsFamiliaux 
                ? request.MedicalInfo.AntecedentsFamiliauxDetails 
                : null;

            // Habitudes de vie
            patient.ConsommationAlcool = request.LifestyleInfo.ConsommationAlcool;
            patient.FrequenceAlcool = request.LifestyleInfo.ConsommationAlcool 
                ? request.LifestyleInfo.FrequenceAlcool 
                : null;
            patient.Tabagisme = request.LifestyleInfo.Tabagisme;
            patient.ActivitePhysique = request.LifestyleInfo.ActivitePhysique;

            // Contacts d'urgence
            patient.PersonneContact = request.EmergencyContact.PersonneContact;
            patient.NumeroContact = request.EmergencyContact.NumeroContact;

            // Déclaration sur l'honneur
            patient.DeclarationHonneurAcceptee = true;
            patient.DeclarationHonneurAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Profile completed for user {userId}");

            return Ok(new CompleteProfileResponse
            {
                Success = true,
                Message = "Profil complété avec succès",
                Profile = new PatientProfileSummary
                {
                    IdUser = user.IdUser,
                    NomComplet = $"{user.Prenom} {user.Nom}",
                    Email = user.Email,
                    Telephone = user.Telephone,
                    DateNaissance = user.Naissance,
                    Nationalite = user.Nationalite,
                    RegionOrigine = user.RegionOrigine,
                    Sexe = user.Sexe,
                    GroupeSanguin = patient.GroupeSanguin,
                    ProfileCompleted = user.ProfileCompleted,
                    ProfileCompletedAt = user.ProfileCompletedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error completing profile: {ex.Message}");
            return StatusCode(500, new CompleteProfileResponse
            {
                Success = false,
                Message = "Une erreur est survenue lors de la complétion du profil",
                ErrorCode = "SERVER_ERROR"
            });
        }
    }

    /// <summary>
    /// Récupère le profil actuel (pour pré-remplir le formulaire)
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult> GetCurrentProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var user = await _context.Utilisateurs
            .Include(u => u.Patient)
            .FirstOrDefaultAsync(u => u.IdUser == userId.Value);

        if (user == null)
            return NotFound(new { message = "Utilisateur non trouvé" });

        var patient = user.Patient;

        return Ok(new
        {
            personalInfo = new PersonalInfoDto
            {
                DateNaissance = user.Naissance ?? DateTime.Today.AddYears(-25),
                Nationalite = user.Nationalite ?? "Cameroun",
                RegionOrigine = user.RegionOrigine ?? "",
                Ethnie = patient?.Ethnie,
                Sexe = user.Sexe ?? "",
                SituationMatrimoniale = user.SituationMatrimoniale,
                NbEnfants = patient?.NbEnfants
            },
            medicalInfo = new
            {
                GroupeSanguin = patient?.GroupeSanguin,
                Profession = patient?.Profession,
                MaladiesChroniques = patient?.MaladiesChroniques?.Split(", ").ToList() ?? new List<string>(),
                OperationsChirurgicales = patient?.OperationsChirurgicales ?? false,
                OperationsDetails = patient?.OperationsDetails,
                AllergiesConnues = patient?.AllergiesConnues ?? false,
                AllergiesDetails = patient?.AllergiesDetails,
                AntecedentsFamiliaux = patient?.AntecedentsFamiliaux ?? false,
                AntecedentsFamiliauxDetails = patient?.AntecedentsFamiliauxDetails
            },
            lifestyleInfo = new
            {
                ConsommationAlcool = patient?.ConsommationAlcool ?? false,
                FrequenceAlcool = patient?.FrequenceAlcool,
                Tabagisme = patient?.Tabagisme ?? false,
                ActivitePhysique = patient?.ActivitePhysique ?? false
            },
            emergencyContact = new
            {
                PersonneContact = patient?.PersonneContact ?? "",
                NumeroContact = patient?.NumeroContact ?? ""
            },
            user = new
            {
                user.IdUser,
                user.Nom,
                user.Prenom,
                user.Email,
                user.Telephone,
                user.ProfileCompleted,
                user.EmailConfirmed
            }
        });
    }

    /// <summary>
    /// Génère un numéro de dossier unique
    /// </summary>
    private string GenerateNumeroDossier()
    {
        var year = DateTime.Now.Year;
        var random = new Random();
        var number = random.Next(100000, 999999);
        return $"PAT-{year}-{number}";
    }
}
