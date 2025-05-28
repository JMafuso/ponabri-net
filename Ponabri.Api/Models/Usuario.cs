using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Para StringLength, se necessário para Role

namespace Ponabri.Api.Models
{
    public static class UserRoles // Classe estática para constantes de papéis
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }

    public class Usuario
    {
        public int Id { get; set; }
        public string Nome { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Senha { get; set; } = default!;
        public string Role { get; set; } = default!;
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
} 