using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Planning;
using Mediconnet_Backend.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Mediconnet_Backend.Services;

public class MedecinPlanningService : IMedecinPlanningService
{
    private readonly ApplicationDbContext _context;
    private static readonly string[] JoursNoms = { "", "Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche" };

    public MedecinPlanningService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ==================== DASHBOARD ====================

    public async Task<PlanningDashboardDto> GetDashboardAsync(int medecinId)
    {
        var now = DateTimeHelper.Now;
        var debutSemaine = now.Date.AddDays(-(int)now.DayOfWeek + 1);
        var finSemaine = debutSemaine.AddDays(7);
        var debutMois = new DateTime(now.Year, now.Month, 1);
        var finMois = debutMois.AddMonths(1);

        var rdvs = await _context.RendezVous
            .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
            .Where(r => r.IdMedecin == medecinId && r.Statut != "annule")
            .ToListAsync();

        var prochainsRdv = rdvs
            .Where(r => r.DateHeure >= now)
            .OrderBy(r => r.DateHeure)
            .Take(5)
            .Select(MapToRdvPlanning)
            .ToList();

        var prochaineIndispo = await _context.IndisponibilitesMedecin
            .Where(i => i.IdMedecin == medecinId && i.DateDebut >= now)
            .OrderBy(i => i.DateDebut)
            .FirstOrDefaultAsync();

        return new PlanningDashboardDto
        {
            RdvAujourdHui = rdvs.Count(r => r.DateHeure.Date == now.Date),
            RdvCetteSemaine = rdvs.Count(r => r.DateHeure >= debutSemaine && r.DateHeure < finSemaine),
            RdvCeMois = rdvs.Count(r => r.DateHeure >= debutMois && r.DateHeure < finMois),
            JoursCongeRestants = 25, // À configurer
            ProchainsRdv = prochainsRdv,
            ProchaineIndisponibilite = prochaineIndispo != null ? MapToIndispoDto(prochaineIndispo) : null
        };
    }

    // ==================== CRÉNEAUX HORAIRES ====================

    public async Task<SemaineTypeDto> GetSemaineTypeAsync(int medecinId)
    {
        var creneaux = await _context.CreneauxDisponibles
            .Where(c => c.IdMedecin == medecinId && c.EstSemaineType)
            .OrderBy(c => c.JourSemaine).ThenBy(c => c.HeureDebut)
            .ToListAsync();

        var jours = new List<JourSemaineDto>();
        var totalMinutes = 0;
        var totalCreneaux = 0;

        for (int jour = 1; jour <= 7; jour++)
        {
            var creneauxJour = creneaux.Where(c => c.JourSemaine == jour).ToList();
            var minutesJour = creneauxJour.Sum(c => (int)(c.HeureFin - c.HeureDebut).TotalMinutes);
            
            jours.Add(new JourSemaineDto
            {
                Numero = jour,
                Nom = JoursNoms[jour],
                Creneaux = creneauxJour.Select(MapToCreneauDto).ToList(),
                Travaille = creneauxJour.Any(c => c.Actif),
                HeuresTotal = $"{minutesJour / 60}h{minutesJour % 60:D2}"
            });

            totalMinutes += minutesJour;
            totalCreneaux += creneauxJour.Count;
        }

        return new SemaineTypeDto
        {
            Jours = jours,
            TotalHeures = totalMinutes / 60,
            TotalCreneaux = totalCreneaux
        };
    }

    public async Task<SemainePlanningDto> GetSemainePlanningAsync(int medecinId, DateTime dateDebut)
    {
        var lundi = dateDebut.Date.AddDays(-(int)dateDebut.DayOfWeek + (dateDebut.DayOfWeek == DayOfWeek.Sunday ? -6 : 1));
        var dimanche = lundi.AddDays(6);
        var now = DateTimeHelper.Today;

        // Récupérer tous les créneaux (semaine type + spécifiques à cette période)
        var tousCreneaux = await _context.CreneauxDisponibles
            .Where(c => c.IdMedecin == medecinId &&
                       (c.EstSemaineType ||
                        (c.DateDebutValidite <= dimanche && (c.DateFinValidite == null || c.DateFinValidite >= lundi))))
            .OrderBy(c => c.JourSemaine).ThenBy(c => c.HeureDebut)
            .ToListAsync();

        // Récupérer les indisponibilités pour cette semaine
        var indispos = await _context.IndisponibilitesMedecin
            .Where(i => i.IdMedecin == medecinId &&
                       i.DateDebut.Date <= dimanche &&
                       i.DateFin.Date >= lundi)
            .ToListAsync();

        var jours = new List<JourSemaineDto>();
        var totalMinutes = 0;
        var totalCreneaux = 0;

        for (int i = 0; i < 7; i++)
        {
            var dateJour = lundi.AddDays(i);
            var jourSemaine = i + 1; // 1=Lundi, 7=Dimanche

            // Vérifier si jour indisponible
            var estIndispo = indispos.Any(ind => ind.DateDebut.Date <= dateJour && ind.DateFin.Date >= dateJour);

            // Priorité aux créneaux spécifiques, sinon semaine type
            var creneauxSpecifiques = tousCreneaux
                .Where(c => !c.EstSemaineType &&
                           c.JourSemaine == jourSemaine &&
                           c.DateDebutValidite?.Date <= dateJour &&
                           (c.DateFinValidite == null || c.DateFinValidite?.Date >= dateJour))
                .ToList();

            var creneauxJour = creneauxSpecifiques.Any()
                ? creneauxSpecifiques
                : tousCreneaux.Where(c => c.EstSemaineType && c.JourSemaine == jourSemaine).ToList();

            var minutesJour = creneauxJour.Sum(c => (int)(c.HeureFin - c.HeureDebut).TotalMinutes);

            jours.Add(new JourSemaineDto
            {
                Numero = jourSemaine,
                Nom = JoursNoms[jourSemaine],
                Date = dateJour,
                Creneaux = creneauxJour.Select(MapToCreneauDto).ToList(),
                Travaille = creneauxJour.Any(c => c.Actif) && !estIndispo,
                HeuresTotal = $"{minutesJour / 60}h{minutesJour % 60:D2}",
                EstIndisponible = estIndispo
            });

            if (!estIndispo)
            {
                totalMinutes += minutesJour;
                totalCreneaux += creneauxJour.Count;
            }
        }

        return new SemainePlanningDto
        {
            DateDebut = lundi,
            DateFin = dimanche,
            Label = FormatSemaineLabel(lundi, dimanche),
            Jours = jours,
            TotalHeures = totalMinutes / 60,
            TotalCreneaux = totalCreneaux,
            EstSemaineCourante = lundi <= now && now <= dimanche
        };
    }

    private static string FormatSemaineLabel(DateTime debut, DateTime fin)
    {
        var moisDebut = debut.ToString("MMMM", new System.Globalization.CultureInfo("fr-FR"));
        var moisFin = fin.ToString("MMMM", new System.Globalization.CultureInfo("fr-FR"));
        
        if (moisDebut == moisFin)
        {
            return $"Du {debut.Day} au {fin.Day} {moisFin} {fin.Year}";
        }
        return $"Du {debut.Day} {moisDebut} au {fin.Day} {moisFin} {fin.Year}";
    }

    public async Task<List<CreneauHoraireDto>> GetCreneauxJourAsync(int medecinId, int jourSemaine)
    {
        var creneaux = await _context.CreneauxDisponibles
            .Where(c => c.IdMedecin == medecinId && c.JourSemaine == jourSemaine)
            .OrderBy(c => c.HeureDebut)
            .ToListAsync();

        return creneaux.Select(MapToCreneauDto).ToList();
    }

    public async Task<(bool Success, string Message, CreneauHoraireDto? Creneau)> CreateCreneauAsync(
        int medecinId, CreateCreneauRequest request)
    {
        // Valider les heures
        if (!TimeSpan.TryParse(request.HeureDebut, out var heureDebut) ||
            !TimeSpan.TryParse(request.HeureFin, out var heureFin))
        {
            return (false, "Format d'heure invalide", null);
        }

        if (heureFin <= heureDebut)
            return (false, "L'heure de fin doit être après l'heure de début", null);

        // Vérifier les chevauchements selon le type de créneau
        IQueryable<CreneauDisponible> queryExistants = _context.CreneauxDisponibles
            .Where(c => c.IdMedecin == medecinId && c.JourSemaine == request.JourSemaine);

        if (request.EstSemaineType)
        {
            // Pour semaine type, vérifier uniquement les autres créneaux de semaine type
            queryExistants = queryExistants.Where(c => c.EstSemaineType);
        }
        else if (request.DateDebutValidite.HasValue)
        {
            // Pour créneau spécifique, vérifier les chevauchements de période
            var debut = request.DateDebutValidite.Value.Date;
            var fin = request.DateFinValidite?.Date ?? debut;
            queryExistants = queryExistants.Where(c => 
                !c.EstSemaineType &&
                c.DateDebutValidite <= fin &&
                (c.DateFinValidite == null || c.DateFinValidite >= debut));
        }

        var creneauxExistants = await queryExistants.ToListAsync();
        var conflit = creneauxExistants.Any(c => heureDebut < c.HeureFin && heureFin > c.HeureDebut);

        if (conflit)
            return (false, "Ce créneau chevauche un créneau existant", null);

        var creneau = new CreneauDisponible
        {
            IdMedecin = medecinId,
            JourSemaine = request.JourSemaine,
            HeureDebut = heureDebut,
            HeureFin = heureFin,
            DureeParDefaut = request.DureeParDefaut,
            Actif = true,
            EstSemaineType = request.EstSemaineType,
            DateDebutValidite = request.EstSemaineType ? null : request.DateDebutValidite,
            DateFinValidite = request.EstSemaineType ? null : request.DateFinValidite
        };

        _context.CreneauxDisponibles.Add(creneau);
        await _context.SaveChangesAsync();

        return (true, "Créneau créé avec succès", MapToCreneauDto(creneau));
    }

    public async Task<(bool Success, string Message)> UpdateCreneauAsync(
        int medecinId, int creneauId, CreateCreneauRequest request)
    {
        var creneau = await _context.CreneauxDisponibles
            .FirstOrDefaultAsync(c => c.IdCreneau == creneauId && c.IdMedecin == medecinId);

        if (creneau == null)
            return (false, "Créneau introuvable");

        if (!TimeSpan.TryParse(request.HeureDebut, out var heureDebut) ||
            !TimeSpan.TryParse(request.HeureFin, out var heureFin))
        {
            return (false, "Format d'heure invalide");
        }

        if (heureFin <= heureDebut)
            return (false, "L'heure de fin doit être après l'heure de début");

        creneau.JourSemaine = request.JourSemaine;
        creneau.HeureDebut = heureDebut;
        creneau.HeureFin = heureFin;
        creneau.DureeParDefaut = request.DureeParDefaut;

        await _context.SaveChangesAsync();
        return (true, "Créneau modifié avec succès");
    }

    public async Task<(bool Success, string Message)> DeleteCreneauAsync(int medecinId, int creneauId)
    {
        var creneau = await _context.CreneauxDisponibles
            .FirstOrDefaultAsync(c => c.IdCreneau == creneauId && c.IdMedecin == medecinId);

        if (creneau == null)
            return (false, "Créneau introuvable");

        _context.CreneauxDisponibles.Remove(creneau);
        await _context.SaveChangesAsync();

        return (true, "Créneau supprimé avec succès");
    }

    public async Task<(bool Success, string Message)> ToggleCreneauAsync(int medecinId, int creneauId)
    {
        var creneau = await _context.CreneauxDisponibles
            .FirstOrDefaultAsync(c => c.IdCreneau == creneauId && c.IdMedecin == medecinId);

        if (creneau == null)
            return (false, "Créneau introuvable");

        creneau.Actif = !creneau.Actif;
        await _context.SaveChangesAsync();

        return (true, creneau.Actif ? "Créneau activé" : "Créneau désactivé");
    }

    // ==================== INDISPONIBILITÉS ====================

    public async Task<List<IndisponibiliteDto>> GetIndisponibilitesAsync(
        int medecinId, DateTime? dateDebut = null, DateTime? dateFin = null)
    {
        var query = _context.IndisponibilitesMedecin
            .Where(i => i.IdMedecin == medecinId);

        if (dateDebut.HasValue)
            query = query.Where(i => i.DateFin >= dateDebut.Value);

        if (dateFin.HasValue)
            query = query.Where(i => i.DateDebut <= dateFin.Value);

        var indispos = await query.OrderBy(i => i.DateDebut).ToListAsync();
        return indispos.Select(MapToIndispoDto).ToList();
    }

    public async Task<(bool Success, string Message, IndisponibiliteDto? Indispo)> CreateIndisponibiliteAsync(
        int medecinId, CreateIndisponibiliteRequest request)
    {
        var dateDebut = request.DateDebut.Date;
        var dateFin = request.DateFin.Date.AddDays(1).AddTicks(-1);

        if (dateFin < dateDebut)
            return (false, "La date de fin doit être après la date de début", null);

        if (dateDebut < DateTimeHelper.Today)
            return (false, "Impossible de créer une indisponibilité dans le passé", null);

        // Vérifier les chevauchements
        var conflit = await _context.IndisponibilitesMedecin
            .AnyAsync(i => i.IdMedecin == medecinId &&
                          i.DateDebut <= dateFin &&
                          i.DateFin >= dateDebut);

        if (conflit)
            return (false, "Cette période chevauche une indisponibilité existante", null);

        // Vérifier les RDV existants
        var rdvExistants = await _context.RendezVous
            .Where(r => r.IdMedecin == medecinId &&
                       r.Statut != "annule" &&
                       r.DateHeure >= dateDebut &&
                       r.DateHeure <= dateFin)
            .CountAsync();

        if (rdvExistants > 0)
            return (false, $"Impossible: {rdvExistants} rendez-vous sont prévus sur cette période", null);

        var indispo = new IndisponibiliteMedecin
        {
            IdMedecin = medecinId,
            DateDebut = dateDebut,
            DateFin = dateFin,
            Type = request.Type,
            Motif = request.Motif,
            JourneeComplete = request.JourneeComplete
        };

        _context.IndisponibilitesMedecin.Add(indispo);
        await _context.SaveChangesAsync();

        return (true, "Indisponibilité créée avec succès", MapToIndispoDto(indispo));
    }

    public async Task<(bool Success, string Message)> DeleteIndisponibiliteAsync(int medecinId, int indispoId)
    {
        var indispo = await _context.IndisponibilitesMedecin
            .FirstOrDefaultAsync(i => i.IdIndisponibilite == indispoId && i.IdMedecin == medecinId);

        if (indispo == null)
            return (false, "Indisponibilité introuvable");

        _context.IndisponibilitesMedecin.Remove(indispo);
        await _context.SaveChangesAsync();

        return (true, "Indisponibilité supprimée avec succès");
    }

    // ==================== CALENDRIER ====================

    public async Task<List<JourneeCalendrierDto>> GetCalendrierSemaineAsync(int medecinId, DateTime dateDebut)
    {
        var semaine = new List<JourneeCalendrierDto>();
        var lundi = dateDebut.Date.AddDays(-(int)dateDebut.DayOfWeek + 1);

        for (int i = 0; i < 7; i++)
        {
            var jour = lundi.AddDays(i);
            semaine.Add(await GetCalendrierJourAsync(medecinId, jour));
        }

        return semaine;
    }

    public async Task<JourneeCalendrierDto> GetCalendrierJourAsync(int medecinId, DateTime date)
    {
        var jourSemaine = (int)date.DayOfWeek;
        if (jourSemaine == 0) jourSemaine = 7;

        // Vérifier indisponibilité
        var indispo = await _context.IndisponibilitesMedecin
            .FirstOrDefaultAsync(i => i.IdMedecin == medecinId &&
                                     i.DateDebut.Date <= date.Date &&
                                     i.DateFin.Date >= date.Date);

        // Récupérer les RDV du jour
        var rdvs = await _context.RendezVous
            .Include(r => r.Patient).ThenInclude(p => p!.Utilisateur)
            .Where(r => r.IdMedecin == medecinId &&
                       r.DateHeure.Date == date.Date &&
                       r.Statut != "annule")
            .OrderBy(r => r.DateHeure)
            .ToListAsync();

        return new JourneeCalendrierDto
        {
            Date = date,
            JourNom = JoursNoms[jourSemaine],
            EstIndisponible = indispo != null,
            MotifIndisponibilite = indispo?.Motif ?? GetTypeLibelle(indispo?.Type),
            RendezVous = rdvs.Select(MapToRdvPlanning).ToList()
        };
    }

    // ==================== HELPERS ====================

    private CreneauHoraireDto MapToCreneauDto(CreneauDisponible c) => new()
    {
        IdCreneau = c.IdCreneau,
        JourSemaine = c.JourSemaine,
        JourNom = JoursNoms[c.JourSemaine],
        HeureDebut = c.HeureDebut.ToString(@"hh\:mm"),
        HeureFin = c.HeureFin.ToString(@"hh\:mm"),
        DureeParDefaut = c.DureeParDefaut,
        Actif = c.Actif,
        DateDebutValidite = c.DateDebutValidite,
        DateFinValidite = c.DateFinValidite,
        EstSemaineType = c.EstSemaineType
    };

    private IndisponibiliteDto MapToIndispoDto(IndisponibiliteMedecin i) => new()
    {
        IdIndisponibilite = i.IdIndisponibilite,
        DateDebut = i.DateDebut,
        DateFin = i.DateFin,
        Type = i.Type,
        TypeLibelle = GetTypeLibelle(i.Type),
        Motif = i.Motif,
        JourneeComplete = i.JourneeComplete,
        NombreJours = (int)(i.DateFin - i.DateDebut).TotalDays + 1
    };

    private RdvPlanningDto MapToRdvPlanning(RendezVous r) => new()
    {
        IdRendezVous = r.IdRendezVous,
        DateHeure = r.DateHeure,
        Duree = r.Duree,
        PatientId = r.Patient?.IdUser ?? 0,
        PatientNom = r.Patient?.Utilisateur?.Nom ?? "",
        PatientPrenom = r.Patient?.Utilisateur?.Prenom ?? "",
        NumeroDossier = r.Patient?.NumeroDossier,
        Motif = r.Motif,
        TypeRdv = r.TypeRdv,
        Statut = r.Statut
    };

    private static string GetTypeLibelle(string? type) => type switch
    {
        "conge" => "Congés",
        "maladie" => "Arrêt maladie",
        "formation" => "Formation",
        "autre" => "Autre",
        _ => ""
    };
}
