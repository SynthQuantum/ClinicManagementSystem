# Scaling Phase 2 Distributed Runtime

## Goal

Convert runtime behavior so the application scales horizontally and remains stable under high load.

## Outcomes

1. Stateless compute.
2. Queue-based background processing.
3. Distributed cache strategy.
4. Independent autoscale for API and Blazor.

## Step-by-Step

### Step 1: Make compute stateless

What to do:

1. Remove in-memory state that must survive restarts.
2. Move shared transient state to distributed cache.
3. Keep user/session-affecting state in shared stores.

Tools:

- Azure Cache for Redis.
- ASP.NET Core distributed cache abstractions.

### Step 2: Extract background hosted services into workers

What to do:

1. Identify current hosted services:
   - ReminderProcessingHostedService
   - PerformanceSampleFlushHostedService
2. Define queue contracts and message payload schema.
3. Create separate worker service process.
4. Publish messages from API, consume in workers.
5. Implement retries, backoff, and dead-letter handling.

Tools:

- Azure Service Bus (preferred for enterprise queues).
- Azure Storage Queues (simpler workloads).

Commands (Service Bus example):

```powershell
az servicebus namespace create --name <sb-namespace> --resource-group <rg-name> --location <region>
az servicebus queue create --resource-group <rg-name> --namespace-name <sb-namespace> --name reminders
az servicebus queue create --resource-group <rg-name> --namespace-name <sb-namespace> --name perf-samples
```

### Step 3: Add idempotency and deduplication

What to do:

1. Add deterministic operation IDs to queue messages.
2. Record processed operation IDs for deduplication.
3. Ensure retries do not produce duplicate reminders or duplicate audit writes.

Tools:

- SQL table for processed message IDs.
- Service Bus duplicate detection if applicable.

### Step 4: Introduce cache-aside for read-heavy paths

What to do:

1. Cache dashboard aggregates and clinic settings.
2. Add explicit TTL per key.
3. Invalidate cache entries on write operations.

Tools:

- Redis with namespaced keys.

Key design example:

1. cms:dashboard:summary:<clinic-id>
2. cms:settings:<clinic-id>

### Step 5: Improve API scalability patterns

What to do:

1. Add paging defaults for large list endpoints.
2. Add query filters and server-side limits.
3. Add per-route rate limiting and request timeouts.

Tools:

- ASP.NET Core rate limiting middleware.
- API gateway policies for edge throttling.

### Step 6: Configure autoscaling

What to do:

1. Scale API using CPU, memory, requests/sec, and latency.
2. Scale worker service using queue depth and processing lag.
3. Scale Blazor separately based on concurrent users and CPU.

Tools:

- App Service autoscale rules or KEDA for containers.

## Validation Checklist

1. System handles rolling restarts without lost work.
2. Queue consumers recover automatically after transient failures.
3. Cache hit ratio is measurable and improves response time.
4. API throughput increases when instances scale out.
