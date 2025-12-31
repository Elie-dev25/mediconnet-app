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
    
    // Médicaments/Stock
    Task<PagedResult<MedicamentStockDto>> GetMedicamentsAsync(string? search, string? statut, int page, int pageSize);
    Task<MedicamentStockDto?> GetMedicamentByIdAsync(int id);
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
}

public class PharmacieStockService : IPharmacieStockService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PharmacieStockService> _logger;

    public PharmacieStockService(ApplicationDbContext context, ILogger<PharmacieStockService> logger)
    {
        _context = context;
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
            OrdonnancesEnAttente = await _context.Ordonnances
                .Where(o => !_context.Dispensations.Any(d => d.IdPrescription == o.IdOrdonnance && d.Statut == "complete"))
                .CountAsync(),
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
        return med == null ? null : MapToMedicamentDto(med);
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

    // ==================== Ordonnances/Dispensations ====================

    public async Task<PagedResult<OrdonnancePharmacieDto>> GetOrdonnancesEnAttenteAsync(string? search, int page, int pageSize)
    {
        var query = _context.Ordonnances
            .Include(o => o.Consultation!)
                .ThenInclude(c => c.Patient!)
                    .ThenInclude(p => p.Utilisateur)
            .Include(o => o.Consultation!)
                .ThenInclude(c => c.Medecin!)
                    .ThenInclude(m => m.Utilisateur)
            .Include(o => o.Medicaments!)
                .ThenInclude(pm => pm.Medicament)
            .Where(o => !_context.Dispensations.Any(d => d.IdPrescription == o.IdOrdonnance && d.Statut == "complete"))
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(o => 
                o.Consultation!.Patient!.Utilisateur!.Nom.Contains(search) ||
                o.Consultation!.Patient!.Utilisateur!.Prenom.Contains(search));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = items.Select(o => {
            var dispensation = _context.Dispensations
                .Include(d => d.Lignes)
                .FirstOrDefault(d => d.IdPrescription == o.IdOrdonnance);

            return new OrdonnancePharmacieDto
            {
                IdOrdonnance = o.IdOrdonnance,
                Date = o.Date,
                IdPatient = o.Consultation?.IdPatient ?? 0,
                NomPatient = o.Consultation?.Patient?.Utilisateur != null 
                    ? $"{o.Consultation.Patient.Utilisateur.Prenom} {o.Consultation.Patient.Utilisateur.Nom}" : "",
                NomMedecin = o.Consultation?.Medecin?.Utilisateur != null 
                    ? $"Dr. {o.Consultation.Medecin.Utilisateur.Prenom} {o.Consultation.Medecin.Utilisateur.Nom}" : "",
                Commentaire = o.Commentaire,
                Statut = dispensation?.Statut ?? "en_attente",
                Medicaments = o.Medicaments?.Select(pm => new MedicamentPrescritDto
                {
                    IdMedicament = pm.IdMedicament,
                    NomMedicament = pm.Medicament?.Nom ?? "",
                    Dosage = pm.Medicament?.Dosage,
                    QuantitePrescrite = pm.Quantite,
                    QuantiteDispensee = dispensation?.Lignes?.FirstOrDefault(l => l.IdMedicament == pm.IdMedicament)?.QuantiteDispensee ?? 0,
                    Posologie = pm.Posologie,
                    DureeTraitement = pm.DureeTraitement,
                    StockDisponible = pm.Medicament?.Stock,
                    PrixUnitaire = pm.Medicament?.Prix
                }).ToList() ?? new List<MedicamentPrescritDto>()
            };
        }).ToList();

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
                .ThenInclude(c => c.Patient)
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
                CreatedAt = DateTime.UtcNow,
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
        dispensation.UpdatedAt = DateTime.UtcNow;

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
}
