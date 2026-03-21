using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ClinicManagementSystem.Services.Implementations;

/// <summary>
/// Rule-based no-show prediction. Replace with an ML.NET model for production.
/// </summary>
public class PredictionService : IPredictionService
{
    private readonly ILogger<PredictionService> _logger;

    public PredictionService(ILogger<PredictionService> logger)
    {
        _logger = logger;
    }

    public Task<NoShowPredictionOutput> PredictNoShowAsync(NoShowPredictionInput input)
    {
        _logger.LogInformation("Running no-show prediction for appointment type {Type}", input.AppointmentType);

        // Rule-based scoring (replace with ML model in production)
        double score = 0;

        if (input.PreviousNoShowCount > 0)
            score += 0.3 * input.PreviousNoShowCount;

        if (input.DaysBetweenBookingAndAppointment > 14)
            score += 0.2;

        if (!input.HasReminderSent)
            score += 0.1;

        if (!input.HasInsurance)
            score += 0.1;

        if (input.DayOfWeek == DayOfWeek.Monday || input.DayOfWeek == DayOfWeek.Friday)
            score += 0.05;

        score = Math.Clamp(score, 0, 1);

        var output = new NoShowPredictionOutput
        {
            WillNoShow = score >= 0.5,
            Probability = (decimal)Math.Round(score, 4),
            RiskLevel = score < 0.3 ? "Low" : score < 0.6 ? "Medium" : "High"
        };

        _logger.LogInformation("No-show prediction result: WillNoShow={WillNoShow}, Risk={Risk}", output.WillNoShow, output.RiskLevel);
        return Task.FromResult(output);
    }

    public async Task<NoShowDatasetGenerationResult> GenerateNoShowDatasetAsync(int rowCount = 1200, CancellationToken cancellationToken = default)
    {
        var normalizedRows = Math.Clamp(rowCount, 500, 2000);
        var random = new Random(20260321);

        var artifactsDirectory = GetArtifactsDirectory();
        Directory.CreateDirectory(artifactsDirectory);
        var datasetPath = Path.Combine(artifactsDirectory, "no_show_training_data.csv");

        var csv = new StringBuilder();
        csv.AppendLine("PatientAge,PreviousNoShows,PreviousCompletedVisits,DaysBetweenBookingAndAppointment,DayOfWeek,AppointmentType,ReminderSent,HasInsurance,Label,ExampleWeight");

        var appointmentTypes = new[] { "General", "FollowUp", "Consultation", "Emergency", "Checkup", "Procedure", "Dental", "Vaccination" };
        var noShowCount = 0;

        for (int i = 0; i < normalizedRows; i++)
        {
            var patientAge = random.Next(18, 88);
            var previousNoShows = random.Next(0, 6);
            var previousCompletedVisits = random.Next(0, 16);
            var daysBetweenBookingAndAppointment = random.Next(0, 61);
            var dayOfWeek = ((DayOfWeek)random.Next(0, 7)).ToString();
            var appointmentType = appointmentTypes[random.Next(appointmentTypes.Length)];
            var reminderSent = random.NextDouble() < 0.72;
            var hasInsurance = random.NextDouble() < 0.76;

            var probability = 0.08
                + (previousNoShows * 0.15)
                + (daysBetweenBookingAndAppointment >= 21 ? 0.12 : daysBetweenBookingAndAppointment >= 10 ? 0.05 : 0.0)
                + (!reminderSent ? 0.14 : -0.03)
                + (!hasInsurance ? 0.08 : -0.02)
                + (previousCompletedVisits >= 8 ? -0.08 : previousCompletedVisits >= 4 ? -0.04 : 0.0)
                + (dayOfWeek is "Monday" or "Friday" ? 0.03 : 0.0)
                + (appointmentType is "Consultation" or "Dental" ? 0.04 : appointmentType == "Emergency" ? -0.06 : 0.0);

            probability = Math.Clamp(probability, 0.02, 0.92);
            var label = random.NextDouble() < probability;
            if (label)
            {
                noShowCount++;
            }

            var exampleWeight = label ? 3.0f : 1.0f;
            csv.AppendLine(string.Join(',',
                patientAge,
                previousNoShows,
                previousCompletedVisits,
                daysBetweenBookingAndAppointment,
                dayOfWeek,
                appointmentType,
                reminderSent.ToString().ToLowerInvariant(),
                hasInsurance.ToString().ToLowerInvariant(),
                label.ToString().ToLowerInvariant(),
                exampleWeight.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        await File.WriteAllTextAsync(datasetPath, csv.ToString(), cancellationToken);

        var showCount = normalizedRows - noShowCount;
        var noShowRate = normalizedRows == 0 ? 0 : Math.Round((decimal)noShowCount / normalizedRows * 100, 2);

        _logger.LogInformation(
            "Synthetic no-show dataset generated at {Path}. Rows: {Rows}, NoShows: {NoShows} ({Rate}%)",
            datasetPath,
            normalizedRows,
            noShowCount,
            noShowRate);

        return new NoShowDatasetGenerationResult
        {
            RequestedRows = rowCount,
            GeneratedRows = normalizedRows,
            NoShowCount = noShowCount,
            ShowCount = showCount,
            NoShowRate = noShowRate,
            DatasetPath = datasetPath
        };
    }

    public async Task<NoShowTrainingResult> TrainNoShowModelAsync(string? datasetPath = null, CancellationToken cancellationToken = default)
    {
        var artifactsDirectory = GetArtifactsDirectory();
        Directory.CreateDirectory(artifactsDirectory);

        var resolvedDatasetPath = string.IsNullOrWhiteSpace(datasetPath)
            ? Path.Combine(artifactsDirectory, "no_show_training_data.csv")
            : datasetPath;

        if (!File.Exists(resolvedDatasetPath))
        {
            await GenerateNoShowDatasetAsync(1200, cancellationToken);
        }

        var mlContext = new MLContext(seed: 20260321);
        var loader = mlContext.Data.CreateTextLoader(new TextLoader.Options
        {
            HasHeader = true,
            Separators = [','],
            Columns =
            [
                new TextLoader.Column(nameof(NoShowMlDataPoint.PatientAge), DataKind.Single, 0),
                new TextLoader.Column(nameof(NoShowMlDataPoint.PreviousNoShows), DataKind.Single, 1),
                new TextLoader.Column(nameof(NoShowMlDataPoint.PreviousCompletedVisits), DataKind.Single, 2),
                new TextLoader.Column(nameof(NoShowMlDataPoint.DaysBetweenBookingAndAppointment), DataKind.Single, 3),
                new TextLoader.Column(nameof(NoShowMlDataPoint.DayOfWeek), DataKind.String, 4),
                new TextLoader.Column(nameof(NoShowMlDataPoint.AppointmentType), DataKind.String, 5),
                new TextLoader.Column(nameof(NoShowMlDataPoint.ReminderSent), DataKind.Boolean, 6),
                new TextLoader.Column(nameof(NoShowMlDataPoint.HasInsurance), DataKind.Boolean, 7),
                new TextLoader.Column("Label", DataKind.Boolean, 8),
                new TextLoader.Column(nameof(NoShowMlDataPoint.ExampleWeight), DataKind.Single, 9)
            ]
        });

        var data = loader.Load(resolvedDatasetPath);

        var split = mlContext.Data.TrainTestSplit(data, testFraction: 0.2, seed: 20260321);

        var pipeline = mlContext.Transforms.Categorical.OneHotEncoding(new[]
            {
                new InputOutputColumnPair("DayOfWeekEncoded", nameof(NoShowMlDataPoint.DayOfWeek)),
                new InputOutputColumnPair("AppointmentTypeEncoded", nameof(NoShowMlDataPoint.AppointmentType))
            })
            .Append(mlContext.Transforms.Conversion.ConvertType("ReminderSentFloat", nameof(NoShowMlDataPoint.ReminderSent), DataKind.Single))
            .Append(mlContext.Transforms.Conversion.ConvertType("HasInsuranceFloat", nameof(NoShowMlDataPoint.HasInsurance), DataKind.Single))
            .Append(mlContext.Transforms.Concatenate("Features",
                nameof(NoShowMlDataPoint.PatientAge),
                nameof(NoShowMlDataPoint.PreviousNoShows),
                nameof(NoShowMlDataPoint.PreviousCompletedVisits),
                nameof(NoShowMlDataPoint.DaysBetweenBookingAndAppointment),
                "ReminderSentFloat",
                "HasInsuranceFloat",
                "DayOfWeekEncoded",
                "AppointmentTypeEncoded"))
            .Append(mlContext.BinaryClassification.Trainers.FastTree(new Microsoft.ML.Trainers.FastTree.FastTreeBinaryTrainer.Options
            {
                LabelColumnName = "Label",
                FeatureColumnName = "Features",
                ExampleWeightColumnName = nameof(NoShowMlDataPoint.ExampleWeight),
                NumberOfLeaves = 20,
                NumberOfTrees = 200,
                MinimumExampleCountPerLeaf = 10,
                LearningRate = 0.1
            }));

        var model = pipeline.Fit(split.TrainSet);
        var predictions = model.Transform(split.TestSet);
        var metrics = mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: "Label");

        var modelPath = Path.Combine(artifactsDirectory, "no_show_model.zip");
        mlContext.Model.Save(model, split.TrainSet.Schema, modelPath);

        var trainCount = mlContext.Data.CreateEnumerable<NoShowMlDataPoint>(split.TrainSet, reuseRowObject: false).Count();
        var testCount = mlContext.Data.CreateEnumerable<NoShowMlDataPoint>(split.TestSet, reuseRowObject: false).Count();

        _logger.LogInformation(
            "No-show model trained. Accuracy={Accuracy:F4}, Precision={Precision:F4}, Recall={Recall:F4}, F1={F1:F4}, AUC={Auc:F4}. Model saved: {ModelPath}",
            metrics.Accuracy,
            metrics.PositivePrecision,
            metrics.PositiveRecall,
            metrics.F1Score,
            metrics.AreaUnderRocCurve,
            modelPath);

        return new NoShowTrainingResult
        {
            DatasetPath = resolvedDatasetPath,
            ModelPath = modelPath,
            TrainRowCount = trainCount,
            TestRowCount = testCount,
            Metrics = new NoShowTrainingMetrics
            {
                Accuracy = metrics.Accuracy,
                Precision = metrics.PositivePrecision,
                Recall = metrics.PositiveRecall,
                F1Score = metrics.F1Score,
                Auc = metrics.AreaUnderRocCurve
            }
        };
    }

    private string GetArtifactsDirectory()
    {
        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "ml-artifacts", "no-show"));
    }
}
