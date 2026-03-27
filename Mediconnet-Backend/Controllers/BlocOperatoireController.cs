using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mediconnet_Backend.DTOs;
using Mediconnet_Backend.Services;
using System.Security.Claims;

namespace Mediconnet_Backend.Controllers
{
    [ApiController]
    [Route("api/blocs-operatoires")]
    [Authorize]
    public class BlocOperatoireController : ControllerBase
    {
        private readonly IBlocOperatoireService _blocService;
        private readonly ILogger<BlocOperatoireController> _logger;

        public BlocOperatoireController(
            IBlocOperatoireService blocService,
            ILogger<BlocOperatoireController> logger)
        {
            _blocService = blocService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("'idUser'")?.Value;
            return int.TryParse(userIdClaim, out var id) ? id : 0;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value
                ?? User.FindFirst("role")?.Value
                ?? "";
        }

        #region Gestion des Blocs (Admin)

        /// <summary>
        /// Liste tous les blocs opératoires
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<BlocOperatoireListDto>>> GetAllBlocs()
        {
            var blocs = await _blocService.GetAllBlocsAsync();
            return Ok(blocs);
        }

        /// <summary>
        /// Récupère un bloc opératoire par son ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<BlocOperatoireDto>> GetBlocById(int id)
        {
            var bloc = await _blocService.GetBlocByIdAsync(id);
            if (bloc == null)
                return NotFound(new { message = "Bloc opératoire non trouvé" });

            return Ok(bloc);
        }

        /// <summary>
        /// Crée un nouveau bloc opératoire (Admin uniquement)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "administrateur")]
        public async Task<ActionResult<BlocOperatoireDto>> CreateBloc([FromBody] CreateBlocOperatoireRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Nom))
                return BadRequest(new { message = "Le nom du bloc est requis" });

            var bloc = await _blocService.CreateBlocAsync(request);
            return CreatedAtAction(nameof(GetBlocById), new { id = bloc.IdBloc }, bloc);
        }

        /// <summary>
        /// Met à jour un bloc opératoire (Admin uniquement)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "administrateur")]
        public async Task<ActionResult<BlocOperatoireDto>> UpdateBloc(int id, [FromBody] UpdateBlocOperatoireRequest request)
        {
            var bloc = await _blocService.UpdateBlocAsync(id, request);
            if (bloc == null)
                return NotFound(new { message = "Bloc opératoire non trouvé" });

            return Ok(bloc);
        }

        /// <summary>
        /// Supprime un bloc opératoire (Admin uniquement)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "administrateur")]
        public async Task<ActionResult> DeleteBloc(int id)
        {
            var result = await _blocService.DeleteBlocAsync(id);
            if (!result)
                return BadRequest(new { message = "Impossible de supprimer ce bloc (réservations actives ou bloc non trouvé)" });

            return Ok(new { message = "Bloc opératoire supprimé avec succès" });
        }

        #endregion

        #region Réservations

        /// <summary>
        /// Liste les réservations d'un bloc
        /// </summary>
        [HttpGet("{idBloc}/reservations")]
        public async Task<ActionResult<List<ReservationBlocListDto>>> GetReservationsByBloc(
            int idBloc,
            [FromQuery] DateTime? dateDebut = null,
            [FromQuery] DateTime? dateFin = null)
        {
            var reservations = await _blocService.GetReservationsByBlocAsync(idBloc, dateDebut, dateFin);
            return Ok(reservations);
        }

        /// <summary>
        /// Liste toutes les réservations pour une date donnée
        /// </summary>
        [HttpGet("reservations/date/{date}")]
        public async Task<ActionResult<List<ReservationBlocListDto>>> GetReservationsByDate(DateTime date)
        {
            var reservations = await _blocService.GetReservationsByDateAsync(date);
            return Ok(reservations);
        }

        /// <summary>
        /// Récupère une réservation par son ID
        /// </summary>
        [HttpGet("reservations/{idReservation}")]
        public async Task<ActionResult<ReservationBlocDto>> GetReservationById(int idReservation)
        {
            var reservation = await _blocService.GetReservationByIdAsync(idReservation);
            if (reservation == null)
                return NotFound(new { message = "Réservation non trouvée" });

            return Ok(reservation);
        }

        /// <summary>
        /// Crée une réservation de bloc (Médecin/Chirurgien)
        /// </summary>
        [HttpPost("reservations")]
        [Authorize(Roles = "medecin,administrateur")]
        public async Task<ActionResult<ReservationBlocDto>> CreateReservation([FromBody] CreateReservationBlocRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            var (success, message, reservation) = await _blocService.CreateReservationAsync(request, userId);

            if (!success)
                return BadRequest(new { message });

            return CreatedAtAction(nameof(GetReservationById), new { idReservation = reservation!.IdReservation }, reservation);
        }

        /// <summary>
        /// Met à jour une réservation
        /// </summary>
        [HttpPut("reservations/{idReservation}")]
        [Authorize(Roles = "medecin,administrateur")]
        public async Task<ActionResult<ReservationBlocDto>> UpdateReservation(
            int idReservation,
            [FromBody] UpdateReservationBlocRequest request)
        {
            var (success, message, reservation) = await _blocService.UpdateReservationAsync(idReservation, request);

            if (!success)
                return BadRequest(new { message });

            return Ok(reservation);
        }

        /// <summary>
        /// Annule une réservation
        /// </summary>
        [HttpPost("reservations/{idReservation}/annuler")]
        [Authorize(Roles = "medecin,administrateur")]
        public async Task<ActionResult> CancelReservation(int idReservation)
        {
            var result = await _blocService.CancelReservationAsync(idReservation);
            if (!result)
                return NotFound(new { message = "Réservation non trouvée" });

            return Ok(new { message = "Réservation annulée avec succès" });
        }

        #endregion

        #region Disponibilité

        /// <summary>
        /// Vérifie la disponibilité des blocs pour un créneau donné
        /// </summary>
        [HttpGet("disponibilites")]
        public async Task<ActionResult<List<DisponibiliteBlocDto>>> GetDisponibilites(
            [FromQuery] DateTime date,
            [FromQuery] string heureDebut,
            [FromQuery] int dureeMinutes)
        {
            if (string.IsNullOrEmpty(heureDebut) || dureeMinutes <= 0)
                return BadRequest(new { message = "Paramètres invalides" });

            var disponibilites = await _blocService.GetDisponibilitesAsync(date, heureDebut, dureeMinutes);
            return Ok(disponibilites);
        }

        /// <summary>
        /// Vérifie si un bloc spécifique est disponible
        /// </summary>
        [HttpPost("{idBloc}/verifier-disponibilite")]
        public async Task<ActionResult<object>> VerifierDisponibilite(
            int idBloc,
            [FromBody] VerifierDisponibiliteBlocRequest request)
        {
            var estDisponible = await _blocService.VerifierDisponibiliteAsync(
                idBloc, request.Date, request.HeureDebut, request.DureeMinutes);

            return Ok(new { estDisponible });
        }

        #endregion

        #region Agenda

        /// <summary>
        /// Récupère l'agenda d'un bloc pour une date donnée
        /// </summary>
        [HttpGet("{idBloc}/agenda")]
        public async Task<ActionResult<AgendaBlocDto>> GetAgendaBloc(int idBloc, [FromQuery] DateTime? date = null)
        {
            var targetDate = date ?? DateTime.Today;
            var agenda = await _blocService.GetAgendaBlocAsync(idBloc, targetDate);
            return Ok(agenda);
        }

        /// <summary>
        /// Récupère l'agenda de tous les blocs pour une date donnée
        /// </summary>
        [HttpGet("agenda")]
        public async Task<ActionResult<List<AgendaBlocDto>>> GetAgendaTousBlocs([FromQuery] DateTime? date = null)
        {
            var targetDate = date ?? DateTime.Today;
            var agendas = await _blocService.GetAgendaTousBlocsAsync(targetDate);
            return Ok(agendas);
        }

        #endregion

        #region Mise à jour des statuts

        /// <summary>
        /// Force la mise à jour des statuts des blocs (Admin)
        /// </summary>
        [HttpPost("update-statuses")]
        [Authorize(Roles = "administrateur")]
        public async Task<ActionResult> UpdateStatuses()
        {
            await _blocService.UpdateBlocStatusesAsync();
            return Ok(new { message = "Statuts mis à jour" });
        }

        #endregion
    }
}
