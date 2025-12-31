namespace Mediconnet_Backend.DTOs.Patient
{
    /// <summary>
    /// DTO pour la recherche de patients
    /// </summary>
    public class PatientSearchRequest
    {
        /// <summary>
        /// Terme de recherche (numéro dossier, nom, email)
        /// </summary>
        public string? SearchTerm { get; set; }
        
        /// <summary>
        /// Nombre maximum de résultats (par défaut 50, max 100)
        /// </summary>
        public int Limit { get; set; } = 50;
    }

    /// <summary>
    /// DTO pour les informations de base d'un patient
    /// </summary>
    public class PatientBasicInfoDto
    {
        public int IdUser { get; set; }
        public string NumeroDossier { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public DateTime? DateNaissance { get; set; }
        public string? Sexe { get; set; }
        public string? GroupeSanguin { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO pour la réponse de recherche de patients
    /// </summary>
    public class PatientSearchResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<PatientBasicInfoDto> Patients { get; set; } = new();
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// DTO pour la réponse des patients récents
    /// </summary>
    public class RecentPatientsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<PatientBasicInfoDto> Patients { get; set; } = new();
    }
}
