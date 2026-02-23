# Code Review Standards — ProviderOptimizerService

> Este documento es un extracto del Engineering Handbook orientado específicamente al proceso de revisión de código.
> Ver `EngineeringHandbook.md` §3 para el documento completo.

## General Principles

- All PRs must be reviewed by at least **one** team member before merging.
- PRs should be small and focused (< 400 lines of diff preferred).
- Each PR must reference a ticket (`[ARS-NNN]` prefix in title).
- Reviewer responds within **24 business hours** (SLA).

## Labels for Review Comments

| Label | Meaning |
|-------|---------|
| `[BLOCKER]` | Must be resolved before merge |
| `[BUG]` | Logic error or incorrect behavior |
| `[SUGGESTION]` | Optional improvement, non-blocking |
| `[QUESTION]` | Clarification needed, may or may not require changes |
| `[NITS]` | Minor style detail, non-blocking |

## Review Checklist

### Correctness
- [ ] Logic is correct and handles edge cases.
- [ ] Domain invariants are preserved.
- [ ] No silent swallowing of exceptions.

### Clean Architecture
- [ ] Dependencies only flow inward (Domain ← Application ← Infrastructure/API).
- [ ] No EF Core or infrastructure types in Domain or Application layers.
- [ ] MediatR handlers used for application logic (no business logic in controllers).

### Security
- [ ] All endpoints require appropriate `[Authorize]` attribute with correct roles.
- [ ] No secrets committed to source control.
- [ ] Input validated via FluentValidation.
- [ ] No raw SQL concatenation.

### Testing
- [ ] Unit tests cover new business logic.
- [ ] Integration tests cover new API endpoints.
- [ ] Tests are deterministic (no time/random dependencies without seeding).

### Performance
- [ ] N+1 query problems avoided (use `.Include()` or projections).
- [ ] Redis cache used for expensive/repeated computations.
- [ ] Async/await + CancellationToken used throughout.

### Style
- [ ] `dotnet format` passes with no changes.
- [ ] Public members have XML doc comments `<summary>`.
- [ ] No unused `using` statements.

## Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Classes | PascalCase | `ProviderScore` |
| Methods | PascalCase | `CalculateScore` |
| Private fields | `_camelCase` | `_repository` |
| Interfaces | `IPascalCase` | `IProviderRepository` |
| DTOs | PascalCase + `Dto` | `ProviderDto` |
| Commands/Queries | PascalCase + suffix | `CreateProviderCommand` |

## Commit Message Format

```
type(scope): short description

Types: feat, fix, docs, test, refactor, perf, chore, ci
Example: feat(optimization): add configurable scoring weights per request
```
