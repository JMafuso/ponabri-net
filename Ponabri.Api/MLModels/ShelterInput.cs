using Microsoft.ML.Data;

namespace Ponabri.Api.MLModels // Ajuste o namespace se necessário
{
    public class ShelterInput
    {
        [LoadColumn(0)]
        public string Description { get; set; }

        [LoadColumn(1)]
        public string Category { get; set; } // Para treinamento
    }
} 