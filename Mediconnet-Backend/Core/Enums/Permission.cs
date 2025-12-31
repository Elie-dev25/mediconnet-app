namespace Mediconnet_Backend.Core.Enums;

/// <summary>
/// Énumération des permissions dans le système
/// Utilisée pour contrôle d'accès granulaire
/// Format : [Resource]_[Action]
/// </summary>
[Flags]
public enum Permission
{
    // Permissions Patient (1000-1999)
    Patient_Read = 1001,
    Patient_Create = 1002,
    Patient_Update = 1003,
    Patient_Delete = 1004,
    Patient_ViewAppointments = 1005,
    Patient_BookAppointment = 1006,
    Patient_ViewMedicalRecords = 1007,

    // Permissions Médecin (2000-2999)
    Doctor_Read = 2001,
    Doctor_ViewPatients = 2002,
    Doctor_EditPatientRecords = 2003,
    Doctor_CreatePrescription = 2004,
    Doctor_ViewAppointments = 2005,
    Doctor_ManageConsultations = 2006,
    Doctor_ViewLabs = 2007,

    // Permissions Infirmier (3000-3999)
    Nurse_Read = 3001,
    Nurse_ViewPatients = 3002,
    Nurse_RecordVitals = 3003,
    Nurse_ManagePatientCare = 3004,
    Nurse_ViewMedications = 3005,
    Nurse_DocumentCare = 3006,

    // Permissions Caissier (4000-4999)
    Cashier_Read = 4001,
    Cashier_ViewInvoices = 4002,
    Cashier_ProcessPayment = 4003,
    Cashier_CreateInvoice = 4004,
    Cashier_ManageTransactions = 4005,
    Cashier_ViewFinancialReports = 4006,

    // Permissions Administrateur (5000-5999)
    Admin_Read = 5001,
    Admin_ManageUsers = 5002,
    Admin_ManageRoles = 5003,
    Admin_ManagePermissions = 5004,
    Admin_ViewLogs = 5005,
    Admin_SystemConfiguration = 5006,
    Admin_ViewReports = 5007,
    Admin_ManageDepartments = 5008,

    // Permissions Globales (9000-9999)
    Global_Read = 9001,
    Global_Create = 9002,
    Global_Update = 9003,
    Global_Delete = 9004,
    Global_Export = 9005,
    Global_Import = 9006,
}
