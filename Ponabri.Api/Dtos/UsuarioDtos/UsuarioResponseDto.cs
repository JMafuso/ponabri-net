using Ponabri.Api.Dtos.Common; // Para LinkDto
using System.Collections.Generic; // Para List<T>

namespace Ponabri.Api.Dtos.UsuarioDtos
{
    public class UsuarioResponseDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Role { get; set; } // Adicionando Role para clareza na resposta
        public List<LinkDto> Links { get; set; } = new List<LinkDto>();
    }
} 