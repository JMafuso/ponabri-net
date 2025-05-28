using Ponabri.Api.Dtos.Common; // Para LinkDto
using System.Collections.Generic; // Para List<T>

namespace Ponabri.Api.Dtos.UsuarioDtos
{
    public class UsuarioResponseDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Role { get; set; } = default!;
        public List<LinkDto> Links { get; set; } = new List<LinkDto>();
    }
} 