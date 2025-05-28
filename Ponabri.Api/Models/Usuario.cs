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
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; } // Lembre-se de usar hashing em um projeto real!
        public string Role { get; set; } // Propriedade para o papel do usuário
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
} 