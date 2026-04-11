# No-Show Prediction (ML.NET)

## Purpose

This document describes the implemented no-show prediction workflow used in the capstone system. The objective is operational support for scheduling teams, not clinical diagnosis.

## Implementation Scope

The prediction subsystem is implemented entirely in .NET using ML.NET and local storage. It supports:

- dataset generation
- model training and evaluation
- direct prediction from feature input
- prediction from appointment context
- optional persistence of prediction results
- fallback scoring when model artifacts are unavailable

## Implemented Components

- Service: ClinicManagementSystem.Services/Implementations/PredictionService.cs
- API controller: ClinicManagementSystem.API/Controllers/PredictionsController.cs
- Stored prediction model entity: ClinicManagementSystem.Models/Entities/PredictionResult.cs
- UI pages:
  - PredictionLab.razor
  - PredictionMetrics.razor

## Input Features

The model consumes the following core fields:

- PatientAge
- DaysBetweenBookingAndAppointment
- PreviousNoShows
- PreviousCompletedVisits
- AppointmentType
- DayOfWeek
- ReminderSent
- HasInsurance

Target label:

- Label (true for no-show, false for attended)

## Dataset Generation Strategy

Dataset generation prioritizes historical appointment data and supplements with synthetic rows when needed.

Current behavior:

1. Pull historical appointment patterns from the local database.
2. Derive temporal and patient history features.
3. Fill shortfall with deterministic synthetic samples.
4. Apply weighting logic to manage class imbalance.

Supported row range for generation endpoint:

- 500 to 2000 rows

## Model Training Pipeline

Training uses an ML.NET FastTree binary classifier with a repeatable preparation pipeline:

1. Dataset load and split
2. Feature engineering and encoding
3. Model fit
4. Evaluation metrics capture
5. Artifact persistence to local storage

## Inference Paths

### Direct Inference

- Endpoint: POST /api/Predictions/no-show
- Input: NoShowPredictionInput
- Output: NoShowPredictionOutput

### Appointment-Based Inference

- Endpoint: POST /api/Predictions/no-show/appointment/{appointmentId}
- Supports persist query flag for saving PredictionResult

### Fallback Behavior

If model loading is unavailable, PredictionService uses deterministic fallback scoring logic so the workflow remains functional.

## Output Shape

Prediction responses include:

- WillNoShow
- Probability
- Score
- RiskLevel
- Recommendation

Risk levels are currently mapped from probability thresholds:

- below 0.40: Low
- 0.40 to 0.70: Medium
- above 0.70: High

## Model Metrics and Artifacts

Training endpoint returns and stores evaluation metrics, including:

- Accuracy
- Precision
- Recall
- F1Score
- Auc
- TrainRowCount
- TestRowCount
- DatasetPath
- ModelPath
- TrainingTimestampUtc

Local artifacts are written under ml-artifacts/no-show:

- no_show_training_data.csv
- no_show_model.zip
- no_show_model_metrics.json

## API Endpoint Summary

| Endpoint                                             | Method | Purpose                              |
| ---------------------------------------------------- | ------ | ------------------------------------ |
| /api/Predictions/no-show                             | POST   | Predict from explicit feature input  |
| /api/Predictions/no-show/appointment/{appointmentId} | POST   | Predict from appointment context     |
| /api/Predictions/no-show/dataset                     | POST   | Generate dataset rows for training   |
| /api/Predictions/no-show/train                       | POST   | Train and evaluate local model       |
| /api/Predictions/no-show/metrics/latest              | GET    | Retrieve latest stored model metrics |

## Validation and Testing Coverage

Current automated tests cover:

- deterministic feature mapping
- dataset generation format
- training result validity
- model load behavior
- fallback predictions when model is absent
- appointment-based prediction persistence behavior

Refer to docs/TESTING.md for exact test classes and commands.

## Limitations

- This is a capstone-grade operational model, not a certified clinical decision-support model.
- Training data includes synthetic and local development records.
- Metrics should be interpreted as technical validation signals, not production clinical performance claims.
