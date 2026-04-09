# Performance Monitoring

## Design

The API uses lightweight request timing middleware to capture:

- request path
- HTTP method
- status code
- elapsed milliseconds
- request timestamp (UTC)

Samples are captured through `PerformanceMonitoringMiddleware` and forwarded to `IPerformanceMonitoringService`.

## Persistence Model

The monitoring service stores recent samples in memory for resilience and optionally persists them to the `PerformanceSamples` table using a background flush service.

This keeps request-time overhead low:

- middleware only enqueues/captures
- database writes happen asynchronously in the hosted flusher
- capture failures are logged and do not break request handling

## Summary Metrics

The summary service computes:

- average latency
- p95 latency
- total request count
- error rate (`status >= 400`)
- request counts by endpoint
- slowest endpoints by average latency
- recent failed requests

## Configuration

`PerformanceMonitoring` section:

- `Enabled`
- `PersistToDatabase`
- `FlushIntervalSeconds`
- `MaxInMemorySamples`
- `MaxSummarySamples`
- `SummaryLookbackHours`
- `SlowEndpointCount`
- `RecentFailedRequestCount`

## Reset In Development

Development and testing environments can clear stored samples with:

- `POST /api/performance/reset`

This endpoint is restricted to `Admin` and blocked outside Development/Testing.

## Limitations

- This is lightweight application-level timing, not full distributed tracing.
- Timing includes middleware/auth/controller execution inside the ASP.NET Core request pipeline.
- Background-flush persistence can lag slightly behind the most recent requests.
- p95 is computed from the current summary window, not from histogram buckets.

## Screenshot Description

Suggested dashboard screenshot description for README/docs:

- "Dashboard performance widget showing average response time, p95 latency, top slow endpoints, and a recent failed requests table next to existing clinic KPIs."
