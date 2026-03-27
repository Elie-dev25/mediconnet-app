using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediconnet_Backend.Core.Entities
{
    [Table("bloc_operatoire")]
    public class BlocOperatoire
    {
        [Key]
        [Column("id_bloc")]
        public int IdBloc { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("nom")]
        public string Nom { get; set; } = string.Empty;

        [MaxLength(500)]
        [Column("description")]
        public string? Description { get; set; }

        [MaxLength(20)]
        [Column("statut")]
        public string Statut { get; set; } = "libre"; // libre, occupe, maintenance

        [Column("actif")]
        public bool Actif { get; set; } = true;

        [MaxLength(100)]
        [Column("localisation")]
        public string? Localisation { get; set; }

        [Column("capacite")]
        public int? Capacite { get; set; }

        [MaxLength(500)]
        [Column("equipements")]
        public string? Equipements { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual ICollection<ReservationBloc> Reservations { get; set; } = new List<ReservationBloc>();
    }

    [Table("reservation_bloc")]
    public class ReservationBloc
    {
        [Key]
        [Column("id_reservation")]
        public int IdReservation { get; set; }

        [Column("id_bloc")]
        public int IdBloc { get; set; }

        [Column("id_programmation")]
        public int IdProgrammation { get; set; }

        [Column("id_medecin")]
        public int IdMedecin { get; set; }

        [Required]
        [Column("date_reservation")]
        public DateTime DateReservation { get; set; }

        [Required]
        [MaxLength(5)]
        [Column("heure_debut")]
        public string HeureDebut { get; set; } = string.Empty;

        [Required]
        [MaxLength(5)]
        [Column("heure_fin")]
        public string HeureFin { get; set; } = string.Empty;

        [Column("duree_minutes")]
        public int DureeMinutes { get; set; }

        [MaxLength(20)]
        [Column("statut")]
        public string Statut { get; set; } = "confirmee"; // confirmee, en_cours, terminee, annulee

        [MaxLength(500)]
        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        [ForeignKey("IdBloc")]
        public virtual BlocOperatoire? Bloc { get; set; }

        [ForeignKey("IdProgrammation")]
        public virtual ProgrammationIntervention? Programmation { get; set; }

        [ForeignKey("IdMedecin")]
        public virtual Medecin? Medecin { get; set; }
    }
}
