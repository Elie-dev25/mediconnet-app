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
