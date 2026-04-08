using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities;

/// <summary>
/// Entité représentant la consultation pré-anesthésique (extension 1-1 de Consultation)
/// Contient toutes les données spécifiques à l'évaluation anesthésique
/// </summary>
[Table("consultation_anesthesique")]
public class ConsultationAnesthesique
{
    [Key]
    [Column("id_consultation")]
    public int IdConsultation { get; set; }

    // ==================== LIEN AVEC COORDINATION ====================
    
    /// <summary>ID de la coordination d'intervention liée (optionnel)</summary>
    [Column("id_coordination")]
    public int? IdCoordination { get; set; }

    // ==================== ANAMNÈSE SPÉCIFIQUE ANESTHÉSIE ====================

    /// <summary>Antécédents médicaux: HTA, diabète, asthme, etc. (JSON)</summary>
    [Column("antecedents_medicaux")]
    public string? AntecedentsMedicaux { get; set; }

    /// <summary>Problèmes cardiaques détaillés</summary>
    [Column("problemes_cardiaques")]
    public string? ProblemesCardiaques { get; set; }

    /// <summary>Problèmes respiratoires détaillés</summary>
    [Column("problemes_respiratoires")]
    public string? ProblemesRespiratoires { get; set; }

    /// <summary>Allergies connues (médicaments, latex, etc.)</summary>
    [Column("allergies_anesthesie")]
    public string? AllergiesAnesthesie { get; set; }

    /// <summary>Antécédents chirurgicaux et anesthésiques</summary>
    [Column("antecedents_chirurgicaux")]
    public string? AntecedentsChirurgicaux { get; set; }

    /// <summary>Problèmes lors d'anesthésies précédentes</summary>
    [Column("problemes_anesthesie_precedente")]
    public string? ProblemesAnesthesiePrecedente { get; set; }

    /// <summary>Médicaments en cours (anticoagulants, aspirine, etc.) (JSON)</summary>
    [Column("medicaments_en_cours")]
    public string? MedicamentsEnCours { get; set; }

    /// <summary>Symptômes: douleurs effort, essoufflement, toux chronique (JSON)</summary>
    [Column("symptomes")]
    public string? Symptomes { get; set; }

    /// <summary>Apnée du sommeil / ronflement</summary>
    [Column("apnee_sommeil")]
    public bool? ApneeSommeil { get; set; }

    /// <summary>Troubles de la coagulation / saignement</summary>
    [Column("troubles_coagulation")]
    public bool? TroublesCoagulation { get; set; }

    /// <summary>Détails troubles coagulation</summary>
    [Column("troubles_coagulation_details")]
    public string? TroublesCoagulationDetails { get; set; }

    // ==================== EXAMEN CLINIQUE ====================

    /// <summary>Poids en kg</summary>
    [Column("poids")]
    public decimal? Poids { get; set; }

    /// <summary>Taille en cm</summary>
    [Column("taille")]
    public decimal? Taille { get; set; }

    /// <summary>IMC calculé</summary>
    [Column("imc")]
    public decimal? IMC { get; set; }

    /// <summary>Tension artérielle systolique</summary>
    [Column("tension_systolique")]
    public int? TensionSystolique { get; set; }

    /// <summary>Tension artérielle diastolique</summary>
    [Column("tension_diastolique")]
    public int? TensionDiastolique { get; set; }

    /// <summary>Fréquence cardiaque</summary>
    [Column("frequence_cardiaque")]
    public int? FrequenceCardiaque { get; set; }

    /// <summary>Saturation en oxygène</summary>
    [Column("saturation_oxygene")]
    public decimal? SaturationOxygene { get; set; }

    /// <summary>Auscultation cardiaque</summary>
    [Column("auscultation_cardiaque")]
    public string? AuscultationCardiaque { get; set; }

    /// <summary>Auscultation pulmonaire</summary>
    [Column("auscultation_pulmonaire")]
    public string? AuscultationPulmonaire { get; set; }

    // ==================== VOIES AÉRIENNES (CRITIQUE) ====================

    /// <summary>Ouverture de la bouche (cm ou classification)</summary>
    [Column("ouverture_bouche")]
    public string? OuvertureBouche { get; set; }

    /// <summary>Score de Mallampati (1-4)</summary>
    [Column("mallampati")]
    public int? Mallampati { get; set; }

    /// <summary>État des dents (normal, prothèse, dents mobiles, etc.)</summary>
    [Column("etat_dents")]
    public string? EtatDents { get; set; }

    /// <summary>Mobilité du cou (normale, limitée, etc.)</summary>
    [Column("mobilite_cou")]
    public string? MobiliteCou { get; set; }

    /// <summary>Distance thyro-mentonnière (cm)</summary>
    [Column("distance_thyro_mentonniere")]
    public decimal? DistanceThyroMentonniere { get; set; }

    /// <summary>Prédiction d'intubation difficile</summary>
    [Column("intubation_difficile_prevue")]
    public bool? IntubationDifficilePrevue { get; set; }

    /// <summary>Notes sur les voies aériennes</summary>
    [Column("notes_voies_aeriennes")]
    public string? NotesVoiesAeriennes { get; set; }

    // ==================== ÉVALUATION DU RISQUE ====================

    /// <summary>Classification ASA (1-5)</summary>
    [Column("classification_asa")]
    public int? ClassificationASA { get; set; }

    /// <summary>Niveau de risque global: faible, moyen, eleve</summary>
    [Column("niveau_risque")]
    public string? NiveauRisque { get; set; }

    /// <summary>Risque cardiaque: faible, moyen, eleve</summary>
    [Column("risque_cardiaque")]
    public string? RisqueCardiaque { get; set; }

    /// <summary>Risque respiratoire: faible, moyen, eleve</summary>
    [Column("risque_respiratoire")]
    public string? RisqueRespiratoire { get; set; }

    /// <summary>Risque allergique: faible, moyen, eleve</summary>
    [Column("risque_allergique")]
    public string? RisqueAllergique { get; set; }

    /// <summary>Risque hémorragique: faible, moyen, eleve</summary>
    [Column("risque_hemorragique")]
    public string? RisqueHemorragique { get; set; }

    /// <summary>Notes sur l'évaluation des risques</summary>
    [Column("notes_risques")]
    public string? NotesRisques { get; set; }

    // ==================== CHOIX DU TYPE D'ANESTHÉSIE ====================

    /// <summary>Type d'anesthésie choisi: generale, locoregionale, locale, sedation</summary>
    [Column("type_anesthesie")]
    public string? TypeAnesthesie { get; set; }

    /// <summary>Sous-type si loco-régionale: rachianesthesie, peridurale, bloc_peripherique</summary>
    [Column("sous_type_anesthesie")]
    public string? SousTypeAnesthesie { get; set; }

    /// <summary>Justification du choix d'anesthésie</summary>
    [Column("justification_anesthesie")]
    public string? JustificationAnesthesie { get; set; }

    /// <summary>Explication donnée au patient</summary>
    [Column("explication_patient")]
    public string? ExplicationPatient { get; set; }

    /// <summary>Consentement éclairé obtenu</summary>
    [Column("consentement_obtenu")]
    public bool? ConsentementObtenu { get; set; }

    /// <summary>Date du consentement</summary>
    [Column("date_consentement")]
    public DateTime? DateConsentement { get; set; }

    // ==================== CONSIGNES PRÉOPÉRATOIRES ====================

    /// <summary>Durée de jeûne recommandée (heures)</summary>
    [Column("duree_jeune")]
    public int? DureeJeune { get; set; }

    /// <summary>Instructions de jeûne détaillées</summary>
    [Column("instructions_jeune")]
    public string? InstructionsJeune { get; set; }

    /// <summary>Médicaments à arrêter (JSON: [{nom, joursAvant}])</summary>
    [Column("medicaments_a_arreter")]
    public string? MedicamentsAArreter { get; set; }

    /// <summary>Médicaments à adapter (JSON)</summary>
    [Column("medicaments_a_adapter")]
    public string? MedicamentsAAdapter { get; set; }

    /// <summary>Médicaments à continuer (JSON)</summary>
    [Column("medicaments_a_continuer")]
    public string? MedicamentsAContinuer { get; set; }

    /// <summary>Arrêt du tabac recommandé</summary>
    [Column("arret_tabac")]
    public bool? ArretTabac { get; set; }

    /// <summary>Délai arrêt tabac (jours avant)</summary>
    [Column("delai_arret_tabac")]
    public int? DelaiArretTabac { get; set; }

    /// <summary>Instructions d'hygiène (douche antiseptique, etc.)</summary>
    [Column("instructions_hygiene")]
    public string? InstructionsHygiene { get; set; }

    /// <summary>Autres consignes préopératoires</summary>
    [Column("autres_consignes")]
    public string? AutresConsignes { get; set; }

    // ==================== CONCLUSION ====================

    /// <summary>Résumé de la consultation</summary>
    [Column("resume_consultation")]
    public string? ResumeConsultation { get; set; }

    /// <summary>Aptitude: apte, apte_avec_reserve, non_apte</summary>
    [Column("aptitude")]
    public string? Aptitude { get; set; }

    /// <summary>Réserves si apte avec réserve</summary>
    [Column("reserves")]
    public string? Reserves { get; set; }

    /// <summary>Motif si non apte</summary>
    [Column("motif_non_aptitude")]
    public string? MotifNonAptitude { get; set; }

    /// <summary>Recommandations finales</summary>
    [Column("recommandations")]
    public string? Recommandations { get; set; }

    /// <summary>Date prévue de l'intervention</summary>
    [Column("date_intervention_prevue")]
    public DateTime? DateInterventionPrevue { get; set; }

    // ==================== MÉTADONNÉES ====================

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // ==================== NAVIGATION ====================

    [ForeignKey(nameof(IdConsultation))]
    public virtual Consultation Consultation { get; set; } = null!;

    [ForeignKey(nameof(IdCoordination))]
    public virtual CoordinationIntervention? Coordination { get; set; }
}
