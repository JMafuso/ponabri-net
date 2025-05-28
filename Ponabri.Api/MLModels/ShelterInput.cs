using Microsoft.ML.Data;

namespace Ponabri.Api.MLModels // Ajuste o namespace se necessário
{
    public class ShelterInput
    {
        [LoadColumn(0)]
        public string Description { get; set; } = default!;

        [LoadColumn(1)]
        public string Category { get; set; } = default!; // Para treinamento
    }
} 