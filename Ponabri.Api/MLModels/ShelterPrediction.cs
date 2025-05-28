using Microsoft.ML.Data;

namespace Ponabri.Api.MLModels // Ajuste o namespace se necessário
{
    public class ShelterPrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedCategory { get; set; } = default!;
    }
} 