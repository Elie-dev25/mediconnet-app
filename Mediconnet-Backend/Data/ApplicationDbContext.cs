using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Entities.Pharmacie;

namespace Mediconnet_Backend.Data;

/// <summary>
/// ApplicationDbContext - Contexte EF Core pour MediConnect
/// Mappe les tables existantes de la base de donnees
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Utilisateur> Utilisateurs { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Medecin> Medecins { get; set; }
    public DbSet<Infirmier> Infirmiers { get; set; }
    public DbSet<Administrateur> Administrateurs { get; set; }
    public DbSet<Caissier> Caissiers { get; set; }
    public DbSet<Accueil> Accueils { get; set; }
    public DbSet<Pharmacien> Pharmaciens { get; set; }
    public DbSet<Biologiste> Biologistes { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<Specialite> Specialites { get; set; }
    
    // Entités Caisse
    public DbSet<Facture> Factures { get; set; }
    public DbSet<LigneFacture> LignesFacture { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<SessionCaisse> SessionsCaisse { get; set; }

    // Entités Rendez-vous
    public DbSet<RendezVous> RendezVous { get; set; }
    public DbSet<CreneauDisponible> CreneauxDisponibles { get; set; }
    public DbSet<IndisponibiliteMedecin> IndisponibilitesMedecin { get; set; }
    public DbSet<SlotLock> SlotLocks { get; set; }

    // Entités Email
    public DbSet<EmailConfirmationToken> EmailConfirmationTokens { get; set; }

    // Entités Consultation et Paramètres
    public DbSet<Consultation> Consultations { get; set; }
    public DbSet<Parametre> Parametres { get; set; }

    // Entités Questions/Réponses (questionnaire consultation)
    public DbSet<Question> Questions { get; set; }
    public DbSet<ConsultationQuestion> ConsultationQuestions { get; set; }
    public DbSet<Reponse> Reponses { get; set; }

    // Entités Assurance
    public DbSet<Assurance> Assurances { get; set; }

    // Entités Prescriptions/Examens/Médicaments
    public DbSet<Medicament> Medicaments { get; set; }
    public DbSet<Ordonnance> Ordonnances { get; set; }
    public DbSet<PrescriptionMedicament> PrescriptionMedicaments { get; set; }
    public DbSet<BulletinExamen> BulletinsExamen { get; set; }
    public DbSet<ExamenCatalogue> ExamensCatalogue { get; set; }

    // Entités Hospitalisation
    public DbSet<Chambre> Chambres { get; set; }
    public DbSet<Lit> Lits { get; set; }
    public DbSet<Hospitalisation> Hospitalisations { get; set; }

    // Entités Pharmacie/Stock
    public DbSet<Fournisseur> Fournisseurs { get; set; }
    public DbSet<CommandePharmacie> CommandesPharmacie { get; set; }
    public DbSet<CommandeLigne> CommandesLignes { get; set; }
    public DbSet<MouvementStock> MouvementsStock { get; set; }
    public DbSet<Dispensation> Dispensations { get; set; }
    public DbSet<DispensationLigne> DispensationsLignes { get; set; }
    public DbSet<Inventaire> Inventaires { get; set; }
    public DbSet<InventaireLigne> InventairesLignes { get; set; }

    // Audit et Sécurité
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Utilisateur Configuration
        modelBuilder.Entity<Utilisateur>(entity =>
        {
            entity.HasKey(e => e.IdUser);
            entity.ToTable("utilisateurs");

            entity.Property(e => e.IdUser).HasColumnName("id_user").ValueGeneratedOnAdd();
            entity.Property(e => e.Nom).HasColumnName("nom").IsRequired();
            entity.Property(e => e.Prenom).HasColumnName("prenom").IsRequired();
            entity.Property(e => e.Naissance).HasColumnName("naissance");
            entity.Property(e => e.Sexe).HasColumnName("sexe").HasMaxLength(10);
            entity.Property(e => e.Telephone).HasColumnName("telephone").HasMaxLength(20);
            entity.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(120);
            entity.Property(e => e.SituationMatrimoniale).HasColumnName("situation_matrimoniale").HasMaxLength(50);
            entity.Property(e => e.Adresse).HasColumnName("adresse");
            entity.Property(e => e.Role).HasColumnName("role").IsRequired().HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(500);
            entity.Property(e => e.EmailConfirmed).HasColumnName("email_confirmed").HasDefaultValue(false);
            entity.Property(e => e.EmailConfirmedAt).HasColumnName("email_confirmed_at");
            entity.Property(e => e.ProfileCompleted).HasColumnName("profile_completed").HasDefaultValue(false);
            entity.Property(e => e.ProfileCompletedAt).HasColumnName("profile_completed_at");
            entity.Property(e => e.Nationalite).HasColumnName("nationalite").HasMaxLength(100);
            entity.Property(e => e.RegionOrigine).HasColumnName("region_origine").HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.Email).IsUnique();

            // Relations
            entity.HasOne(e => e.Patient)
                .WithOne(p => p.Utilisateur)
                .HasForeignKey<Patient>(p => p.IdUser);

            entity.HasOne(e => e.Medecin)
                .WithOne(m => m.Utilisateur)
                .HasForeignKey<Medecin>(m => m.IdUser);

            entity.HasOne(e => e.Infirmier)
                .WithOne(i => i.Utilisateur)
                .HasForeignKey<Infirmier>(i => i.IdUser);

            entity.HasOne(e => e.Administrateur)
                .WithOne(a => a.Utilisateur)
                .HasForeignKey<Administrateur>(a => a.IdUser);

            entity.HasOne(e => e.Caissier)
                .WithOne(c => c.Utilisateur)
                .HasForeignKey<Caissier>(c => c.IdUser);

            entity.HasOne(e => e.Accueil)
                .WithOne(a => a.Utilisateur)
                .HasForeignKey<Accueil>(a => a.IdUser);
        });

        // Patient Configuration
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.IdUser);
            entity.ToTable("patient");

            entity.Property(e => e.IdUser).HasColumnName("id_user");
            entity.Property(e => e.NumeroDossier).HasColumnName("numero_dossier").HasMaxLength(30);
            
            // Informations personnelles
            entity.Property(e => e.Ethnie).HasColumnName("ethnie").HasMaxLength(100);
            
            // Informations médicales
            entity.Property(e => e.GroupeSanguin).HasColumnName("groupe_sanguin").HasMaxLength(10);
            entity.Property(e => e.Profession).HasColumnName("profession").HasMaxLength(255);
            entity.Property(e => e.MaladiesChroniques).HasColumnName("maladies_chroniques");
            entity.Property(e => e.OperationsChirurgicales).HasColumnName("operations_chirurgicales");
            entity.Property(e => e.OperationsDetails).HasColumnName("operations_details");
            entity.Property(e => e.AllergiesConnues).HasColumnName("allergies_connues");
            entity.Property(e => e.AllergiesDetails).HasColumnName("allergies_details");
            entity.Property(e => e.AntecedentsFamiliaux).HasColumnName("antecedents_familiaux");
            entity.Property(e => e.AntecedentsFamiliauxDetails).HasColumnName("antecedents_familiaux_details");
            
            // Habitudes de vie
            entity.Property(e => e.ConsommationAlcool).HasColumnName("consommation_alcool");
            entity.Property(e => e.FrequenceAlcool).HasColumnName("frequence_alcool").HasMaxLength(50);
            entity.Property(e => e.Tabagisme).HasColumnName("tabagisme");
            entity.Property(e => e.ActivitePhysique).HasColumnName("activite_physique");
            
            // Contacts d'urgence
            entity.Property(e => e.NbEnfants).HasColumnName("nb_enfants");
            entity.Property(e => e.PersonneContact).HasColumnName("personne_contact").HasMaxLength(150);
            entity.Property(e => e.NumeroContact).HasColumnName("numero_contact").HasMaxLength(50);
            
            entity.Property(e => e.DateCreation).HasColumnName("date_creation");
            
            // Déclaration sur l'honneur
            entity.Property(e => e.DeclarationHonneurAcceptee).HasColumnName("declaration_honneur_acceptee").HasDefaultValue(false);
            entity.Property(e => e.DeclarationHonneurAt).HasColumnName("declaration_honneur_at");
            
            // Assurance (relation directe 1:N)
            entity.Property(e => e.AssuranceId).HasColumnName("assurance_id");
            entity.Property(e => e.NumeroCarteAssurance).HasColumnName("numero_carte_assurance").HasMaxLength(100);
            entity.Property(e => e.DateDebutValidite).HasColumnName("date_debut_validite");
            entity.Property(e => e.DateFinValidite).HasColumnName("date_fin_validite");
            entity.Property(e => e.CouvertureAssurance).HasColumnName("couverture_assurance").HasColumnType("decimal(5,2)");
            
            entity.HasOne(e => e.Assurance)
                  .WithMany(a => a.Patients)
                  .HasForeignKey(e => e.AssuranceId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Medecin Configuration
        modelBuilder.Entity<Medecin>(entity =>
        {
            entity.HasKey(e => e.IdUser);
            entity.ToTable("medecin");

            entity.Property(e => e.IdUser).HasColumnName("id_user");
            entity.Property(e => e.NumeroOrdre).HasColumnName("numero_ordre").HasMaxLength(50);
            entity.Property(e => e.IdService).HasColumnName("id_service").IsRequired();
            entity.Property(e => e.IdSpecialite).HasColumnName("id_specialite");
        });

        // Infirmier Configuration
        modelBuilder.Entity<Infirmier>(entity =>
        {
            entity.HasKey(e => e.IdUser);
            entity.ToTable("infirmier");

            entity.Property(e => e.IdUser).HasColumnName("id_user");
            entity.Property(e => e.Matricule).HasColumnName("matricule").HasMaxLength(50);
        });

        // Administrateur Configuration
        modelBuilder.Entity<Administrateur>(entity =>
        {
            entity.HasKey(e => e.IdUser);
            entity.ToTable("administrateur");
            entity.Property(e => e.IdUser).HasColumnName("id_user");
        });

        // Caissier Configuration
        modelBuilder.Entity<Caissier>(entity =>
        {
            entity.HasKey(e => e.IdUser);
            entity.ToTable("caissier");
            entity.Property(e => e.IdUser).HasColumnName("id_user");
        });

        // Accueil Configuration
        modelBuilder.Entity<Accueil>(entity =>
        {
            entity.HasKey(e => e.IdUser);
            entity.ToTable("accueil");
            entity.Property(e => e.IdUser).HasColumnName("id_user");
            entity.Property(e => e.Poste).HasColumnName("poste").HasMaxLength(100);
            entity.Property(e => e.DateEmbauche).HasColumnName("date_embauche");
        });

        // Pharmacien Configuration
        modelBuilder.Entity<Pharmacien>(entity =>
        {
            entity.HasKey(e => e.IdPharmacien);
            entity.ToTable("pharmaciens");
            entity.Property(e => e.IdPharmacien).HasColumnName("id_pharmacien").ValueGeneratedOnAdd();
            entity.Property(e => e.IdUser).HasColumnName("id_user");
            entity.Property(e => e.Matricule).HasColumnName("matricule").HasMaxLength(50);
            entity.Property(e => e.NumeroOrdre).HasColumnName("numero_ordre").HasMaxLength(50);
            entity.Property(e => e.DateEmbauche).HasColumnName("date_embauche");
            entity.Property(e => e.Actif).HasColumnName("actif");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(e => e.Utilisateur).WithMany().HasForeignKey(e => e.IdUser);
        });

        // Biologiste Configuration
        modelBuilder.Entity<Biologiste>(entity =>
        {
            entity.HasKey(e => e.IdBiologiste);
            entity.ToTable("biologistes");
            entity.Property(e => e.IdBiologiste).HasColumnName("id_biologiste").ValueGeneratedOnAdd();
            entity.Property(e => e.IdUser).HasColumnName("id_user");
            entity.Property(e => e.Matricule).HasColumnName("matricule").HasMaxLength(50);
            entity.Property(e => e.Specialisation).HasColumnName("specialisation").HasMaxLength(100);
            entity.Property(e => e.DateEmbauche).HasColumnName("date_embauche");
            entity.Property(e => e.Actif).HasColumnName("actif");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(e => e.Utilisateur).WithMany().HasForeignKey(e => e.IdUser);
        });

        // Service Configuration
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.IdService);
            entity.ToTable("service");

            entity.Property(e => e.IdService).HasColumnName("id_service").ValueGeneratedOnAdd();
            entity.Property(e => e.NomService).HasColumnName("nom_service").IsRequired().HasMaxLength(150);
            entity.Property(e => e.ResponsableService).HasColumnName("responsable_service");
            entity.Property(e => e.Description).HasColumnName("description");
        });

        // Specialite Configuration
        modelBuilder.Entity<Specialite>(entity =>
        {
            entity.HasKey(e => e.IdSpecialite);
            entity.ToTable("specialites");

            entity.Property(e => e.IdSpecialite).HasColumnName("id_specialite").ValueGeneratedOnAdd();
            entity.Property(e => e.NomSpecialite).HasColumnName("nom_specialite").IsRequired().HasMaxLength(100);
        });

        // ==================== ENTITÉS CAISSE ====================

        // Facture Configuration
        modelBuilder.Entity<Facture>(entity =>
        {
            entity.HasKey(e => e.IdFacture);
            entity.ToTable("facture");

            entity.Property(e => e.IdFacture).HasColumnName("id_facture").ValueGeneratedOnAdd();
            entity.Property(e => e.NumeroFacture).HasColumnName("numero_facture").IsRequired().HasMaxLength(30);
            entity.Property(e => e.IdPatient).HasColumnName("id_patient").IsRequired();
            entity.Property(e => e.IdMedecin).HasColumnName("id_medecin");
            entity.Property(e => e.IdService).HasColumnName("id_service");
            entity.Property(e => e.IdSpecialite).HasColumnName("id_specialite");
            entity.Property(e => e.IdConsultation).HasColumnName("id_consultation");
            entity.Property(e => e.MontantTotal).HasColumnName("montant_total").HasColumnType("decimal(12,2)");
            entity.Property(e => e.MontantPaye).HasColumnName("montant_paye").HasColumnType("decimal(12,2)");
            entity.Property(e => e.MontantRestant).HasColumnName("montant_restant").HasColumnType("decimal(12,2)");
            entity.Property(e => e.Statut).HasColumnName("statut").HasMaxLength(30);
            entity.Property(e => e.TypeFacture).HasColumnName("type_facture").HasMaxLength(50);
            entity.Property(e => e.DateCreation).HasColumnName("date_creation");
            entity.Property(e => e.DateEcheance).HasColumnName("date_echeance");
            entity.Property(e => e.DatePaiement).HasColumnName("date_paiement");
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(500);
            entity.Property(e => e.CouvertureAssurance).HasColumnName("couverture_assurance");
            entity.Property(e => e.IdAssurance).HasColumnName("id_assurance");
            entity.Property(e => e.TauxCouverture).HasColumnName("taux_couverture").HasColumnType("decimal(5,2)");
            entity.Property(e => e.MontantAssurance).HasColumnName("montant_assurance").HasColumnType("decimal(12,2)");

            entity.HasIndex(e => e.NumeroFacture).IsUnique();

            entity.HasOne(e => e.Patient).WithMany().HasForeignKey(e => e.IdPatient);
            entity.HasOne(e => e.Medecin).WithMany().HasForeignKey(e => e.IdMedecin);
            entity.HasOne(e => e.Service).WithMany().HasForeignKey(e => e.IdService);
            entity.HasOne(e => e.Specialite).WithMany().HasForeignKey(e => e.IdSpecialite);
            entity.HasOne(e => e.Consultation).WithMany().HasForeignKey(e => e.IdConsultation);
        });

        // LigneFacture Configuration - mapped to facture_item table
        modelBuilder.Entity<LigneFacture>(entity =>
        {
            entity.HasKey(e => e.IdLigne);
            entity.ToTable("facture_item");

            entity.Property(e => e.IdLigne).HasColumnName("id_item").ValueGeneratedOnAdd();
            entity.Property(e => e.IdFacture).HasColumnName("id_facture").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").IsRequired().HasMaxLength(255);
            entity.Property(e => e.Quantite).HasColumnName("quantite");
            entity.Property(e => e.PrixUnitaire).HasColumnName("prix_unitaire").HasColumnType("decimal(10,2)");
            entity.Property(e => e.Montant).HasColumnName("total_ligne").HasColumnType("decimal(10,2)");
            entity.Property(e => e.Categorie).HasColumnName("type_service").HasMaxLength(50);

            // Ignorer la propriété Code qui n'existe pas dans facture_item
            entity.Ignore(e => e.Code);

            entity.HasOne(e => e.Facture).WithMany(f => f.Lignes).HasForeignKey(e => e.IdFacture);
        });

        // Transaction Configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.IdTransaction);
            entity.ToTable("transaction_paiement");

            entity.Property(e => e.IdTransaction).HasColumnName("id_transaction").ValueGeneratedOnAdd();
            entity.Property(e => e.NumeroTransaction).HasColumnName("numero_transaction").IsRequired().HasMaxLength(50);
            entity.Property(e => e.TransactionUuid).HasColumnName("transaction_uuid").IsRequired().HasMaxLength(36);
            entity.Property(e => e.IdFacture).HasColumnName("id_facture").IsRequired();
            entity.Property(e => e.IdPatient).HasColumnName("id_patient");
            entity.Property(e => e.IdCaissier).HasColumnName("id_caissier").IsRequired();
            entity.Property(e => e.IdSessionCaisse).HasColumnName("id_session_caisse");
            entity.Property(e => e.Montant).HasColumnName("montant").HasColumnType("decimal(12,2)");
            entity.Property(e => e.ModePaiement).HasColumnName("mode_paiement").HasMaxLength(30);
            entity.Property(e => e.Statut).HasColumnName("statut").HasMaxLength(30);
            entity.Property(e => e.Reference).HasColumnName("reference").HasMaxLength(100);
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(500);
            entity.Property(e => e.DateTransaction).HasColumnName("date_transaction");
            entity.Property(e => e.DateAnnulation).HasColumnName("date_annulation");
            entity.Property(e => e.MotifAnnulation).HasColumnName("motif_annulation").HasMaxLength(500);
            entity.Property(e => e.AnnulePar).HasColumnName("annule_par");
            entity.Property(e => e.EstPaiementPartiel).HasColumnName("est_paiement_partiel");
            entity.Property(e => e.MontantRecu).HasColumnName("montant_recu").HasColumnType("decimal(12,2)");
            entity.Property(e => e.RenduMonnaie).HasColumnName("rendu_monnaie").HasColumnType("decimal(12,2)");

            entity.HasIndex(e => e.TransactionUuid).IsUnique();
            entity.HasIndex(e => e.NumeroTransaction).IsUnique();

            entity.HasOne(e => e.Facture).WithMany(f => f.Transactions).HasForeignKey(e => e.IdFacture);
            entity.HasOne(e => e.Patient).WithMany().HasForeignKey(e => e.IdPatient);
            entity.HasOne(e => e.Caissier).WithMany().HasForeignKey(e => e.IdCaissier);
            entity.HasOne(e => e.SessionCaisse).WithMany(s => s.Transactions).HasForeignKey(e => e.IdSessionCaisse);
        });

        // SessionCaisse Configuration
        modelBuilder.Entity<SessionCaisse>(entity =>
        {
            entity.HasKey(e => e.IdSession);
            entity.ToTable("session_caisse");

            entity.Property(e => e.IdSession).HasColumnName("id_session").ValueGeneratedOnAdd();
            entity.Property(e => e.IdCaissier).HasColumnName("id_caissier").IsRequired();
            entity.Property(e => e.MontantOuverture).HasColumnName("montant_ouverture").HasColumnType("decimal(12,2)");
            entity.Property(e => e.MontantFermeture).HasColumnName("montant_fermeture").HasColumnType("decimal(12,2)");
            entity.Property(e => e.MontantSysteme).HasColumnName("montant_systeme").HasColumnType("decimal(12,2)");
            entity.Property(e => e.Ecart).HasColumnName("ecart").HasColumnType("decimal(12,2)");
            entity.Property(e => e.DateOuverture).HasColumnName("date_ouverture");
            entity.Property(e => e.DateFermeture).HasColumnName("date_fermeture");
            entity.Property(e => e.Statut).HasColumnName("statut").HasMaxLength(20);
            entity.Property(e => e.NotesOuverture).HasColumnName("notes_ouverture").HasMaxLength(500);
            entity.Property(e => e.NotesFermeture).HasColumnName("notes_fermeture").HasMaxLength(500);
            entity.Property(e => e.NotesRapprochement).HasColumnName("notes_rapprochement").HasMaxLength(500);
            entity.Property(e => e.ValidePar).HasColumnName("valide_par");

            entity.HasOne(e => e.Caissier).WithMany().HasForeignKey(e => e.IdCaissier);
        });

        // ==================== ENTITÉS RENDEZ-VOUS ====================

        // RendezVous Configuration
        modelBuilder.Entity<RendezVous>(entity =>
        {
            entity.HasKey(e => e.IdRendezVous);
            entity.ToTable("rendez_vous");

            entity.Property(e => e.IdRendezVous).HasColumnName("id_rdv").ValueGeneratedOnAdd();
            entity.Property(e => e.IdPatient).HasColumnName("id_patient").IsRequired();
            entity.Property(e => e.IdMedecin).HasColumnName("id_medecin").IsRequired();
            entity.Property(e => e.IdService).HasColumnName("id_service");
            entity.Property(e => e.DateHeure).HasColumnName("date_heure").IsRequired();
            entity.Property(e => e.Duree).HasColumnName("duree");
            entity.Property(e => e.Statut).HasColumnName("statut").HasMaxLength(30);
            entity.Property(e => e.Motif).HasColumnName("motif").HasMaxLength(100);
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(500);
            entity.Property(e => e.MotifAnnulation).HasColumnName("motif_annulation").HasMaxLength(500);
            entity.Property(e => e.DateAnnulation).HasColumnName("date_annulation");
            entity.Property(e => e.AnnulePar).HasColumnName("annule_par");
            entity.Property(e => e.DateCreation).HasColumnName("date_creation");
            entity.Property(e => e.DateModification).HasColumnName("date_modification");
            entity.Property(e => e.TypeRdv).HasColumnName("type_rdv").HasMaxLength(50);
            entity.Property(e => e.Notifie).HasColumnName("notifie");
            entity.Property(e => e.RappelEnvoye).HasColumnName("rappel_envoye");
            
            // Verrouillage optimiste avec RowVersion
            entity.Property(e => e.RowVersion)
                .HasColumnName("row_version")
                .IsRowVersion()
                .IsConcurrencyToken();

            // Index pour optimiser les requêtes fréquentes
            entity.HasIndex(e => e.IdMedecin).HasDatabaseName("IX_rdv_medecin");
            entity.HasIndex(e => e.IdPatient).HasDatabaseName("IX_rdv_patient");
            entity.HasIndex(e => e.DateHeure).HasDatabaseName("IX_rdv_date");
            entity.HasIndex(e => e.Statut).HasDatabaseName("IX_rdv_statut");
            entity.HasIndex(e => new { e.IdMedecin, e.DateHeure, e.Statut }).HasDatabaseName("IX_rdv_medecin_date_statut");

            entity.HasOne(e => e.Patient).WithMany().HasForeignKey(e => e.IdPatient);
            entity.HasOne(e => e.Medecin).WithMany().HasForeignKey(e => e.IdMedecin);
            entity.HasOne(e => e.Service).WithMany().HasForeignKey(e => e.IdService);
        });

        // CreneauDisponible Configuration
        modelBuilder.Entity<CreneauDisponible>(entity =>
        {
            entity.HasKey(e => e.IdCreneau);
            entity.ToTable("creneau_disponible");

            entity.Property(e => e.IdCreneau).HasColumnName("id_creneau").ValueGeneratedOnAdd();
            entity.Property(e => e.IdMedecin).HasColumnName("id_medecin").IsRequired();
            entity.Property(e => e.JourSemaine).HasColumnName("jour_semaine").IsRequired();
            entity.Property(e => e.HeureDebut).HasColumnName("heure_debut").IsRequired();
            entity.Property(e => e.HeureFin).HasColumnName("heure_fin").IsRequired();
            entity.Property(e => e.DureeParDefaut).HasColumnName("duree_par_defaut");
            entity.Property(e => e.Actif).HasColumnName("actif");
            entity.Property(e => e.DateDebutValidite).HasColumnName("date_debut_validite");
            entity.Property(e => e.DateFinValidite).HasColumnName("date_fin_validite");
            entity.Property(e => e.EstSemaineType).HasColumnName("est_semaine_type");

            entity.HasOne(e => e.Medecin).WithMany().HasForeignKey(e => e.IdMedecin);
        });

        // IndisponibiliteMedecin Configuration
        modelBuilder.Entity<IndisponibiliteMedecin>(entity =>
        {
            entity.HasKey(e => e.IdIndisponibilite);
            entity.ToTable("indisponibilite_medecin");

            entity.Property(e => e.IdIndisponibilite).HasColumnName("id_indisponibilite").ValueGeneratedOnAdd();
            entity.Property(e => e.IdMedecin).HasColumnName("id_medecin").IsRequired();
            entity.Property(e => e.DateDebut).HasColumnName("date_debut").IsRequired();
            entity.Property(e => e.DateFin).HasColumnName("date_fin").IsRequired();
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(50);
            entity.Property(e => e.Motif).HasColumnName("motif").HasMaxLength(200);
            entity.Property(e => e.JourneeComplete).HasColumnName("journee_complete");

            // Index pour recherche rapide par médecin et période
            entity.HasIndex(e => e.IdMedecin).HasDatabaseName("IX_indispo_medecin");
            entity.HasIndex(e => new { e.IdMedecin, e.DateDebut, e.DateFin }).HasDatabaseName("IX_indispo_medecin_periode");

            entity.HasOne(e => e.Medecin).WithMany().HasForeignKey(e => e.IdMedecin);
        });

        // SlotLock Configuration - Verrou temporaire pour éviter doubles réservations
        modelBuilder.Entity<SlotLock>(entity =>
        {
            entity.HasKey(e => e.IdLock);
            entity.ToTable("slot_lock");

            entity.Property(e => e.IdLock).HasColumnName("id_lock").ValueGeneratedOnAdd();
            entity.Property(e => e.IdMedecin).HasColumnName("id_medecin").IsRequired();
            entity.Property(e => e.DateHeure).HasColumnName("date_heure").IsRequired();
            entity.Property(e => e.Duree).HasColumnName("duree");
            entity.Property(e => e.IdUser).HasColumnName("id_user").IsRequired();
            entity.Property(e => e.LockToken).HasColumnName("lock_token").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            // Index unique pour éviter les doublons sur le même créneau
            entity.HasIndex(e => new { e.IdMedecin, e.DateHeure })
                .HasDatabaseName("IX_slot_lock_medecin_date")
                .IsUnique();

            // Index pour nettoyer les verrous expirés
            entity.HasIndex(e => e.ExpiresAt).HasDatabaseName("IX_slot_lock_expires");

            entity.HasOne(e => e.Medecin).WithMany().HasForeignKey(e => e.IdMedecin);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.IdUser);
        });

        // ==================== ENTITÉS EMAIL ====================

        // EmailConfirmationToken Configuration
        modelBuilder.Entity<EmailConfirmationToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("email_confirmation_tokens");

            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.IdUser).HasColumnName("id_user").IsRequired();
            entity.Property(e => e.Token).HasColumnName("token").IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at").IsRequired();
            entity.Property(e => e.IsUsed).HasColumnName("is_used").HasDefaultValue(false);
            entity.Property(e => e.UsedAt).HasColumnName("used_at");
            entity.Property(e => e.ConfirmedFromIp).HasColumnName("confirmed_from_ip").HasMaxLength(50);

            // Index pour recherche rapide par token
            entity.HasIndex(e => e.Token).IsUnique().HasDatabaseName("IX_email_token");
            entity.HasIndex(e => e.IdUser).HasDatabaseName("IX_email_token_user");

            entity.HasOne(e => e.Utilisateur).WithMany().HasForeignKey(e => e.IdUser);
        });

        // ==================== ENTITÉS CONSULTATION ====================

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("question");

            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.TexteQuestion).HasColumnName("texte_question").IsRequired();
            entity.Property(e => e.TypeQuestion).HasColumnName("type_question").IsRequired().HasMaxLength(50);
            entity.Property(e => e.EstPredefinie).HasColumnName("est_predefinie");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.EstPredefinie).HasDatabaseName("IX_question_predefinie");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_question_created_at");

            entity.HasOne(e => e.Createur).WithMany().HasForeignKey(e => e.CreatedBy);
        });

        modelBuilder.Entity<ConsultationQuestion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("consultation_question");

            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.ConsultationId).HasColumnName("consultation_id").IsRequired();
            entity.Property(e => e.QuestionId).HasColumnName("question_id").IsRequired();
            entity.Property(e => e.OrdreAffichage).HasColumnName("ordre_affichage").IsRequired();

            entity.HasIndex(e => new { e.ConsultationId, e.QuestionId })
                .IsUnique()
                .HasDatabaseName("UX_consultation_question");
            entity.HasIndex(e => new { e.ConsultationId, e.OrdreAffichage })
                .HasDatabaseName("IX_consultation_question_ordre");
            entity.HasIndex(e => e.QuestionId).HasDatabaseName("IX_consultation_question_question");

            entity.HasOne(e => e.Consultation)
                .WithMany(c => c.ConsultationQuestions)
                .HasForeignKey(e => e.ConsultationId);

            entity.HasOne(e => e.Question)
                .WithMany(q => q.ConsultationQuestions)
                .HasForeignKey(e => e.QuestionId);
        });

        modelBuilder.Entity<Reponse>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("reponse");

            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.ConsultationId).HasColumnName("consultation_id").IsRequired();
            entity.Property(e => e.QuestionId).HasColumnName("question_id").IsRequired();
            entity.Property(e => e.ValeurReponse).HasColumnName("valeur_reponse");
            entity.Property(e => e.RempliPar).HasColumnName("rempli_par").IsRequired().HasMaxLength(20);
            entity.Property(e => e.DateSaisie).HasColumnName("date_saisie");

            entity.HasIndex(e => new { e.ConsultationId, e.QuestionId })
                .IsUnique()
                .HasDatabaseName("UX_reponse_consultation_question");
            entity.HasIndex(e => e.DateSaisie).HasDatabaseName("IX_reponse_date_saisie");

            entity.HasOne(e => e.Consultation)
                .WithMany(c => c.Reponses)
                .HasForeignKey(e => e.ConsultationId);

            entity.HasOne(e => e.Question)
                .WithMany(q => q.Reponses)
                .HasForeignKey(e => e.QuestionId);
        });

        // Consultation Configuration
        modelBuilder.Entity<Consultation>(entity =>
        {
            entity.HasKey(e => e.IdConsultation);
            entity.ToTable("consultation");

            entity.Property(e => e.IdConsultation).HasColumnName("id_consultation").ValueGeneratedOnAdd();
            entity.Property(e => e.DateHeure).HasColumnName("date_heure").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.Motif).HasColumnName("motif");
            entity.Property(e => e.Diagnostic).HasColumnName("diagnostic");
            entity.Property(e => e.Statut).HasColumnName("statut").HasMaxLength(20);
            entity.Property(e => e.IdMedecin).HasColumnName("id_medecin").IsRequired();
            entity.Property(e => e.IdPatient).HasColumnName("id_patient").IsRequired();
            entity.Property(e => e.IdRendezVous).HasColumnName("id_rdv");
            entity.Property(e => e.TypeConsultation).HasColumnName("type_consultation").HasMaxLength(100);
            entity.Property(e => e.Antecedents).HasColumnName("antecedents");
            entity.Property(e => e.CheminQuestionnaire).HasColumnName("chemin_questionnaire").HasMaxLength(255);

            // Index
            entity.HasIndex(e => e.IdMedecin).HasDatabaseName("IX_consultation_medecin");
            entity.HasIndex(e => e.IdPatient).HasDatabaseName("IX_consultation_patient");
            entity.HasIndex(e => e.DateHeure).HasDatabaseName("IX_consultation_date");
            entity.HasIndex(e => e.IdRendezVous).HasDatabaseName("IX_consultation_rdv");

            // Relations
            entity.HasOne(e => e.Medecin).WithMany().HasForeignKey(e => e.IdMedecin);
            entity.HasOne(e => e.Patient).WithMany().HasForeignKey(e => e.IdPatient);
            entity.HasOne(e => e.RendezVous).WithMany().HasForeignKey(e => e.IdRendezVous);
            entity.HasOne(e => e.Parametre).WithOne(p => p.Consultation)
                .HasForeignKey<Parametre>(p => p.IdConsultation);
        });

        // Parametre Configuration - Paramètres vitaux
        modelBuilder.Entity<Parametre>(entity =>
        {
            entity.HasKey(e => e.IdParametre);
            entity.ToTable("parametre");

            entity.Property(e => e.IdParametre).HasColumnName("id_parametre").ValueGeneratedOnAdd();
            entity.Property(e => e.IdConsultation).HasColumnName("id_consultation").IsRequired();
            entity.Property(e => e.Poids).HasColumnName("poids").HasColumnType("decimal(5,2)");
            entity.Property(e => e.Temperature).HasColumnName("temperature").HasColumnType("decimal(4,1)");
            entity.Property(e => e.TensionSystolique).HasColumnName("tension_systolique");
            entity.Property(e => e.TensionDiastolique).HasColumnName("tension_diastolique");
            entity.Property(e => e.Taille).HasColumnName("taille").HasColumnType("decimal(5,2)");
            entity.Property(e => e.DateEnregistrement).HasColumnName("date_enregistrement");
            entity.Property(e => e.EnregistrePar).HasColumnName("enregistre_par");

            // Index unique sur consultation (relation 1-1)
            entity.HasIndex(e => e.IdConsultation).IsUnique().HasDatabaseName("IX_parametre_consultation");

            // Relations
            entity.HasOne(e => e.UtilisateurEnregistrant).WithMany().HasForeignKey(e => e.EnregistrePar);
        });

        // ==================== ENTITÉS ASSURANCE ====================

        // Assurance Configuration
        modelBuilder.Entity<Assurance>(entity =>
        {
            entity.HasKey(e => e.IdAssurance);
            entity.ToTable("assurances");

            entity.Property(e => e.IdAssurance).HasColumnName("id_assurance").ValueGeneratedOnAdd();
            entity.Property(e => e.Nom).HasColumnName("nom").IsRequired().HasMaxLength(150);
            entity.Property(e => e.TypeAssurance).HasColumnName("type_assurance").HasMaxLength(50);
            entity.Property(e => e.SiteWeb).HasColumnName("site_web").HasMaxLength(255);
            entity.Property(e => e.TelephoneServiceClient).HasColumnName("telephone_service_client").HasMaxLength(30);
            entity.Property(e => e.Groupe).HasColumnName("groupe").HasMaxLength(100);
            entity.Property(e => e.PaysOrigine).HasColumnName("pays_origine").HasMaxLength(100);
            entity.Property(e => e.StatutJuridique).HasColumnName("statut_juridique").HasMaxLength(50);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000);
            entity.Property(e => e.TypeCouverture).HasColumnName("type_couverture").HasMaxLength(500);
            entity.Property(e => e.IsComplementaire).HasColumnName("is_complementaire");
            entity.Property(e => e.CategorieBeneficiaires).HasColumnName("categorie_beneficiaires").HasMaxLength(255);
            entity.Property(e => e.ConditionsAdhesion).HasColumnName("conditions_adhesion").HasMaxLength(1000);
            entity.Property(e => e.ZoneCouverture).HasColumnName("zone_couverture").HasMaxLength(100);
            entity.Property(e => e.ModePaiement).HasColumnName("mode_paiement").HasMaxLength(255);
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.Nom).HasDatabaseName("IX_assurance_nom");
            entity.HasIndex(e => e.TypeAssurance).HasDatabaseName("IX_assurance_type");
        });

        // ==================== ENTITÉS PRESCRIPTIONS/EXAMENS ====================

        // PrescriptionMedicament Configuration (clé composite)
        modelBuilder.Entity<PrescriptionMedicament>(entity =>
        {
            entity.HasKey(e => new { e.IdOrdonnance, e.IdMedicament });
            entity.ToTable("prescription_medicament");

            entity.Property(e => e.IdOrdonnance).HasColumnName("id_ord");
            entity.Property(e => e.IdMedicament).HasColumnName("id_medicament");
            entity.Property(e => e.Quantite).HasColumnName("quantite");
            entity.Property(e => e.DureeTraitement).HasColumnName("duree_traitement");
            entity.Property(e => e.Posologie).HasColumnName("posologie");

            entity.HasOne(e => e.Ordonnance)
                .WithMany(o => o.Medicaments)
                .HasForeignKey(e => e.IdOrdonnance);

            entity.HasOne(e => e.Medicament)
                .WithMany()
                .HasForeignKey(e => e.IdMedicament);
        });

        // Ordonnance Configuration
        modelBuilder.Entity<Ordonnance>(entity =>
        {
            entity.HasKey(e => e.IdOrdonnance);
            entity.ToTable("prescription");

            entity.Property(e => e.IdOrdonnance).HasColumnName("id_ord").ValueGeneratedOnAdd();
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.IdConsultation).HasColumnName("id_consultation");
            entity.Property(e => e.Commentaire).HasColumnName("commentaire");

            entity.HasOne(e => e.Consultation)
                .WithOne(c => c.Ordonnance)
                .HasForeignKey<Ordonnance>(e => e.IdConsultation);
        });

        // BulletinExamen Configuration
        modelBuilder.Entity<BulletinExamen>(entity =>
        {
            entity.HasKey(e => e.IdBulletinExamen);
            entity.ToTable("bulletin_examen");

            entity.Property(e => e.IdBulletinExamen).HasColumnName("id_bull_exam").ValueGeneratedOnAdd();
            entity.Property(e => e.DateDemande).HasColumnName("date_demande");
            entity.Property(e => e.IdLabo).HasColumnName("id_labo");
            entity.Property(e => e.IdConsultation).HasColumnName("id_consultation");
            entity.Property(e => e.Instructions).HasColumnName("instructions");
            entity.Property(e => e.IdExamen).HasColumnName("id_exam");

            entity.HasOne(e => e.Consultation)
                .WithMany(c => c.BulletinsExamen)
                .HasForeignKey(e => e.IdConsultation);

            entity.HasOne(e => e.Examen)
                .WithMany()
                .HasForeignKey(e => e.IdExamen);
        });

        // ExamenCatalogue Configuration
        modelBuilder.Entity<ExamenCatalogue>(entity =>
        {
            entity.HasKey(e => e.IdExamen);
            entity.ToTable("examens");

            entity.Property(e => e.IdExamen).HasColumnName("id_exam").ValueGeneratedOnAdd();
            entity.Property(e => e.NomExamen).HasColumnName("nom_exam");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.PrixUnitaire).HasColumnName("prix_unitaire");
            entity.Property(e => e.DureeEstimeeMinutes).HasColumnName("duree_estimee_minutes");
            entity.Property(e => e.PreparationRequise).HasColumnName("preparation_requise");
            entity.Property(e => e.TypeExamen).HasColumnName("type_examen");
            entity.Property(e => e.Categorie).HasColumnName("categorie");
            entity.Property(e => e.Actif).HasColumnName("actif");
        });

        // Medicament Configuration
        modelBuilder.Entity<Medicament>(entity =>
        {
            entity.HasKey(e => e.IdMedicament);
            entity.ToTable("medicament");

            entity.Property(e => e.IdMedicament).HasColumnName("id_medicament").ValueGeneratedOnAdd();
            entity.Property(e => e.Nom).HasColumnName("nom");
            entity.Property(e => e.Dosage).HasColumnName("dosage");
            entity.Property(e => e.DateHeureCreation).HasColumnName("date_heure_creation");
            entity.Property(e => e.Stock).HasColumnName("stock");
            entity.Property(e => e.Prix).HasColumnName("prix");
            entity.Property(e => e.SeuilStock).HasColumnName("seuil_stock");
            entity.Property(e => e.CodeATC).HasColumnName("code_ATC");
            entity.Property(e => e.FormeGalenique).HasColumnName("forme_galenique");
            entity.Property(e => e.Laboratoire).HasColumnName("laboratoire");
            entity.Property(e => e.Conditionnement).HasColumnName("conditionnement");
            entity.Property(e => e.DatePeremption).HasColumnName("date_peremption");
            entity.Property(e => e.Actif).HasColumnName("actif");
            entity.Property(e => e.EmplacementRayon).HasColumnName("emplacement_rayon");
            entity.Property(e => e.TemperatureConservation).HasColumnName("temperature_conservation");
        });

        // ==================== ENTITÉS HOSPITALISATION ====================

        // Chambre Configuration
        modelBuilder.Entity<Chambre>(entity =>
        {
            entity.HasKey(e => e.IdChambre);
            entity.ToTable("chambre");

            entity.Property(e => e.IdChambre).HasColumnName("id_chambre").ValueGeneratedOnAdd();
            entity.Property(e => e.Numero).HasColumnName("numero").HasMaxLength(20);
            entity.Property(e => e.Capacite).HasColumnName("capacite");
            entity.Property(e => e.Etat).HasColumnName("etat").HasMaxLength(50);
            entity.Property(e => e.Statut).HasColumnName("statut").HasMaxLength(50);

            entity.HasIndex(e => e.Numero).IsUnique().HasDatabaseName("IX_chambre_numero");
        });

        // Lit Configuration
        modelBuilder.Entity<Lit>(entity =>
        {
            entity.HasKey(e => e.IdLit);
            entity.ToTable("lit");

            entity.Property(e => e.IdLit).HasColumnName("id_lit").ValueGeneratedOnAdd();
            entity.Property(e => e.Numero).HasColumnName("numero").HasMaxLength(20);
            entity.Property(e => e.Statut).HasColumnName("statut").HasMaxLength(50);
            entity.Property(e => e.IdChambre).HasColumnName("id_chambre").IsRequired();

            entity.HasOne(e => e.Chambre)
                .WithMany(c => c.Lits)
                .HasForeignKey(e => e.IdChambre);
        });

        // Hospitalisation Configuration
        modelBuilder.Entity<Hospitalisation>(entity =>
        {
            entity.HasKey(e => e.IdAdmission);
            entity.ToTable("hospitalisation");

            entity.Property(e => e.IdAdmission).HasColumnName("id_admission").ValueGeneratedOnAdd();
            entity.Property(e => e.DateEntree).HasColumnName("date_entree").IsRequired();
            entity.Property(e => e.DateSortie).HasColumnName("date_sortie");
            entity.Property(e => e.Motif).HasColumnName("motif");
            entity.Property(e => e.Statut).HasColumnName("statut").HasMaxLength(20);
            entity.Property(e => e.IdPatient).HasColumnName("id_patient").IsRequired();
            entity.Property(e => e.IdLit).HasColumnName("id_lit").IsRequired();

            entity.HasIndex(e => e.IdPatient).HasDatabaseName("IX_hospitalisation_patient");
            entity.HasIndex(e => e.IdLit).HasDatabaseName("IX_hospitalisation_lit");
            entity.HasIndex(e => e.Statut).HasDatabaseName("IX_hospitalisation_statut");

            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.IdPatient);

            entity.HasOne(e => e.Lit)
                .WithMany(l => l.Hospitalisations)
                .HasForeignKey(e => e.IdLit);
        });
    }
}
