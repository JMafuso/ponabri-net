using System.ComponentModel.DataAnnotations;

namespace Ponabri.Api.Dtos.UsuarioDtos
{
    public class UsuarioLoginDto
    {
        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Formato de email inválido.")]
        public string Email { get; set; } = default!;

        [Required(ErrorMessage = "A senha é obrigatória.")]
        public string Senha { get; set; } = default!;
    }
} 