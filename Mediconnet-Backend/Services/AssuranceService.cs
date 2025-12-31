using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Assurance;

namespace Mediconnet_Backend.Services;

public class AssuranceService : IAssuranceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AssuranceService> _logger;

    public AssuranceService(ApplicationDbContext context, ILogger<AssuranceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ==================== ASSURANCES ====================

    public async Task<AssuranceListResponse> GetAssurancesAsync(AssuranceFilterDto? filter = null)
    {
        var query = _context.Assurances.Include(a => a.Patients).AsQueryable();

        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.TypeAssurance))
                query = query.Where(a => a.TypeAssurance == filter.TypeAssurance);

            if (!string.IsNullOrEmpty(filter.ZoneCouverture))
                query = query.Where(a => a.ZoneCouverture == filter.ZoneCouverture);

            if (filter.IsActive.HasValue)
                query = query.Where(a => a.IsActive == filter.IsActive.Value);

            if (!string.IsNullOrEmpty(filter.Recherche))
            {
                var search = filter.Recherche.ToLower();
                query = query.Where(a => 
                    a.Nom.ToLower().Contains(search) ||
                    (a.Groupe != null && a.Groupe.ToLower().Contains(search)) ||
                    (a.Description != null && a.Description.ToLower().Contains(search)));
            }
        }

        var total = await query.CountAsync();
        var totalActives = await query.CountAsync(a => a.IsActive);

        var page = filter?.Page ?? 1;
        var pageSize = filter?.PageSize ?? 20;
        
        var assurances = await query
            .OrderBy(a => a.Nom)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AssuranceListDto
            {
                IdAssurance = a.IdAssurance,
                Nom = a.Nom,
                TypeAssurance = a.TypeAssurance,
                Groupe = a.Groupe,
                ZoneCouverture = a.ZoneCouverture,
                IsActive = a.IsActive,
                NombrePatientsAssures = a.Patients.Count
            })
            .ToListAsync();

        return new AssuranceListResponse
        {
            Success = true,
            Data = assurances,
            Total = total,
            TotalActives = totalActives
        };
    }

    public async Task<List<AssuranceListDto>> GetAssurancesActivesAsync()
    {
        return await _context.Assurances
            .Where(a => a.IsActive)
            .OrderBy(a => a.Nom)
            .Select(a => new AssuranceListDto
            {
                IdAssurance = a.IdAssurance,
                Nom = a.Nom,
                TypeAssurance = a.TypeAssurance,
                Groupe = a.Groupe,
                ZoneCouverture = a.ZoneCouverture,
                IsActive = a.IsActive,
                NombrePatientsAssures = 0
            })
            .ToListAsync();
    }

    public async Task<AssuranceDetailDto?> GetAssuranceByIdAsync(int idAssurance)
    {
        var assurance = await _context.Assurances
            .Include(a => a.Patients)
            .FirstOrDefaultAsync(a => a.IdAssurance == idAssurance);

        if (assurance == null) return null;

        return new AssuranceDetailDto
        {
            IdAssurance = assurance.IdAssurance,
            Nom = assurance.Nom,
            TypeAssurance = assurance.TypeAssurance,
            SiteWeb = assurance.SiteWeb,
            TelephoneServiceClient = assurance.TelephoneServiceClient,
            Groupe = assurance.Groupe,
            PaysOrigine = assurance.PaysOrigine,
            StatutJuridique = assurance.StatutJuridique,
            Description = assurance.Description,
            TypeCouverture = assurance.TypeCouverture,
            IsComplementaire = assurance.IsComplementaire,
            CategorieBeneficiaires = assurance.CategorieBeneficiaires,
            ConditionsAdhesion = assurance.ConditionsAdhesion,
            ZoneCouverture = assurance.ZoneCouverture,
            ModePaiement = assurance.ModePaiement,
            IsActive = assurance.IsActive,
            CreatedAt = assurance.CreatedAt,
            UpdatedAt = assurance.UpdatedAt,
            NombrePatientsAssures = assurance.Patients.Count
        };
    }

    public async Task<AssuranceResponse> CreateAssuranceAsync(CreateAssuranceDto dto)
    {
        try
        {
            var exists = await _context.Assurances.AnyAsync(a => a.Nom.ToLower() == dto.Nom.ToLower());
            if (exists)
            {
                return new AssuranceResponse { Success = false, Message = "Une assurance avec ce nom existe déjà" };
            }

            var assurance = new Assurance
            {
                Nom = dto.Nom,
                TypeAssurance = dto.TypeAssurance,
                SiteWeb = dto.SiteWeb,
                TelephoneServiceClient = dto.TelephoneServiceClient,
                Groupe = dto.Groupe,
                PaysOrigine = dto.PaysOrigine,
                StatutJuridique = dto.StatutJuridique,
                Description = dto.Description,
                TypeCouverture = dto.TypeCouverture,
                IsComplementaire = dto.IsComplementaire,
                CategorieBeneficiaires = dto.CategorieBeneficiaires,
                ConditionsAdhesion = dto.ConditionsAdhesion,
                ZoneCouverture = dto.ZoneCouverture,
                ModePaiement = dto.ModePaiement,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Assurances.Add(assurance);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Assurance créée: {assurance.Nom} (ID: {assurance.IdAssurance})");

            return new AssuranceResponse
            {
                Success = true,
                Message = "Assurance créée avec succès",
                Data = await GetAssuranceByIdAsync(assurance.IdAssurance)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de l'assurance");
            return new AssuranceResponse { Success = false, Message = "Erreur lors de la création de l'assurance" };
        }
    }

    public async Task<AssuranceResponse> UpdateAssuranceAsync(int idAssurance, UpdateAssuranceDto dto)
    {
        try
        {
            var assurance = await _context.Assurances.FindAsync(idAssurance);
            if (assurance == null)
            {
                return new AssuranceResponse { Success = false, Message = "Assurance non trouvée" };
            }

            if (assurance.Nom.ToLower() != dto.Nom.ToLower())
            {
                var exists = await _context.Assurances.AnyAsync(a => a.Nom.ToLower() == dto.Nom.ToLower() && a.IdAssurance != idAssurance);
                if (exists)
                {
                    return new AssuranceResponse { Success = false, Message = "Une assurance avec ce nom existe déjà" };
                }
            }

            assurance.Nom = dto.Nom;
            assurance.TypeAssurance = dto.TypeAssurance;
            assurance.SiteWeb = dto.SiteWeb;
            assurance.TelephoneServiceClient = dto.TelephoneServiceClient;
            assurance.Groupe = dto.Groupe;
            assurance.PaysOrigine = dto.PaysOrigine;
            assurance.StatutJuridique = dto.StatutJuridique;
            assurance.Description = dto.Description;
            assurance.TypeCouverture = dto.TypeCouverture;
            assurance.IsComplementaire = dto.IsComplementaire;
            assurance.CategorieBeneficiaires = dto.CategorieBeneficiaires;
            assurance.ConditionsAdhesion = dto.ConditionsAdhesion;
            assurance.ZoneCouverture = dto.ZoneCouverture;
            assurance.ModePaiement = dto.ModePaiement;
            assurance.IsActive = dto.IsActive;
            assurance.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Assurance mise à jour: {assurance.Nom} (ID: {assurance.IdAssurance})");

            return new AssuranceResponse
            {
                Success = true,
                Message = "Assurance mise à jour avec succès",
                Data = await GetAssuranceByIdAsync(assurance.IdAssurance)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour de l'assurance");
            return new AssuranceResponse { Success = false, Message = "Erreur lors de la mise à jour de l'assurance" };
        }
    }

    public async Task<AssuranceResponse> ToggleAssuranceStatusAsync(int idAssurance)
    {
        var assurance = await _context.Assurances.FindAsync(idAssurance);
        if (assurance == null)
        {
            return new AssuranceResponse { Success = false, Message = "Assurance non trouvée" };
        }

        assurance.IsActive = !assurance.IsActive;
        assurance.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new AssuranceResponse
        {
            Success = true,
            Message = $"Assurance {(assurance.IsActive ? "activée" : "désactivée")} avec succès",
            Data = await GetAssuranceByIdAsync(assurance.IdAssurance)
        };
    }

    public async Task<AssuranceResponse> DeleteAssuranceAsync(int idAssurance)
    {
        var assurance = await _context.Assurances
            .Include(a => a.Patients)
            .FirstOrDefaultAsync(a => a.IdAssurance == idAssurance);

        if (assurance == null)
        {
            return new AssuranceResponse { Success = false, Message = "Assurance non trouvée" };
        }

        if (assurance.Patients.Any())
        {
            return new AssuranceResponse
            {
                Success = false,
                Message = "Impossible de supprimer une assurance liée à des patients. Désactivez-la plutôt."
            };
        }

        _context.Assurances.Remove(assurance);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Assurance supprimée: {assurance.Nom} (ID: {idAssurance})");

        return new AssuranceResponse { Success = true, Message = "Assurance supprimée avec succès" };
    }

    // ==================== PATIENT ASSURANCE ====================

    public async Task<PatientAssuranceInfoDto?> GetPatientAssuranceAsync(int idPatient)
    {
        var patient = await _context.Patients
            .Include(p => p.Assurance)
            .FirstOrDefaultAsync(p => p.IdUser == idPatient);

        if (patient == null) return null;

        var estValide = patient.AssuranceId.HasValue &&
                        (!patient.DateFinValidite.HasValue || patient.DateFinValidite.Value >= DateTime.UtcNow);

        return new PatientAssuranceInfoDto
        {
            EstAssure = patient.AssuranceId.HasValue,
            AssuranceId = patient.AssuranceId,
            NomAssurance = patient.Assurance?.Nom,
            TypeAssurance = patient.Assurance?.TypeAssurance,
            CouvertureAssurance = patient.CouvertureAssurance,
            NumeroCarteAssurance = patient.NumeroCarteAssurance,
            DateDebutValidite = patient.DateDebutValidite,
            DateFinValidite = patient.DateFinValidite,
            EstValide = estValide
        };
    }

    public async Task<PatientAssuranceResponse> UpdatePatientAssuranceAsync(int idPatient, UpdatePatientAssuranceDto dto)
    {
        try
        {
            var patient = await _context.Patients.FindAsync(idPatient);
            if (patient == null)
            {
                return new PatientAssuranceResponse { Success = false, Message = "Patient non trouvé" };
            }

            // Vérifier que l'assurance existe si fournie
            if (dto.AssuranceId.HasValue)
            {
                var assurance = await _context.Assurances.FindAsync(dto.AssuranceId.Value);
                if (assurance == null || !assurance.IsActive)
                {
                    return new PatientAssuranceResponse { Success = false, Message = "Assurance non trouvée ou inactive" };
                }
            }

            patient.AssuranceId = dto.AssuranceId;
            patient.NumeroCarteAssurance = dto.NumeroCarteAssurance;
            patient.DateDebutValidite = dto.DateDebutValidite;
            patient.DateFinValidite = dto.DateFinValidite;
            patient.CouvertureAssurance = dto.CouvertureAssurance;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Assurance patient mise à jour: Patient {idPatient}, Assurance {dto.AssuranceId}");

            return new PatientAssuranceResponse
            {
                Success = true,
                Message = "Assurance du patient mise à jour avec succès",
                Data = await GetPatientAssuranceAsync(idPatient)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour de l'assurance patient");
            return new PatientAssuranceResponse { Success = false, Message = "Erreur lors de la mise à jour" };
        }
    }

    public async Task<PatientAssuranceResponse> RemovePatientAssuranceAsync(int idPatient)
    {
        var patient = await _context.Patients.FindAsync(idPatient);
        if (patient == null)
        {
            return new PatientAssuranceResponse { Success = false, Message = "Patient non trouvé" };
        }

        patient.AssuranceId = null;
        patient.NumeroCarteAssurance = null;
        patient.DateDebutValidite = null;
        patient.DateFinValidite = null;
        patient.CouvertureAssurance = null;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Assurance retirée du patient: {idPatient}");

        return new PatientAssuranceResponse { Success = true, Message = "Assurance retirée du patient" };
    }
}
