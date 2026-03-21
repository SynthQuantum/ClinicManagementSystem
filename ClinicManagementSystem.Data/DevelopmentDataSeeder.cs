using System.Security.Cryptography;
using System.Text;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Data;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(ClinicDbContext db, ILogger logger)
    {
        var today = DateTime.UtcNow.Date;

        var staff = BuildStaff();
        var patients = BuildPatients(today);
        var clinicSettings = BuildClinicSettings();
        var appointments = BuildAppointments(patients, staff, today);
        var visitRecords = BuildVisitRecords(appointments, today);
        var notifications = BuildNotifications(appointments, today);
        var predictionResults = BuildPredictionResults(appointments, today);

        var existingStaffIds = await GetExistingIdsAsync(db.StaffMembers.IgnoreQueryFilters());
        var existingPatientIds = await GetExistingIdsAsync(db.Patients.IgnoreQueryFilters());
        var existingAppointmentIds = await GetExistingIdsAsync(db.Appointments.IgnoreQueryFilters());
        var existingVisitRecordIds = await GetExistingIdsAsync(db.VisitRecords.IgnoreQueryFilters());
        var existingNotificationIds = await GetExistingIdsAsync(db.Notifications.IgnoreQueryFilters());
        var existingPredictionIds = await GetExistingIdsAsync(db.PredictionResults.IgnoreQueryFilters());
        var existingClinicSettingIds = await GetExistingIdsAsync(db.ClinicSettings.IgnoreQueryFilters());

        var staffToSeed = staff.Where(x => !existingStaffIds.Contains(x.Id)).ToList();
        var patientsToSeed = patients.Where(x => !existingPatientIds.Contains(x.Id)).ToList();
        var appointmentsToSeed = appointments.Where(x => !existingAppointmentIds.Contains(x.Id)).ToList();
        var visitRecordsToSeed = visitRecords.Where(x => !existingVisitRecordIds.Contains(x.Id)).ToList();
        var notificationsToSeed = notifications.Where(x => !existingNotificationIds.Contains(x.Id)).ToList();
        var predictionResultsToSeed = predictionResults.Where(x => !existingPredictionIds.Contains(x.Id)).ToList();
        var clinicSettingsToSeed = existingClinicSettingIds.Contains(clinicSettings.Id)
            ? new List<ClinicSettings>()
            : new List<ClinicSettings> { clinicSettings };

        if (staffToSeed.Count == 0
            && patientsToSeed.Count == 0
            && appointmentsToSeed.Count == 0
            && visitRecordsToSeed.Count == 0
            && notificationsToSeed.Count == 0
            && predictionResultsToSeed.Count == 0
            && clinicSettingsToSeed.Count == 0)
        {
            logger.LogInformation("Seed data already exists. No new records inserted.");
            return;
        }

        db.StaffMembers.AddRange(staffToSeed);
        db.Patients.AddRange(patientsToSeed);
        db.ClinicSettings.AddRange(clinicSettingsToSeed);
        db.Appointments.AddRange(appointmentsToSeed);
        db.VisitRecords.AddRange(visitRecordsToSeed);
        db.Notifications.AddRange(notificationsToSeed);
        db.PredictionResults.AddRange(predictionResultsToSeed);

        await db.SaveChangesAsync();

        logger.LogInformation(
            "Seed completed. Added Patients: {Patients}, Staff: {Staff}, Appointments: {Appointments}, Notifications: {Notifications}, VisitRecords: {VisitRecords}, PredictionResults: {PredictionResults}, ClinicSettings: {ClinicSettings}",
            patientsToSeed.Count,
            staffToSeed.Count,
            appointmentsToSeed.Count,
            notificationsToSeed.Count,
            visitRecordsToSeed.Count,
            predictionResultsToSeed.Count,
            clinicSettingsToSeed.Count);
    }

    private static Task<HashSet<Guid>> GetExistingIdsAsync<TEntity>(IQueryable<TEntity> query)
        where TEntity : BaseEntity
    {
        return query.Select(x => x.Id).ToHashSetAsync();
    }

    private static List<StaffMember> BuildStaff()
    {
        var rows = new[]
        {
            ("Amina", "Rahman", UserRole.Doctor, "Family Medicine"),
            ("Liam", "Patel", UserRole.Doctor, "Internal Medicine"),
            ("Sophia", "Chen", UserRole.Doctor, "Pediatrics"),
            ("Noah", "Okafor", UserRole.Nurse, "Triage"),
            ("Maya", "Fernandez", UserRole.Nurse, "Chronic Care"),
            ("Ethan", "Williams", UserRole.Receptionist, "Front Desk")
        };

        var list = new List<StaffMember>(rows.Length);
        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            var emailSlug = $"{row.Item1}.{row.Item2}".ToLowerInvariant();
            list.Add(new StaffMember
            {
                Id = CreateDeterministicGuid($"staff-{i + 1}"),
                FirstName = row.Item1,
                LastName = row.Item2,
                Email = $"{emailSlug}@clinicdemo.local",
                PhoneNumber = $"+1-555-200-{(100 + i):000}",
                Role = row.Item3,
                Specialty = row.Item4,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        return list;
    }

    private static List<Patient> BuildPatients(DateTime today)
    {
        var rows = new[]
        {
            ("Emma", "Johnson", Gender.Female), ("Olivia", "Nguyen", Gender.Female),
            ("Ava", "Smith", Gender.Female), ("Isabella", "Brown", Gender.Female),
            ("Mia", "Garcia", Gender.Female), ("Amelia", "Martinez", Gender.Female),
            ("Harper", "Davis", Gender.Female), ("Evelyn", "Moore", Gender.Female),
            ("Lucas", "Taylor", Gender.Male), ("Mason", "Anderson", Gender.Male),
            ("Elijah", "Thomas", Gender.Male), ("Logan", "Jackson", Gender.Male),
            ("James", "White", Gender.Male), ("Benjamin", "Harris", Gender.Male),
            ("Henry", "Clark", Gender.Male), ("Alexander", "Lewis", Gender.Male),
            ("Aria", "Walker", Gender.Female), ("Daniel", "Hall", Gender.Male),
            ("Layla", "Allen", Gender.Female), ("Samuel", "Young", Gender.Male)
        };

        var bloodTypes = new[] { "A+", "A-", "B+", "B-", "AB+", "O+", "O-" };
        var list = new List<Patient>(rows.Length);

        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            var first = row.Item1;
            var last = row.Item2;
            var hasInsurance = i % 4 != 0;
            var dob = today.AddYears(-(22 + (i % 45))).AddDays(i * -27);
            var email = $"{first}.{last}".ToLowerInvariant();

            list.Add(new Patient
            {
                Id = CreateDeterministicGuid($"patient-{i + 1}"),
                FirstName = first,
                LastName = last,
                DateOfBirth = dob,
                Gender = row.Item3,
                PhoneNumber = $"+1-555-100-{(100 + i):000}",
                Email = $"{email}@maildemo.local",
                Address = $"{100 + i} Cedar Street",
                BloodType = bloodTypes[i % bloodTypes.Length],
                InsuranceProvider = hasInsurance ? (i % 2 == 0 ? "BlueShield" : "MediCare Plus") : null,
                InsurancePolicyNumber = hasInsurance ? $"PLN-{202600 + i}" : null,
                InsuranceExpiryDate = hasInsurance ? today.AddMonths(6 + i) : null,
                EmergencyContactName = $"Contact {first}",
                EmergencyContactPhone = $"+1-555-900-{(100 + i):000}",
                EmergencyContactRelationship = i % 2 == 0 ? "Spouse" : "Sibling",
                Notes = i % 5 == 0 ? "Requires follow-up reminders." : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        return list;
    }

    private static ClinicSettings BuildClinicSettings()
    {
        return new ClinicSettings
        {
            Id = CreateDeterministicGuid("clinic-settings-main"),
            ClinicName = "Downtown Family Health Clinic",
            Address = "120 Wellness Ave, Suite 200",
            PhoneNumber = "+1-555-010-2000",
            Email = "contact@downtownfamilyclinic.local",
            OpeningTime = new TimeSpan(8, 0, 0),
            ClosingTime = new TimeSpan(18, 0, 0),
            DefaultAppointmentDurationMinutes = 30,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static List<Appointment> BuildAppointments(IReadOnlyList<Patient> patients, IReadOnlyList<StaffMember> staff, DateTime today)
    {
        var list = new List<Appointment>(45);
        var appointmentTypes = Enum.GetValues<AppointmentType>();

        for (int i = 0; i < 45; i++)
        {
            var patient = patients[i % patients.Count];
            var doctor = staff[i % 3];
            var dayOffset = (i % 31) - 15;
            var date = today.AddDays(dayOffset);
            var startHour = 8 + ((i * 2) % 9);
            var startMinute = (i % 2) * 30;
            var duration = i % 3 == 0 ? 30 : i % 3 == 1 ? 45 : 60;
            var status = ResolveStatus(dayOffset, i);

            var appointment = new Appointment
            {
                Id = CreateDeterministicGuid($"appointment-{i + 1}"),
                PatientId = patient.Id,
                StaffMemberId = doctor.Id,
                AppointmentType = appointmentTypes[i % appointmentTypes.Length],
                AppointmentDate = date,
                StartTime = new TimeSpan(startHour, startMinute, 0),
                EndTime = new TimeSpan(startHour, startMinute, 0).Add(TimeSpan.FromMinutes(duration)),
                Status = status,
                Reason = $"{appointmentTypes[i % appointmentTypes.Length]} consultation",
                Notes = i % 6 == 0 ? "Patient requested morning slot." : null,
                ReminderSent = dayOffset >= -1,
                IsPredictedNoShow = i % 7 == 0,
                NoShowProbability = i % 7 == 0 ? Math.Round(0.62m + (i % 3) * 0.1m, 4) : Math.Round(0.08m + (i % 5) * 0.05m, 4),
                PredictedDurationMinutes = duration,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            list.Add(appointment);
        }

        return list;
    }

    private static List<VisitRecord> BuildVisitRecords(IReadOnlyList<Appointment> appointments, DateTime today)
    {
        var completed = appointments.Where(a => a.Status == AppointmentStatus.Completed).Take(16).ToList();
        var list = new List<VisitRecord>(completed.Count);

        for (int i = 0; i < completed.Count; i++)
        {
            var appt = completed[i];
            list.Add(new VisitRecord
            {
                Id = CreateDeterministicGuid($"visit-record-{i + 1}"),
                PatientId = appt.PatientId,
                AppointmentId = appt.Id,
                StaffMemberId = appt.StaffMemberId,
                VisitDate = appt.AppointmentDate.AddHours(appt.StartTime.TotalHours),
                Diagnosis = i % 3 == 0 ? "Upper respiratory infection" : i % 3 == 1 ? "Hypertension follow-up" : "General wellness review",
                Treatment = i % 2 == 0 ? "Medication adjustment and hydration guidance" : "Lifestyle counseling and monitoring",
                Prescription = i % 4 == 0 ? "Amoxicillin 500mg" : i % 4 == 1 ? "Lisinopril 10mg" : null,
                Notes = today.Subtract(appt.AppointmentDate.Date).Days > 20 ? "Stable condition." : "Return in 2 weeks if symptoms persist.",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        return list;
    }

    private static List<Notification> BuildNotifications(IReadOnlyList<Appointment> appointments, DateTime today)
    {
        var list = new List<Notification>();

        foreach (var appt in appointments.Where(a => a.Status is AppointmentStatus.Scheduled or AppointmentStatus.Confirmed).Take(24))
        {
            list.Add(new Notification
            {
                Id = CreateDeterministicGuid($"notification-reminder-{appt.Id}"),
                AppointmentId = appt.Id,
                NotificationType = NotificationType.AppointmentReminder,
                Status = appt.AppointmentDate >= today ? NotificationStatus.Pending : NotificationStatus.Sent,
                Message = "Reminder: you have an upcoming clinic appointment.",
                ScheduledFor = appt.AppointmentDate.AddDays(-1).Add(appt.StartTime),
                SentAt = appt.AppointmentDate < today ? appt.AppointmentDate.AddDays(-1).Add(appt.StartTime) : null,
                Recipient = $"patient-{appt.PatientId}@maildemo.local",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        foreach (var appt in appointments.Where(a => a.Status == AppointmentStatus.Cancelled).Take(8))
        {
            list.Add(new Notification
            {
                Id = CreateDeterministicGuid($"notification-cancel-{appt.Id}"),
                AppointmentId = appt.Id,
                NotificationType = NotificationType.AppointmentCancellation,
                Status = NotificationStatus.Sent,
                Message = "Your appointment has been cancelled. Please contact reception to reschedule.",
                ScheduledFor = appt.AppointmentDate.AddDays(-2).Add(appt.StartTime),
                SentAt = appt.AppointmentDate.AddDays(-2).Add(appt.StartTime),
                Recipient = $"patient-{appt.PatientId}@maildemo.local",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        return list;
    }

    private static List<PredictionResult> BuildPredictionResults(IReadOnlyList<Appointment> appointments, DateTime today)
    {
        var list = new List<PredictionResult>();
        var candidates = appointments.Where(a => a.IsPredictedNoShow).Take(12).ToList();

        for (int i = 0; i < candidates.Count; i++)
        {
            var appt = candidates[i];
            var score = appt.NoShowProbability ?? 0.50m;
            list.Add(new PredictionResult
            {
                Id = CreateDeterministicGuid($"prediction-result-{i + 1}"),
                AppointmentId = appt.Id,
                ModelName = "NoShowRiskBaseline-v1",
                PredictionType = "NoShow",
                ProbabilityScore = score,
                PredictedDurationMinutes = appt.PredictedDurationMinutes,
                PredictedLabel = score >= 0.55m ? "HighRisk" : "LowRisk",
                InputFeaturesJson = "{\"Source\":\"DevSeed\",\"FeatureSet\":\"Baseline\"}",
                OutputJson = $"{{\"NoShowProbability\":{score},\"GeneratedOn\":\"{today:yyyy-MM-dd}\"}}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        return list;
    }

    private static AppointmentStatus ResolveStatus(int dayOffset, int index)
    {
        if (dayOffset <= -7)
        {
            return (index % 6) switch
            {
                0 => AppointmentStatus.NoShow,
                1 => AppointmentStatus.Cancelled,
                _ => AppointmentStatus.Completed
            };
        }

        if (dayOffset < 0)
        {
            return (index % 4) switch
            {
                0 => AppointmentStatus.Completed,
                1 => AppointmentStatus.Confirmed,
                2 => AppointmentStatus.Cancelled,
                _ => AppointmentStatus.NoShow
            };
        }

        if (dayOffset <= 2)
        {
            return (index % 3) switch
            {
                0 => AppointmentStatus.Confirmed,
                1 => AppointmentStatus.Scheduled,
                _ => AppointmentStatus.Completed
            };
        }

        return (index % 3) switch
        {
            0 => AppointmentStatus.Scheduled,
            1 => AppointmentStatus.Confirmed,
            _ => AppointmentStatus.Cancelled
        };
    }

    private static Guid CreateDeterministicGuid(string key)
    {
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = MD5.HashData(bytes);
        return new Guid(hash);
    }
}
