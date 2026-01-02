using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Core.Entities.Prescription;
using Mediconnet_Backend.Data;
using System.Text;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service de prescriptions électroniques - Intégration pharmacies externes
/// </summary>
public class PrescriptionElectroniqueService : IPrescriptionElectroniqueService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PrescriptionElectroniqueService> _logger;

    public PrescriptionElectroniqueService(ApplicationDbContext context, ILogger<PrescriptionElectroniqueService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<OrdonnanceElectroniqueDto> CreerOrdonnanceElectroniqueAsync(CreateOrdonnanceElectroniqueRequest request, int medecinId)
    {
        var codeUnique = GenerateCodeUnique();

        var ordonnance = new OrdonnanceElectronique
        {
            CodeUnique = codeUnique,
            IdPatient = request.IdPatient,
            IdMedecin = medecinId,
            IdConsultation = request.IdConsultation,
            DatePrescription = DateTime.UtcNow,
            DateExpiration = DateTime.UtcNow.AddDays(request.DureeValiditeJours),
            Renouvelable = request.Renouvelable,
            NombreRenouvellements = request.NombreRenouvellements,
            RenouvellementRestants = request.NombreRenouvellements,
            Notes = request.Notes,
            Statut = "active",
            QRCodeData = GenerateQRCodeData(codeUnique)
        };

        _context.OrdonnancesElectroniques.Add(ordonnance);
        await _context.SaveChangesAsync();

        // Ajouter les lignes
        foreach (var ligne in request.Lignes)
        {
            var lignePrescription = new LignePrescription
            {
                IdOrdonnance = ordonnance.IdOrdonnance,
                IdMedicament = ligne.IdMedicament,
                Dosage = ligne.Dosage,
                Quantite = ligne.Quantite,
                Posologie = ligne.Posologie,
                DureeTraitement = ligne.DureeTraitement,
                Instructions = ligne.Instructions,
                Substitutable = ligne.Substitutable
            };
            _context.LignesPrescription.Add(lignePrescription);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Ordonnance électronique créée: {CodeUnique} par médecin {MedecinId}", codeUnique, medecinId);

        return await GetOrdonnanceAsync(ordonnance.IdOrdonnance) ?? throw new Exception("Erreur création");
    }

    public async Task<OrdonnanceElectroniqueDto?> GetOrdonnanceAsync(int idOrdonnance)
    {
        var ordonnance = await _context.OrdonnancesElectroniques
            .Include(o => o.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(o => o.Medecin).ThenInclude(m => m!.Utilisateur)
            .Include(o => o.Lignes).ThenInclude(l => l.Medicament)
            .Include(o => o.PharmacieExterne)
            .FirstOrDefaultAsync(o => o.IdOrdonnance == idOrdonnance);

        if (ordonnance == null) return null;

        return MapToDto(ordonnance);
    }

    public async Task<OrdonnanceElectroniqueDto?> GetOrdonnanceByCodeAsync(string codeUnique)
    {
        var ordonnance = await _context.OrdonnancesElectroniques
            .Include(o => o.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(o => o.Medecin).ThenInclude(m => m!.Utilisateur)
            .Include(o => o.Lignes).ThenInclude(l => l.Medicament)
            .Include(o => o.PharmacieExterne)
            .FirstOrDefaultAsync(o => o.CodeUnique == codeUnique);

        if (ordonnance == null) return null;

        return MapToDto(ordonnance);
    }

    public async Task<List<OrdonnanceElectroniqueDto>> GetOrdonnancesPatientAsync(int idPatient)
    {
        var ordonnances = await _context.OrdonnancesElectroniques
            .Include(o => o.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(o => o.Medecin).ThenInclude(m => m!.Utilisateur)
            .Include(o => o.Lignes).ThenInclude(l => l.Medicament)
            .Where(o => o.IdPatient == idPatient)
            .OrderByDescending(o => o.DatePrescription)
            .ToListAsync();

        return ordonnances.Select(MapToDto).ToList();
    }

    public async Task<TransmissionResult> TransmettreAPharmacieAsync(int idOrdonnance, int idPharmacie)
    {
        var ordonnance = await _context.OrdonnancesElectroniques.FindAsync(idOrdonnance);
        if (ordonnance == null)
            return new TransmissionResult { Success = false, Message = "Ordonnance non trouvée" };

        var pharmacie = await _context.PharmaciesExternes.FindAsync(idPharmacie);
        if (pharmacie == null || !pharmacie.EstConnectee)
            return new TransmissionResult { Success = false, Message = "Pharmacie non disponible" };

        // Simuler la transmission (dans une vraie implémentation, appel API)
        ordonnance.IdPharmacieExterne = idPharmacie;
        ordonnance.DateTransmission = DateTime.UtcNow;
        ordonnance.ReferenceTransmission = $"TX-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        ordonnance.Statut = "transmise";

        await _context.SaveChangesAsync();

        _logger.LogInformation("Ordonnance {IdOrdonnance} transmise à pharmacie {IdPharmacie}", idOrdonnance, idPharmacie);

        return new TransmissionResult
        {
            Success = true,
            Message = "Ordonnance transmise avec succès",
            ReferenceTransmission = ordonnance.ReferenceTransmission,
            DateTransmission = ordonnance.DateTransmission
        };
    }

    public async Task<List<PharmacieExterneDto>> GetPharmaciesPartenairesAsync(string? ville = null)
    {
        var query = _context.PharmaciesExternes.Where(p => p.Actif);

        if (!string.IsNullOrEmpty(ville))
            query = query.Where(p => p.Ville.Contains(ville));

        return await query
            .Select(p => new PharmacieExterneDto
            {
                IdPharmacie = p.IdPharmacie,
                Nom = p.Nom,
                Adresse = p.Adresse,
                Ville = p.Ville,
                Telephone = p.Telephone,
                Email = p.Email,
                EstConnectee = p.EstConnectee,
                HorairesOuverture = p.HorairesOuverture
            })
            .ToListAsync();
    }

    public async Task<TransmissionResult> AnnulerTransmissionAsync(int idOrdonnance)
    {
        var ordonnance = await _context.OrdonnancesElectroniques.FindAsync(idOrdonnance);
        if (ordonnance == null)
            return new TransmissionResult { Success = false, Message = "Ordonnance non trouvée" };

        if (ordonnance.Statut == "dispensee")
            return new TransmissionResult { Success = false, Message = "Impossible d'annuler, ordonnance déjà dispensée" };

        ordonnance.Statut = "active";
        ordonnance.IdPharmacieExterne = null;
        ordonnance.DateTransmission = null;
        ordonnance.ReferenceTransmission = null;

        await _context.SaveChangesAsync();

        return new TransmissionResult { Success = true, Message = "Transmission annulée" };
    }

    public async Task<bool> MarquerDispenseeAsync(int idOrdonnance, DispensationExterneRequest request)
    {
        var ordonnance = await _context.OrdonnancesElectroniques
            .Include(o => o.Lignes)
            .FirstOrDefaultAsync(o => o.IdOrdonnance == idOrdonnance);

        if (ordonnance == null) return false;

        foreach (var ligneDispensee in request.LignesDispensees)
        {
            var ligne = ordonnance.Lignes.FirstOrDefault(l => l.IdLigne == ligneDispensee.IdLigne);
            if (ligne != null)
            {
                ligne.Dispense = true;
                ligne.DateDispensation = request.DateDispensation;
                ligne.QuantiteDispensee = ligneDispensee.QuantiteDispensee;
                ligne.MedicamentSubstitue = ligneDispensee.MedicamentSubstitue;
            }
        }

        if (ordonnance.Lignes.All(l => l.Dispense))
        {
            ordonnance.Statut = "dispensee";
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<StatutDispensationDto> GetStatutDispensationAsync(int idOrdonnance)
    {
        var ordonnance = await _context.OrdonnancesElectroniques
            .Include(o => o.Lignes)
            .Include(o => o.PharmacieExterne)
            .FirstOrDefaultAsync(o => o.IdOrdonnance == idOrdonnance);

        if (ordonnance == null) throw new Exception("Ordonnance non trouvée");

        return new StatutDispensationDto
        {
            IdOrdonnance = ordonnance.IdOrdonnance,
            StatutGlobal = ordonnance.Statut,
            TotalLignes = ordonnance.Lignes.Count,
            LignesDispensees = ordonnance.Lignes.Count(l => l.Dispense),
            DerniereDispensation = ordonnance.Lignes.Where(l => l.Dispense).Max(l => l.DateDispensation),
            PharmacieDispensatrice = ordonnance.PharmacieExterne?.Nom
        };
    }

    public async Task<byte[]> GenerateQRCodeAsync(int idOrdonnance)
    {
        var ordonnance = await _context.OrdonnancesElectroniques.FindAsync(idOrdonnance);
        if (ordonnance == null) throw new Exception("Ordonnance non trouvée");

        // Retourner les données du QR code (à convertir en image avec une bibliothèque QR)
        var qrData = ordonnance.QRCodeData ?? ordonnance.CodeUnique;
        return Encoding.UTF8.GetBytes(qrData);
    }

    public async Task<OrdonnanceElectroniqueDto?> ScanOrdonnanceAsync(string codeScanne)
    {
        return await GetOrdonnanceByCodeAsync(codeScanne);
    }

    public async Task<OrdonnanceElectroniqueDto> RenouvelerOrdonnanceAsync(int idOrdonnance, int medecinId)
    {
        var ordonnanceOriginale = await _context.OrdonnancesElectroniques
            .Include(o => o.Lignes)
            .FirstOrDefaultAsync(o => o.IdOrdonnance == idOrdonnance);

        if (ordonnanceOriginale == null) throw new Exception("Ordonnance non trouvée");
        if (!ordonnanceOriginale.Renouvelable || (ordonnanceOriginale.RenouvellementRestants ?? 0) <= 0)
            throw new Exception("Cette ordonnance ne peut pas être renouvelée");

        // Décrémenter les renouvellements restants
        ordonnanceOriginale.RenouvellementRestants--;
        await _context.SaveChangesAsync();

        // Créer nouvelle ordonnance
        var request = new CreateOrdonnanceElectroniqueRequest
        {
            IdPatient = ordonnanceOriginale.IdPatient,
            IdConsultation = null,
            Renouvelable = ordonnanceOriginale.Renouvelable,
            NombreRenouvellements = 0,
            Notes = $"Renouvellement de l'ordonnance {ordonnanceOriginale.CodeUnique}",
            Lignes = ordonnanceOriginale.Lignes.Select(l => new CreateLignePrescriptionRequest
            {
                IdMedicament = l.IdMedicament,
                Dosage = l.Dosage,
                Quantite = l.Quantite,
                Posologie = l.Posologie,
                DureeTraitement = l.DureeTraitement,
                Instructions = l.Instructions,
                Substitutable = l.Substitutable
            }).ToList()
        };

        return await CreerOrdonnanceElectroniqueAsync(request, medecinId);
    }

    public async Task<List<OrdonnanceElectroniqueDto>> GetOrdonnancesARenouvelerAsync(int medecinId)
    {
        var dateLimit = DateTime.UtcNow.AddDays(30);

        var ordonnances = await _context.OrdonnancesElectroniques
            .Include(o => o.Patient).ThenInclude(p => p!.Utilisateur)
            .Include(o => o.Medecin).ThenInclude(m => m!.Utilisateur)
            .Include(o => o.Lignes).ThenInclude(l => l.Medicament)
            .Where(o => o.IdMedecin == medecinId && 
                o.Renouvelable && 
                (o.RenouvellementRestants ?? 0) > 0 &&
                o.DateExpiration <= dateLimit &&
                o.Statut == "dispensee")
            .OrderBy(o => o.DateExpiration)
            .ToListAsync();

        return ordonnances.Select(MapToDto).ToList();
    }

    private static string GenerateCodeUnique()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }

    private static string GenerateQRCodeData(string codeUnique)
    {
        return $"MEDICONNET|ORD|{codeUnique}|{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    private static OrdonnanceElectroniqueDto MapToDto(OrdonnanceElectronique o)
    {
        return new OrdonnanceElectroniqueDto
        {
            IdOrdonnance = o.IdOrdonnance,
            CodeUnique = o.CodeUnique,
            QRCode = o.QRCodeData ?? "",
            IdPatient = o.IdPatient,
            NomPatient = o.Patient?.Utilisateur != null 
                ? $"{o.Patient.Utilisateur.Prenom} {o.Patient.Utilisateur.Nom}" : "",
            IdMedecin = o.IdMedecin,
            NomMedecin = o.Medecin?.Utilisateur != null 
                ? $"Dr. {o.Medecin.Utilisateur.Prenom} {o.Medecin.Utilisateur.Nom}" : "",
            NumeroOrdre = o.Medecin?.NumeroOrdre,
            DatePrescription = o.DatePrescription,
            DateExpiration = o.DateExpiration,
            Statut = o.Statut,
            Renouvelable = o.Renouvelable,
            NombreRenouvellements = o.NombreRenouvellements,
            RenouvellementRestants = o.RenouvellementRestants,
            Notes = o.Notes,
            Lignes = o.Lignes.Select(l => new LignePrescriptionDto
            {
                IdLigne = l.IdLigne,
                IdMedicament = l.IdMedicament,
                NomMedicament = l.Medicament?.Nom ?? "",
                CodeCIP = l.Medicament?.CodeATC,
                Dosage = l.Dosage,
                Quantite = l.Quantite,
                Posologie = l.Posologie,
                DureeTraitement = l.DureeTraitement,
                Instructions = l.Instructions,
                Substitutable = l.Substitutable,
                Dispense = l.Dispense,
                DateDispensation = l.DateDispensation
            }).ToList(),
            TransmissionInfo = o.IdPharmacieExterne.HasValue ? new TransmissionInfoDto
            {
                IdPharmacie = o.IdPharmacieExterne.Value,
                NomPharmacie = o.PharmacieExterne?.Nom ?? "",
                DateTransmission = o.DateTransmission ?? DateTime.MinValue,
                Statut = o.Statut,
                ReferenceExterne = o.ReferenceTransmission
            } : null
        };
    }
}
