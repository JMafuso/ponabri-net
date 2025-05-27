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
        public string CodigoReserva { get; set; } // Ex: PONABRI-12345
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; }
        public int AbrigoId { get; set; }
        public Abrigo Abrigo { get; set; }
        public int QuantidadePessoas { get; set; }
        public bool UsouVagaCarro { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public ReservaStatus Status { get; set; } = ReservaStatus.Ativa;
    }
} 