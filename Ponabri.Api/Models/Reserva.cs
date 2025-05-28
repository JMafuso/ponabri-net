using System;

namespace Ponabri.Api.Models
{
    public enum ReservaStatus
    {
        Ativa,    // Reserva feita, aguardando check-in
        Concluida, // Check-in realizado
        Cancelada // Reserva foi cancelada
    }

    public class Reserva
    {
        public int Id { get; set; }
        public string CodigoReserva { get; set; } = default!;
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;
        public int AbrigoId { get; set; }
        public Abrigo Abrigo { get; set; } = null!;
        public int QuantidadePessoas { get; set; }
        public bool UsouVagaCarro { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public ReservaStatus Status { get; set; } = ReservaStatus.Ativa;
    }
} 