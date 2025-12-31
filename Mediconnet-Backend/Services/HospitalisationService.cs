using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.DTOs.Hospitalisation;

namespace Mediconnet_Backend.Services;

public interface IHospitalisationService
{
    Task<ChambresResponse> GetChambresAsync();
    Task<LitsDisponiblesResponse> GetLitsDisponiblesAsync();
    Task<List<HospitalisationDto>> GetHospitalisationsAsync(FiltreHospitalisationRequest? filtre = null);
    Task<HospitalisationDto?> GetHospitalisationByIdAsync(int idAdmission);
    Task<HospitalisationResponse> CreerHospitalisationAsync(CreerHospitalisationRequest request);
    Task<HospitalisationResponse> DemanderHospitalisationAsync(DemandeHospitalisationRequest request, int medecinId);
    Task<HospitalisationResponse> TerminerHospitalisationAsync(TerminerHospitalisationRequest request);
    Task<List<HospitalisationDto>> GetHospitalisationsPatientAsync(int idPatient);
}

public class HospitalisationService : IHospitalisationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HospitalisationService> _logger;

    public HospitalisationService(ApplicationDbContext context, ILogger<HospitalisationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ChambresResponse> GetChambresAsync()
    {
        var chambres = await _context.Chambres
            .Include(c => c.Lits)
            .ToListAsync();

        var chambresDto = chambres.Select(c => new ChambreDto
        {
            IdChambre = c.IdChambre,
            Numero = c.Numero,
            Capacite = c.Capacite,
            Etat = c.Etat,
            Statut = c.Statut,
            LitsDisponibles = c.Lits?.Count(l => l.Statut == "libre") ?? 0,
            LitsOccupes = c.Lits?.Count(l => l.Statut == "occupe") ?? 0,
            Lits = c.Lits?.Select(l => new LitDto
            {
                IdLit = l.IdLit,
                Numero = l.Numero,
                Statut = l.Statut,
                IdChambre = l.IdChambre,
                NumeroChambre = c.Numero,
                EstDisponible = l.Statut == "libre"
            }).ToList()
        }).ToList();

        return new ChambresResponse
        {
            Chambres = chambresDto,
            TotalChambres = chambresDto.Count,
            TotalLits = chambresDto.Sum(c => c.Lits?.Count ?? 0),
            LitsDisponibles = chambresDto.Sum(c => c.LitsDisponibles)
        };
    }

    public async Task<LitsDisponiblesResponse> GetLitsDisponiblesAsync()
    {
        var lits = await _context.Lits
            .Include(l => l.Chambre)
            .Where(l => l.Statut == "libre")
            .ToListAsync();

        var litsDto = lits.Select(l => new LitDto
        {
            IdLit = l.IdLit,
            Numero = l.Numero,
            Statut = l.Statut,
            IdChambre = l.IdChambre,
            NumeroChambre = l.Chambre?.Numero,
            EstDisponible = true
        }).ToList();

        return new LitsDisponiblesResponse
        {
            Lits = litsDto,
            TotalDisponibles = litsDto.Count
        };
    }

    public async Task<List<HospitalisationDto>> GetHospitalisationsAsync(FiltreHospitalisationRequest? filtre = null)
    {
        var query = _context.Hospitalisations
            .Include(h => h.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(h => h.Lit).ThenInclude(l => l!.Chambre)
            .AsQueryable();

        if (filtre != null)
        {
            if (!string.IsNullOrEmpty(filtre.Statut))
                query = query.Where(h => h.Statut == filtre.Statut);

            if (filtre.IdPatient.HasValue)
                query = query.Where(h => h.IdPatient == filtre.IdPatient.Value);

            if (filtre.DateDebut.HasValue)
                query = query.Where(h => h.DateEntree >= filtre.DateDebut.Value);

            if (filtre.DateFin.HasValue)
                query = query.Where(h => h.DateEntree <= filtre.DateFin.Value);
        }

        var hospitalisations = await query
            .OrderByDescending(h => h.DateEntree)
            .ToListAsync();

        return hospitalisations.Select(MapToDto).ToList();
    }

    public async Task<HospitalisationDto?> GetHospitalisationByIdAsync(int idAdmission)
    {
        var hospitalisation = await _context.Hospitalisations
            .Include(h => h.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(h => h.Lit).ThenInclude(l => l!.Chambre)
            .FirstOrDefaultAsync(h => h.IdAdmission == idAdmission);

        return hospitalisation != null ? MapToDto(hospitalisation) : null;
    }

    public async Task<HospitalisationResponse> CreerHospitalisationAsync(CreerHospitalisationRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Vérifier que le lit existe et est disponible
            var lit = await _context.Lits
                .Include(l => l.Chambre)
                .FirstOrDefaultAsync(l => l.IdLit == request.IdLit);

            if (lit == null)
            {
                return new HospitalisationResponse
                {
                    Success = false,
                    Message = "Lit non trouvé"
                };
            }

            if (lit.Statut != "libre")
            {
                return new HospitalisationResponse
                {
                    Success = false,
                    Message = "Ce lit n'est pas disponible"
                };
            }

            // Vérifier que le patient existe
            var patient = await _context.Patients
                .Include(p => p.Utilisateur)
                .FirstOrDefaultAsync(p => p.IdUser == request.IdPatient);

            if (patient == null)
            {
                return new HospitalisationResponse
                {
                    Success = false,
                    Message = "Patient non trouvé"
                };
            }

            // Créer l'hospitalisation
            var hospitalisation = new Hospitalisation
            {
                IdPatient = request.IdPatient,
                IdLit = request.IdLit,
                DateEntree = request.DateEntreePrevue ?? DateTime.UtcNow,
                Motif = request.Motif,
                Statut = "en_cours"
            };

            _context.Hospitalisations.Add(hospitalisation);

            // Marquer le lit comme occupé
            lit.Statut = "occupe";

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Hospitalisation créée: {IdAdmission} pour patient {IdPatient}", 
                hospitalisation.IdAdmission, request.IdPatient);

            // Recharger avec les relations
            hospitalisation = await _context.Hospitalisations
                .Include(h => h.Patient).ThenInclude(p => p!.Utilisateur)
                .Include(h => h.Lit).ThenInclude(l => l!.Chambre)
                .FirstOrDefaultAsync(h => h.IdAdmission == hospitalisation.IdAdmission);

            return new HospitalisationResponse
            {
                Success = true,
                Message = "Hospitalisation créée avec succès",
                IdAdmission = hospitalisation!.IdAdmission,
                Hospitalisation = MapToDto(hospitalisation)
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erreur lors de la création de l'hospitalisation");
            return new HospitalisationResponse
            {
                Success = false,
                Message = "Erreur lors de la création de l'hospitalisation"
            };
        }
    }

    public async Task<HospitalisationResponse> DemanderHospitalisationAsync(DemandeHospitalisationRequest request, int medecinId)
    {
        // Trouver un lit disponible
        var litDisponible = await _context.Lits
            .Include(l => l.Chambre)
            .Where(l => l.Statut == "libre")
            .FirstOrDefaultAsync();

        if (litDisponible == null)
        {
            return new HospitalisationResponse
            {
                Success = false,
                Message = "Aucun lit disponible actuellement. Demande mise en attente."
            };
        }

        // Créer la demande d'hospitalisation
        var creerRequest = new CreerHospitalisationRequest
        {
            IdPatient = request.IdPatient,
            IdLit = litDisponible.IdLit,
            Motif = request.Motif,
            IdConsultation = request.IdConsultation
        };

        return await CreerHospitalisationAsync(creerRequest);
    }

    public async Task<HospitalisationResponse> TerminerHospitalisationAsync(TerminerHospitalisationRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var hospitalisation = await _context.Hospitalisations
                .Include(h => h.Lit)
                .FirstOrDefaultAsync(h => h.IdAdmission == request.IdAdmission);

            if (hospitalisation == null)
            {
                return new HospitalisationResponse
                {
                    Success = false,
                    Message = "Hospitalisation non trouvée"
                };
            }

            // Mettre à jour l'hospitalisation
            hospitalisation.DateSortie = request.DateSortie ?? DateTime.UtcNow;
            hospitalisation.Statut = "terminee";

            // Libérer le lit
            if (hospitalisation.Lit != null)
            {
                hospitalisation.Lit.Statut = "libre";
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Hospitalisation terminée: {IdAdmission}", request.IdAdmission);

            return new HospitalisationResponse
            {
                Success = true,
                Message = "Hospitalisation terminée avec succès",
                IdAdmission = hospitalisation.IdAdmission
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erreur lors de la terminaison de l'hospitalisation");
            return new HospitalisationResponse
            {
                Success = false,
                Message = "Erreur lors de la terminaison de l'hospitalisation"
            };
        }
    }

    public async Task<List<HospitalisationDto>> GetHospitalisationsPatientAsync(int idPatient)
    {
        var hospitalisations = await _context.Hospitalisations
            .Include(h => h.Lit).ThenInclude(l => l!.Chambre)
            .Where(h => h.IdPatient == idPatient)
            .OrderByDescending(h => h.DateEntree)
            .ToListAsync();

        return hospitalisations.Select(h => new HospitalisationDto
        {
            IdAdmission = h.IdAdmission,
            DateEntree = h.DateEntree,
            DateSortie = h.DateSortie,
            Motif = h.Motif,
            Statut = h.Statut,
            IdPatient = h.IdPatient,
            IdLit = h.IdLit,
            NumeroLit = h.Lit?.Numero,
            NumeroChambre = h.Lit?.Chambre?.Numero,
            DureeJours = h.DateSortie.HasValue 
                ? (int)(h.DateSortie.Value - h.DateEntree).TotalDays 
                : (int)(DateTime.UtcNow - h.DateEntree).TotalDays
        }).ToList();
    }

    private static HospitalisationDto MapToDto(Hospitalisation h)
    {
        return new HospitalisationDto
        {
            IdAdmission = h.IdAdmission,
            DateEntree = h.DateEntree,
            DateSortie = h.DateSortie,
            Motif = h.Motif,
            Statut = h.Statut,
            IdPatient = h.IdPatient,
            PatientNom = h.Patient?.Utilisateur?.Nom,
            PatientPrenom = h.Patient?.Utilisateur?.Prenom,
            PatientNumeroDossier = h.Patient?.NumeroDossier,
            IdLit = h.IdLit,
            NumeroLit = h.Lit?.Numero,
            NumeroChambre = h.Lit?.Chambre?.Numero,
            DureeJours = h.DateSortie.HasValue 
                ? (int)(h.DateSortie.Value - h.DateEntree).TotalDays 
                : (int)(DateTime.UtcNow - h.DateEntree).TotalDays
        };
    }
}
