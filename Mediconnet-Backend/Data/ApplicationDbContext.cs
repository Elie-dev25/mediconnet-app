using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Entities.Pharmacie;
using Mediconnet_Backend.Core.Entities.Facturation;
using Mediconnet_Backend.Core.Entities.Medical;
using Mediconnet_Backend.Core.Entities.GestionLits;
using Mediconnet_Backend.Core.Entities.DMP;
using Mediconnet_Backend.Core.Entities.Documents;

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
    public DbSet<Laborantin> Laborantins { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<Specialite> Specialites { get; set; }
    public DbSet<SpecialiteInfirmier> SpecialitesInfirmiers { get; set; }
    
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
    public DbSet<ConsultationGynecologique> ConsultationsGynecologiques { get; set; }
    public DbSet<ConsultationChirurgicale> ConsultationsChirurgicales { get; set; }
    public DbSet<ProgrammationIntervention> ProgrammationsInterventions { get; set; }

    // Entités Questions/Réponses (questionnaire consultation)
    public DbSet<Question> Questions { get; set; }
    public DbSet<ConsultationQuestion> ConsultationQuestions { get; set; }
    public DbSet<Reponse> Reponses { get; set; }

    // Entités Assurance
    public DbSet<Assurance> Assurances { get; set; }
    public DbSet<AssuranceCouverture> AssuranceCouvertures { get; set; }

    // Tables de référence Assurance
    public DbSet<TypePrestation> TypesPrestations { get; set; }
    public DbSet<CategorieBeneficiaire> CategoriesBeneficiaires { get; set; }
    public DbSet<ModePaiement> ModesPaiement { get; set; }
    public DbSet<ZoneCouverture> ZonesCouverture { get; set; }
    public DbSet<TypeCouvertureSante> TypesCouvertureSante { get; set; }

    // Entités Prescriptions/Examens/Médicaments
    public DbSet<Medicament> Medicaments { get; set; }
    public DbSet<Ordonnance> Ordonnances { get; set; }
    public DbSet<PrescriptionMedicament> PrescriptionMedicaments { get; set; }
    public DbSet<BulletinExamen> BulletinsExamen { get; set; }
    public DbSet<CategorieExamen> CategoriesExamens { get; set; }
    public DbSet<SpecialiteExamen> SpecialitesExamens { get; set; }
    public DbSet<ExamenCatalogue> ExamensCatalogue { get; set; }
    public DbSet<Laboratoire> Laboratoires { get; set; }
    // Orientation unifiée (remplace OrientationSpecialiste et Recommandation)
    public DbSet<OrientationPreConsultation> OrientationsPreConsultation { get; set; }

    // Entités Hospitalisation
    public DbSet<StandardChambre> StandardsChambres { get; set; }
    public DbSet<Chambre> Chambres { get; set; }
    public DbSet<Lit> Lits { get; set; }
    public DbSet<Hospitalisation> Hospitalisations { get; set; }
    public DbSet<SoinHospitalisation> SoinsHospitalisation { get; set; }
    public DbSet<ExecutionSoin> ExecutionsSoins { get; set; }

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

    // Notifications
    public DbSet<Notification> Notifications { get; set; }

    // Entités Facturation Avancée
    public DbSet<Echeancier> Echeanciers { get; set; }
    public DbSet<Echeance> Echeances { get; set; }
    public DbSet<DemandeRemboursement> DemandesRemboursement { get; set; }

    // Entités Alertes Médicales
    public DbSet<AllergiePatient> AllergiesPatients { get; set; }
    public DbSet<InteractionMedicamenteuse> InteractionsMedicamenteuses { get; set; }
    public DbSet<ContreIndication> ContreIndications { get; set; }
    public DbSet<AlerteMedicale> AlertesMedicales { get; set; }

    // Entités Formes Pharmaceutiques et Voies d'Administration
    public DbSet<FormePharmaceutique> FormesPharmaceutiques { get; set; }
    public DbSet<VoieAdministration> VoiesAdministration { get; set; }
    public DbSet<MedicamentForme> MedicamentFormes { get; set; }
    public DbSet<MedicamentVoie> MedicamentVoies { get; set; }

    // Entités Gestion des Lits
    public DbSet<ReservationLit> ReservationsLits { get; set; }
    public DbSet<TransfertLit> TransfertsLits { get; set; }
    public DbSet<MaintenanceLit> MaintenancesLits { get; set; }

    // Entités DMP
    public DbSet<DossierMedicalPartage> DossiersMP { get; set; }
    public DbSet<DocumentDMP> DocumentsDMP { get; set; }
    public DbSet<AutorisationDMP> AutorisationsDMP { get; set; }
    public DbSet<AccesDMP> AccesDMP { get; set; }

    // Entités Permissions (RBAC)
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }

    // Entités Documents Médicaux (stockage UUID)
    public DbSet<DocumentMedical> DocumentsMedicaux { get; set; }
    public DbSet<AuditAccesDocument> AuditAccesDocuments { get; set; }
    public DbSet<VerificationIntegrite> VerificationsIntegrite { get; set; }
    public DbSet<AlerteSysteme> AlertesSysteme { get; set; }

    // Entités Bloc Opératoire
    public DbSet<BlocOperatoire> BlocsOperatoires { get; set; }
    public DbSet<ReservationBloc> ReservationsBlocs { get; set; }

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
            entity.Property(e => e.MustChangePassword).HasColumnName("must_change_password").HasDefaultValue(false);
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
            entity.Property(e => e.AssuranceId).HasColumnName("id_assurance");
            entity.Property(e => e.NumeroCarteAssurance).HasColumnName("numero_carte_assurance").HasMaxLength(100);
            entity.Property(e => e.DateDebutValidite).HasColumnName("date_debut_validite");
            entity.Property(e => e.DateFinValidite).HasColumnName("date_fin_validite");
            
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

        // ==================== SPECIALITE INFIRMIER ====================
        modelBuilder.Entity<SpecialiteInfirmier>(entity =>
        {
            entity.HasKey(e => e.IdSpecialite);
            entity.ToTable("specialite_infirmier");

            entity.Property(e => e.IdSpecialite).HasColumnName("id_specialite").ValueGeneratedOnAdd();
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(20);
            entity.Property(e => e.Nom).HasColumnName("nom").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(e => e.Actif).HasColumnName("actif").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        // Infirmier Configuration
        // Note: Le Major est maintenant identifié via Service.IdMajor, pas via Infirmier.IsMajor/IdServiceMajor
        modelBuilder.Entity<Infirmier>(entity =>
        {
            entity.HasKey(e => e.IdUser);
            entity.ToTable("infirmier");

            entity.Property(e => e.IdUser).HasColumnName("id_user");
            entity.Property(e => e.Matricule).HasColumnName("matricule").HasMaxLength(50);
            entity.Property(e => e.Statut).HasColumnName("statut").HasMaxLength(20).HasDefaultValue("actif");
            entity.Property(e => e.IdService).HasColumnName("id_service");
            entity.Property(e => e.DateNominationMajor).HasColumnName("date_nomination_major");
            entity.Property(e => e.Accreditations).HasColumnName("accreditations").HasMaxLength(500);
            entity.Property(e => e.IdSpecialite).HasColumnName("id_specialite");

            // Relation vers le service de rattachement
            entity.HasOne(e => e.Service)
                .WithMany(s => s.Infirmiers)
                .HasForeignKey(e => e.IdService)
                .OnDelete(DeleteBehavior.Restrict);

            // Relation vers la spécialité infirmier
            entity.HasOne(e => e.Specialite)
                .WithMany(s => s.Infirmiers)
                .HasForeignKey(e => e.IdSpecialite)
                .OnDelete(DeleteBehavior.SetNull);
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
            entity.HasKey(e => e.IdUser);
            entity.ToTable("pharmacien");
            entity.Property(e => e.IdUser).HasColumnName("id_user");
            entity.Ignore(e => e.IdPharmacien); // Ignorer cette propriété qui n'existe pas dans la table
            entity.Property(e => e.Matricule).HasColumnName("matricule").HasMaxLength(50);
            entity.Property(e => e.NumeroOrdre).HasColumnName("numero_ordre").HasMaxLength(50);
            entity.Property(e => e.DateEmbauche).HasColumnName("date_embauche");
            entity.Property(e => e.Actif).HasColumnName("actif");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(e => e.Utilisateur).WithMany().HasForeignKey(e => e.IdUser);
        });

        // Laborantin Configuration (remplace Biologiste)
        modelBuilder.Entity<Laborantin>(entity =>
        {
            entity.HasKey(e => e.IdUser);
            entity.ToTable("laborantin");
            entity.Property(e => e.IdUser).HasColumnName("id_user");
            entity.Property(e => e.Matricule).HasColumnName("matricule").HasMaxLength(50);
            entity.Property(e => e.Specialisation).HasColumnName("specialisation").HasMaxLength(100);
            entity.Property(e => e.IdLabo).HasColumnName("id_labo");
            entity.Property(e => e.DateEmbauche).HasColumnName("date_embauche");
            entity.Property(e => e.Actif).HasColumnName("actif");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(e => e.Utilisateur).WithMany().HasForeignKey(e => e.IdUser);
            entity.HasOne(e => e.Laboratoire).WithMany().HasForeignKey(e => e.IdLabo);
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
            entity.Property(e => e.MontantPatient).HasColumnName("montant_patient").HasColumnType("decimal(12,2)");
            entity.Property(e => e.DateEnvoiAssurance).HasColumnName("date_envoi_assurance");

            entity.HasIndex(e => e.NumeroFacture).IsUnique();

            entity.HasOne(e => e.Patient).WithMany().HasForeignKey(e => e.IdPatient);
            entity.HasOne(e => e.Medecin).WithMany().HasForeignKey(e => e.IdMedecin);
            entity.HasOne(e => e.Service).WithMany().HasForeignKey(e => e.IdService);
            entity.HasOne(e => e.Specialite).WithMany().HasForeignKey(e => e.IdSpecialite);
            entity.HasOne(e => e.Consultation).WithMany().HasForeignKey(e => e.IdConsultation);
            entity.HasOne(e => e.Assurance).WithMany().HasForeignKey(e => e.IdAssurance);
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
            entity.Property(e => e.Montant).HasColumnName("total_ligne").HasColumnType("decimal(10,2)")
                .ValueGeneratedOnAddOrUpdate(); // Colonne générée automatiquement par MySQL
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

            entity.Property(e => e.Id).HasColumnName("id_question").ValueGeneratedOnAdd();
            entity.Property(e => e.TexteQuestion).HasColumnName("texte").IsRequired();
            entity.Property(e => e.TypeQuestion).HasColumnName("type").HasMaxLength(50);
            entity.Property(e => e.Categorie).HasColumnName("categorie").HasMaxLength(100);
            entity.Property(e => e.Ordre).HasColumnName("ordre");
            entity.Property(e => e.Obligatoire).HasColumnName("obligatoire");
            entity.Property(e => e.Actif).HasColumnName("actif");
        });

        modelBuilder.Entity<ConsultationQuestion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("consultation_question");

            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.ConsultationId).HasColumnName("id_consultation").IsRequired();
            entity.Property(e => e.QuestionId).HasColumnName("id_question").IsRequired();

            entity.HasIndex(e => new { e.ConsultationId, e.QuestionId })
                .IsUnique()
                .HasDatabaseName("UX_consultation_question");
            entity.HasIndex(e => e.QuestionId).HasDatabaseName("IX_consultation_question_question");

            entity.HasOne(e => e.Consultation)
                .WithMany(c => c.ConsultationQuestions)
                .HasForeignKey(e => e.ConsultationId);

            entity.HasOne(e => e.Question)
                .WithMany(q => q.ConsultationQuestions)
                .HasForeignKey(e => e.QuestionId);

            entity.HasMany(e => e.Reponses)
                .WithOne(r => r.ConsultationQuestion)
                .HasForeignKey(r => r.ConsultationQuestionId);
        });

        modelBuilder.Entity<Reponse>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("reponse");

            entity.Property(e => e.Id).HasColumnName("id_reponse").ValueGeneratedOnAdd();
            entity.Property(e => e.ConsultationQuestionId).HasColumnName("id_consultation_question").IsRequired();
            entity.Property(e => e.ValeurReponse).HasColumnName("valeur");
            entity.Property(e => e.DateReponse).HasColumnName("date_reponse");

            entity.HasOne(e => e.ConsultationQuestion)
                .WithMany(cq => cq.Reponses)
                .HasForeignKey(e => e.ConsultationQuestionId);
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

        modelBuilder.Entity<ConsultationGynecologique>(entity =>
        {
            entity.HasKey(e => e.IdConsultation);
            entity.ToTable("consultation_gyneco");

            entity.Property(e => e.IdConsultation).HasColumnName("id_consultation");
            entity.Property(e => e.InspectionExterne).HasColumnName("inspection_externe");
            entity.Property(e => e.ExamenSpeculum).HasColumnName("examen_speculum");
            entity.Property(e => e.ToucherVaginal).HasColumnName("toucher_vaginal");
            entity.Property(e => e.AutresObservations).HasColumnName("autres_observations");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.Consultation)
                .WithOne(c => c.ConsultationGynecologique)
                .HasForeignKey<ConsultationGynecologique>(e => e.IdConsultation)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.IdConsultation)
                .IsUnique()
                .HasDatabaseName("IX_consultation_gyneco_consultation");
        });

        // ConsultationChirurgicale Configuration
        modelBuilder.Entity<ConsultationChirurgicale>(entity =>
        {
            entity.HasKey(e => e.IdConsultation);
            entity.ToTable("consultation_chirurgicale");

            entity.Property(e => e.IdConsultation).HasColumnName("id_consultation");
            entity.Property(e => e.ZoneExaminee).HasColumnName("zone_examinee");
            entity.Property(e => e.InspectionLocale).HasColumnName("inspection_locale");
            entity.Property(e => e.PalpationLocale).HasColumnName("palpation_locale");
            entity.Property(e => e.SignesInflammatoires).HasColumnName("signes_inflammatoires");
            entity.Property(e => e.CicatricesExistantes).HasColumnName("cicatrices_existantes");
            entity.Property(e => e.MobiliteFonction).HasColumnName("mobilite_fonction");
            entity.Property(e => e.ConclusionChirurgicale).HasColumnName("conclusion_chirurgicale");
            entity.Property(e => e.Decision).HasColumnName("decision");
            entity.Property(e => e.NotesComplementaires).HasColumnName("notes_complementaires");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.Consultation)
                .WithOne(c => c.ConsultationChirurgicale)
                .HasForeignKey<ConsultationChirurgicale>(e => e.IdConsultation)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.IdConsultation)
                .IsUnique()
                .HasDatabaseName("IX_consultation_chirurgicale_consultation");
        });

        // ProgrammationIntervention Configuration
        modelBuilder.Entity<ProgrammationIntervention>(entity =>
        {
            entity.HasKey(e => e.IdProgrammation);
            entity.ToTable("programmation_intervention");

            entity.Property(e => e.IdProgrammation).HasColumnName("id_programmation").ValueGeneratedOnAdd();
            entity.Property(e => e.IdConsultation).HasColumnName("id_consultation").IsRequired();
            entity.Property(e => e.IdPatient).HasColumnName("id_patient").IsRequired();
            entity.Property(e => e.IdChirurgien).HasColumnName("id_medecin").IsRequired();
            entity.Property(e => e.TypeIntervention).HasColumnName("type_intervention").HasMaxLength(50);
            entity.Property(e => e.ClassificationAsa).HasColumnName("classification_asa").HasMaxLength(10);
            entity.Property(e => e.RisqueOperatoire).HasColumnName("risque_operatoire").HasMaxLength(20);
            entity.Property(e => e.ConsentementEclaire).HasColumnName("consentement_eclaire");
            entity.Property(e => e.DateConsentement).HasColumnName("date_consentement");
            entity.Property(e => e.IndicationOperatoire).HasColumnName("indication_operatoire").HasColumnType("text");
            entity.Property(e => e.TechniquePrevue).HasColumnName("technique_prevue").HasColumnType("text");
            entity.Property(e => e.DatePrevue).HasColumnName("date_prevue");
            entity.Property(e => e.DureeEstimee).HasColumnName("duree_estimee");
            entity.Property(e => e.NotesAnesthesie).HasColumnName("notes_anesthesie").HasColumnType("text");
            entity.Property(e => e.BilanPreoperatoire).HasColumnName("bilan_preoperatoire").HasColumnType("text");
            entity.Property(e => e.InstructionsPatient).HasColumnName("instructions_patient").HasColumnType("text");
            entity.Property(e => e.Statut).HasColumnName("statut").HasMaxLength(20);
            entity.Property(e => e.MotifAnnulation).HasColumnName("motif_annulation").HasColumnType("text");
            entity.Property(e => e.Notes).HasColumnName("notes").HasColumnType("text");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            // Relations
            entity.HasOne(e => e.Consultation)
                .WithMany()
                .HasForeignKey(e => e.IdConsultation)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.IdPatient)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Chirurgien)
                .WithMany()
                .HasForeignKey(e => e.IdChirurgien)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.IdConsultation).HasDatabaseName("IX_programmation_intervention_consultation");
            entity.HasIndex(e => e.IdPatient).HasDatabaseName("IX_programmation_intervention_patient");
            entity.HasIndex(e => e.IdChirurgien).HasDatabaseName("IX_programmation_intervention_chirurgien");
            entity.HasIndex(e => e.Statut).HasDatabaseName("IX_programmation_intervention_statut");
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

        // PrescriptionMedicament Configuration
        modelBuilder.Entity<PrescriptionMedicament>(entity =>
        {
            entity.HasKey(e => e.IdPrescriptionMed);
            entity.ToTable("ordonnance_medicament");

            entity.Property(e => e.IdPrescriptionMed).HasColumnName("id_prescription_med").ValueGeneratedOnAdd();
            entity.Property(e => e.IdOrdonnance).HasColumnName("id_ordonnance");
            entity.Property(e => e.IdMedicament).HasColumnName("id_medicament");
            entity.Property(e => e.NomMedicamentLibre).HasColumnName("nom_medicament_libre");
            entity.Property(e => e.DosageLibre).HasColumnName("dosage_libre");
            entity.Property(e => e.EstHorsCatalogue).HasColumnName("est_hors_catalogue");
            entity.Property(e => e.Quantite).HasColumnName("quantite");
            entity.Property(e => e.DureeTraitement).HasColumnName("duree_traitement");
            entity.Property(e => e.Posologie).HasColumnName("posologie");
            entity.Property(e => e.Frequence).HasColumnName("frequence");
            entity.Property(e => e.VoieAdministration).HasColumnName("voie_administration");
            entity.Property(e => e.FormePharmaceutique).HasColumnName("forme_pharmaceutique");
            entity.Property(e => e.Instructions).HasColumnName("instructions");

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
            entity.ToTable("ordonnance");

            entity.Property(e => e.IdOrdonnance).HasColumnName("id_ordonnance").ValueGeneratedOnAdd();
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.IdConsultation).HasColumnName("id_consultation");
            entity.Property(e => e.IdPatient).HasColumnName("id_patient");
            entity.Property(e => e.IdMedecin).HasColumnName("id_medecin");
            entity.Property(e => e.IdHospitalisation).HasColumnName("id_hospitalisation");
            entity.Property(e => e.TypeContexte).HasColumnName("type_contexte");
            entity.Property(e => e.Statut).HasColumnName("statut");
            entity.Property(e => e.Commentaire).HasColumnName("commentaire");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.DateExpiration).HasColumnName("date_expiration");
            entity.Property(e => e.Renouvelable).HasColumnName("renouvelable");
            entity.Property(e => e.NombreRenouvellements).HasColumnName("nombre_renouvellements");
            entity.Property(e => e.RenouvellementRestants).HasColumnName("renouvellements_restants");
            entity.Property(e => e.IdOrdonnanceOriginale).HasColumnName("id_ordonnance_originale");

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
            entity.Property(e => e.IdHospitalisation).HasColumnName("id_hospitalisation");
            entity.Property(e => e.Instructions).HasColumnName("instructions");
            entity.Property(e => e.IdExamen).HasColumnName("id_exam");
            entity.Property(e => e.Urgence).HasColumnName("urgence");

            entity.HasOne(e => e.Consultation)
                .WithMany(c => c.BulletinsExamen)
                .HasForeignKey(e => e.IdConsultation);

            entity.HasOne(e => e.Hospitalisation)
                .WithMany()
                .HasForeignKey(e => e.IdHospitalisation);

            entity.HasOne(e => e.Examen)
                .WithMany()
                .HasForeignKey(e => e.IdExamen);
        });

        // SoinHospitalisation Configuration
        modelBuilder.Entity<SoinHospitalisation>(entity =>
        {
            entity.HasKey(e => e.IdSoin);
            entity.ToTable("soin_hospitalisation");

            entity.Property(e => e.IdSoin).HasColumnName("id_soin").ValueGeneratedOnAdd();
            entity.Property(e => e.IdHospitalisation).HasColumnName("id_hospitalisation");
            entity.Property(e => e.TypeSoin).HasColumnName("type_soin");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Frequence).HasColumnName("frequence");
            entity.Property(e => e.DureeJours).HasColumnName("duree_jours");
            entity.Property(e => e.Moments).HasColumnName("moments");
            entity.Property(e => e.Priorite).HasColumnName("priorite");
            entity.Property(e => e.Instructions).HasColumnName("instructions");
            entity.Property(e => e.Statut).HasColumnName("statut");
            entity.Property(e => e.DatePrescription).HasColumnName("date_prescription");
            entity.Property(e => e.DateDebut).HasColumnName("date_debut");
            entity.Property(e => e.DateFinPrevue).HasColumnName("date_fin_prevue");
            entity.Property(e => e.IdPrescripteur).HasColumnName("id_prescripteur");
            entity.Property(e => e.NbExecutionsPrevues).HasColumnName("nb_executions_prevues");
            entity.Property(e => e.NbExecutionsEffectuees).HasColumnName("nb_executions_effectuees");

            entity.HasOne(e => e.Hospitalisation)
                .WithMany(h => h.Soins)
                .HasForeignKey(e => e.IdHospitalisation);

            entity.HasOne(e => e.Prescripteur)
                .WithMany()
                .HasForeignKey(e => e.IdPrescripteur);
        });

        // ==================== HIÉRARCHIE EXAMENS (3 niveaux) ====================

        // CategorieExamen Configuration (Niveau 1)
        modelBuilder.Entity<CategorieExamen>(entity =>
        {
            entity.HasKey(e => e.IdCategorie);
            entity.ToTable("categories_examens");

            entity.Property(e => e.IdCategorie).HasColumnName("id_categorie").ValueGeneratedOnAdd();
            entity.Property(e => e.Nom).HasColumnName("nom");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Icone).HasColumnName("icone");
            entity.Property(e => e.OrdreAffichage).HasColumnName("ordre_affichage");
            entity.Property(e => e.Actif).HasColumnName("actif");

            entity.HasMany(c => c.Specialites)
                .WithOne(s => s.Categorie)
                .HasForeignKey(s => s.IdCategorie);
        });

        // SpecialiteExamen Configuration (Niveau 2)
        modelBuilder.Entity<SpecialiteExamen>(entity =>
        {
            entity.HasKey(e => e.IdSpecialite);
            entity.ToTable("specialites_examens");

            entity.Property(e => e.IdSpecialite).HasColumnName("id_specialite").ValueGeneratedOnAdd();
            entity.Property(e => e.IdCategorie).HasColumnName("id_categorie");
            entity.Property(e => e.Nom).HasColumnName("nom");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Icone).HasColumnName("icone");
            entity.Property(e => e.OrdreAffichage).HasColumnName("ordre_affichage");
            entity.Property(e => e.Actif).HasColumnName("actif");

            entity.HasMany(s => s.Examens)
                .WithOne(e => e.Specialite)
                .HasForeignKey(e => e.IdSpecialite);
        });

        // ExamenCatalogue Configuration (Niveau 3)
        modelBuilder.Entity<ExamenCatalogue>(entity =>
        {
            entity.HasKey(e => e.IdExamen);
            entity.ToTable("examens");

            entity.Property(e => e.IdExamen).HasColumnName("id_exam").ValueGeneratedOnAdd();
            entity.Property(e => e.IdSpecialite).HasColumnName("id_specialite");
            entity.Property(e => e.NomExamen).HasColumnName("nom_exam");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.PrixUnitaire).HasColumnName("prix_unitaire");
            entity.Property(e => e.DureeEstimeeMinutes).HasColumnName("duree_estimee_minutes");
            entity.Property(e => e.PreparationRequise).HasColumnName("preparation_requise");
            entity.Property(e => e.Disponible).HasColumnName("disponible");
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
            entity.Property(e => e.IdLit).HasColumnName("id_lit"); // Nullable - pas de lit pour EN_ATTENTE
            entity.Property(e => e.IdMedecin).HasColumnName("id_medecin"); // Nullable
            entity.Property(e => e.IdService).HasColumnName("id_service"); // Nullable
            entity.Property(e => e.IdConsultation).HasColumnName("id_consultation"); // Nullable
            entity.Property(e => e.Urgence).HasColumnName("urgence").HasMaxLength(20);
            entity.Property(e => e.DiagnosticPrincipal).HasColumnName("diagnostic_principal");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.IdPatient).HasDatabaseName("IX_hospitalisation_patient");
            entity.HasIndex(e => e.IdLit).HasDatabaseName("IX_hospitalisation_lit");
            entity.HasIndex(e => e.Statut).HasDatabaseName("IX_hospitalisation_statut");
            entity.HasIndex(e => e.IdService).HasDatabaseName("IX_hospitalisation_service");

            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.IdPatient);

            entity.HasOne(e => e.Lit)
                .WithMany(l => l.Hospitalisations)
                .HasForeignKey(e => e.IdLit)
                .IsRequired(false); // Relation optionnelle

            entity.HasOne(e => e.Medecin)
                .WithMany()
                .HasForeignKey(e => e.IdMedecin)
                .IsRequired(false); // Relation optionnelle

            entity.HasOne(e => e.Service)
                .WithMany()
                .HasForeignKey(e => e.IdService)
                .IsRequired(false); // Relation optionnelle

            entity.HasOne(e => e.Consultation)
                .WithMany()
                .HasForeignKey(e => e.IdConsultation)
                .IsRequired(false); // Relation optionnelle
        });

        // Permission Configuration (RBAC)
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.IdPermission);
            entity.ToTable("permissions");

            entity.Property(e => e.IdPermission).HasColumnName("id_permission").ValueGeneratedOnAdd();
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Nom).HasColumnName("nom").HasMaxLength(150).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(e => e.Module).HasColumnName("module").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Actif).HasColumnName("actif").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("UK_permission_code");
            entity.HasIndex(e => e.Module).HasDatabaseName("IX_permission_module");
        });

        // RolePermission Configuration
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => e.IdRolePermission);
            entity.ToTable("role_permissions");

            entity.Property(e => e.IdRolePermission).HasColumnName("id_role_permission").ValueGeneratedOnAdd();
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(50).IsRequired();
            entity.Property(e => e.IdPermission).HasColumnName("id_permission").IsRequired();
            entity.Property(e => e.Actif).HasColumnName("actif").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => new { e.Role, e.IdPermission }).IsUnique().HasDatabaseName("UK_role_permission");
            entity.HasIndex(e => e.Role).HasDatabaseName("IX_role_permission_role");

            entity.HasOne(e => e.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(e => e.IdPermission)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserPermission Configuration
        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(e => e.IdUserPermission);
            entity.ToTable("user_permissions");

            entity.Property(e => e.IdUserPermission).HasColumnName("id_user_permission").ValueGeneratedOnAdd();
            entity.Property(e => e.IdUser).HasColumnName("id_user").IsRequired();
            entity.Property(e => e.IdPermission).HasColumnName("id_permission").IsRequired();
            entity.Property(e => e.Granted).HasColumnName("granted").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => new { e.IdUser, e.IdPermission }).IsUnique().HasDatabaseName("UK_user_permission");

            entity.HasOne(e => e.Utilisateur)
                .WithMany()
                .HasForeignKey(e => e.IdUser)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Permission)
                .WithMany()
                .HasForeignKey(e => e.IdPermission)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ==================== FACTURATION AVANCÉE ====================

        // Echeancier Configuration
        modelBuilder.Entity<Echeancier>(entity =>
        {
            entity.HasKey(e => e.IdEcheancier);
            entity.ToTable("echeancier");

            entity.Property(e => e.IdEcheancier).HasColumnName("id_echeancier").ValueGeneratedOnAdd();
            entity.Property(e => e.IdFacture).HasColumnName("id_facture").IsRequired();
            entity.Property(e => e.MontantTotal).HasColumnName("montant_total").HasColumnType("decimal(12,2)");
            entity.Property(e => e.NombreEcheances).HasColumnName("nombre_echeances");
            entity.Property(e => e.MontantParEcheance).HasColumnName("montant_par_echeance").HasColumnType("decimal(12,2)");
            entity.Property(e => e.DateDebut).HasColumnName("date_debut");
            entity.Property(e => e.Frequence).HasColumnName("frequence").HasMaxLength(20);
            entity.Property(e => e.Statut).HasColumnName("statut").HasMaxLength(50);
            entity.Property(e => e.DateCreation).HasColumnName("date_creation");
            entity.Property(e => e.CreePar).HasColumnName("cree_par");

            entity.HasOne(e => e.Facture).WithMany().HasForeignKey(e => e.IdFacture);
            entity.HasMany(e => e.Echeances).WithOne(e => e.Echeancier).HasForeignKey(e => e.IdEcheancier);
        });

        // Echeance Configuration
        modelBuilder.Entity<Echeance>(entity =>
        {
            entity.HasKey(e => e.IdEcheance);
            entity.ToTable("echeance");

            entity.Property(e => e.IdEcheance).HasColumnName("id_echeance").ValueGeneratedOnAdd();
            entity.Property(e => e.IdEcheancier).HasColumnName("id_echeancier").IsRequired();
            entity.Property(e => e.NumeroEcheance).HasColumnName("numero");
            entity.Property(e => e.Montant).HasColumnName("montant").HasColumnType("decimal(12,2)");
            entity.Property(e => e.DateEcheance).HasColumnName("date_echeance");
            entity.Property(e => e.DatePaiement).HasColumnName("date_paiement");
            entity.Property(e => e.Statut).HasColumnName("statut").HasMaxLength(50);
            entity.Property(e => e.IdTransaction).HasColumnName("id_transaction");
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(500);

            entity.HasOne(e => e.Transaction).WithMany().HasForeignKey(e => e.IdTransaction);
        });

        // DemandeRemboursement Configuration
        modelBuilder.Entity<DemandeRemboursement>(entity =>
        {
            entity.HasKey(e => e.IdDemande);
            entity.ToTable("demande_remboursement");

            entity.Property(e => e.IdDemande).HasColumnName("id_demande").ValueGeneratedOnAdd();
            entity.Property(e => e.NumeroDemande).HasColumnName("numero_demande").HasMaxLength(50);
            entity.Property(e => e.IdFacture).HasColumnName("id_facture").IsRequired();
            entity.Property(e => e.IdAssurance).HasColumnName("id_assurance").IsRequired();
            entity.Property(e => e.IdPatient).HasColumnName("id_patient").IsRequired();
            entity.Property(e => e.MontantDemande).HasColumnName("montant_demande").HasColumnType("decimal(12,2)");
            entity.Property(e => e.MontantApprouve).HasColumnName("montant_approuve").HasColumnType("decimal(12,2)");
            entity.Property(e => e.Statut).HasColumnName("statut").HasMaxLength(50);
            entity.Property(e => e.DateDemande).HasColumnName("date_demande");
            entity.Property(e => e.DateTraitement).HasColumnName("date_traitement");
            entity.Property(e => e.MotifRejet).HasColumnName("motif_rejet").HasMaxLength(500);
            entity.Property(e => e.ReferenceAssurance).HasColumnName("reference").HasMaxLength(100);
            entity.Property(e => e.Justificatif).HasColumnName("justificatif").HasMaxLength(500);
            entity.Property(e => e.TraitePar).HasColumnName("traite_par");

            entity.HasOne(e => e.Facture).WithMany().HasForeignKey(e => e.IdFacture);
            entity.HasOne(e => e.Assurance).WithMany().HasForeignKey(e => e.IdAssurance);
            entity.HasOne(e => e.Patient).WithMany().HasForeignKey(e => e.IdPatient);
        });

        // ==================== ASSURANCE COUVERTURE ====================

        modelBuilder.Entity<AssuranceCouverture>(entity =>
        {
            entity.HasKey(e => e.IdCouverture);
            entity.ToTable("assurance_couverture");

            entity.Property(e => e.IdCouverture).HasColumnName("id_couverture").ValueGeneratedOnAdd();
            entity.Property(e => e.IdAssurance).HasColumnName("id_assurance").IsRequired();
            entity.Property(e => e.TypePrestation).HasColumnName("type_prestation").HasMaxLength(50).IsRequired();
            entity.Property(e => e.TauxCouverture).HasColumnName("taux_couverture").HasColumnType("decimal(5,2)");
            entity.Property(e => e.PlafondAnnuel).HasColumnName("plafond_annuel").HasColumnType("decimal(12,2)");
            entity.Property(e => e.PlafondParActe).HasColumnName("plafond_par_acte").HasColumnType("decimal(12,2)");
            entity.Property(e => e.Franchise).HasColumnName("franchise").HasColumnType("decimal(12,2)");
            entity.Property(e => e.Actif).HasColumnName("actif");
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.Assurance)
                .WithMany(a => a.Couvertures)
                .HasForeignKey(e => e.IdAssurance);

            // Index unique: une seule couverture par type de prestation par assurance
            entity.HasIndex(e => new { e.IdAssurance, e.TypePrestation }).IsUnique();
        });

        // DocumentMedical Configuration - Force UUID as string to avoid Guid conversion
        modelBuilder.Entity<DocumentMedical>(entity =>
        {
            // Force Uuid to be treated as string, not Guid
            entity.Property(e => e.Uuid)
                .HasConversion<string>();
        });

        // ==================== TABLES DE RÉFÉRENCE ASSURANCE ====================

        // TypePrestation (clé primaire = code string)
        modelBuilder.Entity<TypePrestation>(entity =>
        {
            entity.HasKey(e => e.Code);
            entity.ToTable("type_prestation");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50);
            entity.Property(e => e.Libelle).HasColumnName("libelle").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
            entity.Property(e => e.Icone).HasColumnName("icone").HasMaxLength(50);
            entity.Property(e => e.Ordre).HasColumnName("ordre");
            entity.Property(e => e.Actif).HasColumnName("actif");
        });

        // CategorieBeneficiaire
        modelBuilder.Entity<CategorieBeneficiaire>(entity =>
        {
            entity.HasKey(e => e.IdCategorie);
            entity.ToTable("categorie_beneficiaire");
            entity.Property(e => e.IdCategorie).HasColumnName("id_categorie").ValueGeneratedOnAdd();
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Libelle).HasColumnName("libelle").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
            entity.Property(e => e.Actif).HasColumnName("actif");
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // ModePaiement
        modelBuilder.Entity<ModePaiement>(entity =>
        {
            entity.HasKey(e => e.IdMode);
            entity.ToTable("mode_paiement");
            entity.Property(e => e.IdMode).HasColumnName("id_mode").ValueGeneratedOnAdd();
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Libelle).HasColumnName("libelle").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
            entity.Property(e => e.Actif).HasColumnName("actif");
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // ZoneCouverture
        modelBuilder.Entity<ZoneCouverture>(entity =>
        {
            entity.HasKey(e => e.IdZone);
            entity.ToTable("zone_couverture");
            entity.Property(e => e.IdZone).HasColumnName("id_zone").ValueGeneratedOnAdd();
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Libelle).HasColumnName("libelle").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
            entity.Property(e => e.Actif).HasColumnName("actif");
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // TypeCouvertureSante
        modelBuilder.Entity<TypeCouvertureSante>(entity =>
        {
            entity.HasKey(e => e.IdTypeCouverture);
            entity.ToTable("type_couverture_sante");
            entity.Property(e => e.IdTypeCouverture).HasColumnName("id_type_couverture").ValueGeneratedOnAdd();
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Libelle).HasColumnName("libelle").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
            entity.Property(e => e.Actif).HasColumnName("actif");
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // ==================== RELATIONS MANY-TO-MANY ASSURANCE ====================

        // Assurance <-> TypeCouvertureSante (many-to-many)
        modelBuilder.Entity<Assurance>()
            .HasMany(a => a.TypesCouvertureSante)
            .WithMany(t => t.Assurances)
            .UsingEntity<Dictionary<string, object>>(
                "assurance_type_couverture",
                j => j.HasOne<TypeCouvertureSante>().WithMany().HasForeignKey("id_type_couverture"),
                j => j.HasOne<Assurance>().WithMany().HasForeignKey("id_assurance"),
                j =>
                {
                    j.HasKey("id_assurance", "id_type_couverture");
                    j.ToTable("assurance_type_couverture");
                });

        // Assurance <-> CategorieBeneficiaire (many-to-many)
        modelBuilder.Entity<Assurance>()
            .HasMany(a => a.CategoriesBeneficiaires)
            .WithMany(c => c.Assurances)
            .UsingEntity<Dictionary<string, object>>(
                "assurance_categorie_beneficiaire",
                j => j.HasOne<CategorieBeneficiaire>().WithMany().HasForeignKey("id_categorie"),
                j => j.HasOne<Assurance>().WithMany().HasForeignKey("id_assurance"),
                j =>
                {
                    j.HasKey("id_assurance", "id_categorie");
                    j.ToTable("assurance_categorie_beneficiaire");
                });

        // Assurance <-> ModePaiement (many-to-many)
        modelBuilder.Entity<Assurance>()
            .HasMany(a => a.ModesPaiement)
            .WithMany(m => m.Assurances)
            .UsingEntity<Dictionary<string, object>>(
                "assurance_mode_paiement",
                j => j.HasOne<ModePaiement>().WithMany().HasForeignKey("id_mode"),
                j => j.HasOne<Assurance>().WithMany().HasForeignKey("id_assurance"),
                j =>
                {
                    j.HasKey("id_assurance", "id_mode");
                    j.ToTable("assurance_mode_paiement");
                });

        // Assurance -> ZoneCouverture (FK)
        modelBuilder.Entity<Assurance>()
            .HasOne(a => a.Zone)
            .WithMany(z => z.Assurances)
            .HasForeignKey(a => a.IdZoneCouverture)
            .OnDelete(DeleteBehavior.SetNull);

        // ==================== ORIENTATION PRE-CONSULTATION ====================
        modelBuilder.Entity<OrientationPreConsultation>(entity =>
        {
            entity.HasKey(e => e.IdOrientation);
            entity.ToTable("orientation_pre_consultation");

            entity.Property(e => e.IdOrientation).HasColumnName("id_orientation").ValueGeneratedOnAdd();
            entity.Property(e => e.IdConsultation).HasColumnName("id_consultation").IsRequired();
            entity.Property(e => e.IdPatient).HasColumnName("id_patient").IsRequired();
            entity.Property(e => e.IdMedecinPrescripteur).HasColumnName("id_medecin_prescripteur").IsRequired();
            entity.Property(e => e.TypeOrientation).HasColumnName("type_orientation").HasMaxLength(30).IsRequired();
            entity.Property(e => e.IdSpecialite).HasColumnName("id_specialite");
            entity.Property(e => e.IdMedecinOriente).HasColumnName("id_medecin_oriente");
            entity.Property(e => e.NomDestinataire).HasColumnName("nom_destinataire").HasMaxLength(255);
            entity.Property(e => e.SpecialiteTexte).HasColumnName("specialite_texte").HasMaxLength(100);
            entity.Property(e => e.AdresseDestinataire).HasColumnName("adresse_destinataire");
            entity.Property(e => e.TelephoneDestinataire).HasColumnName("telephone_destinataire").HasMaxLength(20);
            entity.Property(e => e.Motif).HasColumnName("motif").IsRequired();
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.Urgence).HasColumnName("urgence");
            entity.Property(e => e.Prioritaire).HasColumnName("prioritaire");
            entity.Property(e => e.Statut).HasColumnName("statut").HasMaxLength(30);
            entity.Property(e => e.DateOrientation).HasColumnName("date_orientation");
            entity.Property(e => e.DateRdvPropose).HasColumnName("date_rdv_propose");
            entity.Property(e => e.IdRdvCree).HasColumnName("id_rdv_cree");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            // Relations
            entity.HasOne(e => e.Consultation)
                .WithMany(c => c.OrientationsPreConsultation)
                .HasForeignKey(e => e.IdConsultation)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.IdPatient)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.MedecinPrescripteur)
                .WithMany()
                .HasForeignKey(e => e.IdMedecinPrescripteur)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Specialite)
                .WithMany()
                .HasForeignKey(e => e.IdSpecialite)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.MedecinOriente)
                .WithMany()
                .HasForeignKey(e => e.IdMedecinOriente)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.RendezVousCree)
                .WithMany()
                .HasForeignKey(e => e.IdRdvCree)
                .OnDelete(DeleteBehavior.SetNull);

            // Index
            entity.HasIndex(e => e.IdConsultation);
            entity.HasIndex(e => e.IdPatient);
            entity.HasIndex(e => e.Statut);
        });

        // ==================== FORMES PHARMACEUTIQUES ====================
        modelBuilder.Entity<FormePharmaceutique>(entity =>
        {
            entity.HasKey(e => e.IdForme);
            entity.ToTable("forme_pharmaceutique");

            entity.Property(e => e.IdForme).HasColumnName("id_forme").ValueGeneratedOnAdd();
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Libelle).HasColumnName("libelle").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
            entity.Property(e => e.Icone).HasColumnName("icone").HasMaxLength(50);
            entity.Property(e => e.Ordre).HasColumnName("ordre");
            entity.Property(e => e.Actif).HasColumnName("actif");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.Code).IsUnique();
        });

        // ==================== VOIES D'ADMINISTRATION ====================
        modelBuilder.Entity<VoieAdministration>(entity =>
        {
            entity.HasKey(e => e.IdVoie);
            entity.ToTable("voie_administration");

            entity.Property(e => e.IdVoie).HasColumnName("id_voie").ValueGeneratedOnAdd();
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Libelle).HasColumnName("libelle").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
            entity.Property(e => e.Icone).HasColumnName("icone").HasMaxLength(50);
            entity.Property(e => e.Ordre).HasColumnName("ordre");
            entity.Property(e => e.Actif).HasColumnName("actif");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.Code).IsUnique();
        });

        // ==================== MEDICAMENT-FORME (Many-to-Many) ====================
        modelBuilder.Entity<MedicamentForme>(entity =>
        {
            entity.HasKey(e => new { e.IdMedicament, e.IdForme });
            entity.ToTable("medicament_forme");

            entity.Property(e => e.IdMedicament).HasColumnName("id_medicament");
            entity.Property(e => e.IdForme).HasColumnName("id_forme");
            entity.Property(e => e.EstDefaut).HasColumnName("est_defaut");

            entity.HasOne(e => e.Medicament)
                .WithMany(m => m.MedicamentFormes)
                .HasForeignKey(e => e.IdMedicament)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.FormePharmaceutique)
                .WithMany(f => f.MedicamentFormes)
                .HasForeignKey(e => e.IdForme)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ==================== MEDICAMENT-VOIE (Many-to-Many) ====================
        modelBuilder.Entity<MedicamentVoie>(entity =>
        {
            entity.HasKey(e => new { e.IdMedicament, e.IdVoie });
            entity.ToTable("medicament_voie");

            entity.Property(e => e.IdMedicament).HasColumnName("id_medicament");
            entity.Property(e => e.IdVoie).HasColumnName("id_voie");
            entity.Property(e => e.EstDefaut).HasColumnName("est_defaut");

            entity.HasOne(e => e.Medicament)
                .WithMany(m => m.MedicamentVoies)
                .HasForeignKey(e => e.IdMedicament)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.VoieAdministration)
                .WithMany(v => v.MedicamentVoies)
                .HasForeignKey(e => e.IdVoie)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ==================== BLOC OPERATOIRE ====================
        modelBuilder.Entity<BlocOperatoire>(entity =>
        {
            entity.HasKey(e => e.IdBloc);
            entity.ToTable("bloc_operatoire");

            entity.Property(e => e.IdBloc).HasColumnName("id_bloc").ValueGeneratedOnAdd();
            entity.Property(e => e.Nom).HasColumnName("nom").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(e => e.Statut).HasColumnName("statut").HasMaxLength(20).HasDefaultValue("libre");
            entity.Property(e => e.Actif).HasColumnName("actif").HasDefaultValue(true);
            entity.Property(e => e.Localisation).HasColumnName("localisation").HasMaxLength(100);
            entity.Property(e => e.Capacite).HasColumnName("capacite");
            entity.Property(e => e.Equipements).HasColumnName("equipements").HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasMany(e => e.Reservations)
                .WithOne(r => r.Bloc)
                .HasForeignKey(r => r.IdBloc)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ==================== RESERVATION BLOC ====================
        modelBuilder.Entity<ReservationBloc>(entity =>
        {
            entity.HasKey(e => e.IdReservation);
            entity.ToTable("reservation_bloc");

            entity.Property(e => e.IdReservation).HasColumnName("id_reservation").ValueGeneratedOnAdd();
            entity.Property(e => e.IdBloc).HasColumnName("id_bloc");
            entity.Property(e => e.IdProgrammation).HasColumnName("id_programmation");
            entity.Property(e => e.IdMedecin).HasColumnName("id_medecin");
            entity.Property(e => e.DateReservation).HasColumnName("date_reservation");
            entity.Property(e => e.HeureDebut).HasColumnName("heure_debut").IsRequired().HasMaxLength(5);
            entity.Property(e => e.HeureFin).HasColumnName("heure_fin").IsRequired().HasMaxLength(5);
            entity.Property(e => e.DureeMinutes).HasColumnName("duree_minutes");
            entity.Property(e => e.Statut).HasColumnName("statut").HasMaxLength(20).HasDefaultValue("confirmee");
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.Bloc)
                .WithMany(b => b.Reservations)
                .HasForeignKey(e => e.IdBloc)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Programmation)
                .WithOne(p => p.ReservationBloc)
                .HasForeignKey<ReservationBloc>(e => e.IdProgrammation)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Medecin)
                .WithMany()
                .HasForeignKey(e => e.IdMedecin)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.IdBloc, e.DateReservation, e.HeureDebut })
                .HasDatabaseName("IX_reservation_bloc_unique");
        });
    }
}
