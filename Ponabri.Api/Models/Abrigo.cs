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
        public string NomeLocal { get; set; } = default!;
        public string Endereco { get; set; } = default!;
        public string Regiao { get; set; } = default!;
        public int CapacidadePessoas { get; set; }
        public int VagasPessoasDisponiveis { get; set; }
        public int CapacidadeCarros { get; set; }
        public int VagasCarrosDisponiveis { get; set; }
        public string ContatoResponsavel { get; set; } = default!;
        public AbrigoStatus Status { get; set; } = AbrigoStatus.Aberto;
        public string Descricao { get; set; } = default!;
        public string? CategoriaSugeridaML { get; set; }
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
} 