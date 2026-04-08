namespace Mediconnet_Backend.DTOs
{
    // ==================== BLOC OPERATOIRE DTOs ====================

    public class BlocOperatoireDto
    {
        public int IdBloc { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Statut { get; set; } = "libre";
        public bool Actif { get; set; } = true;
        public string? Localisation { get; set; }
        public int? Capacite { get; set; }
        public string? Equipements { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int NombreReservationsAujourdhui { get; set; }
        public ReservationBlocDto? ReservationEnCours { get; set; }
    }

    public class BlocOperatoireListDto
    {
        public int IdBloc { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Statut { get; set; } = "libre";
        public bool Actif { get; set; } = true;
        public string? Localisation { get; set; }
        public int NombreReservationsAujourdhui { get; set; }
    }

    public class CreateBlocOperatoireRequest
    {
        public string Nom { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Localisation { get; set; }
        public int? Capacite { get; set; }
        public string? Equipements { get; set; }
    }

    public class UpdateBlocOperatoireRequest
    {
        public string? Nom { get; set; }
        public string? Description { get; set; }
        public string? Statut { get; set; }
        public bool? Actif { get; set; }
        public string? Localisation { get; set; }
        public int? Capacite { get; set; }
        public string? Equipements { get; set; }
    }

    // ==================== RESERVATION BLOC DTOs ====================

    public class ReservationBlocDto
    {
        public int IdReservation { get; set; }
        public int IdBloc { get; set; }
        public string NomBloc { get; set; } = string.Empty;
        public int IdProgrammation { get; set; }
        public int IdMedecin { get; set; }
        public string MedecinNom { get; set; } = string.Empty;
        public string MedecinPrenom { get; set; } = string.Empty;
        public int IdPatient { get; set; }
        public string PatientNom { get; set; } = string.Empty;
        public string PatientPrenom { get; set; } = string.Empty;
        public string TypeIntervention { get; set; } = string.Empty;
        public string? IndicationOperatoire { get; set; }
        public DateTime DateReservation { get; set; }
        public string HeureDebut { get; set; } = string.Empty;
        public string HeureFin { get; set; } = string.Empty;
        public int DureeMinutes { get; set; }
        public string Statut { get; set; } = "confirmee";
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ReservationBlocListDto
    {
        public int IdReservation { get; set; }
        public int IdBloc { get; set; }
        public string NomBloc { get; set; } = string.Empty;
        public string MedecinNom { get; set; } = string.Empty;
        public string PatientNom { get; set; } = string.Empty;
        public string TypeIntervention { get; set; } = string.Empty;
        public DateTime DateReservation { get; set; }
        public string HeureDebut { get; set; } = string.Empty;
        public string HeureFin { get; set; } = string.Empty;
        public int DureeMinutes { get; set; }
        public string Statut { get; set; } = "confirmee";
    }

    public class CreateReservationBlocRequest
    {
        public int IdBloc { get; set; }
        public int IdProgrammation { get; set; }
        public DateTime DateReservation { get; set; }
        public string HeureDebut { get; set; } = string.Empty;
        public int DureeMinutes { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateReservationBlocRequest
    {
        public int? IdBloc { get; set; }
        public DateTime? DateReservation { get; set; }
        public string? HeureDebut { get; set; }
        public int? DureeMinutes { get; set; }
        public string? Statut { get; set; }
        public string? Notes { get; set; }
    }

    // ==================== AGENDA BLOC DTOs ====================

    public class AgendaBlocDto
    {
        public int IdBloc { get; set; }
        public string NomBloc { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public List<CreneauBlocDto> Creneaux { get; set; } = new();
    }

    public class CreneauBlocDto
    {
        public string HeureDebut { get; set; } = string.Empty;
        public string HeureFin { get; set; } = string.Empty;
        public bool EstReserve { get; set; }
        public ReservationBlocDto? Reservation { get; set; }
    }

    public class DisponibiliteBlocDto
    {
        public int IdBloc { get; set; }
        public string NomBloc { get; set; } = string.Empty;
        public bool EstDisponible { get; set; }
        public List<string> CreneauxDisponibles { get; set; } = new();
        public List<string> CreneauxOccupes { get; set; } = new();
    }

    public class VerifierDisponibiliteBlocRequest
    {
        public DateTime Date { get; set; }
        public string HeureDebut { get; set; } = string.Empty;
        public int DureeMinutes { get; set; }
    }

    public class ConfirmerAnnulationRdvRequest
    {
        public DateTime Date { get; set; }
        public string HeureDebut { get; set; } = string.Empty;
        public int DureeMinutes { get; set; }
        public string PatientIntervention { get; set; } = string.Empty;
        public string NomChirurgien { get; set; } = string.Empty;
    }
}
