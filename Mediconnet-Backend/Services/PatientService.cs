using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Patient;

namespace Mediconnet_Backend.Services
{
    /// <summary>
    /// Service pour la gestion des patients
    /// </summary>
    public class PatientService : IPatientService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PatientService> _logger;

        public PatientService(
            ApplicationDbContext context,
            ILogger<PatientService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<PatientProfileDto?> GetProfileAsync(int userId)
        {
            var utilisateur = await _context.Utilisateurs
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.IdUser == userId);

            if (utilisateur?.Patient == null)
                return null;

            return new PatientProfileDto
            {
                IdUser = utilisateur.IdUser,
                Nom = utilisateur.Nom,
                Prenom = utilisateur.Prenom,
                Email = utilisateur.Email,
                Naissance = utilisateur.Naissance,
                Sexe = utilisateur.Sexe,
                Telephone = utilisateur.Telephone,
                SituationMatrimoniale = utilisateur.SituationMatrimoniale,
                Adresse = utilisateur.Adresse,
                Photo = utilisateur.Photo,
                NumeroDossier = utilisateur.Patient.NumeroDossier,
                Ethnie = utilisateur.Patient.Ethnie,
                GroupeSanguin = utilisateur.Patient.GroupeSanguin,
                NbEnfants = utilisateur.Patient.NbEnfants,
                PersonneContact = utilisateur.Patient.PersonneContact,
                NumeroContact = utilisateur.Patient.NumeroContact,
                Profession = utilisateur.Patient.Profession,
                CreatedAt = utilisateur.CreatedAt,
                IsProfileComplete = IsProfileComplete(utilisateur)
            };
        }

        /// <inheritdoc />
        public async Task<ProfileStatusDto> GetProfileStatusAsync(int userId)
        {
            var utilisateur = await _context.Utilisateurs
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.IdUser == userId);

            var missingFields = new List<string>();

            if (utilisateur == null)
            {
                return new ProfileStatusDto
                {
                    IsComplete = false,
                    MissingFields = new List<string> { "Utilisateur non trouvé" },
                    Message = "Utilisateur non trouvé"
                };
            }

            if (!utilisateur.Naissance.HasValue) missingFields.Add("Date de naissance");
            if (string.IsNullOrEmpty(utilisateur.Sexe)) missingFields.Add("Sexe");
            if (string.IsNullOrEmpty(utilisateur.Telephone)) missingFields.Add("Telephone");
            if (string.IsNullOrEmpty(utilisateur.Adresse)) missingFields.Add("Adresse");

            if (utilisateur.Patient != null)
            {
                if (string.IsNullOrEmpty(utilisateur.Patient.PersonneContact)) 
                    missingFields.Add("Personne a contacter");
                if (string.IsNullOrEmpty(utilisateur.Patient.NumeroContact)) 
                    missingFields.Add("Numero de contact d'urgence");
            }

            return new ProfileStatusDto
            {
                IsComplete = missingFields.Count == 0,
                MissingFields = missingFields,
                Message = missingFields.Count == 0
                    ? "Votre profil est complet"
                    : $"Il manque {missingFields.Count} information(s) dans votre profil"
            };
        }

        /// <inheritdoc />
        public async Task<bool> UpdateProfileAsync(int userId, UpdatePatientProfileRequest request)
        {
            var utilisateur = await _context.Utilisateurs
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.IdUser == userId);

            if (utilisateur == null)
                return false;

            // Mise a jour des informations utilisateur
            if (request.Naissance.HasValue) utilisateur.Naissance = request.Naissance;
            if (!string.IsNullOrEmpty(request.Sexe)) utilisateur.Sexe = request.Sexe;
            if (!string.IsNullOrEmpty(request.Telephone)) utilisateur.Telephone = request.Telephone;
            if (!string.IsNullOrEmpty(request.SituationMatrimoniale)) 
                utilisateur.SituationMatrimoniale = request.SituationMatrimoniale;
            if (!string.IsNullOrEmpty(request.Adresse)) utilisateur.Adresse = request.Adresse;

            utilisateur.UpdatedAt = DateTime.UtcNow;

            // Mise a jour des informations patient
            if (utilisateur.Patient != null)
            {
                if (!string.IsNullOrEmpty(request.Ethnie)) 
                    utilisateur.Patient.Ethnie = request.Ethnie;
                if (!string.IsNullOrEmpty(request.GroupeSanguin)) 
                    utilisateur.Patient.GroupeSanguin = request.GroupeSanguin;
                if (request.NbEnfants.HasValue) 
                    utilisateur.Patient.NbEnfants = request.NbEnfants;
                if (!string.IsNullOrEmpty(request.PersonneContact)) 
                    utilisateur.Patient.PersonneContact = request.PersonneContact;
                if (!string.IsNullOrEmpty(request.NumeroContact)) 
                    utilisateur.Patient.NumeroContact = request.NumeroContact;
                if (!string.IsNullOrEmpty(request.Profession)) 
                    utilisateur.Patient.Profession = request.Profession;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Profile updated for patient {UserId}", userId);
            return true;
        }

        private static bool IsProfileComplete(Core.Entities.Utilisateur utilisateur)
        {
            if (!utilisateur.Naissance.HasValue) return false;
            if (string.IsNullOrEmpty(utilisateur.Sexe)) return false;
            if (string.IsNullOrEmpty(utilisateur.Telephone)) return false;
            if (string.IsNullOrEmpty(utilisateur.Adresse)) return false;
            if (utilisateur.Patient == null) return false;
            if (string.IsNullOrEmpty(utilisateur.Patient.PersonneContact)) return false;
            if (string.IsNullOrEmpty(utilisateur.Patient.NumeroContact)) return false;
            return true;
        }

        /// <summary>
        /// Récupère les N patients les plus récemment enregistrés
        /// </summary>
        public async Task<RecentPatientsResponse> GetRecentPatientsAsync(int count = 6)
        {
            try
            {
                // Limiter le nombre de résultats pour éviter les abus
                if (count <= 0 || count > 100)
                {
                    count = 6;
                }

                var patients = await _context.Patients
                    .Include(p => p.Utilisateur)
                    .OrderByDescending(p => p.Utilisateur!.CreatedAt)
                    .Take(count)
                    .Select(p => new PatientBasicInfoDto
                    {
                        IdUser = p.IdUser,
                        NumeroDossier = p.NumeroDossier ?? string.Empty,
                        Nom = p.Utilisateur!.Nom,
                        Prenom = p.Utilisateur.Prenom,
                        Email = p.Utilisateur.Email,
                        Telephone = p.Utilisateur.Telephone ?? string.Empty,
                        DateNaissance = p.Utilisateur.Naissance,
                        Sexe = p.Utilisateur.Sexe,
                        GroupeSanguin = p.GroupeSanguin,
                        CreatedAt = p.Utilisateur.CreatedAt ?? DateTime.UtcNow
                    })
                    .ToListAsync();

                _logger.LogInformation($"Récupération de {patients.Count} patients récents");

                return new RecentPatientsResponse
                {
                    Success = true,
                    Message = $"{patients.Count} patient(s) récent(s) récupéré(s)",
                    Patients = patients
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des patients récents");
                return new RecentPatientsResponse
                {
                    Success = false,
                    Message = "Erreur lors de la récupération des patients récents",
                    Patients = new List<PatientBasicInfoDto>()
                };
            }
        }

        /// <summary>
        /// Recherche des patients par numéro de dossier, nom ou email
        /// </summary>
        public async Task<PatientSearchResponse> SearchPatientsAsync(PatientSearchRequest request)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    return new PatientSearchResponse
                    {
                        Success = false,
                        Message = "Le terme de recherche ne peut pas être vide",
                        Patients = new List<PatientBasicInfoDto>(),
                        TotalCount = 0
                    };
                }

                // Limiter le nombre de résultats
                var limit = request.Limit > 0 && request.Limit <= 100 ? request.Limit : 50;

                var searchTerm = request.SearchTerm.Trim().ToLower();

                // Recherche dans la base de données
                var query = _context.Patients
                    .Include(p => p.Utilisateur)
                    .AsQueryable();

                // Appliquer les filtres de recherche
                query = query.Where(p => p.Utilisateur != null);

                query = query.Where(p =>
                    ((p.NumeroDossier ?? string.Empty).ToLower().Contains(searchTerm)) ||
                    ((p.Utilisateur!.Nom ?? string.Empty).ToLower().Contains(searchTerm)) ||
                    ((p.Utilisateur.Prenom ?? string.Empty).ToLower().Contains(searchTerm)) ||
                    ((p.Utilisateur.Email ?? string.Empty).ToLower().Contains(searchTerm))
                );

                // Compter le total avant la limitation
                var totalCount = await query.CountAsync();

                // Récupérer les résultats avec limitation
                var patients = await query
                    .OrderByDescending(p => p.Utilisateur!.CreatedAt)
                    .Take(limit)
                    .Select(p => new PatientBasicInfoDto
                    {
                        IdUser = p.IdUser,
                        NumeroDossier = p.NumeroDossier ?? string.Empty,
                        Nom = p.Utilisateur!.Nom,
                        Prenom = p.Utilisateur.Prenom,
                        Email = p.Utilisateur.Email,
                        Telephone = p.Utilisateur.Telephone ?? string.Empty,
                        DateNaissance = p.Utilisateur.Naissance,
                        Sexe = p.Utilisateur.Sexe,
                        GroupeSanguin = p.GroupeSanguin,
                        CreatedAt = p.Utilisateur.CreatedAt ?? DateTime.UtcNow
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    $"Recherche de patients avec le terme '{request.SearchTerm}': {patients.Count} résultat(s) sur {totalCount}");

                return new PatientSearchResponse
                {
                    Success = true,
                    Message = $"{patients.Count} patient(s) trouvé(s)",
                    Patients = patients,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la recherche de patients avec le terme '{request.SearchTerm}'");
                return new PatientSearchResponse
                {
                    Success = false,
                    Message = "Erreur lors de la recherche de patients",
                    Patients = new List<PatientBasicInfoDto>(),
                    TotalCount = 0
                };
            }
        }
    }
}
