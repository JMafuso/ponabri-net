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
        public string NomeLocal { get; set; } = default!;
        public string Endereco { get; set; } = default!;
    }

    public class UsuarioInfoForReservaDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = default!;
        public string Email { get; set; } = default!;
    }

    public class ReservaResponseDto
    {
        public int Id { get; set; }
        public string CodigoReserva { get; set; } = default!;
        public int UsuarioId { get; set; }
        public UsuarioInfoForReservaDto? Usuario {get; set; }
        public int AbrigoId { get; set; }
        public AbrigoInfoForReservaDto? Abrigo { get; set; }
        public int QuantidadePessoas { get; set; }
        public bool UsouVagaCarro { get; set; }
        public DateTime DataCriacao { get; set; }
        public ReservaStatus Status { get; set; }
        public List<LinkDto> Links { get; set; } = new List<LinkDto>(); // Para HATEOAS
    }
} 