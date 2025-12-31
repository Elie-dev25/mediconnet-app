namespace Mediconnet_Backend.DTOs.Consultation;

/// <summary>
/// DTO pour afficher les paramètres vitaux
/// </summary>
public class ParametreDto
{
    public int IdParametre { get; set; }
    public int IdConsultation { get; set; }
    public decimal? Poids { get; set; }
    public decimal? Temperature { get; set; }
    public int? TensionSystolique { get; set; }
    public int? TensionDiastolique { get; set; }
    public decimal? Taille { get; set; }
    public DateTime DateEnregistrement { get; set; }
    public int? EnregistrePar { get; set; }
    public string? NomEnregistrant { get; set; }
    
    // Propriétés calculées
    public string? TensionFormatee => 
        TensionSystolique.HasValue && TensionDiastolique.HasValue 
            ? $"{TensionSystolique}/{TensionDiastolique}" 
            : null;

    public decimal? IMC => 
        Poids.HasValue && Taille.HasValue && Taille.Value > 0 
            ? Math.Round(Poids.Value / ((Taille.Value / 100) * (Taille.Value / 100)), 2) 
            : null;
}

/// <summary>
/// DTO pour créer ou mettre à jour les paramètres vitaux
/// </summary>
public class CreateParametreRequest
{
    public int IdConsultation { get; set; }
    public decimal? Poids { get; set; }
    public decimal? Temperature { get; set; }
    public int? TensionSystolique { get; set; }
    public int? TensionDiastolique { get; set; }
    public decimal? Taille { get; set; }
}

/// <summary>
/// DTO pour mettre à jour les paramètres vitaux
/// </summary>
public class UpdateParametreRequest
{
    public decimal? Poids { get; set; }
    public decimal? Temperature { get; set; }
    public int? TensionSystolique { get; set; }
    public int? TensionDiastolique { get; set; }
    public decimal? Taille { get; set; }
}

/// <summary>
/// DTO pour créer des paramètres vitaux directement pour un patient (sans consultation existante)
/// Utilisé par l'infirmière depuis la liste des patients
/// </summary>
public class CreateParametreByPatientRequest
{
    public int IdPatient { get; set; }
    public decimal? Poids { get; set; }
    public decimal? Temperature { get; set; }
    public int? TensionSystolique { get; set; }
    public int? TensionDiastolique { get; set; }
    public decimal? Taille { get; set; }
}

/// <summary>
/// DTO pour afficher une consultation avec ses paramètres
/// </summary>
public class ConsultationWithParametresDto
{
    public int IdConsultation { get; set; }
    public DateTime DateHeure { get; set; }
    public string? Motif { get; set; }
    public string? Diagnostic { get; set; }
    public string? Statut { get; set; }
    public string? TypeConsultation { get; set; }
    
    // Infos médecin
    public int IdMedecin { get; set; }
    public string? NomMedecin { get; set; }
    public string? PrenomMedecin { get; set; }
    
    // Infos patient
    public int IdPatient { get; set; }
    public string? NomPatient { get; set; }
    public string? PrenomPatient { get; set; }
    
    // Paramètres vitaux
    public ParametreDto? Parametres { get; set; }
}
