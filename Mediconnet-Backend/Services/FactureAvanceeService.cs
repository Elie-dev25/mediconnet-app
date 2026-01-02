using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Entities.Facturation;
using Mediconnet_Backend.Data;
using System.Text;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service de facturation avancée - PDF, échéanciers, assurances
/// </summary>
public class FactureAvanceeService : IFactureService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FactureAvanceeService> _logger;

    public FactureAvanceeService(ApplicationDbContext context, ILogger<FactureAvanceeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<byte[]> GenerateFacturePdfAsync(int idFacture)
    {
        var facture = await _context.Factures
            .Include(f => f.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(f => f.Lignes)
            .Include(f => f.Service)
            .FirstOrDefaultAsync(f => f.IdFacture == idFacture);

        if (facture == null) throw new Exception("Facture non trouvée");

        // Génération HTML pour PDF (à convertir avec une bibliothèque comme iTextSharp ou DinkToPdf)
        var html = GenerateFactureHtml(facture);
        
        // Pour l'instant, retourner le HTML encodé en bytes
        // Dans une vraie implémentation, utiliser une bibliothèque PDF
        return Encoding.UTF8.GetBytes(html);
    }

    public async Task<byte[]> GenerateRecuPdfAsync(int idTransaction)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Facture).ThenInclude(f => f!.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(t => t.Caissier).ThenInclude(c => c!.Utilisateur)
            .FirstOrDefaultAsync(t => t.IdTransaction == idTransaction);

        if (transaction == null) throw new Exception("Transaction non trouvée");

        var html = GenerateRecuHtml(transaction);
        return Encoding.UTF8.GetBytes(html);
    }

    public async Task<EcheancierDto> CreerEcheancierAsync(CreateEcheancierRequest request)
    {
        var facture = await _context.Factures.FindAsync(request.IdFacture);
        if (facture == null) throw new Exception("Facture non trouvée");

        var montantParEcheance = Math.Round(facture.MontantRestant / request.NombreEcheances, 2);

        var echeancier = new Echeancier
        {
            IdFacture = request.IdFacture,
            MontantTotal = facture.MontantRestant,
            NombreEcheances = request.NombreEcheances,
            MontantParEcheance = montantParEcheance,
            DateDebut = request.DatePremierPaiement,
            Frequence = request.Frequence,
            Statut = "actif"
        };

        _context.Echeanciers.Add(echeancier);
        await _context.SaveChangesAsync();

        // Créer les échéances individuelles
        var dateEcheance = request.DatePremierPaiement;
        for (int i = 1; i <= request.NombreEcheances; i++)
        {
            var echeance = new Echeance
            {
                IdEcheancier = echeancier.IdEcheancier,
                NumeroEcheance = i,
                Montant = i == request.NombreEcheances 
                    ? facture.MontantRestant - (montantParEcheance * (request.NombreEcheances - 1)) 
                    : montantParEcheance,
                DateEcheance = dateEcheance,
                Statut = "en_attente"
            };

            _context.Echeances.Add(echeance);
            dateEcheance = GetNextEcheanceDate(dateEcheance, request.Frequence);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Échéancier créé pour facture {IdFacture}: {NombreEcheances} échéances", 
            request.IdFacture, request.NombreEcheances);

        return await GetEcheancierAsync(request.IdFacture) ?? throw new Exception("Erreur lors de la création");
    }

    public async Task<EcheancierDto?> GetEcheancierAsync(int idFacture)
    {
        var echeancier = await _context.Echeanciers
            .Include(e => e.Facture)
            .Include(e => e.Echeances)
            .FirstOrDefaultAsync(e => e.IdFacture == idFacture && e.Statut == "actif");

        if (echeancier == null) return null;

        return new EcheancierDto
        {
            IdEcheancier = echeancier.IdEcheancier,
            IdFacture = echeancier.IdFacture,
            NumeroFacture = echeancier.Facture?.NumeroFacture ?? "",
            MontantTotal = echeancier.MontantTotal,
            NombreEcheances = echeancier.NombreEcheances,
            MontantParEcheance = echeancier.MontantParEcheance,
            DateDebut = echeancier.DateDebut,
            Frequence = echeancier.Frequence,
            Statut = echeancier.Statut,
            Echeances = echeancier.Echeances.OrderBy(e => e.NumeroEcheance).Select(e => new EcheanceDto
            {
                IdEcheance = e.IdEcheance,
                NumeroEcheance = e.NumeroEcheance,
                Montant = e.Montant,
                DateEcheance = e.DateEcheance,
                DatePaiement = e.DatePaiement,
                Statut = e.DateEcheance < DateTime.UtcNow && e.Statut == "en_attente" ? "en_retard" : e.Statut,
                IdTransaction = e.IdTransaction,
                JoursRetard = e.Statut == "en_attente" && e.DateEcheance < DateTime.UtcNow 
                    ? (int)(DateTime.UtcNow - e.DateEcheance).TotalDays : 0
            }).ToList()
        };
    }

    public async Task<List<EcheanceDto>> GetEcheancesEnRetardAsync()
    {
        var today = DateTime.UtcNow.Date;

        return await _context.Echeances
            .Include(e => e.Echeancier).ThenInclude(ec => ec!.Facture)
            .Where(e => e.Statut == "en_attente" && e.DateEcheance < today)
            .Select(e => new EcheanceDto
            {
                IdEcheance = e.IdEcheance,
                NumeroEcheance = e.NumeroEcheance,
                Montant = e.Montant,
                DateEcheance = e.DateEcheance,
                Statut = "en_retard",
                JoursRetard = (int)(today - e.DateEcheance).TotalDays
            })
            .ToListAsync();
    }

    public async Task<bool> MarquerEcheancePayeeAsync(int idEcheance, int transactionId)
    {
        var echeance = await _context.Echeances.FindAsync(idEcheance);
        if (echeance == null) return false;

        echeance.Statut = "payee";
        echeance.DatePaiement = DateTime.UtcNow;
        echeance.IdTransaction = transactionId;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<DemandeRemboursementDto> CreerDemandeRemboursementAsync(CreateDemandeRemboursementRequest request)
    {
        var facture = await _context.Factures
            .Include(f => f.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(f => f.Patient).ThenInclude(p => p!.Assurance)
            .FirstOrDefaultAsync(f => f.IdFacture == request.IdFacture);

        if (facture == null) throw new Exception("Facture non trouvée");
        if (facture.Patient?.AssuranceId == null) throw new Exception("Le patient n'a pas d'assurance");

        var demande = new DemandeRemboursement
        {
            NumeroDemande = $"RMB-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
            IdFacture = request.IdFacture,
            IdAssurance = facture.Patient.AssuranceId.Value,
            IdPatient = facture.IdPatient,
            MontantDemande = request.MontantDemande,
            Justificatif = request.Justificatif,
            Statut = "en_attente"
        };

        _context.DemandesRemboursement.Add(demande);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Demande de remboursement créée: {NumeroDemande}", demande.NumeroDemande);

        return new DemandeRemboursementDto
        {
            IdDemande = demande.IdDemande,
            NumeroDemande = demande.NumeroDemande,
            IdFacture = demande.IdFacture,
            NumeroFacture = facture.NumeroFacture,
            IdAssurance = demande.IdAssurance,
            NomAssurance = facture.Patient.Assurance?.Nom ?? "",
            IdPatient = demande.IdPatient,
            NomPatient = facture.Patient.Utilisateur != null 
                ? $"{facture.Patient.Utilisateur.Prenom} {facture.Patient.Utilisateur.Nom}" : "",
            MontantDemande = demande.MontantDemande,
            Statut = demande.Statut,
            DateDemande = demande.DateDemande
        };
    }

    public async Task<List<DemandeRemboursementDto>> GetDemandesRemboursementAsync(int? idAssurance = null, string? statut = null)
    {
        var query = _context.DemandesRemboursement
            .Include(d => d.Facture)
            .Include(d => d.Assurance)
            .Include(d => d.Patient).ThenInclude(p => p!.Utilisateur)
            .AsQueryable();

        if (idAssurance.HasValue)
            query = query.Where(d => d.IdAssurance == idAssurance.Value);

        if (!string.IsNullOrEmpty(statut))
            query = query.Where(d => d.Statut == statut);

        return await query
            .OrderByDescending(d => d.DateDemande)
            .Select(d => new DemandeRemboursementDto
            {
                IdDemande = d.IdDemande,
                NumeroDemande = d.NumeroDemande,
                IdFacture = d.IdFacture,
                NumeroFacture = d.Facture != null ? d.Facture.NumeroFacture : "",
                IdAssurance = d.IdAssurance,
                NomAssurance = d.Assurance != null ? d.Assurance.Nom : "",
                IdPatient = d.IdPatient,
                NomPatient = d.Patient != null && d.Patient.Utilisateur != null 
                    ? $"{d.Patient.Utilisateur.Prenom} {d.Patient.Utilisateur.Nom}" : "",
                MontantDemande = d.MontantDemande,
                MontantApprouve = d.MontantApprouve,
                Statut = d.Statut,
                DateDemande = d.DateDemande,
                DateTraitement = d.DateTraitement,
                MotifRejet = d.MotifRejet,
                ReferenceAssurance = d.ReferenceAssurance
            })
            .ToListAsync();
    }

    public async Task<DemandeRemboursementDto> TraiterDemandeRemboursementAsync(int idDemande, TraiterDemandeRequest request)
    {
        var demande = await _context.DemandesRemboursement
            .Include(d => d.Facture)
            .Include(d => d.Assurance)
            .Include(d => d.Patient).ThenInclude(p => p!.Utilisateur)
            .FirstOrDefaultAsync(d => d.IdDemande == idDemande);

        if (demande == null) throw new Exception("Demande non trouvée");

        demande.Statut = request.Decision;
        demande.DateTraitement = DateTime.UtcNow;
        demande.ReferenceAssurance = request.ReferenceAssurance;

        if (request.Decision == "approuvee")
        {
            demande.MontantApprouve = request.MontantApprouve ?? demande.MontantDemande;
        }
        else if (request.Decision == "rejetee")
        {
            demande.MotifRejet = request.MotifRejet;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Demande {IdDemande} traitée: {Decision}", idDemande, request.Decision);

        return new DemandeRemboursementDto
        {
            IdDemande = demande.IdDemande,
            NumeroDemande = demande.NumeroDemande,
            IdFacture = demande.IdFacture,
            NumeroFacture = demande.Facture?.NumeroFacture ?? "",
            IdAssurance = demande.IdAssurance,
            NomAssurance = demande.Assurance?.Nom ?? "",
            IdPatient = demande.IdPatient,
            NomPatient = demande.Patient?.Utilisateur != null 
                ? $"{demande.Patient.Utilisateur.Prenom} {demande.Patient.Utilisateur.Nom}" : "",
            MontantDemande = demande.MontantDemande,
            MontantApprouve = demande.MontantApprouve,
            Statut = demande.Statut,
            DateDemande = demande.DateDemande,
            DateTraitement = demande.DateTraitement,
            MotifRejet = demande.MotifRejet,
            ReferenceAssurance = demande.ReferenceAssurance
        };
    }

    public async Task<decimal> CalculerCouvertureAssuranceAsync(int idPatient, decimal montantTotal, string typeActe)
    {
        var patient = await _context.Patients
            .Include(p => p.Assurance)
            .FirstOrDefaultAsync(p => p.IdUser == idPatient);

        if (patient?.Assurance == null || patient.CouvertureAssurance == null)
            return 0;

        // Vérifier validité de l'assurance
        if (patient.DateFinValidite.HasValue && patient.DateFinValidite.Value < DateTime.UtcNow)
            return 0;

        var tauxCouverture = patient.CouvertureAssurance.Value / 100;
        return Math.Round(montantTotal * tauxCouverture, 2);
    }

    private static string GenerateFactureHtml(Facture facture)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Facture</title>");
        sb.AppendLine("<style>body{font-family:Arial;margin:20px} table{width:100%;border-collapse:collapse} th,td{border:1px solid #ddd;padding:8px;text-align:left}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine($"<h1>FACTURE N° {facture.NumeroFacture}</h1>");
        sb.AppendLine($"<p><strong>Date:</strong> {facture.DateCreation:dd/MM/yyyy}</p>");
        sb.AppendLine($"<p><strong>Patient:</strong> {facture.Patient?.Utilisateur?.Prenom} {facture.Patient?.Utilisateur?.Nom}</p>");
        sb.AppendLine($"<p><strong>Service:</strong> {facture.Service?.NomService}</p>");
        sb.AppendLine("<table><thead><tr><th>Description</th><th>Qté</th><th>P.U.</th><th>Total</th></tr></thead><tbody>");
        
        foreach (var ligne in facture.Lignes)
        {
            sb.AppendLine($"<tr><td>{ligne.Description}</td><td>{ligne.Quantite}</td><td>{ligne.PrixUnitaire:N0} FCFA</td><td>{ligne.Montant:N0} FCFA</td></tr>");
        }
        
        sb.AppendLine("</tbody></table>");
        sb.AppendLine($"<p style='text-align:right;font-size:18px'><strong>TOTAL: {facture.MontantTotal:N0} FCFA</strong></p>");
        sb.AppendLine("</body></html>");
        
        return sb.ToString();
    }

    private static string GenerateRecuHtml(Transaction transaction)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Reçu</title>");
        sb.AppendLine("<style>body{font-family:Arial;margin:20px;text-align:center}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine($"<h2>REÇU DE PAIEMENT</h2>");
        sb.AppendLine($"<p><strong>N°:</strong> {transaction.NumeroTransaction}</p>");
        sb.AppendLine($"<p><strong>Date:</strong> {transaction.DateTransaction:dd/MM/yyyy HH:mm}</p>");
        sb.AppendLine($"<p><strong>Facture:</strong> {transaction.Facture?.NumeroFacture}</p>");
        sb.AppendLine($"<p><strong>Patient:</strong> {transaction.Facture?.Patient?.Utilisateur?.Prenom} {transaction.Facture?.Patient?.Utilisateur?.Nom}</p>");
        sb.AppendLine($"<p><strong>Montant:</strong> {transaction.Montant:N0} FCFA</p>");
        sb.AppendLine($"<p><strong>Mode:</strong> {transaction.ModePaiement}</p>");
        sb.AppendLine($"<p><strong>Caissier:</strong> {transaction.Caissier?.Utilisateur?.Prenom} {transaction.Caissier?.Utilisateur?.Nom}</p>");
        sb.AppendLine("</body></html>");
        
        return sb.ToString();
    }

    private static DateTime GetNextEcheanceDate(DateTime current, string frequence) => frequence.ToLower() switch
    {
        "hebdomadaire" => current.AddDays(7),
        "bimensuel" => current.AddDays(14),
        "mensuel" => current.AddMonths(1),
        _ => current.AddMonths(1)
    };
}
