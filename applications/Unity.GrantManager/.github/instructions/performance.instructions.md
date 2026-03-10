---
applyTo: "**/*.cs"
description: "Performance optimization guidelines for Unity Grant Manager"
---

# Performance Standards

Apply the repository-wide guidance from `../copilot-instructions.md` to all performance-sensitive code.

## Entity Framework Core

- Use async methods for all database operations (`ToListAsync`, `FirstOrDefaultAsync`, etc.)
- Avoid N+1 queries — use `Include()` and `ThenInclude()` for eager loading when needed
- Add database indexes for frequently queried columns and foreign keys
- Use projections (`.Select()`) when you don't need full entities
- Avoid loading entire collections into memory — use server-side pagination
- Configure entity properties with appropriate max lengths in fluent API

## Caching Strategy

- Use Redis distributed cache for frequently accessed, rarely changing data
- Follow ABP's caching patterns with `IDistributedCache<T>`
- Set appropriate expiration times based on data volatility
- Invalidate cache entries when underlying data changes

## Query Optimization

- Use `IQueryable` to build queries and let EF Core translate to SQL
- Avoid `ToList()` before applying filters — filter at the database level
- Use `AsNoTracking()` for read-only queries
- Implement pagination using ABP's `PagedAndSortedResultRequestDto`

## Background Jobs

- Use Quartz.NET for long-running or scheduled operations
- Don't block HTTP requests with expensive computations
- Use distributed events (RabbitMQ) for cross-module async processing
- Keep background job execution time reasonable with proper error handling

## Frontend Performance

- Use ABP's bundling and minification system for client-side assets
- Implement server-side pagination in DataTables
- Lazy-load non-critical resources
- Use ABP's dynamic JavaScript proxies — they handle serialization efficiently

## Monitoring

- Use Serilog structured logging for performance-relevant events
- Use MiniProfiler for development-time query profiling
- Log slow queries and long-running operations
- Monitor memory usage in multi-tenant scenarios
