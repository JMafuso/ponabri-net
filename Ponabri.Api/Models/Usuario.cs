using System.Collections.Generic;

namespace Ponabri.Api.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; } // Lembre-se de usar hashing em um projeto real!
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
} 