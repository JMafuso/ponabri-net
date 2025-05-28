using Ponabri.Api.Models; // Para ReservaStatus
using System.ComponentModel.DataAnnotations;

namespace Ponabri.Api.Dtos.ReservaDtos
{
    public class ReservaUpdateDto
    {
        [Required(ErrorMessage = "O novo status da reserva é obrigatório.")]
        public ReservaStatus? Status { get; set; }
    }
} 