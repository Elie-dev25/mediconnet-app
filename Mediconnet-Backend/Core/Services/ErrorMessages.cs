namespace Mediconnet_Backend.Core.Services;

/// <summary>
/// Messages d'erreur centralisés pour une meilleure cohérence et maintenabilité
/// </summary>
public static class ErrorMessages
{
    #region Authentification et Autorisation
    
    public const string NonAuthentifie = "Vous devez être connecté pour effectuer cette action";
    public const string NonAutorise = "Vous n'êtes pas autorisé à effectuer cette action";
    public const string SessionExpiree = "Votre session a expiré, veuillez vous reconnecter";
    public const string TokenInvalide = "Token d'authentification invalide";
    
    #endregion

    #region Ressources non trouvées
    
    public const string PatientNonTrouve = "Patient non trouvé";
    public const string MedecinNonTrouve = "Médecin non trouvé";
    public const string ConsultationNonTrouvee = "Consultation non trouvée";
    public const string RendezVousNonTrouve = "Rendez-vous non trouvé";
    public const string ExamenNonTrouve = "Examen non trouvé";
    public const string HospitalisationNonTrouvee = "Hospitalisation non trouvée";
    public const string OrdonnanceNonTrouvee = "Ordonnance non trouvée";
    public const string DocumentNonTrouve = "Document non trouvé";
    public const string LitNonTrouve = "Lit non trouvé";
    public const string ChambreNonTrouvee = "Chambre non trouvée";
    public const string MedicamentNonTrouve = "Médicament non trouvé";
    public const string FactureNonTrouvee = "Facture non trouvée";
    
    #endregion

    #region Validation des données
    
    public const string DonneesInvalides = "Les données fournies sont invalides";
    public const string ChampObligatoire = "Ce champ est obligatoire";
    public const string FormatInvalide = "Le format des données est invalide";
    public const string DateInvalide = "La date fournie est invalide";
    public const string EmailInvalide = "L'adresse email est invalide";
    public const string TelephoneInvalide = "Le numéro de téléphone est invalide";
    
    public static string ChampObligatoireNomme(string nomChamp) => $"Le champ '{nomChamp}' est obligatoire";
    public static string FormatInvalideNomme(string nomChamp) => $"Le format du champ '{nomChamp}' est invalide";
    public static string LongueurMaxDepassee(string nomChamp, int max) => $"Le champ '{nomChamp}' ne doit pas dépasser {max} caractères";
    public static string ValeurHorsLimites(string nomChamp, int min, int max) => $"La valeur de '{nomChamp}' doit être entre {min} et {max}";
    
    #endregion

    #region Opérations métier
    
    public const string OperationImpossible = "Cette opération ne peut pas être effectuée";
    public const string EtatIncoherent = "L'état actuel ne permet pas cette opération";
    
    // Consultation
    public const string ConsultationDejaTerminee = "Cette consultation est déjà terminée";
    public const string ConsultationDejaAnnulee = "Cette consultation a été annulée";
    public const string ConsultationNonDemarree = "La consultation n'a pas encore été démarrée";
    public const string PatientNonArrive = "Le patient n'est pas encore arrivé";
    
    // Examen
    public const string ExamenDejaTermine = "Cet examen est déjà terminé";
    public const string ExamenDejaAnnule = "Cet examen a été annulé";
    public const string ExamenNonDemarre = "L'examen n'a pas encore été démarré";
    public const string ResultatDejaEnregistre = "Le résultat de cet examen a déjà été enregistré";
    
    // Hospitalisation
    public const string HospitalisationDejaTerminee = "Cette hospitalisation est déjà terminée";
    public const string LitDejaOccupe = "Ce lit est déjà occupé";
    public const string LitNonDisponible = "Ce lit n'est pas disponible";
    public const string PatientDejaHospitalise = "Ce patient est déjà hospitalisé";
    
    // Rendez-vous
    public const string RendezVousDejaConfirme = "Ce rendez-vous est déjà confirmé";
    public const string RendezVousDejaAnnule = "Ce rendez-vous a été annulé";
    public const string RendezVousDejaTermine = "Ce rendez-vous est déjà terminé";
    public const string CreneauNonDisponible = "Ce créneau horaire n'est pas disponible";
    
    // Paiement
    public const string FactureNonPayee = "La facture n'a pas été payée";
    public const string FactureDejaPayee = "Cette facture a déjà été payée";
    public const string MontantInvalide = "Le montant est invalide";
    
    #endregion

    #region Fichiers et documents
    
    public const string FichierInvalide = "Le fichier est invalide";
    public const string FichierTropVolumineux = "Le fichier est trop volumineux";
    public const string TypeFichierNonAutorise = "Ce type de fichier n'est pas autorisé";
    public const string ErreurUpload = "Erreur lors de l'upload du fichier";
    public const string ErreurTelechargement = "Erreur lors du téléchargement du fichier";
    
    public static string FichierTropVolumineuxAvecLimite(long maxMo) => $"Le fichier ne doit pas dépasser {maxMo} Mo";
    public static string TypesAutorises(string types) => $"Types de fichiers autorisés: {types}";
    
    #endregion

    #region Erreurs système
    
    public const string ErreurServeur = "Une erreur serveur s'est produite. Veuillez réessayer plus tard";
    public const string ErreurBaseDonnees = "Erreur de connexion à la base de données";
    public const string ServiceIndisponible = "Le service est temporairement indisponible";
    public const string OperationTimeout = "L'opération a pris trop de temps";
    
    #endregion

    #region Helpers
    
    /// <summary>
    /// Génère un message d'erreur pour une ressource non trouvée
    /// </summary>
    public static string RessourceNonTrouvee(string typeRessource, object? id = null)
    {
        return id != null 
            ? $"{typeRessource} avec l'identifiant '{id}' non trouvé(e)"
            : $"{typeRessource} non trouvé(e)";
    }

    /// <summary>
    /// Génère un message d'erreur pour une action non autorisée
    /// </summary>
    public static string ActionNonAutorisee(string action, string? raison = null)
    {
        return raison != null
            ? $"Impossible de {action}: {raison}"
            : $"Vous n'êtes pas autorisé à {action}";
    }

    /// <summary>
    /// Génère un message d'erreur pour une transition de statut invalide
    /// </summary>
    public static string TransitionStatutInvalide(string entite, string? statutActuel, string? nouveauStatut)
    {
        return $"Impossible de passer le statut de {entite} de '{statutActuel ?? "inconnu"}' à '{nouveauStatut ?? "inconnu"}'";
    }

    #endregion
}
