# ML.NET No-Show Prediction

## Scope

The no-show subsystem uses ML.NET only (no Python and no external AI APIs). It supports local training, evaluation, model persistence, and appointment-level inference.

## Implemented Components

- Service implementation: [ClinicManagementSystem.Services/Implementations/PredictionService.cs](../ClinicManagementSystem.Services/Implementations/PredictionService.cs)
- DTO contracts: [ClinicManagementSystem.Models/DTOs](../ClinicManagementSystem.Models/DTOs)
- API endpoints: [ClinicManagementSystem.API/Controllers/PredictionsController.cs](../ClinicManagementSystem.API/Controllers/PredictionsController.cs)
- Metrics UI page: `/predictions/metrics`

## Feature List

The model uses the following features for binary no-show prediction:

- `PatientAge`
- `DaysBetweenBookingAndAppointment`
- `PreviousNoShows`
- `PreviousCompletedVisits`
- `AppointmentType`
- `DayOfWeek`
- `ReminderSent`
- `HasInsurance`

Target label:

- `Label` (`true` for no-show, `false` otherwise)

## Dataset Creation Strategy

Dataset generation now prioritizes historical appointments when available:

- Uses completed and no-show historical appointments from the database.
- Builds per-patient temporal history so `PreviousNoShows` and `PreviousCompletedVisits` are based on earlier appointments only.
- Computes booking lead-time using appointment `CreatedAt` and appointment date.
- Uses patient insurance flag where available.

If historical rows are insufficient for the requested dataset size, synthetic rows are generated to fill the gap.

- Supported range: `500` to `2000` rows
- Deterministic random seed for reproducible synthetic augmentation
- Class imbalance handled via `ExampleWeight`

## Training Flow (Refactored)

Training logic is separated into explicit stages:

1. Dataset creation/load
2. Feature mapping pipeline (encoding + feature concatenation)
3. Model training (FastTree binary classifier)
4. Evaluation
5. Persistence (model + latest metrics metadata)

## Evaluation Output

Latest evaluation payload includes:

- Accuracy
- Precision
- Recall
- F1Score
- AUC
- Confusion matrix counts (true/false positives/negatives when available)
- Train row count
- Test row count
- Model path
- Dataset path
- Training timestamp (UTC)

## Persistence

Artifacts written to [ml-artifacts/no-show](../ml-artifacts/no-show):

- `no_show_training_data.csv`
- `no_show_model.zip`
- `no_show_model_metrics.json`

Additionally, latest model metrics are persisted in database audit logs (`AuditLogs`) under:

- `EntityName = "PredictionResult"`
- `ActionType = "NoShowModelMetricsStored"`

## API Endpoints

- `POST /api/Predictions/no-show/dataset?rows=1200`
- `POST /api/Predictions/no-show/train`
- `GET /api/Predictions/no-show/metrics/latest`
- `POST /api/Predictions/no-show/appointment/{appointmentId}?persist=true`

## Risk Buckets and Recommendation Behavior

- Probability `< 0.40`: Low
- Probability `0.40 - 0.70`: Medium
- Probability `> 0.70`: High

High-risk recommendations now return stronger operational guidance (direct call, explicit reconfirmation, same-day reminder, and waitlist backfill prep).

## Evaluation Notes and Limitations

- Synthetic/local training remains a limitation for production-grade calibration.
- Historical training quality depends on appointment status integrity and reminder logging quality.
- Model metrics from synthetic-augmented data should be treated as local validation signals, not clinical-grade performance claims.
