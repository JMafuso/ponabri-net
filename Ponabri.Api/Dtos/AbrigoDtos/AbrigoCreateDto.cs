using System.ComponentModel.DataAnnotations;

namespace Ponabri.Api.Dtos.AbrigoDtos
{
    /// <summary>
    /// DTO para criação de um novo abrigo.
    /// </summary>
    public class AbrigoCreateDto
    {
        [Required(ErrorMessage = "O nome do local é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome do local deve ter no máximo 100 caracteres.")]
        public string NomeLocal { get; set; } = default!;

        [Required(ErrorMessage = "O endereço é obrigatório.")]
        [StringLength(200, ErrorMessage = "O endereço deve ter no máximo 200 caracteres.")]
        public string Endereco { get; set; } = default!;

        [Required(ErrorMessage = "A região é obrigatória.")]
        [StringLength(100, ErrorMessage = "A região deve ter no máximo 100 caracteres.")]
        public string Regiao { get; set; } = default!;

        [Required(ErrorMessage = "A capacidade de pessoas é obrigatória.")]
        [Range(1, 1000, ErrorMessage = "A capacidade de pessoas deve ser entre 1 e 1000.")]
        public int CapacidadePessoas { get; set; }

        [Required(ErrorMessage = "A capacidade de carros é obrigatória.")]
        [Range(0, 100, ErrorMessage = "A capacidade de carros deve ser entre 0 e 100.")]
        public int CapacidadeCarros { get; set; }

        [Required(ErrorMessage = "O contato do responsável é obrigatório.")]
        [StringLength(100, ErrorMessage = "O contato do responsável deve ter no máximo 100 caracteres.")]
        public string ContatoResponsavel { get; set; } = default!;

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        public string Descricao { get; set; } = default!;
    }
} 