using System.ComponentModel.DataAnnotations;
using Ponabri.Api.Models; // Necessário para AbrigoStatus

namespace Ponabri.Api.Dtos.AbrigoDtos // Namespace ATUALIZADO
{
    /// <summary>
    /// DTO para atualização de um abrigo existente.
    /// Permite atualizações parciais (campos não fornecidos não serão alterados).
    /// </summary>
    public class AbrigoUpdateDto
    {
        [StringLength(100, ErrorMessage = "O nome do local deve ter no máximo 100 caracteres.")]
        public string? NomeLocal { get; set; }

        [StringLength(200, ErrorMessage = "O endereço deve ter no máximo 200 caracteres.")]
        public string? Endereco { get; set; }

        [StringLength(100, ErrorMessage = "A região deve ter no máximo 100 caracteres.")]
        public string? Regiao { get; set; }

        [Range(1, 1000, ErrorMessage = "A capacidade de pessoas deve ser entre 1 e 1000.")]
        public int? CapacidadePessoas { get; set; }

        [Range(0, 100, ErrorMessage = "A capacidade de carros deve ser entre 0 e 100.")]
        public int? CapacidadeCarros { get; set; }

        [StringLength(100, ErrorMessage = "O contato do responsável deve ter no máximo 100 caracteres.")]
        public string? ContatoResponsavel { get; set; }

        [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
        public string? Descricao { get; set; }

        public AbrigoStatus? Status { get; set; }
    }
} 