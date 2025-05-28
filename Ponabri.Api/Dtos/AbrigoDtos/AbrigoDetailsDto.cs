using Ponabri.Api.Dtos.Common;
using Ponabri.Api.Models;
using System.Collections.Generic;

namespace Ponabri.Api.Dtos.AbrigoDtos
{
    public class AbrigoDetailsDto
    {
        public int Id { get; set; }
        public string NomeLocal { get; set; } = default!;
        public string Endereco { get; set; } = default!;
        public string Regiao { get; set; } = default!;
        public int CapacidadePessoas { get; set; }
        public int VagasPessoasDisponiveis { get; set; }
        public int CapacidadeCarros { get; set; }
        public int VagasCarrosDisponiveis { get; set; }
        public string ContatoResponsavel { get; set; } = default!;
        public string Descricao { get; set; } = default!;
        public string? CategoriaSugeridaML { get; set; }
        public AbrigoStatus Status { get; set; }
        public List<LinkDto> Links { get; set; } = new List<LinkDto>();
    }
} 