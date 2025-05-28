using System.ComponentModel.DataAnnotations;

namespace Ponabri.Api.Dtos.ReservaDtos
{
    public class ReservaCreateDto
    {
        [Required(ErrorMessage = "O ID do abrigo é obrigatório.")]
        public int AbrigoId { get; set; }

        [Required(ErrorMessage = "A quantidade de pessoas é obrigatória.")]
        [Range(1, int.MaxValue, ErrorMessage = "A quantidade de pessoas deve ser pelo menos 1.")]
        public int QuantidadePessoas { get; set; }

        [Required(ErrorMessage = "Informar se usará vaga de carro é obrigatório.")]
        public bool UsouVagaCarro { get; set; }
    }
} 