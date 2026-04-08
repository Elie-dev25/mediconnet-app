using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Patient;
using Mediconnet_Backend.Core.Entities.Pharmacie;

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
                    .ThenInclude(p => p!.Assurance)
                .FirstOrDefaultAsync(u => u.IdUser == userId);

            if (utilisateur?.Patient == null)
                return null;

            var patient = utilisateur.Patient;

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
                NumeroDossier = patient.NumeroDossier,
                Ethnie = patient.Ethnie,
                GroupeSanguin = patient.GroupeSanguin,
                NbEnfants = patient.NbEnfants,
                PersonneContact = patient.PersonneContact,
                NumeroContact = patient.NumeroContact,
                Profession = patient.Profession,
                CreatedAt = utilisateur.CreatedAt,
                IsProfileComplete = IsProfileComplete(utilisateur),
                // Informations utilisateur étendues
                Nationalite = utilisateur.Nationalite,
                RegionOrigine = utilisateur.RegionOrigine,
                // Informations médicales
                MaladiesChroniques = patient.MaladiesChroniques,
                AllergiesConnues = patient.AllergiesConnues,
                AllergiesDetails = patient.AllergiesDetails,
                AntecedentsFamiliaux = patient.AntecedentsFamiliaux,
                AntecedentsFamiliauxDetails = patient.AntecedentsFamiliauxDetails,
                OperationsChirurgicales = patient.OperationsChirurgicales,
                OperationsDetails = patient.OperationsDetails,
                // Habitudes de vie
                ConsommationAlcool = patient.ConsommationAlcool,
                FrequenceAlcool = patient.FrequenceAlcool,
                Tabagisme = patient.Tabagisme,
                ActivitePhysique = patient.ActivitePhysique,
                // Assurance
                AssuranceId = patient.AssuranceId,
                NomAssurance = patient.Assurance?.Nom,
                NumeroCarteAssurance = patient.NumeroCarteAssurance,
                TauxCouvertureOverride = patient.TauxCouvertureOverride,
                DateDebutValidite = patient.DateDebutValidite,
                DateFinValidite = patient.DateFinValidite
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

            // Champs utilisateur obligatoires
            if (!utilisateur.Naissance.HasValue) missingFields.Add("naissance");
            if (string.IsNullOrEmpty(utilisateur.Sexe)) missingFields.Add("sexe");
            if (string.IsNullOrEmpty(utilisateur.Telephone)) missingFields.Add("telephone");
            if (string.IsNullOrEmpty(utilisateur.Adresse)) missingFields.Add("adresse");
            if (string.IsNullOrEmpty(utilisateur.SituationMatrimoniale)) missingFields.Add("situationMatrimoniale");
            if (string.IsNullOrEmpty(utilisateur.Nationalite)) missingFields.Add("nationalite");
            if (string.IsNullOrEmpty(utilisateur.RegionOrigine)) missingFields.Add("regionOrigine");

            if (utilisateur.Patient != null)
            {
                // Champs patient obligatoires
                if (string.IsNullOrEmpty(utilisateur.Patient.GroupeSanguin)) 
                    missingFields.Add("groupeSanguin");
                if (string.IsNullOrEmpty(utilisateur.Patient.Profession)) 
                    missingFields.Add("profession");
                if (string.IsNullOrEmpty(utilisateur.Patient.PersonneContact)) 
                    missingFields.Add("personneContact");
                if (string.IsNullOrEmpty(utilisateur.Patient.NumeroContact)) 
                    missingFields.Add("numeroContact");
                if (string.IsNullOrEmpty(utilisateur.Patient.Ethnie)) 
                    missingFields.Add("ethnie");
                if (!utilisateur.Patient.NbEnfants.HasValue) 
                    missingFields.Add("nbEnfants");
                
                // Champs conditionnels - si consommation alcool = true, fréquence requise
                if (utilisateur.Patient.ConsommationAlcool == true && 
                    string.IsNullOrEmpty(utilisateur.Patient.FrequenceAlcool))
                    missingFields.Add("frequenceAlcool");
                
                // Champs conditionnels - si allergies = true, détails requis
                if (utilisateur.Patient.AllergiesConnues == true && 
                    string.IsNullOrEmpty(utilisateur.Patient.AllergiesDetails))
                    missingFields.Add("allergiesDetails");
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
            if (!string.IsNullOrEmpty(request.Nationalite)) utilisateur.Nationalite = request.Nationalite;
            if (!string.IsNullOrEmpty(request.RegionOrigine)) utilisateur.RegionOrigine = request.RegionOrigine;

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
                if (!string.IsNullOrEmpty(request.FrequenceAlcool)) 
                    utilisateur.Patient.FrequenceAlcool = request.FrequenceAlcool;
                if (!string.IsNullOrEmpty(request.AllergiesDetails)) 
                    utilisateur.Patient.AllergiesDetails = request.AllergiesDetails;
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

        /// <summary>
        /// Récupère le dossier pharmaceutique complet du patient
        /// Inclut toutes les ordonnances avec leur statut de délivrance
        /// </summary>
        public async Task<DossierPharmaceutiqueDto?> GetDossierPharmaceutiqueAsync(int patientId, FiltreOrdonnancesPatientRequest? filtre = null)
        {
            try
            {
                // Vérifier que le patient existe
                var patient = await _context.Patients
                    .Include(p => p.Utilisateur)
                    .FirstOrDefaultAsync(p => p.IdUser == patientId);

                if (patient == null)
                {
                    _logger.LogWarning("Patient {PatientId} non trouvé pour le dossier pharmaceutique", patientId);
                    return null;
                }

                // Récupérer toutes les ordonnances du patient
                var query = _context.Ordonnances
                    .Include(o => o.Medecin)
                        .ThenInclude(m => m!.Utilisateur)
                    .Include(o => o.Medecin)
                        .ThenInclude(m => m!.Service)
                    .Include(o => o.Consultation)
                    .Include(o => o.Hospitalisation)
                        .ThenInclude(h => h!.Service)
                    .Include(o => o.Medicaments!)
                        .ThenInclude(m => m.Medicament)
                    .Where(o => o.IdPatient == patientId)
                    .AsQueryable();

                // Appliquer les filtres
                if (filtre != null)
                {
                    if (!string.IsNullOrEmpty(filtre.Statut))
                        query = query.Where(o => o.Statut == filtre.Statut);
                    
                    if (!string.IsNullOrEmpty(filtre.TypeContexte))
                        query = query.Where(o => o.TypeContexte == filtre.TypeContexte);
                    
                    if (filtre.DateDebut.HasValue)
                        query = query.Where(o => o.Date >= filtre.DateDebut.Value);
                    
                    if (filtre.DateFin.HasValue)
                        query = query.Where(o => o.Date <= filtre.DateFin.Value.AddDays(1));
                    
                    if (filtre.IdMedecin.HasValue)
                        query = query.Where(o => o.IdMedecin == filtre.IdMedecin.Value);
                }

                // Tri
                var tri = filtre?.Tri ?? "date_desc";
                query = tri switch
                {
                    "date_asc" => query.OrderBy(o => o.Date),
                    "medecin" => query.OrderBy(o => o.Medecin!.Utilisateur!.Nom),
                    _ => query.OrderByDescending(o => o.Date)
                };

                var ordonnances = await query.ToListAsync();

                // Récupérer les dispensations pour ces ordonnances
                var ordonnanceIds = ordonnances.Select(o => o.IdOrdonnance).ToList();
                var dispensations = await _context.Dispensations
                    .Include(d => d.Pharmacien)
                        .ThenInclude(p => p!.Utilisateur)
                    .Include(d => d.Lignes!)
                        .ThenInclude(l => l.Medicament)
                    .Where(d => d.IdPrescription.HasValue && ordonnanceIds.Contains(d.IdPrescription.Value))
                    .ToListAsync();

                // Mapper les ordonnances en DTOs
                var ordonnancesDto = ordonnances.Select(o => MapToOrdonnancePatientDto(o, dispensations)).ToList();

                // Calculer les statistiques
                var result = new DossierPharmaceutiqueDto
                {
                    IdPatient = patientId,
                    NomPatient = patient.Utilisateur != null 
                        ? $"{patient.Utilisateur.Prenom} {patient.Utilisateur.Nom}" 
                        : "Patient",
                    TotalOrdonnances = ordonnancesDto.Count,
                    OrdonnancesActives = ordonnancesDto.Count(o => o.Statut == "active" && !o.EstExpire),
                    OrdonnancesDelivrees = ordonnancesDto.Count(o => o.StatutDelivrance == "delivre"),
                    OrdonnancesPartielles = ordonnancesDto.Count(o => o.StatutDelivrance == "partiel"),
                    Ordonnances = ordonnancesDto
                };

                _logger.LogInformation("Dossier pharmaceutique récupéré pour patient {PatientId}: {Count} ordonnances", 
                    patientId, ordonnancesDto.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du dossier pharmaceutique pour patient {PatientId}", patientId);
                throw;
            }
        }

        /// <summary>
        /// Récupère une ordonnance spécifique pour le patient
        /// </summary>
        public async Task<OrdonnancePatientDto?> GetOrdonnancePatientAsync(int patientId, int ordonnanceId)
        {
            var ordonnance = await _context.Ordonnances
                .Include(o => o.Medecin)
                    .ThenInclude(m => m!.Utilisateur)
                .Include(o => o.Medecin)
                    .ThenInclude(m => m!.Service)
                .Include(o => o.Consultation)
                .Include(o => o.Hospitalisation)
                    .ThenInclude(h => h!.Service)
                .Include(o => o.Medicaments!)
                    .ThenInclude(m => m.Medicament)
                .FirstOrDefaultAsync(o => o.IdOrdonnance == ordonnanceId && o.IdPatient == patientId);

            if (ordonnance == null)
                return null;

            // Récupérer les dispensations
            var dispensations = await _context.Dispensations
                .Include(d => d.Pharmacien)
                    .ThenInclude(p => p!.Utilisateur)
                .Include(d => d.Lignes!)
                    .ThenInclude(l => l.Medicament)
                .Where(d => d.IdPrescription == ordonnanceId)
                .ToListAsync();

            return MapToOrdonnancePatientDto(ordonnance, dispensations);
        }

        private OrdonnancePatientDto MapToOrdonnancePatientDto(
            Core.Entities.Ordonnance ordonnance, 
            List<Dispensation> dispensations)
        {
            // Trouver les dispensations pour cette ordonnance
            var ordonnanceDispensations = dispensations
                .Where(d => d.IdPrescription == ordonnance.IdOrdonnance)
                .ToList();

            // Calculer le statut de délivrance global
            var statutDelivrance = "non_delivre";
            DateTime? dateDelivrance = null;
            string? nomPharmacien = null;

            if (ordonnanceDispensations.Any())
            {
                var dernierDispensation = ordonnanceDispensations
                    .OrderByDescending(d => d.DateDispensation)
                    .First();

                dateDelivrance = dernierDispensation.DateDispensation;
                nomPharmacien = dernierDispensation.Pharmacien?.Utilisateur != null
                    ? $"{dernierDispensation.Pharmacien.Utilisateur.Prenom} {dernierDispensation.Pharmacien.Utilisateur.Nom}"
                    : null;

                // Vérifier si tous les médicaments sont délivrés
                var totalPrescrit = ordonnance.Medicaments?.Sum(m => m.Quantite) ?? 0;
                var totalDelivre = ordonnanceDispensations
                    .SelectMany(d => d.Lignes ?? new List<DispensationLigne>())
                    .Sum(l => l.QuantiteDispensee);

                if (totalDelivre >= totalPrescrit)
                    statutDelivrance = "delivre";
                else if (totalDelivre > 0)
                    statutDelivrance = "partiel";
                else
                    statutDelivrance = "en_attente";
            }

            // Déterminer le service
            string? service = null;
            if (ordonnance.Hospitalisation?.Service != null)
                service = ordonnance.Hospitalisation.Service.NomService;
            else if (ordonnance.Medecin?.Service != null)
                service = ordonnance.Medecin.Service.NomService;

            // Déterminer le diagnostic
            string? diagnostic = null;
            if (ordonnance.Consultation != null)
                diagnostic = ordonnance.Consultation.Diagnostic;

            return new OrdonnancePatientDto
            {
                IdOrdonnance = ordonnance.IdOrdonnance,
                DatePrescription = ordonnance.Date,
                IdMedecin = ordonnance.IdMedecin ?? 0,
                NomMedecin = ordonnance.Medecin?.Utilisateur != null
                    ? $"Dr. {ordonnance.Medecin.Utilisateur.Prenom} {ordonnance.Medecin.Utilisateur.Nom}"
                    : "Médecin inconnu",
                SpecialiteMedecin = ordonnance.Medecin?.Specialite?.NomSpecialite,
                TypeContexte = ordonnance.TypeContexte ?? "consultation",
                Service = service,
                IdConsultation = ordonnance.IdConsultation,
                IdHospitalisation = ordonnance.IdHospitalisation,
                Diagnostic = diagnostic,
                Notes = ordonnance.Commentaire,
                Statut = ordonnance.Statut,
                StatutDelivrance = statutDelivrance,
                DateExpiration = ordonnance.DateExpiration,
                EstExpire = ordonnance.DateExpiration.HasValue && ordonnance.DateExpiration.Value < DateTime.UtcNow,
                Renouvelable = ordonnance.Renouvelable,
                NombreRenouvellements = ordonnance.NombreRenouvellements,
                RenouvellementRestants = ordonnance.RenouvellementRestants,
                DateDelivrance = dateDelivrance,
                NomPharmacien = nomPharmacien,
                Medicaments = ordonnance.Medicaments?.Select(m => MapToMedicamentOrdonnanceDto(m, ordonnanceDispensations)).ToList() 
                    ?? new List<MedicamentOrdonnanceDto>()
            };
        }

        private MedicamentOrdonnanceDto MapToMedicamentOrdonnanceDto(
            Core.Entities.PrescriptionMedicament med,
            List<Dispensation> dispensations)
        {
            // Calculer la quantité délivrée pour ce médicament
            var quantiteDelivree = dispensations
                .SelectMany(d => d.Lignes ?? new List<DispensationLigne>())
                .Where(l => l.IdMedicament == med.IdMedicament)
                .Sum(l => l.QuantiteDispensee);

            var statutDelivrance = "non_delivre";
            DateTime? dateDelivrance = null;

            if (quantiteDelivree >= med.Quantite)
            {
                statutDelivrance = "delivre";
                dateDelivrance = dispensations
                    .Where(d => d.Lignes?.Any(l => l.IdMedicament == med.IdMedicament) == true)
                    .OrderByDescending(d => d.DateDispensation)
                    .FirstOrDefault()?.DateDispensation;
            }
            else if (quantiteDelivree > 0)
            {
                statutDelivrance = "partiel";
                dateDelivrance = dispensations
                    .Where(d => d.Lignes?.Any(l => l.IdMedicament == med.IdMedicament) == true)
                    .OrderByDescending(d => d.DateDispensation)
                    .FirstOrDefault()?.DateDispensation;
            }

            return new MedicamentOrdonnanceDto
            {
                IdPrescriptionMed = med.IdPrescriptionMed,
                IdMedicament = med.IdMedicament,
                NomMedicament = med.EstHorsCatalogue 
                    ? med.NomMedicamentLibre ?? "Médicament non spécifié"
                    : med.Medicament?.Nom ?? med.NomMedicamentLibre ?? "Médicament inconnu",
                Dosage = med.EstHorsCatalogue ? med.DosageLibre : med.Medicament?.Dosage ?? med.DosageLibre,
                FormePharmaceutique = med.FormePharmaceutique ?? med.Medicament?.FormeGalenique,
                VoieAdministration = med.VoieAdministration,
                EstHorsCatalogue = med.EstHorsCatalogue,
                QuantitePrescrite = med.Quantite,
                Posologie = med.Posologie,
                Frequence = med.Frequence,
                DureeTraitement = med.DureeTraitement,
                Instructions = med.Instructions,
                QuantiteDelivree = quantiteDelivree,
                StatutDelivrance = statutDelivrance,
                DateDelivrance = dateDelivrance
            };
        }
    }
}
