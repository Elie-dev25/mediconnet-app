using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Caisse;
using Mediconnet_Backend.DTOs.RendezVous;
using Mediconnet_Backend.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mediconnet_Backend.Services;

/// <summary>
/// Service pour la gestion de la caisse
/// </summary>
public class CaisseService : ICaisseService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CaisseService> _logger;
    private readonly IRendezVousService _rendezVousService;
    private readonly ISlotLockService _slotLockService;
    private readonly IAppointmentNotificationService _notificationService;

    public CaisseService(
        ApplicationDbContext context,
        ILogger<CaisseService> logger,
        IRendezVousService rendezVousService,
        ISlotLockService slotLockService,
        IAppointmentNotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _rendezVousService = rendezVousService;
        _slotLockService = slotLockService;
        _notificationService = notificationService;
    }

    private async Task<(bool Success, string Message, int? IdRendezVous)> TryAssignConfirmedRdvTodayAsync(
        Facture facture,
        int actorUserId,
        string? motif)
    {
        if (facture.IdMedecin == null)
            return (false, "Impossible d'assigner un créneau: médecin non défini sur la facture", null);

        var medecinId = facture.IdMedecin.Value;
        var todayStart = DateTime.Today;
        var todayEnd = todayStart.AddDays(1).AddSeconds(-1);

        // Éviter doublon: le patient a déjà un RDV aujourd'hui avec ce médecin
        var hasExisting = await _context.RendezVous
            .AnyAsync(r => r.IdPatient == facture.IdPatient &&
                          r.IdMedecin == medecinId &&
                          r.Statut != "annule" &&
                          r.DateHeure.Date == todayStart.Date);
        if (hasExisting)
            return (true, "Le patient a déjà un rendez-vous aujourd'hui", null);

        var slots = await _rendezVousService.GetCreneauxDisponiblesAsync(medecinId, todayStart, todayEnd);
        var nextSlot = slots.Creneaux.FirstOrDefault(c => c.Disponible);
        if (nextSlot == null)
            return (false, "Aucun créneau disponible aujourd'hui pour ce médecin", null);

        var lockResult = await _slotLockService.AcquireLockAsync(
            medecinId,
            nextSlot.DateHeure,
            nextSlot.Duree,
            actorUserId);

        if (!lockResult.Success)
            return (false, lockResult.Message, null);

        try
        {
            // Double vérification après verrou
            var conflit = await _context.RendezVous
                .AnyAsync(r => r.IdMedecin == medecinId &&
                              r.Statut != "annule" &&
                              r.DateHeure < nextSlot.DateHeure.AddMinutes(nextSlot.Duree) &&
                              r.DateHeure.AddMinutes(r.Duree) > nextSlot.DateHeure);

            if (conflit)
            {
                await _slotLockService.ReleaseLockAsync(lockResult.LockToken!, actorUserId);
                return (false, "Le créneau sélectionné vient d'être occupé. Veuillez réessayer.", null);
            }

            var rdv = new RendezVous
            {
                IdPatient = facture.IdPatient,
                IdMedecin = medecinId,
                IdService = facture.IdService,
                DateHeure = nextSlot.DateHeure,
                Duree = nextSlot.Duree,
                Motif = motif,
                TypeRdv = "consultation",
                Statut = "confirme",
                DateCreation = DateTime.UtcNow
            };

            _context.RendezVous.Add(rdv);
            await _context.SaveChangesAsync();

            await _slotLockService.ReleaseLockAsync(lockResult.LockToken!, actorUserId);
            return (true, "Rendez-vous assigné et confirmé", rdv.IdRendezVous);
        }
        catch
        {
            if (lockResult.LockToken != null)
                await _slotLockService.ReleaseLockAsync(lockResult.LockToken, actorUserId);
            throw;
        }
    }

    // ==================== KPIs ====================

    public async Task<CaisseKpiDto> GetKpisAsync(int caissierUserId)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        // Session active
        var sessionActive = await _context.SessionsCaisse
            .FirstOrDefaultAsync(s => s.IdCaissier == caissierUserId && s.Statut == "ouverte");

        // Transactions du jour
        var transactionsJour = await _context.Transactions
            .Where(t => t.DateTransaction >= today && t.DateTransaction < tomorrow)
            .Where(t => t.Statut == "complete")
            .ToListAsync();

        // Factures en attente
        var facturesEnAttente = await _context.Factures
            .CountAsync(f => f.Statut == "en_attente" || f.Statut == "partiel");

        // Remboursements et annulations du jour
        var annulations = await _context.Transactions
            .Where(t => t.DateAnnulation >= today && t.DateAnnulation < tomorrow)
            .ToListAsync();

        // Calcul solde caisse (session active)
        var soldeCaisse = sessionActive?.MontantOuverture ?? 0;
        if (sessionActive != null)
        {
            var transactionsSession = await _context.Transactions
                .Where(t => t.IdSessionCaisse == sessionActive.IdSession)
                .Where(t => t.Statut == "complete" && t.ModePaiement == "especes")
                .SumAsync(t => t.Montant);
            soldeCaisse += transactionsSession;
        }

        return new CaisseKpiDto
        {
            RevenuJour = transactionsJour.Sum(t => t.Montant),
            NombreTransactionsJour = transactionsJour.Count,
            FacturesEnAttente = facturesEnAttente,
            SoldeCaisse = soldeCaisse,
            EcartCaisse = 0, // Calculé lors du rapprochement
            RemboursementsJour = annulations.Where(a => a.Statut == "rembourse").Sum(a => a.Montant),
            AnnulationsJour = annulations.Count(a => a.Statut == "annule"),
            CaisseOuverte = sessionActive != null,
            IdSessionActive = sessionActive?.IdSession
        };
    }

    // ==================== FACTURES ====================

    public async Task<List<FactureListItemDto>> GetFacturesEnAttenteAsync()
    {
        return await _context.Factures
            .Include(f => f.Patient)
                .ThenInclude(p => p!.Utilisateur)
            .Where(f => f.Statut == "en_attente" || f.Statut == "partiel")
            .OrderByDescending(f => f.DateCreation)
            .Select(f => new FactureListItemDto
            {
                IdFacture = f.IdFacture,
                NumeroFacture = f.NumeroFacture,
                PatientNom = f.Patient != null && f.Patient.Utilisateur != null
                    ? $"{f.Patient.Utilisateur.Prenom} {f.Patient.Utilisateur.Nom}"
                    : "Inconnu",
                NumeroDossier = f.Patient != null ? f.Patient.NumeroDossier : null,
                MontantTotal = f.MontantTotal,
                MontantRestant = f.MontantRestant,
                Statut = f.Statut,
                DateCreation = f.DateCreation,
                DateEcheance = f.DateEcheance,
                CouvertureAssurance = f.CouvertureAssurance,
                TauxCouverture = f.TauxCouverture,
                MontantAssurance = f.MontantAssurance,
                NomAssurance = f.Patient != null && f.Patient.Assurance != null ? f.Patient.Assurance.Nom : null
            })
            .ToListAsync();
    }

    public async Task<List<FactureListItemDto>> GetFacturesPatientAsync(int idPatient)
    {
        return await _context.Factures
            .Include(f => f.Patient)
                .ThenInclude(p => p!.Utilisateur)
            .Where(f => f.IdPatient == idPatient)
            .Where(f => f.Statut == "en_attente" || f.Statut == "partiel")
            .OrderByDescending(f => f.DateCreation)
            .Select(f => new FactureListItemDto
            {
                IdFacture = f.IdFacture,
                NumeroFacture = f.NumeroFacture,
                PatientNom = f.Patient != null && f.Patient.Utilisateur != null
                    ? $"{f.Patient.Utilisateur.Prenom} {f.Patient.Utilisateur.Nom}"
                    : "Inconnu",
                NumeroDossier = f.Patient != null ? f.Patient.NumeroDossier : null,
                MontantTotal = f.MontantTotal,
                MontantRestant = f.MontantRestant,
                Statut = f.Statut,
                DateCreation = f.DateCreation,
                DateEcheance = f.DateEcheance,
                CouvertureAssurance = f.CouvertureAssurance,
                TauxCouverture = f.TauxCouverture,
                MontantAssurance = f.MontantAssurance,
                NomAssurance = f.Patient != null && f.Patient.Assurance != null ? f.Patient.Assurance.Nom : null
            })
            .ToListAsync();
    }

    public async Task<FactureDto?> GetFactureAsync(int idFacture)
    {
        var facture = await _context.Factures
            .Include(f => f.Patient)
                .ThenInclude(p => p!.Utilisateur)
            .Include(f => f.Lignes)
            .Include(f => f.Service)
            .FirstOrDefaultAsync(f => f.IdFacture == idFacture);

        if (facture == null) return null;

        return new FactureDto
        {
            IdFacture = facture.IdFacture,
            NumeroFacture = facture.NumeroFacture,
            IdPatient = facture.IdPatient,
            PatientNom = facture.Patient?.Utilisateur?.Nom ?? "",
            PatientPrenom = facture.Patient?.Utilisateur?.Prenom ?? "",
            NumeroDossier = facture.Patient?.NumeroDossier,
            MontantTotal = facture.MontantTotal,
            MontantPaye = facture.MontantPaye,
            MontantRestant = facture.MontantRestant,
            Statut = facture.Statut,
            TypeFacture = facture.TypeFacture,
            DateCreation = facture.DateCreation,
            DateEcheance = facture.DateEcheance,
            ServiceNom = facture.Service?.NomService,
            CouvertureAssurance = facture.CouvertureAssurance,
            TauxCouverture = facture.TauxCouverture,
            MontantAssurance = facture.MontantAssurance,
            NomAssurance = facture.Patient?.Assurance?.Nom,
            Lignes = facture.Lignes.Select(l => new LigneFactureDto
            {
                IdLigne = l.IdLigne,
                Description = l.Description,
                Code = l.Code,
                Quantite = l.Quantite,
                PrixUnitaire = l.PrixUnitaire,
                Montant = l.Montant,
                Categorie = l.Categorie
            }).ToList()
        };
    }

    // ==================== TRANSACTIONS ====================

    public async Task<List<TransactionDto>> GetTransactionsJourAsync(int? caissierUserId = null)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var query = _context.Transactions
            .Include(t => t.Facture)
                .ThenInclude(f => f!.Patient)
                    .ThenInclude(p => p!.Utilisateur)
            .Include(t => t.Caissier)
                .ThenInclude(c => c!.Utilisateur)
            .Where(t => t.DateTransaction >= today && t.DateTransaction < tomorrow);

        if (caissierUserId.HasValue)
        {
            query = query.Where(t => t.IdCaissier == caissierUserId.Value);
        }

        var transactions = await query
            .OrderByDescending(t => t.DateTransaction)
            .ToListAsync();

        return transactions.Select(t => new TransactionDto
        {
            IdTransaction = t.IdTransaction,
            NumeroTransaction = t.NumeroTransaction,
            IdFacture = t.IdFacture,
            NumeroFacture = t.Facture?.NumeroFacture ?? "",
            PatientNom = t.Facture?.Patient?.Utilisateur != null
                ? $"{t.Facture.Patient.Utilisateur.Prenom} {t.Facture.Patient.Utilisateur.Nom}"
                : "",
            NumeroDossier = t.Facture?.Patient?.NumeroDossier,
            Montant = t.Montant,
            ModePaiement = t.ModePaiement,
            Statut = t.Statut,
            Reference = t.Reference,
            DateTransaction = t.DateTransaction,
            CaissierNom = t.Caissier?.Utilisateur != null
                ? $"{t.Caissier.Utilisateur.Prenom} {t.Caissier.Utilisateur.Nom}"
                : "",
            MontantRecu = t.MontantRecu,
            RenduMonnaie = t.RenduMonnaie
        }).ToList();
    }

    public async Task<List<TransactionDto>> GetTransactionsAsync(
        DateTime? dateDebut, DateTime? dateFin, string? modePaiement)
    {
        var query = _context.Transactions
            .Include(t => t.Facture)
                .ThenInclude(f => f!.Patient)
                    .ThenInclude(p => p!.Utilisateur)
            .Include(t => t.Caissier)
                .ThenInclude(c => c!.Utilisateur)
            .AsQueryable();

        if (dateDebut.HasValue)
            query = query.Where(t => t.DateTransaction >= dateDebut.Value);
        if (dateFin.HasValue)
            query = query.Where(t => t.DateTransaction <= dateFin.Value);
        if (!string.IsNullOrEmpty(modePaiement))
            query = query.Where(t => t.ModePaiement == modePaiement);

        var transactions = await query
            .OrderByDescending(t => t.DateTransaction)
            .Take(100)
            .ToListAsync();

        return transactions.Select(t => new TransactionDto
        {
            IdTransaction = t.IdTransaction,
            NumeroTransaction = t.NumeroTransaction,
            IdFacture = t.IdFacture,
            NumeroFacture = t.Facture?.NumeroFacture ?? "",
            PatientNom = t.Facture?.Patient?.Utilisateur != null
                ? $"{t.Facture.Patient.Utilisateur.Prenom} {t.Facture.Patient.Utilisateur.Nom}"
                : "",
            NumeroDossier = t.Facture?.Patient?.NumeroDossier,
            Montant = t.Montant,
            ModePaiement = t.ModePaiement,
            Statut = t.Statut,
            Reference = t.Reference,
            DateTransaction = t.DateTransaction,
            CaissierNom = t.Caissier?.Utilisateur != null
                ? $"{t.Caissier.Utilisateur.Prenom} {t.Caissier.Utilisateur.Nom}"
                : "",
            MontantRecu = t.MontantRecu,
            RenduMonnaie = t.RenduMonnaie
        }).ToList();
    }

    public async Task<(bool Success, string Message, TransactionDto? Transaction)> CreerTransactionAsync(
        CreateTransactionRequest request, int caissierUserId)
    {
        // Vérifier idempotence
        if (!string.IsNullOrEmpty(request.IdempotencyToken))
        {
            var existante = await _context.Transactions
                .FirstOrDefaultAsync(t => t.TransactionUuid == request.IdempotencyToken);
            if (existante != null)
            {
                return (false, "Transaction déjà traitée", null);
            }
        }

        // Vérifier la facture
        var facture = await _context.Factures
            .Include(f => f.Patient)
                .ThenInclude(p => p!.Utilisateur)
            .FirstOrDefaultAsync(f => f.IdFacture == request.IdFacture);

        if (facture == null)
            return (false, "Facture introuvable", null);

        if (facture.Statut == "payee")
            return (false, "Cette facture est déjà payée", null);

        if (request.Montant > facture.MontantRestant)
            return (false, $"Le montant ne peut pas dépasser {facture.MontantRestant} FCFA", null);

        // Validation: le montant doit être >= montant restant pour valider le paiement complet
        if (request.Montant < facture.MontantRestant)
            return (false, $"Le montant doit être au moins égal à {facture.MontantRestant} FCFA pour valider le paiement", null);

        // Vérifier session caisse ouverte
        var session = await _context.SessionsCaisse
            .FirstOrDefaultAsync(s => s.IdCaissier == caissierUserId && s.Statut == "ouverte");

        // Générer numéro transaction
        var numeroTransaction = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        // Calculer rendu monnaie
        decimal? renduMonnaie = null;
        if (request.MontantRecu.HasValue && request.MontantRecu > request.Montant)
        {
            renduMonnaie = request.MontantRecu.Value - request.Montant;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Créer la transaction
            var newTransaction = new Transaction
            {
                NumeroTransaction = numeroTransaction,
                TransactionUuid = request.IdempotencyToken ?? Guid.NewGuid().ToString(),
                IdFacture = request.IdFacture,
                IdPatient = facture.IdPatient,
                IdCaissier = caissierUserId,
                IdSessionCaisse = session?.IdSession,
                Montant = request.Montant,
                ModePaiement = request.ModePaiement,
                Statut = "complete",
                Reference = request.Reference,
                Notes = request.Notes,
                MontantRecu = request.MontantRecu,
                RenduMonnaie = renduMonnaie,
                EstPaiementPartiel = request.Montant < facture.MontantRestant
            };

            _context.Transactions.Add(newTransaction);

            // Mettre à jour la facture
            facture.MontantPaye += request.Montant;
            facture.MontantRestant -= request.Montant;

            if (facture.MontantRestant <= 0)
            {
                facture.Statut = "payee";
                facture.DatePaiement = DateTime.UtcNow;
            }
            else
            {
                facture.Statut = "partiel";
            }

            // Si la facture consultation est entièrement payée, assigner un RDV confirmé au créneau le plus proche aujourd'hui
            // et ainsi alimenter la file d'attente du médecin (RDV confirmés du jour).
            if (facture.Statut == "payee" && facture.TypeFacture == "consultation")
            {
                var (assigned, assignMessage, idRdv) = await TryAssignConfirmedRdvTodayAsync(
                    facture,
                    caissierUserId,
                    motif: "Consultation");

                if (!assigned)
                {
                    _logger.LogWarning($"Paiement bloqué: pas de créneau disponible aujourd'hui. Facture={facture.IdFacture}, Raison={assignMessage}");
                    await transaction.RollbackAsync();
                    return (false, assignMessage, null);
                }
                else if (idRdv.HasValue)
                {
                    _logger.LogInformation($"RDV confirmé créé automatiquement après paiement: RDV={idRdv.Value}, Facture={facture.IdFacture}");

                    if (facture.IdConsultation.HasValue)
                    {
                        var consultation = await _context.Consultations
                            .FirstOrDefaultAsync(c => c.IdConsultation == facture.IdConsultation.Value);

                        if (consultation != null)
                        {
                            consultation.IdRendezVous = idRdv.Value;
                            consultation.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            if (facture.Statut == "payee")
            {
                await _notificationService.NotifyFacturePaidAsync(new
                {
                    idFacture = facture.IdFacture,
                    numeroFacture = facture.NumeroFacture,
                    typeFacture = facture.TypeFacture,
                    statut = facture.Statut,
                    idPatient = facture.IdPatient,
                    idMedecin = facture.IdMedecin,
                    idService = facture.IdService,
                    idSpecialite = facture.IdSpecialite,
                    datePaiement = facture.DatePaiement
                });
            }

            _logger.LogInformation($"Transaction créée: {numeroTransaction} pour facture {facture.NumeroFacture}");

            return (true, "Paiement enregistré avec succès", new TransactionDto
            {
                IdTransaction = newTransaction.IdTransaction,
                NumeroTransaction = newTransaction.NumeroTransaction,
                IdFacture = newTransaction.IdFacture,
                NumeroFacture = facture.NumeroFacture,
                PatientNom = facture.Patient?.Utilisateur != null
                    ? $"{facture.Patient.Utilisateur.Prenom} {facture.Patient.Utilisateur.Nom}"
                    : "",
                NumeroDossier = facture.Patient?.NumeroDossier,
                Montant = newTransaction.Montant,
                ModePaiement = newTransaction.ModePaiement,
                Statut = newTransaction.Statut,
                Reference = newTransaction.Reference,
                DateTransaction = newTransaction.DateTransaction,
                MontantRecu = newTransaction.MontantRecu,
                RenduMonnaie = newTransaction.RenduMonnaie
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError($"Erreur création transaction: {ex.Message}");
            return (false, "Erreur lors de l'enregistrement du paiement", null);
        }
    }

    public async Task<(bool Success, string Message)> AnnulerTransactionAsync(
        AnnulerTransactionRequest request, int caissierUserId)
    {
        var trans = await _context.Transactions
            .Include(t => t.Facture)
            .FirstOrDefaultAsync(t => t.IdTransaction == request.IdTransaction);

        if (trans == null)
            return (false, "Transaction introuvable");

        if (trans.Statut != "complete")
            return (false, "Cette transaction ne peut pas être annulée");

        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Annuler la transaction
            trans.Statut = "annule";
            trans.DateAnnulation = DateTime.UtcNow;
            trans.MotifAnnulation = request.Motif;
            trans.AnnulePar = caissierUserId;

            // Mettre à jour la facture
            if (trans.Facture != null)
            {
                trans.Facture.MontantPaye -= trans.Montant;
                trans.Facture.MontantRestant += trans.Montant;
                trans.Facture.Statut = trans.Facture.MontantPaye > 0 ? "partiel" : "en_attente";
                trans.Facture.DatePaiement = null;
            }

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            _logger.LogInformation($"Transaction annulée: {trans.NumeroTransaction}");
            return (true, "Transaction annulée avec succès");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError($"Erreur annulation transaction: {ex.Message}");
            return (false, "Erreur lors de l'annulation");
        }
    }

    // ==================== SESSION CAISSE ====================

    public async Task<SessionCaisseDto?> GetSessionActiveAsync(int caissierUserId)
    {
        var session = await _context.SessionsCaisse
            .Include(s => s.Caissier)
                .ThenInclude(c => c!.Utilisateur)
            .Include(s => s.Transactions)
            .FirstOrDefaultAsync(s => s.IdCaissier == caissierUserId && s.Statut == "ouverte");

        if (session == null) return null;

        return new SessionCaisseDto
        {
            IdSession = session.IdSession,
            CaissierNom = session.Caissier?.Utilisateur != null
                ? $"{session.Caissier.Utilisateur.Prenom} {session.Caissier.Utilisateur.Nom}"
                : "",
            MontantOuverture = session.MontantOuverture,
            DateOuverture = session.DateOuverture,
            Statut = session.Statut,
            NombreTransactions = session.Transactions.Count(t => t.Statut == "complete"),
            TotalEncaisse = session.Transactions
                .Where(t => t.Statut == "complete" && t.ModePaiement == "especes")
                .Sum(t => t.Montant)
        };
    }

    public async Task<List<SessionCaisseDto>> GetHistoriqueSessionsAsync(int caissierUserId, int limite = 10)
    {
        var sessions = await _context.SessionsCaisse
            .Include(s => s.Caissier)
                .ThenInclude(c => c!.Utilisateur)
            .Include(s => s.Transactions)
            .Where(s => s.IdCaissier == caissierUserId)
            .OrderByDescending(s => s.DateOuverture)
            .Take(limite)
            .ToListAsync();

        return sessions.Select(s => new SessionCaisseDto
        {
            IdSession = s.IdSession,
            CaissierNom = s.Caissier?.Utilisateur != null
                ? $"{s.Caissier.Utilisateur.Prenom} {s.Caissier.Utilisateur.Nom}"
                : "",
            MontantOuverture = s.MontantOuverture,
            MontantFermeture = s.MontantFermeture,
            MontantSysteme = s.MontantSysteme,
            Ecart = s.Ecart,
            DateOuverture = s.DateOuverture,
            DateFermeture = s.DateFermeture,
            Statut = s.Statut,
            NombreTransactions = s.Transactions.Count(t => t.Statut == "complete"),
            TotalEncaisse = s.Transactions
                .Where(t => t.Statut == "complete" && t.ModePaiement == "especes")
                .Sum(t => t.Montant)
        }).ToList();
    }

    public async Task<(bool Success, string Message, SessionCaisseDto? Session)> OuvrirCaisseAsync(
        OuvrirCaisseRequest request, int caissierUserId)
    {
        // Vérifier si une session est déjà ouverte
        var sessionExistante = await _context.SessionsCaisse
            .FirstOrDefaultAsync(s => s.IdCaissier == caissierUserId && s.Statut == "ouverte");

        if (sessionExistante != null)
            return (false, "Une session de caisse est déjà ouverte", null);

        var session = new SessionCaisse
        {
            IdCaissier = caissierUserId,
            MontantOuverture = request.MontantOuverture,
            NotesOuverture = request.Notes,
            Statut = "ouverte"
        };

        _context.SessionsCaisse.Add(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Caisse ouverte par utilisateur {caissierUserId}");

        return (true, "Caisse ouverte avec succès", new SessionCaisseDto
        {
            IdSession = session.IdSession,
            MontantOuverture = session.MontantOuverture,
            DateOuverture = session.DateOuverture,
            Statut = session.Statut,
            NombreTransactions = 0,
            TotalEncaisse = 0
        });
    }

    public async Task<(bool Success, string Message, SessionCaisseDto? Session)> FermerCaisseAsync(
        FermerCaisseRequest request, int caissierUserId)
    {
        var session = await _context.SessionsCaisse
            .Include(s => s.Transactions)
            .FirstOrDefaultAsync(s => s.IdCaissier == caissierUserId && s.Statut == "ouverte");

        if (session == null)
            return (false, "Aucune session de caisse ouverte", null);

        // Calculer le montant système (espèces encaissées)
        var montantSysteme = session.MontantOuverture + session.Transactions
            .Where(t => t.Statut == "complete" && t.ModePaiement == "especes")
            .Sum(t => t.Montant);

        session.MontantFermeture = request.MontantFermeture;
        session.MontantSysteme = montantSysteme;
        session.Ecart = request.MontantFermeture - montantSysteme;
        session.DateFermeture = DateTime.UtcNow;
        session.NotesFermeture = request.Notes;
        session.Statut = "fermee";

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Caisse fermée par utilisateur {caissierUserId}, écart: {session.Ecart}");

        return (true, "Caisse fermée avec succès", new SessionCaisseDto
        {
            IdSession = session.IdSession,
            MontantOuverture = session.MontantOuverture,
            MontantFermeture = session.MontantFermeture,
            MontantSysteme = session.MontantSysteme,
            Ecart = session.Ecart,
            DateOuverture = session.DateOuverture,
            DateFermeture = session.DateFermeture,
            Statut = session.Statut,
            NombreTransactions = session.Transactions.Count(t => t.Statut == "complete"),
            TotalEncaisse = session.Transactions
                .Where(t => t.Statut == "complete" && t.ModePaiement == "especes")
                .Sum(t => t.Montant)
        });
    }

    // ==================== RECHERCHE PATIENT ====================

    public async Task<List<PatientSearchResultDto>> RechercherPatientsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new List<PatientSearchResultDto>();

        var patients = await _context.Patients
            .Include(p => p.Utilisateur)
            .Where(p => p.Utilisateur != null && (
                p.Utilisateur.Nom.Contains(query) ||
                p.Utilisateur.Prenom.Contains(query) ||
                (p.NumeroDossier != null && p.NumeroDossier.Contains(query)) ||
                (p.Utilisateur.Telephone != null && p.Utilisateur.Telephone.Contains(query))
            ))
            .Take(10)
            .ToListAsync();

        var result = new List<PatientSearchResultDto>();
        foreach (var p in patients)
        {
            var facturesEnAttente = await _context.Factures
                .CountAsync(f => f.IdPatient == p.IdUser && 
                    (f.Statut == "en_attente" || f.Statut == "partiel"));

            result.Add(new PatientSearchResultDto
            {
                IdPatient = p.IdUser,
                Nom = p.Utilisateur?.Nom ?? "",
                Prenom = p.Utilisateur?.Prenom ?? "",
                NumeroDossier = p.NumeroDossier,
                Telephone = p.Utilisateur?.Telephone,
                FacturesEnAttente = facturesEnAttente
            });
        }

        return result;
    }

    // ==================== STATISTIQUES ====================

    public async Task<List<RepartitionPaiementDto>> GetRepartitionPaiementsAsync(
        DateTime? dateDebut, DateTime? dateFin)
    {
        var query = _context.Transactions
            .Where(t => t.Statut == "complete");

        if (dateDebut.HasValue)
            query = query.Where(t => t.DateTransaction >= dateDebut.Value);
        if (dateFin.HasValue)
            query = query.Where(t => t.DateTransaction <= dateFin.Value);

        var transactions = await query.ToListAsync();
        var total = transactions.Sum(t => t.Montant);

        return transactions
            .GroupBy(t => t.ModePaiement)
            .Select(g => new RepartitionPaiementDto
            {
                ModePaiement = g.Key,
                Montant = g.Sum(t => t.Montant),
                Nombre = g.Count(),
                Pourcentage = total > 0 ? Math.Round(g.Sum(t => t.Montant) / total * 100, 2) : 0
            })
            .OrderByDescending(r => r.Montant)
            .ToList();
    }

    public async Task<List<RevenuParServiceDto>> GetRevenusParServiceAsync(DateTime date)
    {
        var dateDebut = date.Date;
        var dateFin = dateDebut.AddDays(1);

        var factures = await _context.Factures
            .Include(f => f.Service)
            .Include(f => f.Transactions)
            .Where(f => f.Transactions.Any(t => 
                t.DateTransaction >= dateDebut && 
                t.DateTransaction < dateFin && 
                t.Statut == "complete"))
            .ToListAsync();

        return factures
            .GroupBy(f => f.Service?.NomService ?? "Autre")
            .Select(g => new RevenuParServiceDto
            {
                ServiceNom = g.Key,
                Montant = g.SelectMany(f => f.Transactions)
                    .Where(t => t.DateTransaction >= dateDebut && 
                               t.DateTransaction < dateFin && 
                               t.Statut == "complete")
                    .Sum(t => t.Montant)
            })
            .OrderByDescending(r => r.Montant)
            .ToList();
    }

    public async Task<List<FactureRetardDto>> GetFacturesEnRetardAsync(int limite = 5)
    {
        var today = DateTime.UtcNow.Date;

        var factures = await _context.Factures
            .Include(f => f.Patient)
                .ThenInclude(p => p!.Utilisateur)
            .Where(f => (f.Statut == "en_attente" || f.Statut == "partiel") && 
                       f.DateEcheance.HasValue && f.DateEcheance < today)
            .OrderBy(f => f.DateEcheance)
            .Take(limite)
            .ToListAsync();

        return factures.Select(f => new FactureRetardDto
        {
            IdFacture = f.IdFacture,
            NumeroFacture = f.NumeroFacture,
            PatientNom = f.Patient?.Utilisateur != null
                ? $"{f.Patient.Utilisateur.Prenom} {f.Patient.Utilisateur.Nom}"
                : "",
            MontantRestant = f.MontantRestant,
            JoursRetard = (int)(today - f.DateEcheance!.Value).TotalDays
        }).ToList();
    }

    public async Task<RecuTransactionDto?> GetRecuTransactionAsync(int idTransaction)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Facture)
                .ThenInclude(f => f!.Patient)
                    .ThenInclude(p => p!.Utilisateur)
            .Include(t => t.Facture)
                .ThenInclude(f => f!.Patient)
                    .ThenInclude(p => p!.Assurance)
            .Include(t => t.Facture)
                .ThenInclude(f => f!.Service)
            .Include(t => t.Facture)
                .ThenInclude(f => f!.Medecin)
                    .ThenInclude(m => m!.Utilisateur)
            .Include(t => t.Facture)
                .ThenInclude(f => f!.Lignes)
            .Include(t => t.Caissier)
                .ThenInclude(c => c!.Utilisateur)
            .FirstOrDefaultAsync(t => t.IdTransaction == idTransaction);

        if (transaction == null || transaction.Facture == null)
            return null;

        var facture = transaction.Facture;
        var patient = facture.Patient;
        var utilisateurPatient = patient?.Utilisateur;

        return new RecuTransactionDto
        {
            NumeroRecu = $"REC-{transaction.NumeroTransaction}",
            NumeroTransaction = transaction.NumeroTransaction,
            NumeroFacture = facture.NumeroFacture,
            DateTransaction = transaction.DateTransaction,
            
            // Patient
            PatientNom = utilisateurPatient?.Nom ?? "",
            PatientPrenom = utilisateurPatient?.Prenom ?? "",
            NumeroDossier = patient?.NumeroDossier,
            Telephone = utilisateurPatient?.Telephone,
            
            // Paiement
            MontantTotal = facture.MontantTotal,
            MontantPaye = transaction.Montant,
            MontantRecu = transaction.MontantRecu,
            RenduMonnaie = transaction.RenduMonnaie,
            ModePaiement = transaction.ModePaiement,
            Reference = transaction.Reference,
            
            // Assurance
            CouvertureAssurance = facture.CouvertureAssurance,
            NomAssurance = patient?.Assurance?.Nom,
            TauxCouverture = facture.TauxCouverture,
            MontantAssurance = facture.MontantAssurance,
            MontantPatient = facture.MontantTotal - (facture.MontantAssurance ?? 0),
            
            // Facture
            TypeFacture = facture.TypeFacture,
            ServiceNom = facture.Service?.NomService,
            MedecinNom = facture.Medecin?.Utilisateur != null 
                ? $"Dr. {facture.Medecin.Utilisateur.Prenom} {facture.Medecin.Utilisateur.Nom}"
                : null,
            Lignes = facture.Lignes?.Select(l => new LigneFactureDto
            {
                IdLigne = l.IdLigne,
                Description = l.Description,
                Code = l.Code,
                Quantite = l.Quantite,
                PrixUnitaire = l.PrixUnitaire,
                Montant = l.Montant,
                Categorie = l.Categorie
            }).ToList() ?? new List<LigneFactureDto>(),
            
            // Caissier
            CaissierNom = transaction.Caissier?.Utilisateur != null
                ? $"{transaction.Caissier.Utilisateur.Prenom} {transaction.Caissier.Utilisateur.Nom}"
                : ""
        };
    }
}
