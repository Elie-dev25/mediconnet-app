using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Entities.GestionLits;
using Mediconnet_Backend.Data;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service de gestion des lits - Suivi temps réel et affectation automatique
/// </summary>
public class LitManagementService : ILitManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LitManagementService> _logger;

    public LitManagementService(ApplicationDbContext context, ILogger<LitManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<OccupationDashboardDto> GetOccupationDashboardAsync()
    {
        var lits = await _context.Lits.ToListAsync();
        var maintenances = await _context.MaintenancesLits.Where(m => m.Statut == "en_cours").Select(m => m.IdLit).ToListAsync();
        var reservations = await _context.ReservationsLits.Where(r => r.Statut == "active").Select(r => r.IdLit).ToListAsync();

        var today = DateTime.UtcNow.Date;
        var sortiesPrevues = await _context.Hospitalisations
            .CountAsync(h => h.Statut == "en_cours" && h.DateSortie != null && h.DateSortie.Value.Date == today);

        return new OccupationDashboardDto
        {
            TotalLits = lits.Count,
            LitsOccupes = lits.Count(l => l.Statut == "occupe"),
            LitsLibres = lits.Count(l => l.Statut == "libre" && !maintenances.Contains(l.IdLit) && !reservations.Contains(l.IdLit)),
            LitsReserves = reservations.Count,
            LitsEnMaintenance = maintenances.Count,
            TauxOccupation = lits.Count > 0 ? Math.Round((decimal)lits.Count(l => l.Statut == "occupe") / lits.Count * 100, 1) : 0,
            SortiesPrevuesAujourdhui = sortiesPrevues,
            AdmissionsPrevuesAujourdhui = reservations.Count
        };
    }

    public async Task<List<ChambreOccupationDto>> GetOccupationParChambreAsync()
    {
        var chambres = await _context.Chambres
            .Include(c => c.Lits)
            .ToListAsync();

        var hospitalisations = await _context.Hospitalisations
            .Include(h => h.Patient).ThenInclude(p => p!.Utilisateur)
            .Where(h => h.Statut == "en_cours")
            .ToListAsync();

        var maintenances = await _context.MaintenancesLits.Where(m => m.Statut == "en_cours").ToListAsync();
        var reservations = await _context.ReservationsLits.Where(r => r.Statut == "active").ToListAsync();

        return chambres.Select(c => new ChambreOccupationDto
        {
            IdChambre = c.IdChambre,
            Numero = c.Numero ?? "",
            Capacite = c.Capacite ?? 0,
            Occupes = c.Lits?.Count(l => l.Statut == "occupe") ?? 0,
            Libres = c.Lits?.Count(l => l.Statut == "libre") ?? 0,
            Reserves = c.Lits != null ? c.Lits.Count(l => reservations.Any(r => r.IdLit == l.IdLit)) : 0,
            EnMaintenance = c.Lits?.Count(l => maintenances.Any(m => m.IdLit == l.IdLit)) ?? 0,
            Lits = c.Lits?.Select(l =>
            {
                var hospi = hospitalisations.FirstOrDefault(h => h.IdLit == l.IdLit);
                return new LitOccupationDto
                {
                    IdLit = l.IdLit,
                    Numero = l.Numero,
                    Statut = maintenances.Any(m => m.IdLit == l.IdLit) ? "maintenance" :
                             reservations.Any(r => r.IdLit == l.IdLit) ? "reserve" : l.Statut,
                    IdPatient = hospi?.IdPatient,
                    NomPatient = hospi?.Patient?.Utilisateur != null 
                        ? $"{hospi.Patient.Utilisateur.Prenom} {hospi.Patient.Utilisateur.Nom}" : null,
                    DateEntree = hospi?.DateEntree,
                    MotifHospitalisation = hospi?.Motif
                };
            }).ToList() ?? new List<LitOccupationDto>()
        }).ToList();
    }

    public async Task<AffectationResult> AffecterLitAutomatiqueAsync(AffectationRequest request)
    {
        var suggestions = await GetLitsSuggeresAsync(request.IdPatient, request.TypeChambre);
        
        if (!suggestions.Any())
        {
            return new AffectationResult
            {
                Success = false,
                Message = "Aucun lit disponible correspondant aux critères",
                Raison = "Tous les lits sont occupés ou en maintenance"
            };
        }

        var meilleurLit = suggestions.OrderByDescending(s => s.Score).First();

        // Marquer le lit comme occupé
        var lit = await _context.Lits.Include(l => l.Chambre).FirstOrDefaultAsync(l => l.IdLit == meilleurLit.IdLit);
        if (lit == null || lit.Statut != "libre")
        {
            return new AffectationResult { Success = false, Message = "Le lit n'est plus disponible" };
        }

        lit.Statut = "occupe";
        await _context.SaveChangesAsync();

        _logger.LogInformation("Lit {IdLit} affecté automatiquement au patient {IdPatient}", lit.IdLit, request.IdPatient);

        return new AffectationResult
        {
            Success = true,
            Message = "Lit affecté avec succès",
            IdLit = lit.IdLit,
            NumeroLit = lit.Numero,
            NumeroChambre = lit.Chambre?.Numero,
            Raison = meilleurLit.Raison
        };
    }

    public async Task<List<LitSuggestionDto>> GetLitsSuggeresAsync(int idPatient, string? criteres = null)
    {
        var litsLibres = await _context.Lits
            .Include(l => l.Chambre)
            .Where(l => l.Statut == "libre")
            .ToListAsync();

        var maintenances = await _context.MaintenancesLits.Where(m => m.Statut == "en_cours").Select(m => m.IdLit).ToListAsync();
        var reservations = await _context.ReservationsLits.Where(r => r.Statut == "active").Select(r => r.IdLit).ToListAsync();

        return litsLibres
            .Where(l => !maintenances.Contains(l.IdLit) && !reservations.Contains(l.IdLit))
            .Select(l => new LitSuggestionDto
            {
                IdLit = l.IdLit,
                NumeroLit = l.Numero,
                NumeroChambre = l.Chambre?.Numero ?? "",
                Score = CalculerScoreLit(l, criteres),
                Raison = "Lit disponible"
            })
            .OrderByDescending(l => l.Score)
            .Take(5)
            .ToList();
    }

    public async Task<ReservationLitDto> ReserverLitAsync(ReservationLitRequest request)
    {
        var lit = await _context.Lits.Include(l => l.Chambre).FirstOrDefaultAsync(l => l.IdLit == request.IdLit);
        if (lit == null) throw new Exception("Lit non trouvé");

        var patient = await _context.Patients.Include(p => p.Utilisateur).FirstOrDefaultAsync(p => p.IdUser == request.IdPatient);
        if (patient == null) throw new Exception("Patient non trouvé");

        var reservation = new ReservationLit
        {
            IdLit = request.IdLit,
            IdPatient = request.IdPatient,
            DateReservation = request.DateReservation,
            DateExpiration = request.DateExpiration ?? request.DateReservation.AddHours(24),
            Notes = request.Notes,
            Statut = "active"
        };

        _context.ReservationsLits.Add(reservation);
        await _context.SaveChangesAsync();

        return new ReservationLitDto
        {
            IdReservation = reservation.IdReservation,
            IdLit = reservation.IdLit,
            NumeroLit = lit.Numero,
            NumeroChambre = lit.Chambre?.Numero ?? "",
            IdPatient = reservation.IdPatient,
            NomPatient = patient.Utilisateur != null ? $"{patient.Utilisateur.Prenom} {patient.Utilisateur.Nom}" : "",
            DateReservation = reservation.DateReservation,
            DateExpiration = reservation.DateExpiration,
            Statut = reservation.Statut
        };
    }

    public async Task<bool> AnnulerReservationAsync(int idReservation)
    {
        var reservation = await _context.ReservationsLits.FindAsync(idReservation);
        if (reservation == null) return false;

        reservation.Statut = "annulee";
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ReservationLitDto>> GetReservationsEnCoursAsync()
    {
        return await _context.ReservationsLits
            .Include(r => r.Lit).ThenInclude(l => l!.Chambre)
            .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
            .Where(r => r.Statut == "active")
            .Select(r => new ReservationLitDto
            {
                IdReservation = r.IdReservation,
                IdLit = r.IdLit,
                NumeroLit = r.Lit != null ? r.Lit.Numero : "",
                NumeroChambre = r.Lit != null && r.Lit.Chambre != null ? r.Lit.Chambre.Numero : "",
                IdPatient = r.IdPatient,
                NomPatient = r.Patient != null && r.Patient.Utilisateur != null 
                    ? $"{r.Patient.Utilisateur.Prenom} {r.Patient.Utilisateur.Nom}" : "",
                DateReservation = r.DateReservation,
                DateExpiration = r.DateExpiration,
                Statut = r.Statut
            })
            .ToListAsync();
    }

    public async Task<TransfertResult> TransfererPatientAsync(TransfertRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var hospitalisation = await _context.Hospitalisations
                .Include(h => h.Lit)
                .FirstOrDefaultAsync(h => h.IdAdmission == request.IdAdmission);

            if (hospitalisation == null)
                return new TransfertResult { Success = false, Message = "Hospitalisation non trouvée" };

            var nouveauLit = await _context.Lits.FindAsync(request.IdNouveauLit);
            if (nouveauLit == null || nouveauLit.Statut != "libre")
                return new TransfertResult { Success = false, Message = "Le nouveau lit n'est pas disponible" };

            var ancienLit = hospitalisation.Lit;

            // Créer le transfert
            var transfert = new TransfertLit
            {
                IdAdmission = request.IdAdmission,
                IdPatient = hospitalisation.IdPatient,
                IdLitOrigine = hospitalisation.IdLit,
                IdLitDestination = request.IdNouveauLit,
                Motif = request.Motif,
                DateTransfert = DateTime.UtcNow
            };

            _context.TransfertsLits.Add(transfert);

            // Libérer l'ancien lit
            if (ancienLit != null) ancienLit.Statut = "libre";

            // Occuper le nouveau lit
            nouveauLit.Statut = "occupe";
            hospitalisation.IdLit = request.IdNouveauLit;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Transfert effectué: Patient {IdPatient} de lit {AncienLit} vers {NouveauLit}",
                hospitalisation.IdPatient, transfert.IdLitOrigine, transfert.IdLitDestination);

            return new TransfertResult
            {
                Success = true,
                Message = "Transfert effectué avec succès",
                IdTransfert = transfert.IdTransfert
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erreur lors du transfert");
            return new TransfertResult { Success = false, Message = "Erreur lors du transfert" };
        }
    }

    public async Task<List<TransfertHistoriqueDto>> GetHistoriqueTransfertsAsync(int? idPatient = null)
    {
        var query = _context.TransfertsLits
            .Include(t => t.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(t => t.LitOrigine)
            .Include(t => t.LitDestination)
            .AsQueryable();

        if (idPatient.HasValue)
            query = query.Where(t => t.IdPatient == idPatient.Value);

        return await query
            .OrderByDescending(t => t.DateTransfert)
            .Take(50)
            .Select(t => new TransfertHistoriqueDto
            {
                IdTransfert = t.IdTransfert,
                IdPatient = t.IdPatient,
                NomPatient = t.Patient != null && t.Patient.Utilisateur != null 
                    ? $"{t.Patient.Utilisateur.Prenom} {t.Patient.Utilisateur.Nom}" : "",
                IdLitOrigine = t.IdLitOrigine,
                NumeroLitOrigine = t.LitOrigine != null ? t.LitOrigine.Numero : "",
                IdLitDestination = t.IdLitDestination,
                NumeroLitDestination = t.LitDestination != null ? t.LitDestination.Numero : "",
                Motif = t.Motif,
                DateTransfert = t.DateTransfert
            })
            .ToListAsync();
    }

    public async Task<bool> MarquerLitEnMaintenanceAsync(int idLit, string motif)
    {
        var lit = await _context.Lits.FindAsync(idLit);
        if (lit == null || lit.Statut == "occupe") return false;

        var maintenance = new MaintenanceLit
        {
            IdLit = idLit,
            Motif = motif,
            DateDebut = DateTime.UtcNow,
            Statut = "en_cours"
        };

        _context.MaintenancesLits.Add(maintenance);
        lit.Statut = "maintenance";
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> LibererLitMaintenanceAsync(int idLit)
    {
        var maintenance = await _context.MaintenancesLits
            .FirstOrDefaultAsync(m => m.IdLit == idLit && m.Statut == "en_cours");

        if (maintenance == null) return false;

        maintenance.Statut = "terminee";
        maintenance.DateFin = DateTime.UtcNow;

        var lit = await _context.Lits.FindAsync(idLit);
        if (lit != null) lit.Statut = "libre";

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<LitMaintenanceDto>> GetLitsEnMaintenanceAsync()
    {
        return await _context.MaintenancesLits
            .Include(m => m.Lit).ThenInclude(l => l!.Chambre)
            .Where(m => m.Statut == "en_cours")
            .Select(m => new LitMaintenanceDto
            {
                IdLit = m.IdLit,
                NumeroLit = m.Lit != null ? m.Lit.Numero : "",
                NumeroChambre = m.Lit != null && m.Lit.Chambre != null ? m.Lit.Chambre.Numero : "",
                Motif = m.Motif,
                DateDebut = m.DateDebut,
                DateFinPrevue = m.DateFinPrevue
            })
            .ToListAsync();
    }

    public async Task<OccupationStatsDto> GetStatistiquesOccupationAsync(DateTime dateDebut, DateTime dateFin)
    {
        var hospitalisations = await _context.Hospitalisations
            .Where(h => h.DateEntree >= dateDebut && h.DateEntree <= dateFin)
            .ToListAsync();

        var transferts = await _context.TransfertsLits
            .CountAsync(t => t.DateTransfert >= dateDebut && t.DateTransfert <= dateFin);

        var sorties = hospitalisations.Count(h => h.DateSortie.HasValue);
        var durees = hospitalisations
            .Where(h => h.DateSortie.HasValue)
            .Select(h => (h.DateSortie!.Value - h.DateEntree).TotalDays);

        return new OccupationStatsDto
        {
            NombreAdmissions = hospitalisations.Count,
            NombreSorties = sorties,
            NombreTransferts = transferts,
            DureeMoyenneSejour = durees.Any() ? (decimal)durees.Average() : 0
        };
    }

    private static int CalculerScoreLit(Lit lit, string? criteres)
    {
        var score = 100;
        // Logique de scoring basée sur les critères
        return score;
    }
}
