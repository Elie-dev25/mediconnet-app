/**
 * Messages d'erreur centralisés pour le frontend
 * Assure une cohérence dans l'affichage des erreurs à l'utilisateur
 */

export const ErrorMessages = {
  // Authentification
  AUTH: {
    NON_CONNECTE: 'Vous devez être connecté pour effectuer cette action',
    SESSION_EXPIREE: 'Votre session a expiré, veuillez vous reconnecter',
    NON_AUTORISE: 'Vous n\'êtes pas autorisé à effectuer cette action',
    IDENTIFIANTS_INVALIDES: 'Email ou mot de passe incorrect'
  },

  // Réseau et serveur
  RESEAU: {
    ERREUR_SERVEUR: 'Une erreur serveur s\'est produite. Veuillez réessayer plus tard',
    CONNEXION_IMPOSSIBLE: 'Impossible de se connecter au serveur',
    TIMEOUT: 'La requête a pris trop de temps. Veuillez réessayer',
    HORS_LIGNE: 'Vous êtes hors ligne. Vérifiez votre connexion internet'
  },

  // Ressources non trouvées
  NON_TROUVE: {
    PATIENT: 'Patient non trouvé',
    CONSULTATION: 'Consultation non trouvée',
    RENDEZ_VOUS: 'Rendez-vous non trouvé',
    EXAMEN: 'Examen non trouvé',
    ORDONNANCE: 'Ordonnance non trouvée',
    DOCUMENT: 'Document non trouvé',
    HOSPITALISATION: 'Hospitalisation non trouvée'
  },

  // Validation de formulaire
  VALIDATION: {
    CHAMP_OBLIGATOIRE: 'Ce champ est obligatoire',
    EMAIL_INVALIDE: 'Adresse email invalide',
    TELEPHONE_INVALIDE: 'Numéro de téléphone invalide',
    DATE_INVALIDE: 'Date invalide',
    FORMAT_INVALIDE: 'Format invalide',
    MOT_DE_PASSE_FAIBLE: 'Le mot de passe doit contenir au moins 8 caractères',
    MOTS_DE_PASSE_DIFFERENTS: 'Les mots de passe ne correspondent pas'
  },

  // Opérations métier
  OPERATION: {
    ECHEC: 'L\'opération a échoué',
    ANNULEE: 'L\'opération a été annulée',
    NON_AUTORISEE: 'Cette opération n\'est pas autorisée',
    DEJA_EFFECTUEE: 'Cette opération a déjà été effectuée'
  },

  // Fichiers
  FICHIER: {
    TROP_VOLUMINEUX: 'Le fichier est trop volumineux',
    TYPE_NON_AUTORISE: 'Ce type de fichier n\'est pas autorisé',
    UPLOAD_ECHEC: 'Échec de l\'upload du fichier',
    TELECHARGEMENT_ECHEC: 'Échec du téléchargement'
  },

  // Chargement de données
  CHARGEMENT: {
    DOSSIER_PATIENT: 'Impossible de charger le dossier patient',
    CONSULTATIONS: 'Impossible de charger les consultations',
    EXAMENS: 'Impossible de charger les examens',
    ORDONNANCES: 'Impossible de charger les ordonnances',
    LISTE: 'Impossible de charger la liste'
  }
} as const;

const HTTP_STATUS_MESSAGES: Record<number, string> = {
  400: 'Requête invalide',
  401: ErrorMessages.AUTH.NON_CONNECTE,
  403: ErrorMessages.AUTH.NON_AUTORISE,
  404: 'Ressource non trouvée',
  408: ErrorMessages.RESEAU.TIMEOUT,
  500: ErrorMessages.RESEAU.ERREUR_SERVEUR,
  503: 'Service temporairement indisponible'
};

function extractFromErrorBody(errorBody: any): string | null {
  if (typeof errorBody === 'string') return errorBody;
  if (errorBody.message) return errorBody.message;
  if (errorBody.errors) {
    const firstKey = Object.keys(errorBody.errors)[0];
    if (firstKey && Array.isArray(errorBody.errors[firstKey])) {
      return errorBody.errors[firstKey][0];
    }
  }
  return null;
}

/**
 * Extrait un message d'erreur lisible depuis une réponse HTTP
 */
export function extractErrorMessage(error: any, defaultMessage: string = ErrorMessages.RESEAU.ERREUR_SERVEUR): string {
  if (!error) return defaultMessage;

  if (error.error) {
    const bodyMessage = extractFromErrorBody(error.error);
    if (bodyMessage) return bodyMessage;
  }

  if (error.message) return error.message;

  if (error.status && HTTP_STATUS_MESSAGES[error.status]) {
    return HTTP_STATUS_MESSAGES[error.status];
  }

  return defaultMessage;
}

/**
 * Génère un message pour un champ obligatoire
 */
export function champObligatoire(nomChamp: string): string {
  return `Le champ "${nomChamp}" est obligatoire`;
}

/**
 * Génère un message pour une longueur maximale dépassée
 */
export function longueurMaxDepassee(nomChamp: string, max: number): string {
  return `Le champ "${nomChamp}" ne doit pas dépasser ${max} caractères`;
}

/**
 * Génère un message pour une valeur hors limites
 */
export function valeurHorsLimites(nomChamp: string, min: number, max: number): string {
  return `La valeur de "${nomChamp}" doit être entre ${min} et ${max}`;
}

/**
 * Génère un message pour un fichier trop volumineux
 */
export function fichierTropVolumineux(maxMo: number): string {
  return `Le fichier ne doit pas dépasser ${maxMo} Mo`;
}
