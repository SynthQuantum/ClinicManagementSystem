namespace ClinicManagementSystem.Services.Options;

public class MlArtifactsOptions
{
    public const string SectionName = "MlArtifacts";

    public string NoShowArtifactsPath { get; set; } = "ml-artifacts/no-show";

    public string ModelFileName { get; set; } = "no_show_model.zip";

    public string DatasetFileName { get; set; } = "no_show_training_data.csv";

    public string MetricsFileName { get; set; } = "no_show_model_metrics.json";
}
