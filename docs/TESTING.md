# Testing Status

## Current State

Automated test projects are not yet included in this repository.

## Manual Verification Performed

- Build verification on all projects
- API endpoint checks for core modules
- UI flow checks for:
  - patient CRUD
  - staff CRUD
  - appointment create/update/delete
  - scheduling conflict warnings
  - dashboard cards and trend/workload
  - prediction risk display and ML metrics page

## Recommended Next Additions

- Unit tests for services:
  - AppointmentService conflict rules
  - DashboardService summary calculations
  - PredictionService risk mapping and fallback behavior
- Integration tests for API endpoints
- UI smoke tests for main scheduling and prediction workflows

## Evidence Placeholder (to fill per sprint)

- Build status: pass/fail
- API latency sample for top 5 endpoints
- Regression checklist coverage percent
- Defect count by module
