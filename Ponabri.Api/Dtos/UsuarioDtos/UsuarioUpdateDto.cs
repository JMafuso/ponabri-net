using System.ComponentModel.DataAnnotations;

namespace Ponabri.Api.Dtos.UsuarioDtos
{
    public class UsuarioUpdateDto
    {
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        public string? Nome { get; set; }

        [EmailAddress(ErrorMessage = "Formato de email inválido.")]
        [StringLength(100, ErrorMessage = "O email deve ter no máximo 100 caracteres.")]
        public string? Email { get; set; } 
        // Não incluir alteração de senha aqui para simplificar, como solicitado.
        // A alteração de senha geralmente é um fluxo separado e mais seguro.
    }
} 