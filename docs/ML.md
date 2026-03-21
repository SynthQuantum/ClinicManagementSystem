# ML.NET No-Show Prediction

## Scope

This prototype uses ML.NET only (no Python, no external AI APIs) to estimate appointment no-show risk.

## Implemented Components

- Synthetic dataset generator in [ClinicManagementSystem.Services/Implementations/PredictionService.cs](../ClinicManagementSystem.Services/Implementations/PredictionService.cs)
- Training/evaluation pipeline in the same service
- Appointment inference endpoint in [ClinicManagementSystem.API/Controllers/PredictionsController.cs](../ClinicManagementSystem.API/Controllers/PredictionsController.cs)
- UI metrics page at /predictions/metrics

## Input Features

- PatientAge
- PreviousNoShows
- PreviousCompletedVisits
- DaysBetweenBookingAndAppointment
- DayOfWeek
- AppointmentType
- ReminderSent
- HasInsurance

## Target Label

- Label (binary no-show target)

## Dataset Strategy

- Deterministic synthetic generation with fixed random seed
- Supported range: 500 to 2000 rows
- Risk logic includes:
  - prior no-show history impact
  - booking lead-time effect
  - reminder impact
  - insurance signal
  - day-of-week and appointment-type adjustments
- Class imbalance handling via ExampleWeight in training

## Trainer and Evaluation

- Trainer: FastTreeBinaryClassification
- Split: 80/20 train/test
- Metrics returned:
  - Accuracy
  - Precision
  - Recall
  - F1Score
  - AUC

## Artifact Paths

- CSV dataset: [ml-artifacts/no-show/no_show_training_data.csv](../ml-artifacts/no-show/no_show_training_data.csv)
- Model: [ml-artifacts/no-show/no_show_model.zip](../ml-artifacts/no-show/no_show_model.zip)

## API Usage

- POST /api/Predictions/no-show/dataset?rows=1200
- POST /api/Predictions/no-show/train
- POST /api/Predictions/no-show/appointment/{appointmentId}?persist=true

## Risk Mapping Used in UI

- Probability < 0.40: Low
- 0.40 to 0.70: Medium
- > 0.70: High

## Current Limitation

- Training data is synthetic; production quality depends on replacing with validated historical data.
