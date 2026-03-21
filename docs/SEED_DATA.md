# Seed Data

## Seed Mechanism

Seed implementation is in:

- [ClinicManagementSystem.Data/DevelopmentDataSeeder.cs](../ClinicManagementSystem.Data/DevelopmentDataSeeder.cs)

Startup trigger points:

- [ClinicManagementSystem.API/Program.cs](../ClinicManagementSystem.API/Program.cs)
- [ClinicManagementSystem.Blazor/Program.cs](../ClinicManagementSystem.Blazor/Program.cs)

## Current Trigger Rule

Seed runs only when:

1. migrations exist
2. pending migrations are detected
3. migration is applied at startup

If no pending migrations, seed is skipped.

## Seeded Entities and Volumes

- Patients: 20
- StaffMembers: 6
- Appointments: 45
- VisitRecords: 16
- Notifications: 32
- ClinicSettings: 1
- PredictionResults: derived from predicted no-show records

## Seed Data Quality

- Deterministic IDs generated from stable keys
- Realistic relationships and foreign keys
- Mix of past/current/future appointments
- Appointment statuses include Scheduled, Confirmed, Completed, Cancelled, NoShow

## Verification Queries

Use API endpoints or SQL counts to verify rows after migration-triggered seed.

Example SQL checks:

```sql
SELECT COUNT(*) AS Patients FROM Patients WHERE IsDeleted = 0;
SELECT COUNT(*) AS StaffMembers FROM StaffMembers WHERE IsDeleted = 0;
SELECT COUNT(*) AS Appointments FROM Appointments WHERE IsDeleted = 0;
```
