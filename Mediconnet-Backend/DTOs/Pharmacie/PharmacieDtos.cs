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
    public string Statut { get; set; } = "en_attente"; // en_attente, partielle, complete
    public List<MedicamentPrescritDto> Medicaments { get; set; } = new();
}

public class MedicamentPrescritDto
{
    public int IdMedicament { get; set; }
    public string NomMedicament { get; set; } = "";
    public string? Dosage { get; set; }
    public int QuantitePrescrite { get; set; }
    public int QuantiteDispensee { get; set; }
    public string? Posologie { get; set; }
    public string? DureeTraitement { get; set; }
    public int? StockDisponible { get; set; }
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
    public int IdPrescription { get; set; }
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

// ==================== Pagination ====================

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}
