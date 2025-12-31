namespace Mediconnet_Backend.Core.Enums;

/// <summary>
/// Énumération des rôles utilisateurs dans MediConnect
/// Permet l'ajout facile de nouveaux rôles sans modification structurelle
/// </summary>
public enum UserRole
{
    /// <summary>Patient - Accès aux services médicaux</summary>
    Patient = 1,
    
    /// <summary>Médecin - Gestion des consultations et diagnostics</summary>
    Doctor = 2,
    
    /// <summary>Infirmier - Assistance médicale et soins</summary>
    Nurse = 3,
    
    /// <summary>Caissier - Gestion financière et facturation</summary>
    Cashier = 4,
    
    /// <summary>Administrateur - Accès complet au système</summary>
    Administrator = 5,
    
    /// <summary>Pharmacien - Gestion des prescriptions et médicaments</summary>
    Pharmacist = 6,
    
    /// <summary>Biologiste - Gestion des analyses et résultats de laboratoire</summary>
    Biologist = 7,
    
    /// <summary>Réceptionniste/Accueil - Gestion des enregistrements patients</summary>
    Receptionist = 8,
}
