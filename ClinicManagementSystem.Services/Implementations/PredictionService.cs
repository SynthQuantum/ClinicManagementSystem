using System.Globalization;
using System.Text;
using System.Text.Json;
using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using ClinicManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace ClinicManagementSystem.Services.Implementations;

public class PredictionService : IPredictionService
{
    private const int MinRows = 500;
    private const int MaxRows = 2000;
    private const int DefaultRows = 1200;

    private readonly ILogger<PredictionService> _logger;
    private readonly ClinicDbContext _db;

    private static readonly object ModelSyncLock = new();
    private static ITransformer? _cachedModel;
    private static PredictionEngine<NoShowMlDataPoint, NoShowMlPrediction>? _cachedPredictionEngine;
    private static string? _cachedModelPath;

    private readonly MLContext _mlContext = new(seed: 20260321);

    public PredictionService(ILogger<PredictionService> logger, ClinicDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public NoShowMlDataPoint MapInputToFeatureVector(NoShowPredictionInput input)
    {
        return new NoShowMlDataPoint
        {
            PatientAge = input.PatientAge,
            PreviousNoShows = input.PreviousNoShowCount,
            PreviousCompletedVisits = input.PreviousCompletedCount,
            DaysBetweenBookingAndAppointment = input.DaysBetweenBookingAndAppointment,
            DayOfWeek = input.DayOfWeek.ToString(),
            AppointmentType = input.AppointmentType.ToString(),
            ReminderSent = input.HasReminderSent,
            HasInsurance = input.HasInsurance,
            ExampleWeight = 1.0f
        };
    }

    public Task<bool> TryLoadNoShowModelAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return Task.FromResult(TryGetPredictionEngine(out _));
    }

    public async Task<NoShowModelEvaluationResult?> GetLatestNoShowModelMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metricsPath = GetMetricsPath();
        if (File.Exists(metricsPath))
        {
            var json = await File.ReadAllTextAsync(metricsPath, cancellationToken);
            var result = JsonSerializer.Deserialize<NoShowModelEvaluationResult>(json);
            if (result is not null)
            {
                return result;
            }
        }

        var latestAudit = await _db.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityName == "PredictionResult" && a.ActionType == "NoShowModelMetricsStored")
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(latestAudit?.ChangesJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<NoShowModelEvaluationResult>(latestAudit.ChangesJson);
    }

    public Task<NoShowPredictionOutput> PredictNoShowAsync(NoShowPredictionInput input)
    {
        _logger.LogInformation("Running no-show prediction for appointment type {Type}", input.AppointmentType);

        if (TryGetPredictionEngine(out var predictionEngine) && predictionEngine is not null)
        {
            var modelInput = MapInputToFeatureVector(input);
            var modelPrediction = predictionEngine.Predict(modelInput);
            var probability = NormalizeProbability(modelPrediction.Probability, modelPrediction.Score);
            var mlOutput = BuildPredictionOutput(probability, modelPrediction.Score, modelPrediction.PredictedLabel);
            _logger.LogInformation("ML no-show prediction result: Probability={Probability}, Risk={Risk}", mlOutput.Probability, mlOutput.RiskLevel);
            return Task.FromResult(mlOutput);
        }

        var probabilityFallback = ComputeRuleBasedProbability(input);
        var fallback = BuildPredictionOutput(probabilityFallback, probabilityFallback, probabilityFallback >= 0.5f);

        _logger.LogInformation("Fallback no-show prediction result: Probability={Probability}, Risk={Risk}", fallback.Probability, fallback.RiskLevel);
        return Task.FromResult(fallback);
    }

    public async Task<NoShowPredictionOutput> PredictNoShowForAppointmentAsync(Guid appointmentId, bool persistResult = true)
    {
        var appointment = await _db.Appointments
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment is null)
        {
            throw new ArgumentException($"Appointment {appointmentId} was not found.");
        }

        if (appointment.Patient is null)
        {
            throw new ArgumentException("Appointment patient data is required for prediction.");
        }

        var input = await BuildPredictionInputFromAppointmentAsync(appointment);
        var prediction = await PredictNoShowAsync(input);

        if (!persistResult)
        {
            return prediction;
        }

        appointment.IsPredictedNoShow = prediction.WillNoShow;
        appointment.NoShowProbability = prediction.Probability;

        var predictionEntity = new PredictionResult
        {
            AppointmentId = appointment.Id,
            ModelName = TryGetPredictionEngine(out _) ? "ML.NET-FastTree-NoShow-v2" : "RuleBased-NoShow-v1",
            PredictionType = "NoShow",
            ProbabilityScore = prediction.Probability,
            PredictedLabel = prediction.RiskLevel,
            InputFeaturesJson = JsonSerializer.Serialize(input),
            OutputJson = JsonSerializer.Serialize(prediction)
        };

        _db.PredictionResults.Add(predictionEntity);
        await _db.SaveChangesAsync();

        return prediction;
    }

    public async Task<NoShowDatasetGenerationResult> GenerateNoShowDatasetAsync(int rowCount = DefaultRows, CancellationToken cancellationToken = default)
    {
        var normalizedRows = Math.Clamp(rowCount, MinRows, MaxRows);
        var rows = await CreateDatasetRowsAsync(normalizedRows, cancellationToken);

        var artifactsDirectory = GetArtifactsDirectory();
        Directory.CreateDirectory(artifactsDirectory);
        var datasetPath = Path.Combine(artifactsDirectory, "no_show_training_data.csv");

        await PersistDatasetCsvAsync(rows, datasetPath, cancellationToken);

        var noShowCount = rows.Count(r => r.Label);
        var showCount = rows.Count - noShowCount;
        var noShowRate = rows.Count == 0 ? 0 : Math.Round((decimal)noShowCount / rows.Count * 100, 2);

        _logger.LogInformation(
            "No-show dataset generated at {Path}. Rows: {Rows}, HistoricalRows: {HistoricalRows}, NoShows: {NoShows} ({Rate}%)",
            datasetPath,
            rows.Count,
            rows.Count(r => r.Source == "Historical"),
            noShowCount,
            noShowRate);

        return new NoShowDatasetGenerationResult
        {
            RequestedRows = rowCount,
            GeneratedRows = rows.Count,
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

        var resolvedDatasetPath = await EnsureDatasetAsync(datasetPath, cancellationToken);

        var mlContext = new MLContext(seed: 20260321);
        var data = LoadDataset(mlContext, resolvedDatasetPath);
        var split = mlContext.Data.TrainTestSplit(data, testFraction: 0.2, seed: 20260321);

        var featureMapping = BuildFeatureMappingPipeline(mlContext);
        var trainer = BuildTrainer(mlContext);
        var trainingPipeline = featureMapping.Append(trainer);

        var model = TrainModel(trainingPipeline, split.TrainSet);
        var metrics = EvaluateModel(mlContext, model, split.TestSet);

        var modelPath = PersistModel(mlContext, model, split.TrainSet, artifactsDirectory);
        CacheModel(mlContext, model, modelPath);

        var trainCount = mlContext.Data.CreateEnumerable<NoShowMlDataPoint>(split.TrainSet, reuseRowObject: false, ignoreMissingColumns: true).Count();
        var testCount = mlContext.Data.CreateEnumerable<NoShowMlDataPoint>(split.TestSet, reuseRowObject: false, ignoreMissingColumns: true).Count();

        var evaluation = BuildEvaluationResult(metrics, trainCount, testCount, modelPath, resolvedDatasetPath);
        await PersistLatestMetricsAsync(evaluation, cancellationToken);

        _logger.LogInformation(
            "No-show model trained. Accuracy={Accuracy:F4}, Precision={Precision:F4}, Recall={Recall:F4}, F1={F1:F4}, AUC={Auc:F4}. Model saved: {ModelPath}",
            evaluation.Accuracy,
            evaluation.Precision,
            evaluation.Recall,
            evaluation.F1Score,
            evaluation.Auc,
            modelPath);

        return new NoShowTrainingResult
        {
            DatasetPath = resolvedDatasetPath,
            ModelPath = modelPath,
            TrainRowCount = trainCount,
            TestRowCount = testCount,
            Metrics = new NoShowTrainingMetrics
            {
                Accuracy = evaluation.Accuracy,
                Precision = evaluation.Precision,
                Recall = evaluation.Recall,
                F1Score = evaluation.F1Score,
                Auc = evaluation.Auc
            },
            Evaluation = evaluation
        };
    }

    private async Task<NoShowPredictionInput> BuildPredictionInputFromAppointmentAsync(Appointment appointment)
    {
        var previousAppointments = await _db.Appointments
            .Where(a => a.PatientId == appointment.PatientId
                        && a.AppointmentDate < appointment.AppointmentDate
                        && a.Id != appointment.Id)
            .ToListAsync();

        return new NoShowPredictionInput
        {
            PatientAge = CalculatePatientAgeAtDate(appointment.Patient!.DateOfBirth, appointment.AppointmentDate),
            PreviousNoShowCount = previousAppointments.Count(a => a.Status == AppointmentStatus.NoShow),
            PreviousCompletedCount = previousAppointments.Count(a => a.Status == AppointmentStatus.Completed),
            DaysBetweenBookingAndAppointment = Math.Max(0, (appointment.AppointmentDate.Date - appointment.CreatedAt.Date).Days),
            DayOfWeek = appointment.AppointmentDate.DayOfWeek,
            AppointmentType = appointment.AppointmentType,
            HasInsurance = appointment.Patient.HasInsurance,
            HasReminderSent = appointment.ReminderSent
        };
    }

    private async Task<string> EnsureDatasetAsync(string? datasetPath, CancellationToken cancellationToken)
    {
        var artifactsDirectory = GetArtifactsDirectory();
        var resolvedDatasetPath = string.IsNullOrWhiteSpace(datasetPath)
            ? Path.Combine(artifactsDirectory, "no_show_training_data.csv")
            : datasetPath;

        if (!File.Exists(resolvedDatasetPath))
        {
            var generationResult = await GenerateNoShowDatasetAsync(DefaultRows, cancellationToken);
            resolvedDatasetPath = generationResult.DatasetPath;
        }

        return resolvedDatasetPath;
    }

    private IDataView LoadDataset(MLContext mlContext, string datasetPath)
    {
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

        return loader.Load(datasetPath);
    }

    private static IEstimator<ITransformer> BuildFeatureMappingPipeline(MLContext mlContext)
    {
        return mlContext.Transforms.Categorical.OneHotEncoding(new[]
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
                "AppointmentTypeEncoded"));
    }

    private static IEstimator<ITransformer> BuildTrainer(MLContext mlContext)
    {
        return mlContext.BinaryClassification.Trainers.FastTree(new Microsoft.ML.Trainers.FastTree.FastTreeBinaryTrainer.Options
        {
            LabelColumnName = "Label",
            FeatureColumnName = "Features",
            ExampleWeightColumnName = nameof(NoShowMlDataPoint.ExampleWeight),
            NumberOfLeaves = 24,
            NumberOfTrees = 240,
            MinimumExampleCountPerLeaf = 8,
            LearningRate = 0.08
        });
    }

    private static ITransformer TrainModel(IEstimator<ITransformer> pipeline, IDataView trainSet)
    {
        return pipeline.Fit(trainSet);
    }

    private static BinaryClassificationMetrics EvaluateModel(MLContext mlContext, ITransformer model, IDataView testSet)
    {
        var predictions = model.Transform(testSet);
        return mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: "Label");
    }

    private static string PersistModel(MLContext mlContext, ITransformer model, IDataView trainSet, string artifactsDirectory)
    {
        var modelPath = Path.Combine(artifactsDirectory, "no_show_model.zip");
        mlContext.Model.Save(model, trainSet.Schema, modelPath);
        return modelPath;
    }

    private void CacheModel(MLContext mlContext, ITransformer model, string modelPath)
    {
        lock (ModelSyncLock)
        {
            _cachedModel = model;
            _cachedPredictionEngine = mlContext.Model.CreatePredictionEngine<NoShowMlDataPoint, NoShowMlPrediction>(model);
            _cachedModelPath = modelPath;
        }
    }

    private NoShowModelEvaluationResult BuildEvaluationResult(
        BinaryClassificationMetrics metrics,
        int trainCount,
        int testCount,
        string modelPath,
        string datasetPath)
    {
        return new NoShowModelEvaluationResult
        {
            Accuracy = metrics.Accuracy,
            Precision = metrics.PositivePrecision,
            Recall = metrics.PositiveRecall,
            F1Score = metrics.F1Score,
            Auc = metrics.AreaUnderRocCurve,
            ConfusionMatrix = BuildConfusionMatrixCounts(metrics),
            TrainRowCount = trainCount,
            TestRowCount = testCount,
            ModelPath = modelPath,
            DatasetPath = datasetPath,
            TrainingTimestampUtc = DateTime.UtcNow
        };
    }

    private async Task PersistLatestMetricsAsync(NoShowModelEvaluationResult evaluation, CancellationToken cancellationToken)
    {
        var metricsPath = GetMetricsPath();
        var json = JsonSerializer.Serialize(evaluation, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(metricsPath, json, cancellationToken);

        _db.AuditLogs.Add(new AuditLog
        {
            EntityName = "PredictionResult",
            ActionType = "NoShowModelMetricsStored",
            Description = "Latest no-show model training metrics persisted",
            ChangesJson = json
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<NoShowMlDataPoint>> CreateDatasetRowsAsync(int targetRows, CancellationToken cancellationToken)
    {
        var historicalRows = await BuildHistoricalRowsAsync(cancellationToken);
        var rows = new List<NoShowMlDataPoint>(targetRows);

        if (historicalRows.Count >= targetRows)
        {
            rows.AddRange(historicalRows.TakeLast(targetRows));
        }
        else
        {
            rows.AddRange(historicalRows);
            var syntheticRows = GenerateSyntheticRows(targetRows - historicalRows.Count);
            rows.AddRange(syntheticRows);
        }

        ApplyClassWeights(rows);
        return rows;
    }

    private async Task<List<NoShowMlDataPoint>> BuildHistoricalRowsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var historicalAppointments = await _db.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Where(a => (a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.NoShow)
                        && a.AppointmentDate < now)
            .OrderBy(a => a.PatientId)
            .ThenBy(a => a.AppointmentDate)
            .ToListAsync(cancellationToken);

        var rows = new List<NoShowMlDataPoint>(historicalAppointments.Count);
        var historyByPatient = new Dictionary<Guid, (int noShowCount, int completedCount)>();

        foreach (var appointment in historicalAppointments)
        {
            if (appointment.Patient is null)
            {
                continue;
            }

            historyByPatient.TryGetValue(appointment.PatientId, out var state);

            rows.Add(new NoShowMlDataPoint
            {
                PatientAge = CalculatePatientAgeAtDate(appointment.Patient.DateOfBirth, appointment.AppointmentDate),
                PreviousNoShows = state.noShowCount,
                PreviousCompletedVisits = state.completedCount,
                DaysBetweenBookingAndAppointment = Math.Max(0, (appointment.AppointmentDate.Date - appointment.CreatedAt.Date).Days),
                DayOfWeek = appointment.AppointmentDate.DayOfWeek.ToString(),
                AppointmentType = appointment.AppointmentType.ToString(),
                ReminderSent = appointment.ReminderSent,
                HasInsurance = appointment.Patient.HasInsurance,
                Label = appointment.Status == AppointmentStatus.NoShow,
                Source = "Historical"
            });

            if (appointment.Status == AppointmentStatus.NoShow)
            {
                state.noShowCount++;
            }
            else if (appointment.Status == AppointmentStatus.Completed)
            {
                state.completedCount++;
            }

            historyByPatient[appointment.PatientId] = state;
        }

        return rows;
    }

    private static List<NoShowMlDataPoint> GenerateSyntheticRows(int count)
    {
        var random = new Random(20260321);
        var rows = new List<NoShowMlDataPoint>(count);
        var appointmentTypes = Enum.GetNames<AppointmentType>();

        for (int i = 0; i < count; i++)
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

            rows.Add(new NoShowMlDataPoint
            {
                PatientAge = patientAge,
                PreviousNoShows = previousNoShows,
                PreviousCompletedVisits = previousCompletedVisits,
                DaysBetweenBookingAndAppointment = daysBetweenBookingAndAppointment,
                DayOfWeek = dayOfWeek,
                AppointmentType = appointmentType,
                ReminderSent = reminderSent,
                HasInsurance = hasInsurance,
                Label = random.NextDouble() < probability,
                Source = "Synthetic"
            });
        }

        return rows;
    }

    private static void ApplyClassWeights(List<NoShowMlDataPoint> rows)
    {
        var noShowCount = rows.Count(r => r.Label);
        var showCount = rows.Count - noShowCount;

        if (noShowCount == 0 || showCount == 0)
        {
            foreach (var row in rows)
            {
                row.ExampleWeight = 1.0f;
            }

            return;
        }

        var noShowWeight = MathF.Max(1.0f, (float)showCount / noShowCount);
        foreach (var row in rows)
        {
            row.ExampleWeight = row.Label ? noShowWeight : 1.0f;
        }
    }

    private static async Task PersistDatasetCsvAsync(List<NoShowMlDataPoint> rows, string datasetPath, CancellationToken cancellationToken)
    {
        var csv = new StringBuilder();
        csv.AppendLine("PatientAge,PreviousNoShows,PreviousCompletedVisits,DaysBetweenBookingAndAppointment,DayOfWeek,AppointmentType,ReminderSent,HasInsurance,Label,ExampleWeight");

        foreach (var row in rows)
        {
            csv.AppendLine(string.Join(',',
                row.PatientAge.ToString(CultureInfo.InvariantCulture),
                row.PreviousNoShows.ToString(CultureInfo.InvariantCulture),
                row.PreviousCompletedVisits.ToString(CultureInfo.InvariantCulture),
                row.DaysBetweenBookingAndAppointment.ToString(CultureInfo.InvariantCulture),
                row.DayOfWeek,
                row.AppointmentType,
                row.ReminderSent.ToString().ToLowerInvariant(),
                row.HasInsurance.ToString().ToLowerInvariant(),
                row.Label.ToString().ToLowerInvariant(),
                row.ExampleWeight.ToString(CultureInfo.InvariantCulture)));
        }

        await File.WriteAllTextAsync(datasetPath, csv.ToString(), cancellationToken);
    }

    private string GetArtifactsDirectory()
    {
        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "ml-artifacts", "no-show"));
    }

    private string GetMetricsPath()
    {
        return Path.Combine(GetArtifactsDirectory(), "no_show_model_metrics.json");
    }

    private bool TryGetPredictionEngine(out PredictionEngine<NoShowMlDataPoint, NoShowMlPrediction>? predictionEngine)
    {
        var modelPath = Path.Combine(GetArtifactsDirectory(), "no_show_model.zip");
        if (!File.Exists(modelPath))
        {
            predictionEngine = null;
            return false;
        }

        if (_cachedPredictionEngine is not null && string.Equals(_cachedModelPath, modelPath, StringComparison.OrdinalIgnoreCase))
        {
            predictionEngine = _cachedPredictionEngine;
            return true;
        }

        lock (ModelSyncLock)
        {
            if (_cachedPredictionEngine is not null && string.Equals(_cachedModelPath, modelPath, StringComparison.OrdinalIgnoreCase))
            {
                predictionEngine = _cachedPredictionEngine;
                return true;
            }

            using var stream = File.OpenRead(modelPath);
            _cachedModel = _mlContext.Model.Load(stream, out _);
            _cachedPredictionEngine = _mlContext.Model.CreatePredictionEngine<NoShowMlDataPoint, NoShowMlPrediction>(_cachedModel);
            _cachedModelPath = modelPath;
            _logger.LogInformation("Loaded no-show ML model from {ModelPath}", modelPath);
        }

        predictionEngine = _cachedPredictionEngine;
        return predictionEngine is not null;
    }

    private static float NormalizeProbability(float probability, float score)
    {
        if (probability > 0 && probability < 1)
        {
            return probability;
        }

        var sigmoid = 1f / (1f + MathF.Exp(-score));
        return Math.Clamp(sigmoid, 0.0001f, 0.9999f);
    }

    private static float ComputeRuleBasedProbability(NoShowPredictionInput input)
    {
        double score = 0.10;

        score += Math.Min(0.45, input.PreviousNoShowCount * 0.12);
        score += input.DaysBetweenBookingAndAppointment >= 21 ? 0.10 : input.DaysBetweenBookingAndAppointment >= 10 ? 0.05 : 0.0;
        score += input.PreviousCompletedCount >= 6 ? -0.08 : input.PreviousCompletedCount >= 3 ? -0.04 : 0.0;
        score += input.HasReminderSent ? -0.07 : 0.12;
        score += input.HasInsurance ? -0.03 : 0.06;
        score += input.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Friday ? 0.03 : 0.0;
        score += input.AppointmentType is AppointmentType.Consultation or AppointmentType.Dental ? 0.03 : 0.0;

        return (float)Math.Clamp(score, 0.02, 0.95);
    }

    private static NoShowPredictionOutput BuildPredictionOutput(float probability, float score, bool predictedLabel)
    {
        var risk = probability < 0.40f ? "Low" : probability <= 0.70f ? "Medium" : "High";
        var recommendation = risk switch
        {
            "High" => "High risk: call patient today, reconfirm attendance, trigger same-day reminder, and prepare waitlist backfill if unconfirmed.",
            "Medium" => "Medium risk: send an additional reminder and request explicit confirmation 24 hours before appointment.",
            _ => "Low risk: continue standard reminder workflow."
        };

        return new NoShowPredictionOutput
        {
            WillNoShow = predictedLabel || probability > 0.5f,
            Probability = Math.Round((decimal)probability, 4),
            Score = Math.Round((decimal)score, 4),
            RiskLevel = risk,
            Recommendation = recommendation
        };
    }

    private static int CalculatePatientAgeAtDate(DateTime dateOfBirth, DateTime referenceDate)
    {
        var age = (int)((referenceDate.Date - dateOfBirth.Date).TotalDays / 365.25);
        return Math.Clamp(age, 0, 120);
    }

    private static NoShowConfusionMatrixCounts? BuildConfusionMatrixCounts(BinaryClassificationMetrics metrics)
    {
        var counts = metrics.ConfusionMatrix?.Counts;
        if (counts is null || counts.Count < 2 || counts[0].Count < 2 || counts[1].Count < 2)
        {
            return null;
        }

        return new NoShowConfusionMatrixCounts
        {
            TrueNegatives = (int)Math.Round(counts[0][0]),
            FalsePositives = (int)Math.Round(counts[0][1]),
            FalseNegatives = (int)Math.Round(counts[1][0]),
            TruePositives = (int)Math.Round(counts[1][1])
        };
    }
}
