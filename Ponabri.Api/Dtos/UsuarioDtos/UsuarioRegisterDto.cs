using System.ComponentModel.DataAnnotations;

namespace Ponabri.Api.Dtos.UsuarioDtos
{
    public class UsuarioRegisterDto
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Formato de email inválido.")]
        [StringLength(100, ErrorMessage = "O email deve ter no máximo 100 caracteres.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
        [StringLength(100, ErrorMessage = "A senha deve ter no máximo 100 caracteres.")] // É uma boa prática limitar o tamanho da senha de entrada
        public string Senha { get; set; }
    }
} 