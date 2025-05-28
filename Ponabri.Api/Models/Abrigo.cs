using System.Collections.Generic;

namespace Ponabri.Api.Models
{
    public enum AbrigoStatus
    {
        Aberto,
        Lotado,
        Fechado
    }

    public class Abrigo
    {
        public int Id { get; set; }
        public string NomeLocal { get; set; }
        public string Endereco { get; set; }
        public string Regiao { get; set; } // Usado para busca
        public int CapacidadePessoas { get; set; }
        public int VagasPessoasDisponiveis { get; set; }
        public int CapacidadeCarros { get; set; }
        public int VagasCarrosDisponiveis { get; set; }
        public string ContatoResponsavel { get; set; }
        public AbrigoStatus Status { get; set; } = AbrigoStatus.Aberto;
        public string Descricao { get; set; } // Novo campo para ML.NET
        public string? CategoriaSugeridaML { get; set; } // Campo para ML.NET
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
} 