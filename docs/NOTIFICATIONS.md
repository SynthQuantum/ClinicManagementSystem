# Notification Reminder Workflow

## Overview

The clinic notification subsystem creates and delivers appointment reminder notifications using local, development-safe delivery adapters.

## Components

- Service orchestration: `INotificationService` / `NotificationService`
- Delivery abstraction:
  - `IEmailSender` -> `LoggingEmailSender`
  - `ISmsSender` -> `LoggingSmsSender`
- Background processing:
  - API hosted service `ReminderProcessingHostedService`

## Reminder Rules

- Primary reminder: 24 hours before appointment start.
- Optional second reminder: 2 hours before appointment start.
- Reminder creation only targets `Scheduled` and `Confirmed` appointments.

## Deduplication Rule

A reminder is not recreated if an existing notification already matches:

- same appointment
- same reminder timestamp (`ScheduledFor`)
- same recipient
- reminder notification type

## Status Lifecycle

- `Pending`: created and waiting for scheduled delivery time.
- `Sent`: successfully delivered by sender abstraction.
- `Failed`: sender rejected delivery; failure reason stored in `FailureReason`.

## Processing Paths

1. Automatic processing via API background service loop.
2. Manual processing via API endpoint `POST /api/notifications/process-reminders`.

## Monitoring APIs

- `GET /api/notifications/summary`
- `GET /api/notifications/pending`
- `GET /api/notifications/history`
- `POST /api/notifications/process-reminders`

## Blazor Monitoring

Page: `/notifications`

Shows:

- pending/sent/failed/cancelled counters
- recent notification table
- due pending table
- manual reminder processing action

## Configuration

`NotificationReminders` section:

- `Enabled`
- `EnableSecondReminder`
- `FirstReminderHoursBefore`
- `SecondReminderHoursBefore`
- `AppointmentLookAheadDays`
- `ProcessingBatchSize`
- `ProcessorIntervalSeconds`
