using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Entities.Pharmacie;
using Mediconnet_Backend.DTOs.Pharmacie;

namespace Mediconnet_Backend.Services;

public interface IPharmacieStockService
{
    // KPIs
    Task<PharmacieKpiDto> GetKpisAsync();
    Task<List<AlerteStockDto>> GetAlertesAsync();
    
    // Profile & Dashboard
    Task<PharmacieProfileDto> GetProfileAsync(int userId);
    Task<PharmacieDashboardDto> GetDashboardAsync(int userId);
    Task<PharmacieProfileDto> UpdateProfileAsync(int userId, UpdatePharmacieProfileRequest request);
    
    // Médicaments/Stock
    Task<PagedResult<MedicamentStockDto>> GetMedicamentsAsync(string? search, string? statut, int page, int pageSize);
    Task<MedicamentStockDto?> GetMedicamentByIdAsync(int id);
    Task<List<FournisseurMedicamentDto>> GetFournisseursByMedicamentAsync(int id);
    Task<List<HistoriqueFournisseurMedicamentDto>> GetHistoriqueFournisseurMedicamentAsync(int id);
    Task<MedicamentStockDto> CreateMedicamentAsync(CreateMedicamentRequest request);
    Task<MedicamentStockDto> UpdateMedicamentAsync(int id, UpdateMedicamentRequest request);
    Task<bool> DeleteMedicamentAsync(int id);
    Task<MouvementStockDto> AjusterStockAsync(AjustementStockRequest request, int userId);
    
    // Mouvements
    Task<PagedResult<MouvementStockDto>> GetMouvementsAsync(MouvementStockFilter filter);
    
    // Fournisseurs
    Task<List<FournisseurDto>> GetFournisseursAsync(bool? actif = null);
    Task<FournisseurDto> CreateFournisseurAsync(CreateFournisseurRequest request);
    Task<FournisseurDto> UpdateFournisseurAsync(int id, CreateFournisseurRequest request);
    Task<FournisseurDto> ToggleFournisseurStatutAsync(int id);
    Task<bool> DeleteFournisseurAsync(int id);
    
    // Commandes
    Task<PagedResult<CommandePharmacieDto>> GetCommandesAsync(string? statut, int page, int pageSize);
    Task<CommandePharmacieDto?> GetCommandeByIdAsync(int id);
    Task<CommandePharmacieDto> CreateCommandeAsync(CreateCommandeRequest request, int userId);
    Task<CommandePharmacieDto> ReceptionnerCommandeAsync(int id, ReceptionCommandeRequest request, int userId);
    Task<bool> AnnulerCommandeAsync(int id);
    
    // Ordonnances/Dispensations
    Task<PagedResult<OrdonnancePharmacieDto>> GetOrdonnancesEnAttenteAsync(string? search, int page, int pageSize);
    Task<DispensationDto> DispenserOrdonnanceAsync(CreateDispensationRequest request, int pharmacienId);
    Task<PagedResult<DispensationDto>> GetDispensationsAsync(DateTime? dateDebut, DateTime? dateFin, int page, int pageSize);
    
    // Nouveau workflow pharmacie : Validation → Paiement → Délivrance
    /// <summary>
    /// Valide une ordonnance et crée la facture associée (sans impact sur le stock)
    /// </summary>
    Task<ValidationOrdonnanceResult> ValiderOrdonnanceAsync(int idOrdonnance, int pharmacienId);
    
    /// <summary>
    /// Délivre les médicaments d'une ordonnance payée (décrémente le stock)
    /// </summary>
    Task<DelivranceResult> DelivrerOrdonnanceAsync(int idOrdonnance, int pharmacienId);
    
    /// <summary>
    /// Récupère le détail d'une ordonnance avec son statut de paiement
    /// </summary>
    Task<OrdonnancePharmacieDetailDto?> GetOrdonnanceDetailAsync(int idOrdonnance);
    
    // ==================== Ventes Directes ====================
    
    /// <summary>
    /// Crée une vente directe sans ordonnance
    /// </summary>
    Task<VenteDirecteResult> CreerVenteDirecteAsync(CreateVenteDirecteRequest request, int pharmacienId);
    
    /// <summary>
    /// Récupère la liste des ventes directes avec pagination et filtres
    /// </summary>
    Task<PagedResult<VenteDirecteDto>> GetVentesDirectesAsync(VenteDirecteFilter filter);
    
    /// <summary>
    /// Récupère le détail d'une vente directe
    /// </summary>
    Task<VenteDirecteDto?> GetVenteDirecteByIdAsync(int idDispensation);
    
    /// <summary>
    /// Délivre une vente directe après paiement à la caisse
    /// Décrémente le stock et met à jour le statut vers "delivre"
    /// </summary>
    Task<VenteDirecteResult> DelivrerVenteDirecteAsync(int idDispensation, int pharmacienId);
}

public class PharmacieStockService : IPharmacieStockService
{
    private readonly ApplicationDbContext _context;
    private readonly IAssuranceCouvertureService _assuranceService;
    private readonly ILogger<PharmacieStockService> _logger;

    public PharmacieStockService(ApplicationDbContext context, IAssuranceCouvertureService assuranceService, ILogger<PharmacieStockService> logger)
    {
        _context = context;
        _assuranceService = assuranceService;
        _logger = logger;
    }

    // ==================== KPIs ====================
    
    public async Task<PharmacieKpiDto> GetKpisAsync()
    {
        var aujourdhui = DateTime.UtcNow.Date;
        var dans30Jours = aujourdhui.AddDays(30);

        var medicaments = await _context.Medicaments.Where(m => m.Actif).ToListAsync();
        
        return new PharmacieKpiDto
        {
            TotalMedicaments = medicaments.Count,
            MedicamentsEnAlerte = medicaments.Count(m => m.Stock <= m.SeuilStock && m.Stock > 0),
            MedicamentsEnRupture = medicaments.Count(m => m.Stock == 0 || m.Stock == null),
            MedicamentsPerimesProches = medicaments.Count(m => m.DatePeremption.HasValue && m.DatePeremption.Value <= dans30Jours),
            OrdonnancesEnAttente = await GetOrdonnancesEnAttenteCountAsync(),
            DispensationsJour = await _context.Dispensations
                .Where(d => d.DateDispensation.Date == aujourdhui)
                .CountAsync(),
            ValeurStock = (decimal)medicaments.Sum(m => (m.Stock ?? 0) * (m.Prix ?? 0)),
            CommandesEnCours = await _context.CommandesPharmacie
                .Where(c => c.Statut == "envoyee" || c.Statut == "partiellement_recue")
                .CountAsync()
        };
    }

    public async Task<List<AlerteStockDto>> GetAlertesAsync()
    {
        var alertes = new List<AlerteStockDto>();
        var aujourdhui = DateTime.UtcNow.Date;
        var dans30Jours = aujourdhui.AddDays(30);

        var medicaments = await _context.Medicaments.Where(m => m.Actif).ToListAsync();

        // Ruptures de stock
        foreach (var med in medicaments.Where(m => m.Stock == 0 || m.Stock == null))
        {
            alertes.Add(new AlerteStockDto
            {
                Type = "rupture",
                IdMedicament = med.IdMedicament,
                NomMedicament = med.Nom,
                StockActuel = med.Stock ?? 0,
                SeuilAlerte = med.SeuilStock,
                Priorite = "high"
            });
        }

        // Stock bas
        foreach (var med in medicaments.Where(m => m.Stock > 0 && m.Stock <= m.SeuilStock))
        {
            alertes.Add(new AlerteStockDto
            {
                Type = "alerte",
                IdMedicament = med.IdMedicament,
                NomMedicament = med.Nom,
                StockActuel = med.Stock,
                SeuilAlerte = med.SeuilStock,
                Priorite = "medium"
            });
        }

        // Péremption proche
        foreach (var med in medicaments.Where(m => m.DatePeremption.HasValue && m.DatePeremption.Value <= dans30Jours))
        {
            var joursRestants = (med.DatePeremption!.Value - aujourdhui).Days;
            alertes.Add(new AlerteStockDto
            {
                Type = "peremption",
                IdMedicament = med.IdMedicament,
                NomMedicament = med.Nom,
                StockActuel = med.Stock,
                DatePeremption = med.DatePeremption,
                JoursRestants = joursRestants,
                Priorite = joursRestants <= 7 ? "high" : "medium"
            });
        }

        return alertes.OrderByDescending(a => a.Priorite == "high").ThenBy(a => a.Type).ToList();
    }

    // ==================== Médicaments/Stock ====================

    public async Task<PagedResult<MedicamentStockDto>> GetMedicamentsAsync(string? search, string? statut, int page, int pageSize)
    {
        var query = _context.Medicaments.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(m => m.Nom.Contains(search) || (m.CodeATC != null && m.CodeATC.Contains(search)));
        }

        if (!string.IsNullOrEmpty(statut))
        {
            query = statut switch
            {
                "rupture" => query.Where(m => m.Stock == 0 || m.Stock == null),
                "alerte" => query.Where(m => m.Stock > 0 && m.Stock <= m.SeuilStock),
                "normal" => query.Where(m => m.Stock > m.SeuilStock),
                "inactif" => query.Where(m => !m.Actif),
                _ => query
            };
        }
        else
        {
            query = query.Where(m => m.Actif);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(m => m.Nom)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => MapToMedicamentDto(m))
            .ToListAsync();

        return new PagedResult<MedicamentStockDto>
        {
            Items = items,
            TotalItems = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<MedicamentStockDto?> GetMedicamentByIdAsync(int id)
    {
        var med = await _context.Medicaments.FindAsync(id);
        if (med == null)
            return null;

        var fournisseurs = await GetFournisseursByMedicamentAsync(id);
        return await MapToMedicamentDtoAsync(med, fournisseurs);
    }

    public async Task<List<FournisseurMedicamentDto>> GetFournisseursByMedicamentAsync(int id)
    {
        var medicament = await _context.Medicaments.FindAsync(id);
        if (medicament == null)
            return new List<FournisseurMedicamentDto>();

        // Récupérer tous les fournisseurs qui ont commandé ce médicament
        var fournisseurs = await _context.CommandesPharmacie
            .Where(c => c.Lignes!.Any(l => l.IdMedicament == id))
            .GroupBy(c => c.IdFournisseur)
            .Select(g => new
            {
                IdFournisseur = g.Key,
                DerniereCommande = g.Max(c => c.DateCommande),
                TotalCommandes = g.Count(),
                Fournisseur = g.FirstOrDefault().Fournisseur
            })
            .Select(f => new FournisseurMedicamentDto
            {
                IdFournisseur = f.IdFournisseur,
                NomFournisseur = f.Fournisseur!.NomFournisseur,
                ContactNom = f.Fournisseur.ContactNom,
                ContactEmail = f.Fournisseur.ContactEmail,
                ContactTelephone = f.Fournisseur.ContactTelephone,
                DelaiLivraisonJours = f.Fournisseur.DelaiLivraisonJours,
                DerniereCommande = f.DerniereCommande,
                TotalCommandes = f.TotalCommandes,
                
                // Détails du médicament pour identification sans ambiguïté
                IdMedicament = medicament.IdMedicament,
                NomMedicament = medicament.Nom,
                Dosage = medicament.Dosage,
                Laboratoire = medicament.Laboratoire,
                FormeGalenique = medicament.FormeGalenique
            })
            .OrderBy(f => f.NomFournisseur)
            .ToListAsync();

        return fournisseurs;
    }

    public async Task<List<HistoriqueFournisseurMedicamentDto>> GetHistoriqueFournisseurMedicamentAsync(int id)
    {
        var medicament = await _context.Medicaments.FindAsync(id);
        if (medicament == null)
            return new List<HistoriqueFournisseurMedicamentDto>();

        // Récupérer l'historique complet des commandes pour ce médicament
        var historique = await _context.CommandesPharmacie
            .Where(c => c.Lignes!.Any(l => l.IdMedicament == id))
            .SelectMany(c => c.Lignes!.Where(l => l.IdMedicament == id), (c, l) => new HistoriqueFournisseurMedicamentDto
            {
                IdCommande = c.IdCommande,
                DateCommande = c.DateCommande,
                DateReceptionPrevue = c.DateReceptionPrevue,
                DateReceptionReelle = c.DateReceptionReelle,
                Statut = c.Statut,
                MontantTotal = c.MontantTotal,
                QuantiteCommandee = l.QuantiteCommandee,
                QuantiteRecue = l.QuantiteRecue,
                PrixAchat = l.PrixAchat,
                NumeroLot = l.NumeroLot,
                DatePeremption = l.DatePeremption,
                
                // Infos fournisseur
                IdFournisseur = c.IdFournisseur,
                NomFournisseur = c.Fournisseur!.NomFournisseur,
                
                // Infos médicament
                IdMedicament = medicament.IdMedicament,
                NomMedicament = medicament.Nom,
                Dosage = medicament.Dosage,
                Laboratoire = medicament.Laboratoire
            })
            .OrderByDescending(h => h.DateCommande)
            .ToListAsync();

        return historique;
    }

    public async Task<MedicamentStockDto> CreateMedicamentAsync(CreateMedicamentRequest request)
    {
        var medicament = new Medicament
        {
            Nom = request.Nom,
            Dosage = request.Dosage,
            FormeGalenique = request.FormeGalenique,
            Laboratoire = request.Laboratoire,
            Stock = request.Stock,
            SeuilStock = request.SeuilStock,
            Prix = request.Prix,
            DatePeremption = request.DatePeremption,
            EmplacementRayon = request.EmplacementRayon,
            CodeATC = request.CodeATC,
            Conditionnement = request.Conditionnement,
            TemperatureConservation = request.TemperatureConservation,
            Actif = true,
            DateHeureCreation = DateTime.UtcNow
        };

        _context.Medicaments.Add(medicament);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Médicament créé: {Nom} (ID: {Id})", medicament.Nom, medicament.IdMedicament);
        return MapToMedicamentDto(medicament);
    }

    public async Task<MedicamentStockDto> UpdateMedicamentAsync(int id, UpdateMedicamentRequest request)
    {
        var medicament = await _context.Medicaments.FindAsync(id)
            ?? throw new KeyNotFoundException($"Médicament {id} non trouvé");

        if (request.Nom != null) medicament.Nom = request.Nom;
        if (request.Dosage != null) medicament.Dosage = request.Dosage;
        if (request.FormeGalenique != null) medicament.FormeGalenique = request.FormeGalenique;
        if (request.Laboratoire != null) medicament.Laboratoire = request.Laboratoire;
        if (request.SeuilStock.HasValue) medicament.SeuilStock = request.SeuilStock;
        if (request.Prix.HasValue) medicament.Prix = request.Prix;
        if (request.DatePeremption.HasValue) medicament.DatePeremption = request.DatePeremption;
        if (request.EmplacementRayon != null) medicament.EmplacementRayon = request.EmplacementRayon;
        if (request.CodeATC != null) medicament.CodeATC = request.CodeATC;
        if (request.Conditionnement != null) medicament.Conditionnement = request.Conditionnement;
        if (request.TemperatureConservation != null) medicament.TemperatureConservation = request.TemperatureConservation;
        if (request.Actif.HasValue) medicament.Actif = request.Actif.Value;

        await _context.SaveChangesAsync();
        return MapToMedicamentDto(medicament);
    }

    public async Task<bool> DeleteMedicamentAsync(int id)
    {
        var medicament = await _context.Medicaments.FindAsync(id);
        if (medicament == null) return false;

        medicament.Actif = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<MouvementStockDto> AjusterStockAsync(AjustementStockRequest request, int userId)
    {
        var medicament = await _context.Medicaments.FindAsync(request.IdMedicament)
            ?? throw new KeyNotFoundException($"Médicament {request.IdMedicament} non trouvé");

        var stockAvant = medicament.Stock ?? 0;
        var quantiteAjustee = request.TypeMouvement == "sortie" || request.TypeMouvement == "perte" 
            ? -request.Quantite 
            : request.Quantite;
        
        medicament.Stock = stockAvant + quantiteAjustee;
        if (medicament.Stock < 0) medicament.Stock = 0;

        var mouvement = new MouvementStock
        {
            IdMedicament = request.IdMedicament,
            TypeMouvement = request.TypeMouvement,
            Quantite = request.Quantite,
            Motif = request.Motif,
            IdUser = userId,
            StockApresMouvement = medicament.Stock.Value,
            DateMouvement = DateTime.UtcNow,
            ReferenceType = "ajustement"
        };

        _context.MouvementsStock.Add(mouvement);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Ajustement stock: {Type} {Quantite} {Medicament}", 
            request.TypeMouvement, request.Quantite, medicament.Nom);

        return new MouvementStockDto
        {
            IdMouvement = mouvement.IdMouvement,
            IdMedicament = mouvement.IdMedicament,
            NomMedicament = medicament.Nom,
            TypeMouvement = mouvement.TypeMouvement,
            Quantite = mouvement.Quantite,
            DateMouvement = mouvement.DateMouvement,
            Motif = mouvement.Motif,
            StockApresMouvement = mouvement.StockApresMouvement
        };
    }

    // ==================== Mouvements ====================

    public async Task<PagedResult<MouvementStockDto>> GetMouvementsAsync(MouvementStockFilter filter)
    {
        var query = _context.MouvementsStock
            .Include(m => m.Medicament)
            .Include(m => m.Utilisateur)
            .AsQueryable();

        if (filter.IdMedicament.HasValue)
            query = query.Where(m => m.IdMedicament == filter.IdMedicament);

        if (!string.IsNullOrEmpty(filter.TypeMouvement))
            query = query.Where(m => m.TypeMouvement == filter.TypeMouvement);

        if (filter.DateDebut.HasValue)
            query = query.Where(m => m.DateMouvement >= filter.DateDebut);

        if (filter.DateFin.HasValue)
            query = query.Where(m => m.DateMouvement <= filter.DateFin.Value.AddDays(1));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.DateMouvement)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(m => new MouvementStockDto
            {
                IdMouvement = m.IdMouvement,
                IdMedicament = m.IdMedicament,
                NomMedicament = m.Medicament != null ? m.Medicament.Nom : "",
                TypeMouvement = m.TypeMouvement,
                Quantite = m.Quantite,
                DateMouvement = m.DateMouvement,
                Motif = m.Motif,
                ReferenceType = m.ReferenceType,
                ReferenceId = m.ReferenceId,
                StockApresMouvement = m.StockApresMouvement,
                NomUtilisateur = m.Utilisateur != null ? $"{m.Utilisateur.Prenom} {m.Utilisateur.Nom}" : ""
            })
            .ToListAsync();

        return new PagedResult<MouvementStockDto>
        {
            Items = items,
            TotalItems = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    // ==================== Fournisseurs ====================

    public async Task<List<FournisseurDto>> GetFournisseursAsync(bool? actif = null)
    {
        var query = _context.Fournisseurs.AsQueryable();
        if (actif.HasValue)
            query = query.Where(f => f.Actif == actif.Value);

        return await query
            .OrderBy(f => f.NomFournisseur)
            .Select(f => new FournisseurDto
            {
                IdFournisseur = f.IdFournisseur,
                NomFournisseur = f.NomFournisseur,
                ContactNom = f.ContactNom,
                ContactEmail = f.ContactEmail,
                ContactTelephone = f.ContactTelephone,
                Adresse = f.Adresse,
                ConditionsPaiement = f.ConditionsPaiement,
                DelaiLivraisonJours = f.DelaiLivraisonJours,
                Actif = f.Actif
            })
            .ToListAsync();
    }

    public async Task<FournisseurDto> CreateFournisseurAsync(CreateFournisseurRequest request)
    {
        var fournisseur = new Fournisseur
        {
            NomFournisseur = request.NomFournisseur,
            ContactNom = request.ContactNom,
            ContactEmail = request.ContactEmail,
            ContactTelephone = request.ContactTelephone,
            Adresse = request.Adresse,
            ConditionsPaiement = request.ConditionsPaiement,
            DelaiLivraisonJours = request.DelaiLivraisonJours,
            Actif = true,
            DateCreation = DateTime.UtcNow
        };

        _context.Fournisseurs.Add(fournisseur);
        await _context.SaveChangesAsync();

        return new FournisseurDto
        {
            IdFournisseur = fournisseur.IdFournisseur,
            NomFournisseur = fournisseur.NomFournisseur,
            ContactNom = fournisseur.ContactNom,
            ContactEmail = fournisseur.ContactEmail,
            ContactTelephone = fournisseur.ContactTelephone,
            Adresse = fournisseur.Adresse,
            ConditionsPaiement = fournisseur.ConditionsPaiement,
            DelaiLivraisonJours = fournisseur.DelaiLivraisonJours,
            Actif = fournisseur.Actif
        };
    }

    public async Task<FournisseurDto> UpdateFournisseurAsync(int id, CreateFournisseurRequest request)
    {
        var fournisseur = await _context.Fournisseurs.FindAsync(id)
            ?? throw new KeyNotFoundException($"Fournisseur {id} non trouvé");

        fournisseur.NomFournisseur = request.NomFournisseur;
        fournisseur.ContactNom = request.ContactNom;
        fournisseur.ContactEmail = request.ContactEmail;
        fournisseur.ContactTelephone = request.ContactTelephone;
        fournisseur.Adresse = request.Adresse;
        fournisseur.ConditionsPaiement = request.ConditionsPaiement;
        fournisseur.DelaiLivraisonJours = request.DelaiLivraisonJours;

        await _context.SaveChangesAsync();

        return new FournisseurDto
        {
            IdFournisseur = fournisseur.IdFournisseur,
            NomFournisseur = fournisseur.NomFournisseur,
            ContactNom = fournisseur.ContactNom,
            ContactEmail = fournisseur.ContactEmail,
            ContactTelephone = fournisseur.ContactTelephone,
            Adresse = fournisseur.Adresse,
            ConditionsPaiement = fournisseur.ConditionsPaiement,
            DelaiLivraisonJours = fournisseur.DelaiLivraisonJours,
            Actif = fournisseur.Actif
        };
    }

    public async Task<FournisseurDto> ToggleFournisseurStatutAsync(int id)
    {
        var fournisseur = await _context.Fournisseurs.FindAsync(id)
            ?? throw new KeyNotFoundException($"Fournisseur {id} non trouvé");

        fournisseur.Actif = !fournisseur.Actif;
        await _context.SaveChangesAsync();

        return new FournisseurDto
        {
            IdFournisseur = fournisseur.IdFournisseur,
            NomFournisseur = fournisseur.NomFournisseur,
            ContactNom = fournisseur.ContactNom,
            ContactEmail = fournisseur.ContactEmail,
            ContactTelephone = fournisseur.ContactTelephone,
            Adresse = fournisseur.Adresse,
            ConditionsPaiement = fournisseur.ConditionsPaiement,
            DelaiLivraisonJours = fournisseur.DelaiLivraisonJours,
            Actif = fournisseur.Actif
        };
    }

    public async Task<bool> DeleteFournisseurAsync(int id)
    {
        var fournisseur = await _context.Fournisseurs.FindAsync(id);
        if (fournisseur == null)
            return false;

        // Vérifier si le fournisseur a des commandes associées
        var hasCommandes = await _context.CommandesPharmacie
            .AnyAsync(c => c.IdFournisseur == id);

        if (hasCommandes)
        {
            throw new InvalidOperationException("Impossible de supprimer un fournisseur avec des commandes associées");
        }

        _context.Fournisseurs.Remove(fournisseur);
        await _context.SaveChangesAsync();
        return true;
    }

    // ==================== Commandes ====================

    public async Task<PagedResult<CommandePharmacieDto>> GetCommandesAsync(string? statut, int page, int pageSize)
    {
        var query = _context.CommandesPharmacie
            .Include(c => c.Fournisseur)
            .Include(c => c.Utilisateur)
            .Include(c => c.Lignes!)
                .ThenInclude(l => l.Medicament)
            .AsQueryable();

        if (!string.IsNullOrEmpty(statut))
            query = query.Where(c => c.Statut == statut);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.DateCommande)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => MapToCommandeDto(c))
            .ToListAsync();

        return new PagedResult<CommandePharmacieDto>
        {
            Items = items,
            TotalItems = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CommandePharmacieDto?> GetCommandeByIdAsync(int id)
    {
        var commande = await _context.CommandesPharmacie
            .Include(c => c.Fournisseur)
            .Include(c => c.Utilisateur)
            .Include(c => c.Lignes!)
                .ThenInclude(l => l.Medicament)
            .FirstOrDefaultAsync(c => c.IdCommande == id);

        return commande == null ? null : MapToCommandeDto(commande);
    }

    public async Task<CommandePharmacieDto> CreateCommandeAsync(CreateCommandeRequest request, int userId)
    {
        var commande = new CommandePharmacie
        {
            IdFournisseur = request.IdFournisseur,
            DateCommande = DateTime.UtcNow,
            DateReceptionPrevue = request.DateReceptionPrevue,
            Statut = "brouillon",
            Notes = request.Notes,
            IdUser = userId,
            CreatedAt = DateTime.UtcNow,
            Lignes = request.Lignes.Select(l => new CommandeLigne
            {
                IdMedicament = l.IdMedicament,
                QuantiteCommandee = l.QuantiteCommandee,
                QuantiteRecue = 0,
                PrixAchat = l.PrixAchat
            }).ToList()
        };

        commande.MontantTotal = commande.Lignes.Sum(l => l.QuantiteCommandee * l.PrixAchat);

        _context.CommandesPharmacie.Add(commande);
        await _context.SaveChangesAsync();

        // Recharger avec les includes
        return (await GetCommandeByIdAsync(commande.IdCommande))!;
    }

    public async Task<CommandePharmacieDto> ReceptionnerCommandeAsync(int id, ReceptionCommandeRequest request, int userId)
    {
        var commande = await _context.CommandesPharmacie
            .Include(c => c.Lignes)
            .FirstOrDefaultAsync(c => c.IdCommande == id)
            ?? throw new KeyNotFoundException($"Commande {id} non trouvée");

        foreach (var reception in request.Lignes)
        {
            var ligne = commande.Lignes!.FirstOrDefault(l => l.IdLigneCommande == reception.IdLigneCommande);
            if (ligne == null) continue;

            ligne.QuantiteRecue += reception.QuantiteRecue;
            ligne.DatePeremption = reception.DatePeremption;
            ligne.NumeroLot = reception.NumeroLot;

            // Mettre à jour le stock du médicament
            var medicament = await _context.Medicaments.FindAsync(ligne.IdMedicament);
            if (medicament != null)
            {
                var stockAvant = medicament.Stock ?? 0;
                medicament.Stock = stockAvant + reception.QuantiteRecue;
                if (reception.DatePeremption.HasValue)
                    medicament.DatePeremption = reception.DatePeremption;

                // Créer mouvement de stock
                _context.MouvementsStock.Add(new MouvementStock
                {
                    IdMedicament = ligne.IdMedicament,
                    TypeMouvement = "entree",
                    Quantite = reception.QuantiteRecue,
                    Motif = $"Réception commande #{id}",
                    ReferenceId = id,
                    ReferenceType = "commande",
                    IdUser = userId,
                    StockApresMouvement = medicament.Stock.Value,
                    DateMouvement = DateTime.UtcNow
                });
            }
        }

        // Mettre à jour le statut de la commande
        var toutRecu = commande.Lignes!.All(l => l.QuantiteRecue >= l.QuantiteCommandee);
        var partielRecu = commande.Lignes!.Any(l => l.QuantiteRecue > 0);
        
        commande.Statut = toutRecu ? "recue" : (partielRecu ? "partiellement_recue" : commande.Statut);
        commande.DateReceptionReelle = toutRecu ? DateTime.UtcNow : null;
        commande.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (await GetCommandeByIdAsync(id))!;
    }

    public async Task<bool> AnnulerCommandeAsync(int id)
    {
        var commande = await _context.CommandesPharmacie.FindAsync(id);
        if (commande == null) return false;

        commande.Statut = "annulee";
        commande.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<int> GetOrdonnancesEnAttenteCountAsync()
    {
        var ordonnancesDispenseesIds = _context.Dispensations
            .Where(d => d.Statut == "complete")
            .Select(d => d.IdPrescription)
            .ToList();
        
        return await _context.Ordonnances
            .Where(o => o.Medicaments != null && o.Medicaments.Any())
            .Where(o => !ordonnancesDispenseesIds.Contains(o.IdOrdonnance))
            .CountAsync();
    }

    // ==================== Ordonnances/Dispensations ====================

    public async Task<PagedResult<OrdonnancePharmacieDto>> GetOrdonnancesEnAttenteAsync(string? search, int page, int pageSize)
    {
        var query = _context.Ordonnances
            // Relations directes uniquement
            .Include(o => o.Patient!)
                .ThenInclude(p => p.Utilisateur)
            .Include(o => o.Medecin!)
                .ThenInclude(m => m.Utilisateur)
            .Include(o => o.Medicaments!)
                .ThenInclude(pm => pm.Medicament)
            .Where(o => o.Medicaments != null && o.Medicaments.Any())
            .AsQueryable();

        // Filtrer les ordonnances déjà dispensées (séparer pour éviter les problèmes EF Core)
        var ordonnancesDispenseesIds = _context.Dispensations
            .Where(d => d.Statut == "complete")
            .Select(d => d.IdPrescription)
            .ToList();
        
        query = query.Where(o => !ordonnancesDispenseesIds.Contains(o.IdOrdonnance));

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(o => 
                o.Patient != null && o.Patient.Utilisateur != null && 
                (o.Patient.Utilisateur.Nom.Contains(search) || o.Patient.Utilisateur.Prenom.Contains(search)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new List<OrdonnancePharmacieDto>();
        foreach (var o in items)
        {
            var dispensation = await _context.Dispensations
                .Include(d => d.Lignes)
                .FirstOrDefaultAsync(d => d.IdPrescription == o.IdOrdonnance);

            // Chercher la facture associée pour déterminer le statut de paiement
            var facture = await _context.Factures
                .FirstOrDefaultAsync(f => f.Notes != null && f.Notes.Contains($"Ordonnance #{o.IdOrdonnance}") && f.TypeFacture == "pharmacie");

            var estValidee = o.Statut == "validee" || o.Statut == "payee" || o.Statut == "dispensee";
            var estPayee = facture != null && (facture.Statut == "payee" || facture.MontantRestant <= 0);
            var estDelivree = o.Statut == "dispensee";

            result.Add(new OrdonnancePharmacieDto
            {
                IdOrdonnance = o.IdOrdonnance,
                Date = o.Date,
                IdPatient = o.IdPatient ?? o.Consultation?.IdPatient ?? 0,
                NomPatient = o.Patient?.Utilisateur != null 
                    ? $"{o.Patient.Utilisateur.Prenom} {o.Patient.Utilisateur.Nom}" 
                    : (o.Consultation?.Patient?.Utilisateur != null 
                        ? $"{o.Consultation.Patient.Utilisateur.Prenom} {o.Consultation.Patient.Utilisateur.Nom}" : ""),
                NomMedecin = o.Medecin?.Utilisateur != null 
                    ? $"Dr. {o.Medecin.Utilisateur.Prenom} {o.Medecin.Utilisateur.Nom}" 
                    : (o.Consultation?.Medecin?.Utilisateur != null 
                        ? $"Dr. {o.Consultation.Medecin.Utilisateur.Prenom} {o.Consultation.Medecin.Utilisateur.Nom}" : ""),
                Commentaire = o.Commentaire,
                Statut = o.Statut ?? "active",
                DateExpiration = o.DateExpiration,
                Renouvelable = o.Renouvelable,
                // Nouveau workflow
                EstValidee = estValidee,
                EstPayee = estPayee,
                EstDelivree = estDelivree,
                IdFacture = facture?.IdFacture,
                MontantTotal = facture?.MontantTotal,
                MontantRestant = facture?.MontantRestant,
                Medicaments = o.Medicaments?.Select(pm => new MedicamentPrescritDto
                {
                    IdMedicament = pm.IdMedicament,
                    NomMedicament = pm.Medicament?.Nom ?? pm.NomMedicamentLibre ?? "",
                    Dosage = pm.Medicament?.Dosage ?? pm.DosageLibre,
                    EstHorsCatalogue = pm.EstHorsCatalogue,
                    QuantitePrescrite = pm.Quantite,
                    QuantiteDispensee = dispensation?.Lignes?.FirstOrDefault(l => l.IdMedicament == pm.IdMedicament)?.QuantiteDispensee ?? 0,
                    Posologie = pm.Posologie,
                    DureeTraitement = pm.DureeTraitement,
                    StockDisponible = pm.Medicament?.Stock,
                    PrixUnitaire = pm.Medicament?.Prix
                }).ToList() ?? new List<MedicamentPrescritDto>()
            });
        }

        return new PagedResult<OrdonnancePharmacieDto>
        {
            Items = result,
            TotalItems = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<DispensationDto> DispenserOrdonnanceAsync(CreateDispensationRequest request, int pharmacienId)
    {
        var prescription = await _context.Ordonnances
            .Include(o => o.Consultation!)
                .ThenInclude(c => c.Patient!)
                    .ThenInclude(p => p.Utilisateur)
            .Include(o => o.Consultation!)
                .ThenInclude(c => c.Patient!)
                    .ThenInclude(p => p.Assurance)
            .FirstOrDefaultAsync(o => o.IdOrdonnance == request.IdPrescription)
            ?? throw new KeyNotFoundException($"Ordonnance {request.IdPrescription} non trouvée");

        var pharmacien = await _context.Pharmaciens.FirstOrDefaultAsync(p => p.IdUser == pharmacienId)
            ?? throw new UnauthorizedAccessException("Pharmacien non trouvé");

        // Vérifier/créer dispensation
        var dispensation = await _context.Dispensations
            .Include(d => d.Lignes)
            .FirstOrDefaultAsync(d => d.IdPrescription == request.IdPrescription);

        if (dispensation == null)
        {
            dispensation = new Dispensation
            {
                IdPrescription = request.IdPrescription,
                IdPharmacien = pharmacien.IdPharmacien,
                IdPatient = prescription.Consultation!.IdPatient,
                DateDispensation = DateTime.UtcNow,
                Statut = "en_attente",
                Notes = request.Notes,
                Lignes = new List<DispensationLigne>()
            };
            _context.Dispensations.Add(dispensation);
            await _context.SaveChangesAsync();
        }

        // Traiter chaque ligne
        foreach (var ligneReq in request.Lignes)
        {
            var medicament = await _context.Medicaments.FindAsync(ligneReq.IdMedicament);
            if (medicament == null || (medicament.Stock ?? 0) < ligneReq.QuantiteDispensee)
                throw new InvalidOperationException($"Stock insuffisant pour {medicament?.Nom ?? "médicament inconnu"}");

            // Réduire le stock
            var stockAvant = medicament.Stock ?? 0;
            medicament.Stock = stockAvant - ligneReq.QuantiteDispensee;

            // Ajouter ligne dispensation
            var prescriptionMed = await _context.PrescriptionMedicaments
                .FirstOrDefaultAsync(pm => pm.IdOrdonnance == request.IdPrescription && pm.IdMedicament == ligneReq.IdMedicament);

            var ligne = dispensation.Lignes!.FirstOrDefault(l => l.IdMedicament == ligneReq.IdMedicament);
            if (ligne == null)
            {
                ligne = new DispensationLigne
                {
                    IdDispensation = dispensation.IdDispensation,
                    IdMedicament = ligneReq.IdMedicament,
                    QuantitePrescrite = prescriptionMed?.Quantite ?? ligneReq.QuantiteDispensee,
                    QuantiteDispensee = ligneReq.QuantiteDispensee,
                    PrixUnitaire = (decimal?)medicament.Prix,
                    MontantTotal = (decimal?)(ligneReq.QuantiteDispensee * medicament.Prix),
                    NumeroLot = ligneReq.NumeroLot
                };
                dispensation.Lignes!.Add(ligne);
            }
            else
            {
                ligne.QuantiteDispensee += ligneReq.QuantiteDispensee;
                ligne.MontantTotal = (decimal?)(ligne.QuantiteDispensee * medicament.Prix);
            }

            // Créer mouvement de stock
            _context.MouvementsStock.Add(new MouvementStock
            {
                IdMedicament = ligneReq.IdMedicament,
                TypeMouvement = "sortie",
                Quantite = ligneReq.QuantiteDispensee,
                Motif = $"Dispensation ordonnance #{request.IdPrescription}",
                ReferenceId = dispensation.IdDispensation,
                ReferenceType = "prescription",
                IdUser = pharmacienId,
                StockApresMouvement = medicament.Stock.Value,
                DateMouvement = DateTime.UtcNow
            });
        }

        // Vérifier si dispensation complète
        var prescriptionMeds = await _context.PrescriptionMedicaments
            .Where(pm => pm.IdOrdonnance == request.IdPrescription)
            .ToListAsync();

        var toutDispense = prescriptionMeds.All(pm => 
            dispensation.Lignes!.Any(l => l.IdMedicament == pm.IdMedicament && l.QuantiteDispensee >= pm.Quantite));

        dispensation.Statut = toutDispense ? "complete" : "partielle";

        // ==================== FACTURATION PHARMACIE ====================
        var patient = prescription.Consultation?.Patient;
        var montantTotalDispensation = dispensation.Lignes!.Sum(l => l.MontantTotal ?? 0);

        if (patient != null && montantTotalDispensation > 0)
        {
            var now = DateTime.UtcNow;
            var couverture = await _assuranceService.CalculerCouvertureAsync(patient, "pharmacie", montantTotalDispensation);

            // Chercher une facture pharmacie en_attente existante pour cette dispensation
            var factureExistante = await _context.Factures
                .Include(f => f.Lignes)
                .FirstOrDefaultAsync(f => f.IdPatient == patient.IdUser
                    && f.TypeFacture == "pharmacie"
                    && f.Statut == "en_attente"
                    && f.Notes != null && f.Notes.Contains($"Dispensation #{dispensation.IdDispensation}"));

            if (factureExistante != null)
            {
                // Mettre à jour la facture existante (dispensation partielle complétée)
                var anciennesLignes = factureExistante.Lignes.Where(l => l.Categorie == "pharmacie").ToList();
                _context.LignesFacture.RemoveRange(anciennesLignes);

                foreach (var ligne in dispensation.Lignes!)
                {
                    _context.LignesFacture.Add(new LigneFacture
                    {
                        IdFacture = factureExistante.IdFacture,
                        Description = ligne.Medicament?.Nom ?? $"Médicament #{ligne.IdMedicament}",
                        Code = ligne.IdMedicament.ToString(),
                        Quantite = ligne.QuantiteDispensee,
                        PrixUnitaire = ligne.PrixUnitaire ?? 0,
                        Categorie = "pharmacie"
                    });
                }

                factureExistante.MontantTotal = montantTotalDispensation;
                factureExistante.MontantAssurance = couverture.EstAssure ? couverture.MontantAssurance : (decimal?)null;
                factureExistante.MontantRestant = couverture.MontantPatient - factureExistante.MontantPaye;
                if (factureExistante.MontantRestant < 0) factureExistante.MontantRestant = 0;
            }
            else
            {
                // Créer une nouvelle facture pharmacie
                var numeroFacture = $"PHA-{now:yyyyMMdd}-{now:HHmmss}-{patient.IdUser}";
                var facture = new Facture
                {
                    NumeroFacture = numeroFacture,
                    IdPatient = patient.IdUser,
                    MontantTotal = montantTotalDispensation,
                    MontantPaye = 0,
                    MontantRestant = couverture.MontantPatient,
                    Statut = "en_attente",
                    TypeFacture = "pharmacie",
                    DateCreation = now,
                    DateEcheance = now.AddDays(30),
                    CouvertureAssurance = couverture.EstAssure,
                    IdAssurance = couverture.IdAssurance,
                    TauxCouverture = couverture.EstAssure ? couverture.TauxCouverture : (decimal?)null,
                    MontantAssurance = couverture.EstAssure ? couverture.MontantAssurance : (decimal?)null,
                    Notes = $"Dispensation #{dispensation.IdDispensation} - Ordonnance #{request.IdPrescription}"
                };

                _context.Factures.Add(facture);
                await _context.SaveChangesAsync();

                foreach (var ligne in dispensation.Lignes!)
                {
                    _context.LignesFacture.Add(new LigneFacture
                    {
                        IdFacture = facture.IdFacture,
                        Description = ligne.Medicament?.Nom ?? $"Médicament #{ligne.IdMedicament}",
                        Code = ligne.IdMedicament.ToString(),
                        Quantite = ligne.QuantiteDispensee,
                        PrixUnitaire = ligne.PrixUnitaire ?? 0,
                        Categorie = "pharmacie"
                    });
                }
            }
        }

        await _context.SaveChangesAsync();

        return await GetDispensationDtoAsync(dispensation.IdDispensation);
    }

    public async Task<PagedResult<DispensationDto>> GetDispensationsAsync(DateTime? dateDebut, DateTime? dateFin, int page, int pageSize)
    {
        var query = _context.Dispensations
            .Include(d => d.Patient!)
                .ThenInclude(p => p.Utilisateur)
            .Include(d => d.Pharmacien!)
                .ThenInclude(ph => ph.Utilisateur)
            .Include(d => d.Lignes!)
                .ThenInclude(l => l.Medicament)
            .AsQueryable();

        if (dateDebut.HasValue)
            query = query.Where(d => d.DateDispensation >= dateDebut);

        if (dateFin.HasValue)
            query = query.Where(d => d.DateDispensation <= dateFin.Value.AddDays(1));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(d => d.DateDispensation)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = items.Select(d => new DispensationDto
        {
            IdDispensation = d.IdDispensation,
            IdPrescription = d.IdPrescription,
            NomPatient = d.Patient?.Utilisateur != null ? $"{d.Patient.Utilisateur.Prenom} {d.Patient.Utilisateur.Nom}" : "",
            NomPharmacien = d.Pharmacien?.Utilisateur != null ? $"{d.Pharmacien.Utilisateur.Prenom} {d.Pharmacien.Utilisateur.Nom}" : "",
            DateDispensation = d.DateDispensation,
            Statut = d.Statut,
            Notes = d.Notes,
            MontantTotal = d.Lignes?.Sum(l => l.MontantTotal ?? 0) ?? 0,
            Lignes = d.Lignes?.Select(l => new DispensationLigneDto
            {
                IdLigne = l.IdLigne,
                IdMedicament = l.IdMedicament,
                NomMedicament = l.Medicament?.Nom ?? "",
                QuantitePrescrite = l.QuantitePrescrite,
                QuantiteDispensee = l.QuantiteDispensee,
                PrixUnitaire = l.PrixUnitaire,
                MontantTotal = l.MontantTotal,
                NumeroLot = l.NumeroLot
            }).ToList() ?? new List<DispensationLigneDto>()
        }).ToList();

        return new PagedResult<DispensationDto>
        {
            Items = result,
            TotalItems = total,
            Page = page,
            PageSize = pageSize
        };
    }

    // ==================== Helpers ====================

    private async Task<MedicamentStockDto> MapToMedicamentDtoAsync(Medicament m, List<FournisseurMedicamentDto>? fournisseurs = null)
    {
        var aujourdhui = DateTime.UtcNow.Date;
        var joursAvantPeremption = m.DatePeremption.HasValue 
            ? (int?)(m.DatePeremption.Value - aujourdhui).Days 
            : null;

        string statut = "normal";
        if (m.Stock == 0 || m.Stock == null) statut = "rupture";
        else if (m.Stock <= m.SeuilStock) statut = "alerte";

        return new MedicamentStockDto
        {
            IdMedicament = m.IdMedicament,
            Nom = m.Nom,
            Dosage = m.Dosage,
            FormeGalenique = m.FormeGalenique,
            Laboratoire = m.Laboratoire,
            Stock = m.Stock,
            SeuilStock = m.SeuilStock,
            Prix = m.Prix,
            DatePeremption = m.DatePeremption,
            EmplacementRayon = m.EmplacementRayon,
            CodeATC = m.CodeATC,
            Actif = m.Actif,
            Conditionnement = m.Conditionnement,
            TemperatureConservation = m.TemperatureConservation,
            StatutStock = statut,
            JoursAvantPeremption = joursAvantPeremption,
            Fournisseurs = fournisseurs
        };
    }

    private static MedicamentStockDto MapToMedicamentDto(Medicament m)
    {
        var aujourdhui = DateTime.UtcNow.Date;
        var joursAvantPeremption = m.DatePeremption.HasValue 
            ? (int?)(m.DatePeremption.Value - aujourdhui).Days 
            : null;

        string statut = "normal";
        if (m.Stock == 0 || m.Stock == null) statut = "rupture";
        else if (m.Stock <= m.SeuilStock) statut = "alerte";

        return new MedicamentStockDto
        {
            IdMedicament = m.IdMedicament,
            Nom = m.Nom,
            Dosage = m.Dosage,
            FormeGalenique = m.FormeGalenique,
            Laboratoire = m.Laboratoire,
            Stock = m.Stock,
            SeuilStock = m.SeuilStock,
            Prix = m.Prix,
            DatePeremption = m.DatePeremption,
            EmplacementRayon = m.EmplacementRayon,
            CodeATC = m.CodeATC,
            Actif = m.Actif,
            Conditionnement = m.Conditionnement,
            TemperatureConservation = m.TemperatureConservation,
            StatutStock = statut,
            JoursAvantPeremption = joursAvantPeremption
        };
    }

    private static CommandePharmacieDto MapToCommandeDto(CommandePharmacie c)
    {
        return new CommandePharmacieDto
        {
            IdCommande = c.IdCommande,
            IdFournisseur = c.IdFournisseur,
            NomFournisseur = c.Fournisseur?.NomFournisseur ?? "",
            DateCommande = c.DateCommande,
            DateReceptionPrevue = c.DateReceptionPrevue,
            DateReceptionReelle = c.DateReceptionReelle,
            Statut = c.Statut,
            MontantTotal = c.MontantTotal,
            Notes = c.Notes,
            NomUtilisateur = c.Utilisateur != null ? $"{c.Utilisateur.Prenom} {c.Utilisateur.Nom}" : "",
            Lignes = c.Lignes?.Select(l => new CommandeLigneDto
            {
                IdLigneCommande = l.IdLigneCommande,
                IdMedicament = l.IdMedicament,
                NomMedicament = l.Medicament?.Nom ?? "",
                QuantiteCommandee = l.QuantiteCommandee,
                QuantiteRecue = l.QuantiteRecue,
                PrixAchat = l.PrixAchat,
                DatePeremption = l.DatePeremption,
                NumeroLot = l.NumeroLot
            }).ToList() ?? new List<CommandeLigneDto>()
        };
    }

    private async Task<DispensationDto> GetDispensationDtoAsync(int idDispensation)
    {
        var d = await _context.Dispensations
            .Include(d => d.Patient!)
                .ThenInclude(p => p.Utilisateur)
            .Include(d => d.Pharmacien!)
                .ThenInclude(ph => ph.Utilisateur)
            .Include(d => d.Lignes!)
                .ThenInclude(l => l.Medicament)
            .FirstAsync(d => d.IdDispensation == idDispensation);

        return new DispensationDto
        {
            IdDispensation = d.IdDispensation,
            IdPrescription = d.IdPrescription,
            NomPatient = d.Patient?.Utilisateur != null ? $"{d.Patient.Utilisateur.Prenom} {d.Patient.Utilisateur.Nom}" : "",
            NomPharmacien = d.Pharmacien?.Utilisateur != null ? $"{d.Pharmacien.Utilisateur.Prenom} {d.Pharmacien.Utilisateur.Nom}" : "",
            DateDispensation = d.DateDispensation,
            Statut = d.Statut,
            Notes = d.Notes,
            MontantTotal = d.Lignes?.Sum(l => l.MontantTotal ?? 0) ?? 0,
            Lignes = d.Lignes?.Select(l => new DispensationLigneDto
            {
                IdLigne = l.IdLigne,
                IdMedicament = l.IdMedicament,
                NomMedicament = l.Medicament?.Nom ?? "",
                QuantitePrescrite = l.QuantitePrescrite,
                QuantiteDispensee = l.QuantiteDispensee,
                PrixUnitaire = l.PrixUnitaire,
                MontantTotal = l.MontantTotal,
                NumeroLot = l.NumeroLot
            }).ToList() ?? new List<DispensationLigneDto>()
        };
    }

    // ==================== NOUVEAU WORKFLOW PHARMACIE ====================
    // Prescription → Validation (Facture) → Paiement → Délivrance (Stock)

    /// <summary>
    /// Valide une ordonnance : crée la facture associée SANS impact sur le stock.
    /// Le patient peut ensuite aller payer à la caisse.
    /// </summary>
    public async Task<ValidationOrdonnanceResult> ValiderOrdonnanceAsync(int idOrdonnance, int pharmacienId)
    {
        var ordonnance = await _context.Ordonnances
            // Relations directes
            .Include(o => o.Patient!)
                .ThenInclude(p => p.Utilisateur)
            .Include(o => o.Patient!)
                .ThenInclude(p => p.Assurance)
            // Fallback via consultation
            .Include(o => o.Consultation!)
                .ThenInclude(c => c.Patient!)
                    .ThenInclude(p => p.Utilisateur)
            .Include(o => o.Consultation!)
                .ThenInclude(c => c.Patient!)
                    .ThenInclude(p => p.Assurance)
            .Include(o => o.Medicaments!)
                .ThenInclude(pm => pm.Medicament)
            .FirstOrDefaultAsync(o => o.IdOrdonnance == idOrdonnance);

        if (ordonnance == null)
        {
            return new ValidationOrdonnanceResult
            {
                Success = false,
                Message = "Ordonnance non trouvée"
            };
        }

        // Vérifier que l'ordonnance n'est pas déjà validée ou délivrée
        if (ordonnance.Statut != "active")
        {
            return new ValidationOrdonnanceResult
            {
                Success = false,
                Message = $"L'ordonnance ne peut pas être validée (statut actuel: {ordonnance.Statut})"
            };
        }

        // Vérifier qu'il n'existe pas déjà une facture pour cette ordonnance
        var factureExistante = await _context.Factures
            .FirstOrDefaultAsync(f => f.Notes != null && f.Notes.Contains($"Ordonnance #{idOrdonnance}") && f.TypeFacture == "pharmacie");

        if (factureExistante != null)
        {
            return new ValidationOrdonnanceResult
            {
                Success = false,
                Message = "Une facture existe déjà pour cette ordonnance",
                IdFacture = factureExistante.IdFacture,
                NumeroFacture = factureExistante.NumeroFacture
            };
        }

        // Récupérer le patient (relation directe ou via consultation)
        var patient = ordonnance.Patient ?? ordonnance.Consultation?.Patient;
        if (patient == null)
        {
            return new ValidationOrdonnanceResult
            {
                Success = false,
                Message = "Patient non trouvé pour cette ordonnance"
            };
        }

        // Calculer le montant total (médicaments du catalogue ET hors catalogue)
        decimal montantTotal = 0;
        var medicamentsFacturables = new List<(PrescriptionMedicament med, string nom, decimal prix)>();
        
        foreach (var med in ordonnance.Medicaments ?? Enumerable.Empty<PrescriptionMedicament>())
        {
            decimal prixUnitaire = 0;
            string nomMedicament = med.NomMedicamentEffectif;
            
            if (med.IdMedicament.HasValue && med.Medicament != null)
            {
                // Médicament du catalogue
                prixUnitaire = (decimal)(med.Medicament.Prix ?? 0);
            }
            else if (med.EstHorsCatalogue || !string.IsNullOrEmpty(med.NomMedicamentLibre))
            {
                // Médicament hors catalogue - chercher un prix par défaut ou utiliser 0
                // On peut facturer même sans prix (le pharmacien ajustera)
                prixUnitaire = 0; // Prix à définir par le pharmacien lors de la délivrance
            }
            
            // Ajouter à la liste des médicaments facturables (même si prix = 0)
            if (!string.IsNullOrEmpty(nomMedicament))
            {
                medicamentsFacturables.Add((med, nomMedicament, prixUnitaire));
                montantTotal += prixUnitaire * med.Quantite;
            }
        }

        if (medicamentsFacturables.Count == 0)
        {
            return new ValidationOrdonnanceResult
            {
                Success = false,
                Message = "Aucun médicament dans cette ordonnance"
            };
        }

        // Calculer la couverture assurance
        var couverture = await _assuranceService.CalculerCouvertureAsync(patient, "pharmacie", montantTotal);

        // Créer la facture
        var now = DateTime.UtcNow;
        var numeroFacture = $"PHA-{now:yyyyMMdd}-{now:HHmmss}-{patient.IdUser}";
        var facture = new Facture
        {
            NumeroFacture = numeroFacture,
            IdPatient = patient.IdUser,
            MontantTotal = montantTotal,
            MontantPaye = 0,
            MontantRestant = couverture.MontantPatient,
            Statut = "en_attente",
            TypeFacture = "pharmacie",
            DateCreation = now,
            DateEcheance = now.AddDays(30),
            CouvertureAssurance = couverture.EstAssure,
            IdAssurance = couverture.IdAssurance,
            TauxCouverture = couverture.EstAssure ? couverture.TauxCouverture : (decimal?)null,
            MontantAssurance = couverture.EstAssure ? couverture.MontantAssurance : (decimal?)null,
            Notes = $"Ordonnance #{idOrdonnance}"
        };

        _context.Factures.Add(facture);
        await _context.SaveChangesAsync();

        // Ajouter les lignes de facture (tous les médicaments facturables)
        foreach (var (med, nom, prix) in medicamentsFacturables)
        {
            _context.LignesFacture.Add(new LigneFacture
            {
                IdFacture = facture.IdFacture,
                Description = nom,
                Code = med.IdMedicament?.ToString() ?? $"HC-{med.IdPrescriptionMed}",
                Quantite = med.Quantite,
                PrixUnitaire = prix,
                Categorie = "medicament" // Valeur ENUM valide: consultation, medicament, hospitalisation, examen
            });
        }

        // Mettre à jour le statut de l'ordonnance
        ordonnance.Statut = "validee";
        ordonnance.UpdatedAt = now;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Ordonnance {IdOrdonnance} validée par pharmacien {PharmacienId}. Facture {NumeroFacture} créée. Montant: {Montant}",
            idOrdonnance, pharmacienId, numeroFacture, montantTotal);

        return new ValidationOrdonnanceResult
        {
            Success = true,
            Message = "Ordonnance validée et facture créée. Le patient peut aller payer à la caisse.",
            IdOrdonnance = idOrdonnance,
            IdFacture = facture.IdFacture,
            NumeroFacture = numeroFacture,
            MontantTotal = montantTotal,
            MontantAssurance = couverture.MontantAssurance,
            MontantPatient = couverture.MontantPatient,
            StatutOrdonnance = "validee"
        };
    }

    /// <summary>
    /// Délivre les médicaments d'une ordonnance PAYÉE.
    /// Décrémente le stock et enregistre la dispensation.
    /// </summary>
    public async Task<DelivranceResult> DelivrerOrdonnanceAsync(int idOrdonnance, int pharmacienId)
    {
        var ordonnance = await _context.Ordonnances
            // Relations directes uniquement
            .Include(o => o.Patient!)
                .ThenInclude(p => p.Utilisateur)
            .Include(o => o.Medicaments!)
                .ThenInclude(pm => pm.Medicament)
            .FirstOrDefaultAsync(o => o.IdOrdonnance == idOrdonnance);

        if (ordonnance == null)
        {
            return new DelivranceResult
            {
                Success = false,
                Message = "Ordonnance non trouvée"
            };
        }

        // Vérifier que l'ordonnance est validée ou payée
        if (ordonnance.Statut != "validee" && ordonnance.Statut != "payee")
        {
            return new DelivranceResult
            {
                Success = false,
                Message = $"L'ordonnance doit être validée et payée avant délivrance (statut actuel: {ordonnance.Statut})"
            };
        }

        // Vérifier que la facture est payée
        var facture = await _context.Factures
            .FirstOrDefaultAsync(f => f.Notes != null && f.Notes.Contains($"Ordonnance #{idOrdonnance}") && f.TypeFacture == "pharmacie");

        if (facture == null)
        {
            return new DelivranceResult
            {
                Success = false,
                Message = "Aucune facture trouvée pour cette ordonnance. Veuillez d'abord valider l'ordonnance."
            };
        }

        if (facture.Statut != "payee" && facture.MontantRestant > 0)
        {
            return new DelivranceResult
            {
                Success = false,
                Message = $"La facture n'est pas entièrement payée. Montant restant: {facture.MontantRestant:N0} FCFA"
            };
        }

        var pharmacien = await _context.Pharmaciens.FirstOrDefaultAsync(p => p.IdUser == pharmacienId);
        if (pharmacien == null)
        {
            _logger.LogError("Pharmacien non trouvé pour IdUser {PharmacienId}", pharmacienId);
            return new DelivranceResult
            {
                Success = false,
                Message = "Pharmacien non trouvé"
            };
        }

        // Vérifier le stock pour tous les médicaments
        var erreurs = new List<string>();
        foreach (var med in ordonnance.Medicaments ?? Enumerable.Empty<PrescriptionMedicament>())
        {
            if (med.IdMedicament.HasValue && med.Medicament != null)
            {
                var stock = med.Medicament.Stock ?? 0;
                if (stock < med.Quantite)
                {
                    erreurs.Add($"Stock insuffisant pour {med.Medicament.Nom}: {stock} disponibles, {med.Quantite} demandés");
                }
            }
        }

        if (erreurs.Any())
        {
            return new DelivranceResult
            {
                Success = false,
                Message = "Stock insuffisant pour certains médicaments",
                Erreurs = erreurs
            };
        }

        // Récupérer l'ID patient
        var idPatient = ordonnance.IdPatient ?? ordonnance.Consultation?.IdPatient;
        if (!idPatient.HasValue || idPatient.Value == 0)
        {
            _logger.LogError("Ordonnance {IdOrdonnance}: IdPatient non trouvé", idOrdonnance);
            return new DelivranceResult
            {
                Success = false,
                Message = "Patient non trouvé pour cette ordonnance"
            };
        }

        // Vérifier si une dispensation existe déjà
        var existingDispensation = await _context.Dispensations
            .FirstOrDefaultAsync(d => d.IdPrescription == idOrdonnance && d.Statut == "complete");
        
        if (existingDispensation != null)
        {
            return new DelivranceResult
            {
                Success = false,
                Message = "Cette ordonnance a déjà été délivrée"
            };
        }

        // Créer la dispensation
        var dispensation = new Dispensation
        {
            IdPrescription = idOrdonnance,
            IdPharmacien = pharmacien.IdUser,
            IdPatient = idPatient.Value,
            DateDispensation = DateTime.UtcNow,
            Statut = "complete",
            Notes = $"Délivrance après paiement facture #{facture.NumeroFacture}",
            Lignes = new List<DispensationLigne>()
        };
        _context.Dispensations.Add(dispensation);
        await _context.SaveChangesAsync();

        var lignesDelivrees = new List<LigneDelivranceDto>();

        // Décrémenter le stock et créer les lignes de dispensation
        foreach (var med in ordonnance.Medicaments ?? Enumerable.Empty<PrescriptionMedicament>())
        {
            if (med.IdMedicament.HasValue && med.Medicament != null)
            {
                var medicament = await _context.Medicaments.FindAsync(med.IdMedicament.Value);
                if (medicament == null) continue;

                var stockAvant = medicament.Stock ?? 0;
                medicament.Stock = stockAvant - med.Quantite;

                // Créer la ligne de dispensation
                var ligne = new DispensationLigne
                {
                    IdDispensation = dispensation.IdDispensation,
                    IdMedicament = med.IdMedicament.Value,
                    QuantitePrescrite = med.Quantite,
                    QuantiteDispensee = med.Quantite,
                    PrixUnitaire = (decimal?)(medicament.Prix),
                    MontantTotal = (decimal?)(med.Quantite * medicament.Prix)
                };
                dispensation.Lignes!.Add(ligne);

                // Créer le mouvement de stock
                _context.MouvementsStock.Add(new MouvementStock
                {
                    IdMedicament = med.IdMedicament.Value,
                    TypeMouvement = "sortie",
                    Quantite = med.Quantite,
                    Motif = $"Délivrance ordonnance #{idOrdonnance}",
                    ReferenceId = dispensation.IdDispensation,
                    ReferenceType = "dispensation",
                    IdUser = pharmacienId,
                    StockApresMouvement = medicament.Stock ?? 0,
                    DateMouvement = DateTime.UtcNow
                });

                lignesDelivrees.Add(new LigneDelivranceDto
                {
                    IdMedicament = med.IdMedicament.Value,
                    NomMedicament = medicament.Nom,
                    QuantiteDelivree = med.Quantite,
                    StockRestant = medicament.Stock ?? 0
                });
            }
        }

        // Mettre à jour le statut de l'ordonnance
        ordonnance.Statut = "dispensee";
        ordonnance.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Ordonnance {IdOrdonnance} délivrée par pharmacien {PharmacienId}. {NbMedicaments} médicaments délivrés.",
            idOrdonnance, pharmacienId, lignesDelivrees.Count);

        return new DelivranceResult
        {
            Success = true,
            Message = "Médicaments délivrés avec succès",
            IdOrdonnance = idOrdonnance,
            IdDispensation = dispensation.IdDispensation,
            StatutOrdonnance = "dispensee",
            LignesDelivrees = lignesDelivrees
        };
    }

    /// <summary>
    /// Récupère le détail d'une ordonnance avec son statut de paiement
    /// </summary>
    public async Task<OrdonnancePharmacieDetailDto?> GetOrdonnanceDetailAsync(int idOrdonnance)
    {
        var ordonnance = await _context.Ordonnances
            // Relations directes
            .Include(o => o.Patient!)
                .ThenInclude(p => p.Utilisateur)
            .Include(o => o.Medecin!)
                .ThenInclude(m => m.Utilisateur)
            // Fallback via consultation
            .Include(o => o.Consultation!)
                .ThenInclude(c => c.Patient!)
                    .ThenInclude(p => p.Utilisateur)
            .Include(o => o.Consultation!)
                .ThenInclude(c => c.Medecin!)
                    .ThenInclude(m => m.Utilisateur)
            .Include(o => o.Medicaments!)
                .ThenInclude(pm => pm.Medicament)
            .FirstOrDefaultAsync(o => o.IdOrdonnance == idOrdonnance);

        if (ordonnance == null) return null;

        // Chercher la facture associée
        var facture = await _context.Factures
            .FirstOrDefaultAsync(f => f.Notes != null && f.Notes.Contains($"Ordonnance #{idOrdonnance}") && f.TypeFacture == "pharmacie");

        var estValidee = ordonnance.Statut == "validee" || ordonnance.Statut == "payee" || ordonnance.Statut == "dispensee";
        var estPayee = facture != null && (facture.Statut == "payee" || facture.MontantRestant <= 0);
        var estDelivree = ordonnance.Statut == "dispensee";

        return new OrdonnancePharmacieDetailDto
        {
            IdOrdonnance = ordonnance.IdOrdonnance,
            Date = ordonnance.Date,
            IdPatient = ordonnance.IdPatient ?? ordonnance.Consultation?.IdPatient ?? 0,
            NomPatient = ordonnance.Patient?.Utilisateur != null
                ? $"{ordonnance.Patient.Utilisateur.Prenom} {ordonnance.Patient.Utilisateur.Nom}"
                : (ordonnance.Consultation?.Patient?.Utilisateur != null
                    ? $"{ordonnance.Consultation.Patient.Utilisateur.Prenom} {ordonnance.Consultation.Patient.Utilisateur.Nom}"
                    : ""),
            NomMedecin = ordonnance.Medecin?.Utilisateur != null
                ? $"Dr. {ordonnance.Medecin.Utilisateur.Prenom} {ordonnance.Medecin.Utilisateur.Nom}"
                : (ordonnance.Consultation?.Medecin?.Utilisateur != null
                    ? $"Dr. {ordonnance.Consultation.Medecin.Utilisateur.Prenom} {ordonnance.Consultation.Medecin.Utilisateur.Nom}"
                    : ""),
            Commentaire = ordonnance.Commentaire,
            StatutOrdonnance = ordonnance.Statut,
            EstValidee = estValidee,
            EstPayee = estPayee,
            EstDelivree = estDelivree,
            IdFacture = facture?.IdFacture,
            NumeroFacture = facture?.NumeroFacture,
            MontantTotal = facture?.MontantTotal,
            MontantRestant = facture?.MontantRestant,
            StatutFacture = facture?.Statut,
            DateExpiration = ordonnance.DateExpiration,
            Renouvelable = ordonnance.Renouvelable,
            Medicaments = ordonnance.Medicaments?.Select(pm => new MedicamentPrescritDto
            {
                IdMedicament = pm.IdMedicament,
                NomMedicament = pm.Medicament?.Nom ?? pm.NomMedicamentLibre ?? "",
                Dosage = pm.Medicament?.Dosage ?? pm.DosageLibre,
                EstHorsCatalogue = pm.EstHorsCatalogue,
                QuantitePrescrite = pm.Quantite,
                QuantiteDispensee = 0, // Sera mis à jour si dispensation existe
                Posologie = pm.Posologie,
                DureeTraitement = pm.DureeTraitement,
                StockDisponible = pm.Medicament?.Stock,
                PrixUnitaire = pm.Medicament?.Prix
            }).ToList() ?? new List<MedicamentPrescritDto>()
        };
    }

    public async Task<PharmacieProfileDto> GetProfileAsync(int userId)
    {
        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(u => u.IdUser == userId)
            ?? throw new KeyNotFoundException($"Utilisateur {userId} non trouvé");

        var pharmacien = await _context.Pharmaciens
            .FirstOrDefaultAsync(p => p.IdUser == userId);

        return new PharmacieProfileDto
        {
            IdPharmacien = pharmacien?.IdUser ?? utilisateur.IdUser,
            Nom = utilisateur.Nom,
            Prenom = utilisateur.Prenom,
            Email = utilisateur.Email,
            Telephone = utilisateur.Telephone,
            Photo = utilisateur.Photo,
            Specialite = pharmacien?.NumeroOrdre != null ? "Pharmacien hospitalier" : "Pharmacien",
            NumeroLicence = pharmacien?.NumeroOrdre ?? pharmacien?.Matricule,
            PharmacieNom = utilisateur.Adresse ?? "Pharmacie Centrale",
            CreatedAt = pharmacien?.CreatedAt ?? utilisateur.CreatedAt ?? DateTime.UtcNow
        };
    }

    public async Task<PharmacieDashboardDto> GetDashboardAsync(int userId)
    {
        // Vérifier que l'utilisateur existe bien (pharmacien ou support)
        var utilisateurExiste = await _context.Utilisateurs.AnyAsync(u => u.IdUser == userId);
        if (!utilisateurExiste)
            throw new KeyNotFoundException($"Utilisateur {userId} non trouvé");

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var tomorrow = now.Date.AddDays(1);
        var today = now.Date;

        var totalMedicamentsTask = _context.Medicaments
            .Where(m => m.Actif)
            .CountAsync();

        var commandesMoisTask = _context.CommandesPharmacie
            .Where(c => c.IdUser == userId && c.DateCommande >= startOfMonth)
            .CountAsync();

        var ordonnancesAujourdHuiTask = _context.Dispensations
            .Where(d => d.IdPharmacien == userId && d.DateDispensation >= today && d.DateDispensation < tomorrow)
            .CountAsync();

        var fournisseursActifsTask = _context.Fournisseurs
            .Where(f => f.Actif)
            .CountAsync();

        await Task.WhenAll(totalMedicamentsTask, commandesMoisTask, ordonnancesAujourdHuiTask, fournisseursActifsTask);

        return new PharmacieDashboardDto
        {
            TotalMedicaments = totalMedicamentsTask.Result,
            CommandesMois = commandesMoisTask.Result,
            OrdonnancesAujourdHui = ordonnancesAujourdHuiTask.Result,
            FournisseursActifs = fournisseursActifsTask.Result
        };
    }

    public async Task<PharmacieProfileDto> UpdateProfileAsync(int userId, UpdatePharmacieProfileRequest request)
    {
        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(u => u.IdUser == userId)
            ?? throw new KeyNotFoundException($"Utilisateur {userId} non trouvé");

        var pharmacien = await _context.Pharmaciens
            .FirstOrDefaultAsync(p => p.IdUser == userId);

        // Mise à jour des champs utilisateur
        if (request.Telephone != null)
            utilisateur.Telephone = request.Telephone;
        if (request.Photo != null)
            utilisateur.Photo = request.Photo;

        // Mise à jour des champs pharmacien (si existant)
        if (pharmacien != null)
        {
            if (request.NumeroLicence != null)
                pharmacien.NumeroOrdre = request.NumeroLicence;
            pharmacien.UpdatedAt = DateTime.UtcNow;
        }

        // Mettre à jour le champ Adresse pour stocker le nom de la pharmacie
        if (request.PharmacieNom != null)
            utilisateur.Adresse = request.PharmacieNom;

        utilisateur.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Retourner le profil mis à jour
        return await GetProfileAsync(userId);
    }

    // ==================== Ventes Directes ====================

    public async Task<VenteDirecteResult> CreerVenteDirecteAsync(CreateVenteDirecteRequest request, int pharmacienId)
    {
        var result = new VenteDirecteResult();
        
        try
        {
            // Vérifier que le pharmacien existe
            var pharmacien = await _context.Pharmaciens
                .Include(p => p.Utilisateur)
                .FirstOrDefaultAsync(p => p.IdUser == pharmacienId);
            
            if (pharmacien == null)
            {
                result.Success = false;
                result.Message = "Pharmacien non trouvé";
                return result;
            }

            // Vérifier qu'il y a au moins une ligne
            if (request.Lignes == null || !request.Lignes.Any())
            {
                result.Success = false;
                result.Message = "La vente doit contenir au moins un médicament";
                return result;
            }

            // Vérifier le stock et calculer le montant total
            decimal montantTotal = 0;
            var lignesValidees = new List<(Medicament med, int quantite, decimal prixUnitaire)>();

            foreach (var ligne in request.Lignes)
            {
                var medicament = await _context.Medicaments.FindAsync(ligne.IdMedicament);
                
                if (medicament == null)
                {
                    result.Erreurs.Add($"Médicament ID {ligne.IdMedicament} non trouvé");
                    continue;
                }

                if (!medicament.Actif)
                {
                    result.Erreurs.Add($"Le médicament '{medicament.Nom}' n'est plus disponible à la vente");
                    continue;
                }

                var stockDisponible = medicament.Stock ?? 0;
                if (stockDisponible < ligne.Quantite)
                {
                    result.Erreurs.Add($"Stock insuffisant pour '{medicament.Nom}' (disponible: {stockDisponible}, demandé: {ligne.Quantite})");
                    continue;
                }

                var prixUnitaire = (decimal)(medicament.Prix ?? 0);
                montantTotal += prixUnitaire * ligne.Quantite;
                lignesValidees.Add((medicament, ligne.Quantite, prixUnitaire));
            }

            // Si des erreurs critiques, arrêter
            if (result.Erreurs.Any() && !lignesValidees.Any())
            {
                result.Success = false;
                result.Message = "Aucun médicament valide dans la vente";
                return result;
            }

            // Générer le numéro de ticket
            var numeroTicket = await GenererNumeroTicketAsync();

            // Créer la dispensation (vente directe) - statut en_attente (pas encore payé)
            var dispensation = new Dispensation
            {
                IdPrescription = null, // Pas d'ordonnance
                IdPharmacien = pharmacien.IdUser, // Utiliser IdUser car c'est la PK de la table pharmacien
                IdPatient = request.IdPatientEnregistre,
                DateDispensation = DateTime.UtcNow,
                Statut = "en_attente", // En attente de paiement à la caisse
                Notes = request.Notes,
                TypeVente = "vente_directe",
                NomClient = request.NomClient,
                TelephoneClient = request.TelephoneClient,
                MontantTotal = montantTotal,
                ModePaiement = request.ModePaiement,
                NumeroTicket = numeroTicket,
                Lignes = new List<DispensationLigne>()
            };

            _context.Dispensations.Add(dispensation);
            await _context.SaveChangesAsync();

            // Créer les lignes SANS décrémenter le stock (sera fait à la délivrance)
            foreach (var (medicament, quantite, prixUnitaire) in lignesValidees)
            {
                // Créer la ligne de dispensation
                var ligne = new DispensationLigne
                {
                    IdDispensation = dispensation.IdDispensation,
                    IdMedicament = medicament.IdMedicament,
                    QuantitePrescrite = quantite, // Pour vente directe, prescrit = dispensé
                    QuantiteDispensee = quantite,
                    PrixUnitaire = prixUnitaire,
                    MontantTotal = prixUnitaire * quantite
                };
                _context.DispensationsLignes.Add(ligne);

                // Ajouter à la liste des lignes pour le résultat
                result.Lignes.Add(new VenteDirecteLigneDto
                {
                    IdLigne = ligne.IdLigne,
                    IdMedicament = medicament.IdMedicament,
                    NomMedicament = medicament.Nom,
                    Dosage = medicament.Dosage,
                    Quantite = quantite,
                    PrixUnitaire = prixUnitaire,
                    MontantTotal = prixUnitaire * quantite,
                    StockRestant = medicament.Stock ?? 0
                });
            }

            await _context.SaveChangesAsync();

            result.Success = true;
            result.Message = "Vente facturée - En attente de paiement à la caisse";
            result.IdDispensation = dispensation.IdDispensation;
            result.NumeroTicket = numeroTicket;
            result.MontantTotal = montantTotal;

            _logger.LogInformation("Vente directe créée: Ticket {NumeroTicket}, Montant {Montant}, Pharmacien {PharmacienId}", 
                numeroTicket, montantTotal, pharmacienId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de la vente directe");
            result.Success = false;
            result.Message = "Erreur lors de l'enregistrement de la vente";
            result.Erreurs.Add(ex.Message);
            return result;
        }
    }

    /// <summary>
    /// Délivre une vente directe après paiement à la caisse
    /// Décrémente le stock et met à jour le statut
    /// </summary>
    public async Task<VenteDirecteResult> DelivrerVenteDirecteAsync(int idDispensation, int pharmacienId)
    {
        var result = new VenteDirecteResult();

        try
        {
            // Récupérer la dispensation avec ses lignes
            var dispensation = await _context.Dispensations
                .Include(d => d.Lignes!)
                    .ThenInclude(l => l.Medicament)
                .FirstOrDefaultAsync(d => d.IdDispensation == idDispensation && d.TypeVente == "vente_directe");

            if (dispensation == null)
            {
                result.Success = false;
                result.Message = "Vente directe non trouvée";
                return result;
            }

            // Vérifier le statut
            if (dispensation.Statut != "paye")
            {
                result.Success = false;
                result.Message = dispensation.Statut == "delivre" 
                    ? "Cette vente a déjà été délivrée"
                    : "Cette vente n'a pas encore été payée à la caisse";
                return result;
            }

            // Décrémenter le stock pour chaque ligne
            foreach (var ligne in dispensation.Lignes ?? new List<DispensationLigne>())
            {
                var medicament = ligne.Medicament;
                if (medicament == null) continue;

                var stockAvant = medicament.Stock ?? 0;
                var quantite = ligne.QuantiteDispensee;

                // Vérifier le stock disponible
                if (stockAvant < quantite)
                {
                    result.Success = false;
                    result.Message = $"Stock insuffisant pour {medicament.Nom} (disponible: {stockAvant}, demandé: {quantite})";
                    return result;
                }

                // Décrémenter le stock
                medicament.Stock = stockAvant - quantite;

                // Créer le mouvement de stock
                var mouvement = new MouvementStock
                {
                    IdMedicament = medicament.IdMedicament,
                    TypeMouvement = "sortie",
                    Quantite = quantite,
                    DateMouvement = DateTime.UtcNow,
                    Motif = $"Délivrance vente directe - Ticket {dispensation.NumeroTicket}",
                    ReferenceId = dispensation.IdDispensation,
                    ReferenceType = "vente_directe",
                    IdUser = pharmacienId,
                    StockApresMouvement = medicament.Stock ?? 0
                };
                _context.MouvementsStock.Add(mouvement);
            }

            // Mettre à jour le statut
            dispensation.Statut = "delivre";

            await _context.SaveChangesAsync();

            result.Success = true;
            result.Message = "Médicaments délivrés avec succès";
            result.IdDispensation = dispensation.IdDispensation;
            result.NumeroTicket = dispensation.NumeroTicket;
            result.MontantTotal = dispensation.MontantTotal ?? 0;

            _logger.LogInformation("Vente directe délivrée: Ticket {NumeroTicket}, Pharmacien {PharmacienId}", 
                dispensation.NumeroTicket, pharmacienId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la délivrance de la vente directe {IdDispensation}", idDispensation);
            result.Success = false;
            result.Message = "Erreur lors de la délivrance";
            return result;
        }
    }

    public async Task<PagedResult<VenteDirecteDto>> GetVentesDirectesAsync(VenteDirecteFilter filter)
    {
        var query = _context.Dispensations
            .Include(d => d.Pharmacien)
                .ThenInclude(p => p!.Utilisateur)
            .Include(d => d.Patient)
                .ThenInclude(p => p!.Utilisateur)
            .Include(d => d.Lignes!)
                .ThenInclude(l => l.Medicament)
            .Where(d => d.TypeVente == "vente_directe")
            .AsQueryable();

        // Filtres
        if (filter.DateDebut.HasValue)
            query = query.Where(d => d.DateDispensation >= filter.DateDebut.Value);
        
        if (filter.DateFin.HasValue)
            query = query.Where(d => d.DateDispensation <= filter.DateFin.Value.AddDays(1));
        
        if (!string.IsNullOrWhiteSpace(filter.NomClient))
            query = query.Where(d => d.NomClient != null && d.NomClient.Contains(filter.NomClient));
        
        if (!string.IsNullOrWhiteSpace(filter.NumeroTicket))
            query = query.Where(d => d.NumeroTicket != null && d.NumeroTicket.Contains(filter.NumeroTicket));

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(d => d.DateDispensation)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(d => new VenteDirecteDto
            {
                IdDispensation = d.IdDispensation,
                DateVente = d.DateDispensation,
                NomClient = d.NomClient,
                TelephoneClient = d.TelephoneClient,
                NomPharmacien = d.Pharmacien != null && d.Pharmacien.Utilisateur != null 
                    ? $"{d.Pharmacien.Utilisateur.Prenom} {d.Pharmacien.Utilisateur.Nom}" 
                    : "Inconnu",
                Statut = d.Statut,
                Notes = d.Notes,
                MontantTotal = d.MontantTotal ?? 0,
                ModePaiement = d.ModePaiement,
                NumeroTicket = d.NumeroTicket,
                TypeVente = d.TypeVente,
                IdPatient = d.IdPatient,
                NomPatientEnregistre = d.Patient != null && d.Patient.Utilisateur != null 
                    ? $"{d.Patient.Utilisateur.Prenom} {d.Patient.Utilisateur.Nom}" 
                    : null,
                Lignes = d.Lignes!.Select(l => new VenteDirecteLigneDto
                {
                    IdLigne = l.IdLigne,
                    IdMedicament = l.IdMedicament,
                    NomMedicament = l.Medicament != null ? l.Medicament.Nom : "Inconnu",
                    Dosage = l.Medicament != null ? l.Medicament.Dosage : null,
                    Quantite = l.QuantiteDispensee,
                    PrixUnitaire = l.PrixUnitaire ?? 0,
                    MontantTotal = l.MontantTotal ?? 0,
                    StockRestant = l.Medicament != null ? l.Medicament.Stock ?? 0 : 0
                }).ToList()
            })
            .ToListAsync();

        return new PagedResult<VenteDirecteDto>
        {
            Items = items,
            TotalItems = totalItems,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<VenteDirecteDto?> GetVenteDirecteByIdAsync(int idDispensation)
    {
        var dispensation = await _context.Dispensations
            .Include(d => d.Pharmacien)
                .ThenInclude(p => p!.Utilisateur)
            .Include(d => d.Patient)
                .ThenInclude(p => p!.Utilisateur)
            .Include(d => d.Lignes!)
                .ThenInclude(l => l.Medicament)
            .FirstOrDefaultAsync(d => d.IdDispensation == idDispensation && d.TypeVente == "vente_directe");

        if (dispensation == null)
            return null;

        return new VenteDirecteDto
        {
            IdDispensation = dispensation.IdDispensation,
            DateVente = dispensation.DateDispensation,
            NomClient = dispensation.NomClient,
            TelephoneClient = dispensation.TelephoneClient,
            NomPharmacien = dispensation.Pharmacien?.Utilisateur != null 
                ? $"{dispensation.Pharmacien.Utilisateur.Prenom} {dispensation.Pharmacien.Utilisateur.Nom}" 
                : "Inconnu",
            Statut = dispensation.Statut,
            Notes = dispensation.Notes,
            MontantTotal = dispensation.MontantTotal ?? 0,
            ModePaiement = dispensation.ModePaiement,
            NumeroTicket = dispensation.NumeroTicket,
            TypeVente = dispensation.TypeVente,
            IdPatient = dispensation.IdPatient,
            NomPatientEnregistre = dispensation.Patient?.Utilisateur != null 
                ? $"{dispensation.Patient.Utilisateur.Prenom} {dispensation.Patient.Utilisateur.Nom}" 
                : null,
            Lignes = dispensation.Lignes?.Select(l => new VenteDirecteLigneDto
            {
                IdLigne = l.IdLigne,
                IdMedicament = l.IdMedicament,
                NomMedicament = l.Medicament?.Nom ?? "Inconnu",
                Dosage = l.Medicament?.Dosage,
                Quantite = l.QuantiteDispensee,
                PrixUnitaire = l.PrixUnitaire ?? 0,
                MontantTotal = l.MontantTotal ?? 0,
                StockRestant = l.Medicament?.Stock ?? 0
            }).ToList() ?? new List<VenteDirecteLigneDto>()
        };
    }

    /// <summary>
    /// Génère un numéro de ticket unique pour les ventes directes
    /// Format: VD-YYYYMMDD-XXXX (ex: VD-20260323-0001)
    /// </summary>
    private async Task<string> GenererNumeroTicketAsync()
    {
        var today = DateTime.UtcNow.Date;
        var prefix = $"VD-{today:yyyyMMdd}-";
        
        // Compter les ventes du jour
        var countToday = await _context.Dispensations
            .Where(d => d.TypeVente == "vente_directe" 
                && d.DateDispensation >= today 
                && d.DateDispensation < today.AddDays(1))
            .CountAsync();
        
        return $"{prefix}{(countToday + 1):D4}";
    }
}
