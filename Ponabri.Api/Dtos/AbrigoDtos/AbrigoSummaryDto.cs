using Ponabri.Api.Dtos.Common;
using Ponabri.Api.Models; // Para AbrigoStatus
using System.Collections.Generic;

namespace Ponabri.Api.Dtos.AbrigoDtos
{
    public class AbrigoSummaryDto
    {
        public int Id { get; set; }
        public string NomeLocal { get; set; } = default!;
        public string Endereco { get; set; } = default!;
        public AbrigoStatus Status { get; set; }
        public int VagasPessoasDisponiveis { get; set; }
        public int VagasCarrosDisponiveis { get; set; }
        public List<LinkDto> Links { get; set; } = new List<LinkDto>();
    }
} 