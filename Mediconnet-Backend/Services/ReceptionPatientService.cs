using System.Security.Cryptography;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Accueil;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service pour la gestion des patients créés par l'accueil
/// </summary>
public class ReceptionPatientService : IReceptionPatientService
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuditService _auditService;
    private readonly IPasswordValidationService _passwordValidationService;
    private readonly ILogger<ReceptionPatientService> _logger;

    public ReceptionPatientService(
        ApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IAuditService auditService,
        IPasswordValidationService passwordValidationService,
        ILogger<ReceptionPatientService> logger)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _auditService = auditService;
        _passwordValidationService = passwordValidationService;
        _logger = logger;
    }

    /// <summary>
    /// Génère un mot de passe temporaire sécurisé
    /// Format: 3 lettres majuscules + 4 chiffres + 1 caractère spécial
    /// Exemple: ABC1234!
    /// </summary>
    private string GenerateTemporaryPassword()
    {
        const string upperCase = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // Exclus I et O pour éviter confusion
        const string digits = "23456789"; // Exclus 0 et 1 pour éviter confusion
        const string special = "!@#$%&*";
        
        var password = new char[8];
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[8];
        rng.GetBytes(bytes);
        
        // 3 lettres majuscules
        for (int i = 0; i < 3; i++)
        {
            password[i] = upperCase[bytes[i] % upperCase.Length];
        }
        
        // 4 chiffres
        for (int i = 3; i < 7; i++)
        {
            password[i] = digits[bytes[i] % digits.Length];
        }
        
        // 1 caractère spécial
        password[7] = special[bytes[7] % special.Length];
        
        return new string(password);
    }

    /// <summary>
    /// Génère un numéro de dossier unique
    /// Format: PAT-YYYYMM-XXXXX
    /// </summary>
    private async Task<string> GenerateNumeroDossierAsync()
    {
        var prefix = $"PAT-{DateTime.UtcNow:yyyyMM}-";
        
        // Trouver le dernier numéro pour ce mois
        var lastPatient = await _context.Patients
            .Where(p => p.NumeroDossier != null && p.NumeroDossier.StartsWith(prefix))
            .OrderByDescending(p => p.NumeroDossier)
            .FirstOrDefaultAsync();
        
        int nextNumber = 1;
        if (lastPatient?.NumeroDossier != null)
        {
            var lastNumberStr = lastPatient.NumeroDossier.Substring(prefix.Length);
            if (int.TryParse(lastNumberStr, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }
        
        return $"{prefix}{nextNumber:D5}";
    }

    public async Task<CreatePatientByReceptionResponse> CreatePatientAsync(CreatePatientByReceptionRequest request, int createdByUserId)
    {
        try
        {
            // Vérifier si le téléphone existe déjà
            var existingByPhone = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.Telephone == request.Telephone);
            
            if (existingByPhone != null)
            {
                return new CreatePatientByReceptionResponse
                {
                    Success = false,
                    Message = "Un utilisateur avec ce numéro de téléphone existe déjà"
                };
            }
            
            // Vérifier si l'email existe déjà (si fourni)
            if (!string.IsNullOrEmpty(request.Email))
            {
                var existingByEmail = await _context.Utilisateurs
                    .FirstOrDefaultAsync(u => u.Email == request.Email);
                
                if (existingByEmail != null)
                {
                    return new CreatePatientByReceptionResponse
                    {
                        Success = false,
                        Message = "Un utilisateur avec cet email existe déjà"
                    };
                }
            }
            
            // Générer le mot de passe temporaire
            var temporaryPassword = GenerateTemporaryPassword();
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
            
            // Générer le numéro de dossier
            var numeroDossier = await GenerateNumeroDossierAsync();
            
            // Créer l'utilisateur
            var utilisateur = new Utilisateur
            {
                Nom = request.Nom.Trim(),
                Prenom = request.Prenom.Trim(),
                Naissance = request.DateNaissance,
                Sexe = request.Sexe,
                Telephone = request.Telephone,
                Email = request.Email?.Trim() ?? $"{request.Telephone}@temp.mediconnet.local",
                SituationMatrimoniale = request.SituationMatrimoniale,
                Adresse = request.Adresse,
                Nationalite = request.Nationalite ?? "Cameroun",
                RegionOrigine = request.RegionOrigine,
                Role = "patient",
                PasswordHash = passwordHash,
                EmailConfirmed = true, // Créé par l'accueil, donc validé
                ProfileCompleted = true, // Profil complété par l'accueil
                ProfileCompletedAt = DateTime.UtcNow,
                MustChangePassword = true, // Doit changer son mot de passe
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _context.Utilisateurs.Add(utilisateur);
            await _context.SaveChangesAsync();
            
            // Créer le patient
            var patient = new Patient
            {
                IdUser = utilisateur.IdUser,
                NumeroDossier = numeroDossier,
                Ethnie = request.Ethnie,
                GroupeSanguin = request.GroupeSanguin,
                Profession = request.Profession,
                MaladiesChroniques = request.MaladiesChroniques,
                OperationsChirurgicales = request.OperationsChirurgicales,
                OperationsDetails = request.OperationsDetails,
                AllergiesConnues = request.AllergiesConnues,
                AllergiesDetails = request.AllergiesDetails,
                AntecedentsFamiliaux = request.AntecedentsFamiliaux,
                AntecedentsFamiliauxDetails = request.AntecedentsFamiliauxDetails,
                ConsommationAlcool = request.ConsommationAlcool,
                FrequenceAlcool = request.FrequenceAlcool,
                Tabagisme = request.Tabagisme,
                ActivitePhysique = request.ActivitePhysique,
                NbEnfants = request.NbEnfants,
                PersonneContact = request.PersonneContact,
                NumeroContact = request.NumeroContact,
                DeclarationHonneurAcceptee = false, // Le patient doit accepter lui-même
                DateCreation = DateTime.UtcNow,
                // Assurance
                AssuranceId = request.AssuranceId,
                NumeroCarteAssurance = request.NumeroCarteAssurance,
                DateDebutValidite = request.DateDebutValidite,
                DateFinValidite = request.DateFinValidite,
                TauxCouvertureOverride = request.TauxCouvertureOverride
            };
            
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();
            
            // Logger l'action
            await _auditService.LogActionAsync(
                createdByUserId,
                "PATIENT_CREATED_BY_RECEPTION",
                "Patient",
                patient.IdUser,
                $"Patient {utilisateur.Nom} {utilisateur.Prenom} créé avec dossier {numeroDossier}"
            );
            
            _logger.LogInformation("Patient créé par accueil: {NumeroDossier} - {Nom} {Prenom}", numeroDossier, utilisateur.Nom, utilisateur.Prenom);
            
            // Construire les instructions de connexion
            var loginIdentifier = request.Telephone;
            var loginInstructions = $@"
=== INSTRUCTIONS DE PREMIÈRE CONNEXION ===

Bonjour {request.Prenom} {request.Nom},

Votre compte MediConnect a été créé.

📱 Identifiant de connexion : {loginIdentifier}
🔐 Mot de passe temporaire : {temporaryPassword}

Lors de votre première connexion :
1. Vérifiez vos informations personnelles
2. Acceptez la déclaration sur l'honneur
3. Changez votre mot de passe

Numéro de dossier : {numeroDossier}

===========================================";
            
            return new CreatePatientByReceptionResponse
            {
                Success = true,
                Message = "Patient créé avec succès",
                IdUser = utilisateur.IdUser,
                NumeroDossier = numeroDossier,
                TemporaryPassword = temporaryPassword,
                LoginIdentifier = loginIdentifier,
                LoginInstructions = loginInstructions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création du patient par l'accueil");
            return new CreatePatientByReceptionResponse
            {
                Success = false,
                Message = "Une erreur est survenue lors de la création du patient"
            };
        }
    }

    public async Task<FirstLoginPatientInfoResponse> GetFirstLoginInfoAsync(int userId)
    {
        try
        {
            var utilisateur = await _context.Utilisateurs
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.IdUser == userId);
            
            if (utilisateur == null)
            {
                return new FirstLoginPatientInfoResponse
                {
                    Success = false,
                    Message = "Utilisateur non trouvé"
                };
            }
            
            var patient = utilisateur.Patient;
            
            return new FirstLoginPatientInfoResponse
            {
                Success = true,
                Message = "Informations récupérées",
                
                // Informations personnelles
                Nom = utilisateur.Nom,
                Prenom = utilisateur.Prenom,
                DateNaissance = utilisateur.Naissance,
                Sexe = utilisateur.Sexe,
                Telephone = utilisateur.Telephone,
                Email = utilisateur.Email?.Contains("@temp.mediconnet.local") == true ? null : utilisateur.Email,
                SituationMatrimoniale = utilisateur.SituationMatrimoniale,
                Adresse = utilisateur.Adresse,
                Nationalite = utilisateur.Nationalite,
                RegionOrigine = utilisateur.RegionOrigine,
                Ethnie = patient?.Ethnie,
                Profession = patient?.Profession,
                
                // Informations médicales
                GroupeSanguin = patient?.GroupeSanguin,
                MaladiesChroniques = patient?.MaladiesChroniques,
                OperationsChirurgicales = patient?.OperationsChirurgicales,
                OperationsDetails = patient?.OperationsDetails,
                AllergiesConnues = patient?.AllergiesConnues,
                AllergiesDetails = patient?.AllergiesDetails,
                AntecedentsFamiliaux = patient?.AntecedentsFamiliaux,
                AntecedentsFamiliauxDetails = patient?.AntecedentsFamiliauxDetails,
                
                // Habitudes de vie
                ConsommationAlcool = patient?.ConsommationAlcool,
                FrequenceAlcool = patient?.FrequenceAlcool,
                Tabagisme = patient?.Tabagisme,
                ActivitePhysique = patient?.ActivitePhysique,
                
                // Contacts d'urgence
                NbEnfants = patient?.NbEnfants,
                PersonneContact = patient?.PersonneContact,
                NumeroContact = patient?.NumeroContact,
                
                // Numéro de dossier
                NumeroDossier = patient?.NumeroDossier,
                
                // Statuts
                MustChangePassword = utilisateur.MustChangePassword,
                DeclarationHonneurAcceptee = patient?.DeclarationHonneurAcceptee ?? false,
                ProfileCompleted = utilisateur.ProfileCompleted
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des informations de première connexion");
            return new FirstLoginPatientInfoResponse
            {
                Success = false,
                Message = "Une erreur est survenue"
            };
        }
    }

    public async Task<FirstLoginValidationResponse> ValidateFirstLoginAsync(int userId, FirstLoginValidationRequest request)
    {
        try
        {
            // Vérifier la déclaration sur l'honneur
            if (!request.DeclarationHonneurAcceptee)
            {
                return new FirstLoginValidationResponse
                {
                    Success = false,
                    Message = "Vous devez accepter la déclaration sur l'honneur"
                };
            }
            
            // Vérifier que les mots de passe correspondent
            if (request.NewPassword != request.ConfirmPassword)
            {
                return new FirstLoginValidationResponse
                {
                    Success = false,
                    Message = "Les mots de passe ne correspondent pas"
                };
            }
            
            // Valider la robustesse du mot de passe
            var passwordValidation = _passwordValidationService.ValidatePassword(request.NewPassword);
            if (!passwordValidation.IsValid)
            {
                return new FirstLoginValidationResponse
                {
                    Success = false,
                    Message = passwordValidation.Errors.FirstOrDefault() ?? "Mot de passe trop faible"
                };
            }
            
            // Récupérer l'utilisateur et le patient
            var utilisateur = await _context.Utilisateurs
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.IdUser == userId);
            
            if (utilisateur == null)
            {
                return new FirstLoginValidationResponse
                {
                    Success = false,
                    Message = "Utilisateur non trouvé"
                };
            }
            
            // Mettre à jour le mot de passe
            utilisateur.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            utilisateur.MustChangePassword = false;
            utilisateur.UpdatedAt = DateTime.UtcNow;
            
            // Mettre à jour la déclaration sur l'honneur
            if (utilisateur.Patient != null)
            {
                utilisateur.Patient.DeclarationHonneurAcceptee = true;
                utilisateur.Patient.DeclarationHonneurAt = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
            
            // Générer un nouveau token
            var token = await _jwtTokenService.GenerateTokenAsync(userId, utilisateur.Role);
            
            // Logger l'action
            await _auditService.LogActionAsync(
                userId,
                "FIRST_LOGIN_COMPLETED",
                "Utilisateur",
                userId,
                "Première connexion validée, mot de passe changé, déclaration acceptée"
            );
            
            _logger.LogInformation("Première connexion validée pour l'utilisateur {UserId}", userId);
            
            return new FirstLoginValidationResponse
            {
                Success = true,
                Message = "Première connexion validée avec succès",
                Token = token,
                ExpiresIn = 3600
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la validation de première connexion");
            return new FirstLoginValidationResponse
            {
                Success = false,
                Message = "Une erreur est survenue lors de la validation"
            };
        }
    }

    public async Task<AcceptDeclarationResponse> AcceptDeclarationAsync(int userId, AcceptDeclarationRequest request)
    {
        try
        {
            if (!request.DeclarationHonneurAcceptee)
            {
                return new AcceptDeclarationResponse
                {
                    Success = false,
                    Message = "Vous devez accepter la déclaration sur l'honneur"
                };
            }

            var utilisateur = await _context.Utilisateurs
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.IdUser == userId);

            if (utilisateur == null)
            {
                return new AcceptDeclarationResponse
                {
                    Success = false,
                    Message = "Utilisateur non trouvé"
                };
            }

            if (utilisateur.Patient == null)
            {
                return new AcceptDeclarationResponse
                {
                    Success = false,
                    Message = "Dossier patient non trouvé"
                };
            }

            // Mettre à jour la déclaration
            utilisateur.Patient.DeclarationHonneurAcceptee = true;
            utilisateur.Patient.DeclarationHonneurAt = DateTime.UtcNow;
            utilisateur.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Logger l'action
            await _auditService.LogActionAsync(
                userId,
                "DECLARATION_ACCEPTED",
                "Patient",
                userId,
                "Déclaration sur l'honneur acceptée"
            );

            _logger.LogInformation("Déclaration acceptée pour l'utilisateur {UserId}", userId);

            return new AcceptDeclarationResponse
            {
                Success = true,
                Message = "Déclaration acceptée avec succès"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'acceptation de la déclaration");
            return new AcceptDeclarationResponse
            {
                Success = false,
                Message = "Une erreur est survenue"
            };
        }
    }

    public async Task<bool> RequiresFirstLoginValidationAsync(int userId)
    {
        var utilisateur = await _context.Utilisateurs
            .Include(u => u.Patient)
            .FirstOrDefaultAsync(u => u.IdUser == userId);
        
        if (utilisateur == null || utilisateur.Role != "patient")
        {
            return false;
        }
        
        // Nécessite validation si:
        // - Doit changer son mot de passe
        // - OU n'a pas accepté la déclaration sur l'honneur
        return utilisateur.MustChangePassword || 
               (utilisateur.Patient != null && !utilisateur.Patient.DeclarationHonneurAcceptee);
    }
}
