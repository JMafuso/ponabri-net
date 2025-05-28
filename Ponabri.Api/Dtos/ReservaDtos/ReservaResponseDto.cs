using Ponabri.Api.Models; // Para ReservaStatus
using Ponabri.Api.Dtos.Common; // Adicionado para LinkDto
using System;
using System.Collections.Generic; // Adicionado para List<T>

namespace Ponabri.Api.Dtos.ReservaDtos
{
    // DTOs aninhados para informações de Abrigo e Usuário dentro da ReservaResponse
    public class AbrigoInfoForReservaDto
    {
        public int Id { get; set; }
        public string NomeLocal { get; set; }
        public string Endereco { get; set; }
    }

    public class UsuarioInfoForReservaDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
    }

    public class ReservaResponseDto
    {
        public int Id { get; set; }
        public string CodigoReserva { get; set; }
        public int UsuarioId { get; set; }
        public UsuarioInfoForReservaDto Usuario {get; set; } // Detalhes do usuário
        public int AbrigoId { get; set; }
        public AbrigoInfoForReservaDto Abrigo { get; set; } // Detalhes do abrigo
        public int QuantidadePessoas { get; set; }
        public bool UsouVagaCarro { get; set; }
        public DateTime DataCriacao { get; set; }
        public ReservaStatus Status { get; set; }
        public List<LinkDto> Links { get; set; } = new List<LinkDto>(); // Para HATEOAS
    }
} 