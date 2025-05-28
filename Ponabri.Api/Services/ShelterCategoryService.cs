using Microsoft.ML;
using Ponabri.Api.MLModels;
using System;
using System.Collections.Generic;

namespace Ponabri.Api.Services
{
    public class ShelterCategoryService
    {
        private readonly MLContext _mlContext;
        private ITransformer _model = default!;

        public ShelterCategoryService()
        {
            _mlContext = new MLContext(seed: 0);
            // Em um cenário real, você carregaria um modelo treinado de um arquivo.
            // Aqui, vamos treinar um modelo simples em memória para demonstração.
            TrainModel();
        }

        private void TrainModel()
        {
            // 1. Criar dados de exemplo (muito poucos para um modelo real, mas ok para demo)
            var sampleData = new List<ShelterInput>
            {
                new ShelterInput { Description = "Espaço amplo para famílias com crianças e parquinho.", Category = "Familiar" },
                new ShelterInput { Description = "Aceitamos cães e gatos de pequeno porte. Temos área para pets.", Category = "PetFriendly" },
                new ShelterInput { Description = "Local seguro para idosos, com acessibilidade e rampas.", Category = "Idosos" },
                new ShelterInput { Description = "Vagas para todos, tragam seus filhos e animais!", Category = "Geral" },
                new ShelterInput { Description = "Ambiente tranquilo, ideal para descanso de casais.", Category = "Casais" },
                new ShelterInput { Description = "Foco em acolher cachorros e seus donos.", Category = "PetFriendly" }
            };

            var dataView = _mlContext.Data.LoadFromEnumerable(sampleData);

            // 2. Definir o pipeline de treinamento
            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey(inputColumnName: "Category", outputColumnName: "Label")
                .Append(_mlContext.Transforms.Text.FeaturizeText(inputColumnName: "Description", outputColumnName: "DescriptionFeaturized"))
                .Append(_mlContext.Transforms.Concatenate("Features", "DescriptionFeaturized"))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            // 3. Treinar o modelo
            Console.WriteLine("Treinando modelo de categorização de abrigos...");
            _model = pipeline.Fit(dataView);
            Console.WriteLine("Modelo treinado.");
        }

        public string PredictCategory(string description)
        {
            if (_model == null) TrainModel(); // Garante que o modelo está treinado

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<ShelterInput, ShelterPrediction>(_model);
            var prediction = predictionEngine.Predict(new ShelterInput { Description = description });
            return prediction.PredictedCategory;
        }
    }
} 