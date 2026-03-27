namespace Mediconnet_Backend.DTOs.Pharmacie;

// ==================== KPIs & Dashboard ====================

public class PharmacieKpiDto
{
    public int TotalMedicaments { get; set; }
    public int MedicamentsEnAlerte { get; set; }
    public int MedicamentsEnRupture { get; set; }
    public int MedicamentsPerimesProches { get; set; }
    public int OrdonnancesEnAttente { get; set; }
    public int DispensationsJour { get; set; }
    public decimal ValeurStock { get; set; }
    public int CommandesEnCours { get; set; }
}

public class PharmacieProfileDto
{
    public int IdPharmacien { get; set; }
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Telephone { get; set; }
    public string? Photo { get; set; }
    public string? Specialite { get; set; }
    public string? NumeroLicence { get; set; }
    public string? PharmacieNom { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class PharmacieDashboardDto
{
    public int TotalMedicaments { get; set; }
    public int CommandesMois { get; set; }
    public int OrdonnancesAujourdHui { get; set; }
    public int FournisseursActifs { get; set; }
}

public class UpdatePharmacieProfileRequest
{
    public string? Telephone { get; set; }
    public string? Photo { get; set; }
    public string? Specialite { get; set; }
    public string? NumeroLicence { get; set; }
    public string? PharmacieNom { get; set; }
}

// ==================== Médicaments/Stock ====================

public class MedicamentStockDto
{
    public int IdMedicament { get; set; }
    public string Nom { get; set; } = "";
    public string? Dosage { get; set; }
    public string? FormeGalenique { get; set; }
    public string? Laboratoire { get; set; }
    public int? Stock { get; set; }
    public int? SeuilStock { get; set; }
    public float? Prix { get; set; }
    public DateTime? DatePeremption { get; set; }
    public string? EmplacementRayon { get; set; }
    public string? CodeATC { get; set; }
    public bool Actif { get; set; }
    public string? Conditionnement { get; set; }
    public string? TemperatureConservation { get; set; }
    public string StatutStock { get; set; } = "normal"; // normal, alerte, rupture
    public int? JoursAvantPeremption { get; set; }
    public List<FournisseurMedicamentDto>? Fournisseurs { get; set; }
}

public class FournisseurMedicamentDto
{
    public int IdFournisseur { get; set; }
    public string NomFournisseur { get; set; } = "";
    public string? ContactNom { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactTelephone { get; set; }
    public int DelaiLivraisonJours { get; set; }
    public DateTime? DerniereCommande { get; set; }
    public int TotalCommandes { get; set; }
    
    // Détails du médicament pour éviter toute confusion
    public int IdMedicament { get; set; }
    public string NomMedicament { get; set; } = "";
    public string? Dosage { get; set; }
    public string? Laboratoire { get; set; }
    public string? FormeGalenique { get; set; }
}

public class HistoriqueFournisseurMedicamentDto
{
    public int IdCommande { get; set; }
    public DateTime DateCommande { get; set; }
    public DateTime? DateReceptionPrevue { get; set; }
    public DateTime? DateReceptionReelle { get; set; }
    public string Statut { get; set; } = "";
    public decimal MontantTotal { get; set; }
    public int QuantiteCommandee { get; set; }
    public int QuantiteRecue { get; set; }
    public decimal PrixAchat { get; set; }
    public string? NumeroLot { get; set; }
    public DateTime? DatePeremption { get; set; }
    
    // Infos fournisseur
    public int IdFournisseur { get; set; }
    public string NomFournisseur { get; set; } = "";
    
    // Infos médicament
    public int IdMedicament { get; set; }
    public string NomMedicament { get; set; } = "";
    public string? Dosage { get; set; }
    public string? Laboratoire { get; set; }
}

public class CreateMedicamentRequest
{
    public string Nom { get; set; } = "";
    public string? Dosage { get; set; }
    public string? FormeGalenique { get; set; }
    public string? Laboratoire { get; set; }
    public int Stock { get; set; }
    public int SeuilStock { get; set; } = 10;
    public float Prix { get; set; }
    public DateTime? DatePeremption { get; set; }
    public string? EmplacementRayon { get; set; }
    public string? CodeATC { get; set; }
    public string? Conditionnement { get; set; }
    public string? TemperatureConservation { get; set; }
}

public class UpdateMedicamentRequest
{
    public string? Nom { get; set; }
    public string? Dosage { get; set; }
    public string? FormeGalenique { get; set; }
    public string? Laboratoire { get; set; }
    public int? SeuilStock { get; set; }
    public float? Prix { get; set; }
    public DateTime? DatePeremption { get; set; }
    public string? EmplacementRayon { get; set; }
    public string? CodeATC { get; set; }
    public string? Conditionnement { get; set; }
    public string? TemperatureConservation { get; set; }
    public bool? Actif { get; set; }
}

public class AjustementStockRequest
{
    public int IdMedicament { get; set; }
    public int Quantite { get; set; }
    public string TypeMouvement { get; set; } = "ajustement"; // entree, sortie, ajustement, perte
    public string? Motif { get; set; }
}

// ==================== Mouvements de Stock ====================

public class MouvementStockDto
{
    public int IdMouvement { get; set; }
    public int IdMedicament { get; set; }
    public string NomMedicament { get; set; } = "";
    public string TypeMouvement { get; set; } = "";
    public int Quantite { get; set; }
    public DateTime DateMouvement { get; set; }
    public string? Motif { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public int StockApresMouvement { get; set; }
    public string NomUtilisateur { get; set; } = "";
}

public class MouvementStockFilter
{
    public int? IdMedicament { get; set; }
    public string? TypeMouvement { get; set; }
    public DateTime? DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// ==================== Fournisseurs ====================

public class FournisseurDto
{
    public int IdFournisseur { get; set; }
    public string NomFournisseur { get; set; } = "";
    public string? ContactNom { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactTelephone { get; set; }
    public string? Adresse { get; set; }
    public string? ConditionsPaiement { get; set; }
    public int DelaiLivraisonJours { get; set; }
    public bool Actif { get; set; }
}

public class CreateFournisseurRequest
{
    public string NomFournisseur { get; set; } = "";
    public string? ContactNom { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactTelephone { get; set; }
    public string? Adresse { get; set; }
    public string? ConditionsPaiement { get; set; }
    public int DelaiLivraisonJours { get; set; } = 7;
}

// ==================== Commandes ====================

public class CommandePharmacieDto
{
    public int IdCommande { get; set; }
    public int IdFournisseur { get; set; }
    public string NomFournisseur { get; set; } = "";
    public DateTime DateCommande { get; set; }
    public DateTime? DateReceptionPrevue { get; set; }
    public DateTime? DateReceptionReelle { get; set; }
    public string Statut { get; set; } = "";
    public decimal MontantTotal { get; set; }
    public string? Notes { get; set; }
    public string NomUtilisateur { get; set; } = "";
    public List<CommandeLigneDto> Lignes { get; set; } = new();
}

public class CommandeLigneDto
{
    public int IdLigneCommande { get; set; }
    public int IdMedicament { get; set; }
    public string NomMedicament { get; set; } = "";
    public int QuantiteCommandee { get; set; }
    public int QuantiteRecue { get; set; }
    public decimal PrixAchat { get; set; }
    public DateTime? DatePeremption { get; set; }
    public string? NumeroLot { get; set; }
}

public class CreateCommandeRequest
{
    public int IdFournisseur { get; set; }
    public DateTime? DateReceptionPrevue { get; set; }
    public string? Notes { get; set; }
    public List<CreateCommandeLigneRequest> Lignes { get; set; } = new();
}

public class CreateCommandeLigneRequest
{
    public int IdMedicament { get; set; }
    public int QuantiteCommandee { get; set; }
    public decimal PrixAchat { get; set; }
}

public class ReceptionCommandeRequest
{
    public List<ReceptionLigneRequest> Lignes { get; set; } = new();
}

public class ReceptionLigneRequest
{
    public int IdLigneCommande { get; set; }
    public int QuantiteRecue { get; set; }
    public DateTime? DatePeremption { get; set; }
    public string? NumeroLot { get; set; }
}

// ==================== Ordonnances/Dispensations ====================

public class OrdonnancePharmacieDto
{
    public int IdOrdonnance { get; set; }
    public DateTime Date { get; set; }
    public int IdPatient { get; set; }
    public string NomPatient { get; set; } = "";
    public string NomMedecin { get; set; } = "";
    public string? Commentaire { get; set; }
    
    /// <summary>
    /// Statut de l'ordonnance : active, validee, payee, dispensee, partielle, annulee, expiree
    /// </summary>
    public string Statut { get; set; } = "active";
    
    public List<MedicamentPrescritDto> Medicaments { get; set; } = new();
    
    /// <summary>
    /// Date d'expiration de l'ordonnance
    /// </summary>
    public DateTime? DateExpiration { get; set; }
    
    /// <summary>
    /// Indique si l'ordonnance est expirée
    /// </summary>
    public bool EstExpiree => DateExpiration.HasValue && DateExpiration.Value < DateTime.UtcNow;
    
    /// <summary>
    /// Indique si l'ordonnance est renouvelable
    /// </summary>
    public bool Renouvelable { get; set; }
    
    // ==================== Nouveau workflow ====================
    
    /// <summary>
    /// Indique si l'ordonnance a été validée (facture créée)
    /// </summary>
    public bool EstValidee { get; set; }
    
    /// <summary>
    /// Indique si la facture est payée (délivrance possible)
    /// </summary>
    public bool EstPayee { get; set; }
    
    /// <summary>
    /// Indique si les médicaments ont été délivrés
    /// </summary>
    public bool EstDelivree { get; set; }
    
    /// <summary>
    /// ID de la facture associée (si validée)
    /// </summary>
    public int? IdFacture { get; set; }
    
    /// <summary>
    /// Montant total de la facture
    /// </summary>
    public decimal? MontantTotal { get; set; }
    
    /// <summary>
    /// Montant restant à payer
    /// </summary>
    public decimal? MontantRestant { get; set; }
}

public class MedicamentPrescritDto
{
    /// <summary>
    /// ID du médicament dans le catalogue (null si hors catalogue)
    /// </summary>
    public int? IdMedicament { get; set; }
    
    /// <summary>
    /// Nom du médicament (catalogue ou saisie libre)
    /// </summary>
    public string NomMedicament { get; set; } = "";
    
    public string? Dosage { get; set; }
    
    /// <summary>
    /// Indique si le médicament est hors catalogue (saisie libre)
    /// </summary>
    public bool EstHorsCatalogue { get; set; } = false;
    
    public int QuantitePrescrite { get; set; }
    public int QuantiteDispensee { get; set; }
    public string? Posologie { get; set; }
    public string? DureeTraitement { get; set; }
    
    /// <summary>
    /// Stock disponible (null si hors catalogue - non géré en stock)
    /// </summary>
    public int? StockDisponible { get; set; }
    
    /// <summary>
    /// Prix unitaire (null si hors catalogue - pas de prix référencé)
    /// </summary>
    public float? PrixUnitaire { get; set; }
}

public class CreateDispensationRequest
{
    public int IdPrescription { get; set; }
    public string? Notes { get; set; }
    public List<DispensationLigneRequest> Lignes { get; set; } = new();
}

public class DispensationLigneRequest
{
    public int IdMedicament { get; set; }
    public int QuantiteDispensee { get; set; }
    public string? NumeroLot { get; set; }
}

public class DispensationDto
{
    public int IdDispensation { get; set; }
    public int? IdPrescription { get; set; }
    public string NomPatient { get; set; } = "";
    public string NomPharmacien { get; set; } = "";
    public DateTime DateDispensation { get; set; }
    public string Statut { get; set; } = "";
    public string? Notes { get; set; }
    public decimal MontantTotal { get; set; }
    public List<DispensationLigneDto> Lignes { get; set; } = new();
}

public class DispensationLigneDto
{
    public int IdLigne { get; set; }
    public int IdMedicament { get; set; }
    public string NomMedicament { get; set; } = "";
    public int QuantitePrescrite { get; set; }
    public int QuantiteDispensee { get; set; }
    public decimal? PrixUnitaire { get; set; }
    public decimal? MontantTotal { get; set; }
    public string? NumeroLot { get; set; }
}

// ==================== Alertes ====================

public class AlerteStockDto
{
    public string Type { get; set; } = ""; // rupture, alerte, peremption
    public int IdMedicament { get; set; }
    public string NomMedicament { get; set; } = "";
    public int? StockActuel { get; set; }
    public int? SeuilAlerte { get; set; }
    public DateTime? DatePeremption { get; set; }
    public int? JoursRestants { get; set; }
    public string Priorite { get; set; } = "medium"; // high, medium, low
}

// ==================== Nouveau Workflow Pharmacie ====================

/// <summary>
/// Résultat de la validation d'une ordonnance (création de facture)
/// </summary>
public class ValidationOrdonnanceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int? IdOrdonnance { get; set; }
    public int? IdFacture { get; set; }
    public string? NumeroFacture { get; set; }
    public decimal MontantTotal { get; set; }
    public decimal MontantAssurance { get; set; }
    public decimal MontantPatient { get; set; }
    public string StatutOrdonnance { get; set; } = "";
}

/// <summary>
/// Résultat de la délivrance d'une ordonnance
/// </summary>
public class DelivranceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int? IdOrdonnance { get; set; }
    public int? IdDispensation { get; set; }
    public string StatutOrdonnance { get; set; } = "";
    public List<LigneDelivranceDto> LignesDelivrees { get; set; } = new();
    public List<string> Erreurs { get; set; } = new();
}

public class LigneDelivranceDto
{
    public int IdMedicament { get; set; }
    public string NomMedicament { get; set; } = "";
    public int QuantiteDelivree { get; set; }
    public int StockRestant { get; set; }
}

/// <summary>
/// Détail complet d'une ordonnance pour la pharmacie avec statut de paiement
/// </summary>
public class OrdonnancePharmacieDetailDto
{
    public int IdOrdonnance { get; set; }
    public DateTime Date { get; set; }
    public int IdPatient { get; set; }
    public string NomPatient { get; set; } = "";
    public string NomMedecin { get; set; } = "";
    public string? Commentaire { get; set; }
    
    /// <summary>
    /// Statut de l'ordonnance : active, validee, payee, dispensee, partielle, annulee, expiree
    /// </summary>
    public string StatutOrdonnance { get; set; } = "active";
    
    /// <summary>
    /// Indique si l'ordonnance a été validée (facture créée)
    /// </summary>
    public bool EstValidee { get; set; }
    
    /// <summary>
    /// Indique si la facture est payée (délivrance possible)
    /// </summary>
    public bool EstPayee { get; set; }
    
    /// <summary>
    /// Indique si les médicaments ont été délivrés
    /// </summary>
    public bool EstDelivree { get; set; }
    
    /// <summary>
    /// ID de la facture associée (si validée)
    /// </summary>
    public int? IdFacture { get; set; }
    
    /// <summary>
    /// Numéro de la facture associée
    /// </summary>
    public string? NumeroFacture { get; set; }
    
    /// <summary>
    /// Montant total de la facture
    /// </summary>
    public decimal? MontantTotal { get; set; }
    
    /// <summary>
    /// Montant restant à payer
    /// </summary>
    public decimal? MontantRestant { get; set; }
    
    /// <summary>
    /// Statut de la facture
    /// </summary>
    public string? StatutFacture { get; set; }
    
    public DateTime? DateExpiration { get; set; }
    public bool EstExpiree => DateExpiration.HasValue && DateExpiration.Value < DateTime.UtcNow;
    public bool Renouvelable { get; set; }
    
    public List<MedicamentPrescritDto> Medicaments { get; set; } = new();
}

// ==================== Ventes Directes ====================

/// <summary>
/// Requête pour créer une vente directe sans ordonnance
/// </summary>
public class CreateVenteDirecteRequest
{
    public List<VenteDirecteLigneRequest> Lignes { get; set; } = new();
    public string? NomClient { get; set; }
    public string? TelephoneClient { get; set; }
    public int? IdPatientEnregistre { get; set; }
    public string? Notes { get; set; }
    public string ModePaiement { get; set; } = "especes";
}

/// <summary>
/// Ligne d'une vente directe
/// </summary>
public class VenteDirecteLigneRequest
{
    public int IdMedicament { get; set; }
    public int Quantite { get; set; }
}

/// <summary>
/// DTO pour afficher une vente directe
/// </summary>
public class VenteDirecteDto
{
    public int IdDispensation { get; set; }
    public DateTime DateVente { get; set; }
    public string? NomClient { get; set; }
    public string? TelephoneClient { get; set; }
    public string NomPharmacien { get; set; } = "";
    public string Statut { get; set; } = "";
    public string? Notes { get; set; }
    public decimal MontantTotal { get; set; }
    public string? ModePaiement { get; set; }
    public string? NumeroTicket { get; set; }
    public string TypeVente { get; set; } = "vente_directe";
    public List<VenteDirecteLigneDto> Lignes { get; set; } = new();
    
    // Si client enregistré
    public int? IdPatient { get; set; }
    public string? NomPatientEnregistre { get; set; }
}

/// <summary>
/// Ligne d'une vente directe (affichage)
/// </summary>
public class VenteDirecteLigneDto
{
    public int IdLigne { get; set; }
    public int IdMedicament { get; set; }
    public string NomMedicament { get; set; } = "";
    public string? Dosage { get; set; }
    public int Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
    public decimal MontantTotal { get; set; }
    public int StockRestant { get; set; }
}

/// <summary>
/// Résultat de la création d'une vente directe
/// </summary>
public class VenteDirecteResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int? IdDispensation { get; set; }
    public string? NumeroTicket { get; set; }
    public decimal MontantTotal { get; set; }
    public List<VenteDirecteLigneDto> Lignes { get; set; } = new();
    public List<string> Erreurs { get; set; } = new();
}

/// <summary>
/// Filtre pour rechercher les ventes directes
/// </summary>
public class VenteDirecteFilter
{
    public DateTime? DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public string? NomClient { get; set; }
    public string? NumeroTicket { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

// ==================== Dispensation étendue ====================

/// <summary>
/// DTO étendu pour afficher une dispensation (avec ou sans ordonnance)
/// </summary>
public class DispensationEtendueDto
{
    public int IdDispensation { get; set; }
    public int? IdPrescription { get; set; }
    public DateTime DateDispensation { get; set; }
    public string TypeVente { get; set; } = "avec_ordonnance";
    public string Statut { get; set; } = "";
    public string? Notes { get; set; }
    public decimal MontantTotal { get; set; }
    public string? ModePaiement { get; set; }
    public string? NumeroTicket { get; set; }
    
    // Informations patient/client
    public int? IdPatient { get; set; }
    public string? NomPatient { get; set; }
    public string? NomClient { get; set; }
    public string? TelephoneClient { get; set; }
    
    // Pharmacien
    public string NomPharmacien { get; set; } = "";
    
    // Lignes
    public List<DispensationLigneDto> Lignes { get; set; } = new();
    
    // Helpers
    public bool EstVenteDirecte => TypeVente == "vente_directe";
    public string NomAffiche => EstVenteDirecte ? (NomClient ?? "Client anonyme") : (NomPatient ?? "Patient inconnu");
}

// ==================== Pagination ====================

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}
