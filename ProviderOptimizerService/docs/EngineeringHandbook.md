# Engineering Handbook — ASISYA Squad
## Estándares Técnicos, Procesos y Gobierno

**Versión:** 2.0 | **Propietario:** Líder Técnico | **Repositorio:** github.com/nestormauricio/AsysYa12

---

## 1. Convenciones de Código

### 1.1 Convenciones .NET 8 / C# (#40)

**Nomenclatura — reglas absolutas**

| Elemento | Convención | Ejemplo |
|----------|-----------|---------|
| Clase, Record, Enum | PascalCase | `ProviderScore`, `ProviderType` |
| Método público | PascalCase | `RankProviders`, `AssignRequest` |
| Campo privado | `_camelCase` | `_repository`, `_logger` |
| Variable local / parámetro | camelCase | `requestLocation`, `providerId` |
| Interfaz | `IPascalCase` | `IProviderRepository` |
| DTO | PascalCase + `Dto` | `ProviderDto`, `OptimizationResultDto` |
| Command / Query | + `Command` / `Query` | `CreateProviderCommand` |
| Handler | + `Handler` | `CreateProviderCommandHandler` |
| Validator | + `Validator` | `CreateProviderCommandValidator` |
| Exception | + `Exception` | `ProviderNotFoundException` |
| Configuración EF | + `Configuration` | `ProviderConfiguration` |

**Asincronía — no negociable**
```csharp
// ✅ SIEMPRE async/await real + CancellationToken
public async Task<ProviderDto> Handle(CreateProviderCommand request, CancellationToken ct)
{
    var provider = Provider.Create(request.Name, request.Type, location, request.MaxCapacity);
    await _repository.AddAsync(provider, ct);
    await _unitOfWork.SaveChangesAsync(ct);
    return _mapper.Map<ProviderDto>(provider);
}

// ❌ PROHIBIDO — bloquea el thread pool, degrada bajo carga
public ProviderDto Handle(CreateProviderCommand request)
    => _repository.GetAll().Result.First(); // nunca .Result ni .Wait()
```

**Inyección de dependencias**
- Siempre por constructor, nunca `IServiceLocator` ni `HttpContext.RequestServices`
- Registrar con el ciclo de vida más restrictivo: `Scoped` para repositorios/handlers, `Singleton` para servicios stateless
- Interfaces para toda infraestructura — garantiza testabilidad

**Guard clauses y Domain Exceptions**
```csharp
// ✅ Guard clauses al inicio, excepciones de dominio semánticas
public void UpdateRating(decimal newRating)
{
    if (newRating < 0 || newRating > 5)
        throw new InvalidRatingException(newRating); // DomainException tipada

    Rating = newRating;
    UpdatedAt = DateTime.UtcNow;
}
```

**Principios SOLID — aplicación práctica**

| Principio | Aplicación en el proyecto |
|-----------|--------------------------|
| SRP | Un handler por Command/Query. Domain entity solo encapsula invariantes de negocio |
| OCP | Nuevos `ProviderType` sin modificar `OptimizationService`. Nuevos behaviors via MediatR pipeline |
| LSP | `AppDbContext : IUnitOfWork` es sustituible; tests usan `InMemory` sin romper contratos |
| ISP | `IProviderRepository` no tiene métodos de `IUserRepository`. Interfaces pequeñas y cohesivas |
| DIP | `OptimizationController` depende de `IMediator`, no de `OptimizationService` concreto |

---

### 1.2 React 18 / TypeScript

**Nomenclatura**
- Componentes: `PascalCase.tsx` → `ProvidersPage.tsx`, `ProtectedRoute.tsx`
- Hooks: `use` + PascalCase → `useAuth.ts`, `useProviders.ts`
- API modules: `camelCase.ts` → `providers.ts`, `auth.ts`
- Types / Interfaces: PascalCase sin prefijo `I` → `Provider`, `OptimizationResult`

**Estructura de carpetas**
```
frontend/src/
├── api/          → Módulos de llamada a API (axios client + funciones tipadas)
├── components/   → Componentes reutilizables sin lógica de negocio
├── hooks/        → Custom hooks (state, side effects, auth)
├── pages/        → Páginas del router (componen componentes y hooks)
└── types/        → Tipos globales compartidos (cuando no están en api/)
```

**Reglas de componentes**
```tsx
// ✅ Props explícitas, FC tipado, lógica separada en hooks
interface ProviderRowProps {
  provider: Provider;
  onSelect?: (id: string) => void;
}
const ProviderRow: React.FC<ProviderRowProps> = ({ provider, onSelect }) => { ... };

// ❌ any, lógica de fetch en JSX, sin tipos
const Row = ({ data }: any) => { useEffect(() => fetch('/api/...'), []); };
```

**Estado y data fetching**
- `@tanstack/react-query` para server state (cache, loading, error, invalidation)
- `useState` / `useReducer` para UI state local
- `localStorage` únicamente para tokens de auth (ver `useAuth.ts`)
- Nunca mutar estado directamente; siempre retornar nuevos objetos

---

### 1.3 Docker

**Imagen multi-stage obligatoria**
```dockerfile
# Stage 1: Build (SDK completo)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# ...copiar .csproj primero (layer cache)...
RUN dotnet restore ProviderOptimizerService.sln
COPY src/ src/
RUN dotnet publish src/API/API.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime (solo runtime, imagen mínima)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
# ✅ Usuario no-root obligatorio
RUN addgroup --system appgroup && adduser --system appuser --ingroup appgroup
USER appuser
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "API.dll"]
```

**Reglas de imágenes**
- Tags específicos siempre, nunca `:latest` en Dockerfiles de producción
- `alpine` como base cuando exista (Redis 7-alpine, Nginx alpine, Node 20-alpine)
- `.dockerignore` en cada proyecto: excluir `bin/`, `obj/`, `node_modules/`, `*.md`, `.git/`
- NUNCA secrets en imágenes — ni en ARG, ni en ENV, ni en COPY. Solo en runtime via Secrets Manager

---

## 2. Estrategia de Branches (Git Flow Adaptado)

```
main          ← Producción. Tag semver en cada merge. Solo desde release/* o hotfix/*
  └─ develop  ← Integración. Base de todos los feature branches
        ├── feature/ARS-123-rank-providers-by-eta
        ├── feature/ARS-124-add-websocket-tracking
        ├── bugfix/ARS-200-fix-geocoordinate-edge-case
        └── hotfix/ARS-300-critical-null-provider   ← brancha desde main directamente
```

**Reglas de branches**
- Nombre: `tipo/TICKET-descripcion-corta` en kebab-case (max 50 chars)
- Tipos válidos: `feature`, `bugfix`, `hotfix`, `chore`, `refactor`, `docs`, `release`
- Vida máxima de un feature branch: **5 días laborales**
- Push directo a `main` o `develop`: **PROHIBIDO** (protección de rama activa)
- Squash merge a `develop`; merge commit a `main`

**Commit messages — Conventional Commits**
```
tipo(alcance): descripción breve en imperativo presente

tipos: feat, fix, docs, test, refactor, perf, chore, ci
Ejemplos:
  feat(optimization): add configurable scoring weights per request
  fix(provider): prevent race condition in AssignRequest method
  test(integration): add auth controller happy path tests
  ci: add trivy vulnerability scan job to pipeline
```

---

## 3. Code Reviews

### Obligaciones del Autor del PR

- Título: `[ARS-NNN] Descripción breve (< 72 chars)`
- Descripción incluye: **Qué**, **Por qué**, **Cómo probar**, screenshot si hay UI, enlace al ticket
- Self-review antes de solicitar revisión — leer cada línea cambiada
- Máximo **400 líneas de diff neto** por PR (excepciones documentadas)
- PRs de más de 600 líneas requieren desglose en PRs menores

### Obligaciones del Revisor

- Respuesta en máximo **24 horas laborales**
- Usar etiquetas de comentarios:
  - `[BLOCKER]` — impide el merge, debe resolverse
  - `[BUG]` — error de lógica o comportamiento incorrecto
  - `[SUGGESTION]` — mejora opcional, no bloquea
  - `[QUESTION]` — clarificación para entender, no necesariamente cambio
  - `[NITS]` — detalle menor de estilo (no requerido resolver)
- **Al menos 1 approver** obligatorio; **2 approvers** para cambios en Domain o Infraestructura
- Aprobar implica co-responsabilidad del código

### Checklist de Revisión

**Correctitud**
- [ ] Lógica correcta, casos borde manejados
- [ ] Invariantes de dominio preservados
- [ ] Sin excepciones silenciadas (catch vacíos o `catch (Exception) { return null; }`)

**Clean Architecture**
- [ ] Flujo de dependencias solo hacia adentro: Domain ← Application ← Infra/API
- [ ] Sin tipos EF Core en Domain o Application
- [ ] MediatR handlers para lógica de aplicación (no lógica en controladores)

**Seguridad**
- [ ] Endpoints con `[Authorize]` apropiado y roles correctos
- [ ] Sin secretos en código, tests ni configuración commitada
- [ ] Input validado con FluentValidation (no validación manual en handlers)
- [ ] Sin SQL concatenado (EF Core parametriza; si hay raw SQL, verificar parámetros)

**Testing**
- [ ] Tests unitarios cubren nueva lógica de negocio
- [ ] Tests de integración cubren nuevos endpoints
- [ ] Tests deterministas (sin dependencia de tiempo real o `Random` sin seed)

**Performance**
- [ ] Sin N+1 queries (usar `.Include()` o proyecciones)
- [ ] Redis usado para operaciones costosas/repetidas (rankings de proveedores)
- [ ] `async/await` + `CancellationToken` en todo I/O

**Estilo**
- [ ] `dotnet format` pasa sin cambios
- [ ] Miembros públicos tienen comentarios XML `<summary>`
- [ ] Sin `using` statements sin usar

---

## 4. Pipeline CI/CD

### Jobs del Pipeline (GitHub Actions)

```
Push/PR a develop o main
         │
    ┌────▼─────┐
    │  build   │  dotnet restore + build -warnaserror
    └────┬─────┘
    ┌────▼──────┐
    │  lint     │  dotnet format --verify-no-changes
    └────┬──────┘
    ┌────▼──────┐
    │  test     │  Unit + Integration (Postgres + Redis en services)
    └────┬──────┘
    ┌────▼──────────┐
    │  security     │  Trivy FS scan (CRITICAL/HIGH bloquea)
    └────┬──────────┘
    ┌────▼────────────┐
    │  docker-build   │  Build imagen + cache GHA
    └────┬────────────┘
         │ (solo main)
    ┌────▼────────────┐
    │  push-ecr       │  Login ECR + push :sha + :latest
    └────┬────────────┘
    ┌────▼──────────────┐
    │  deploy-staging   │  ECS update-service + notificación Slack
    └────┬──────────────┘
         │ (aprobación manual en GitHub Environment "production")
    ┌────▼────────────────┐
    │  deploy-production  │  ECS update-service + smoke test
    └─────────────────────┘
```

### Políticas de Pipeline
- Merge bloqueado si cualquier job falla (incluyendo security scan CRITICAL)
- Cobertura de tests mínima: **70%** en nuevas líneas (reportado por Codecov)
- Tiempo máximo de pipeline: 15 minutos (alerta si supera)
- Rollback automático si health check ECS falla en los 5 min post-deploy
- Credenciales AWS via **OIDC** (IAM Role) — sin access keys en Secrets

---

## 5. Definición de Done (DoD)

Un ítem de backlog se considera **Done** cuando cumple **todos** estos criterios:

| # | Criterio | Verificación |
|---|---------|-------------|
| 1 | Código implementado y PR aprobado (1 o 2 reviewers según alcance) | GitHub PR status |
| 2 | Tests unitarios con cobertura ≥ 70% en nuevas líneas | Codecov report |
| 3 | Al menos 1 test de integración por endpoint nuevo | Test suite verde |
| 4 | Pipeline CI/CD completamente verde | GitHub Actions |
| 5 | Desplegado y verificado manualmente en Staging | Checklist de QA |
| 6 | Swagger/OpenAPI actualizado si hay cambios de contrato | swagger.json diff |
| 7 | ADR creado si hubo decisión arquitectónica importante | docs/adr/ |
| 8 | Criterios de aceptación del ticket verificados | Jira ticket cerrado |
| 9 | Sin TODOs sin ticket asociado introducidos | grep TODO en diff |
| 10 | Deuda técnica documentada si se introduce (justificada + ticket) | Tech debt backlog |

---

## 6. Manejo de Secretos

### Reglas Absolutas (Violación = Incidente de Seguridad)

1. **NUNCA** commitear secretos en ninguna rama del repositorio
2. **NUNCA** hardcodear connection strings, API keys ni passwords en código
3. **NUNCA** secretos en imágenes Docker (ni en `ARG`, `ENV` en build, ni en `COPY`)
4. **NUNCA** loguear valores sensibles (passwords, tokens, números de tarjeta, datos personales)
5. **NUNCA** pasar secretos como parámetros de URL (van en headers o body cifrado)

### Flujo por Ambiente

| Ambiente | Mecanismo | Responsable |
|----------|-----------|-------------|
| Local dev | `.env` en `.gitignore` (plantilla `.env.example` sin valores) | Desarrollador |
| CI/CD | GitHub Secrets (solo variables no sensibles de build) | DevOps |
| Staging / Prod | AWS Secrets Manager cargado en startup | Infra / Tech Lead |

### Rotación

- Secretos críticos (JWT secret, DB passwords): rotación automática cada **90 días** vía AWS Secrets Manager rotation Lambda
- API keys externas: rotación manual cada **6 meses** con notificación en Slack
- En caso de leak sospechado: rotación inmediata + post-mortem

---

## 7. Arquitectura Base del Squad

Todo microservicio nuevo debe seguir **Clean Architecture** con esta estructura:

```
MiServicio/
├── src/
│   ├── Domain/           ← Sin dependencias. Solo C# puro.
│   │   ├── Entities/     ← Aggregate Roots y Entities
│   │   ├── ValueObjects/ ← Objetos de valor inmutables
│   │   ├── Events/       ← Domain Events (implementan IDomainEvent)
│   │   ├── Exceptions/   ← Excepciones de dominio tipadas
│   │   └── Interfaces/   ← Ports (contratos de infraestructura)
│   ├── Application/      ← Depende solo de Domain
│   │   ├── Common/       ← Behaviors, Mappings, Exceptions
│   │   └── Features/     ← Commands, Queries, Handlers, Validators, DTOs
│   ├── Infrastructure/   ← Implementaciones concretas
│   │   ├── Data/         ← EF Core DbContext, Migrations, Configurations
│   │   ├── Repositories/ ← Implementaciones de IRepository
│   │   └── Services/     ← JWT, Cache, Messaging, PasswordHasher
│   └── API/              ← Depende de Application (nunca de Infrastructure directamente)
│       ├── Controllers/
│       ├── Middleware/
│       └── Program.cs
└── tests/
    ├── UnitTests/        ← Domain + Application (mocks de interfaces)
    └── IntegrationTests/ ← API full-stack con Testcontainers
```

**Decisiones de arquitectura (ADRs)**

Archivo: `docs/adr/ADR-NNN-titulo.md`
```markdown
# ADR-NNN: [Título de la decisión]

## Estado: [Propuesto | Aceptado | Superado por ADR-XXX]

## Contexto
¿Qué situación o problema llevó a tomar esta decisión?

## Decisión
¿Qué se decidió exactamente?

## Consecuencias
¿Qué implica esta decisión? ¿Qué se facilita y qué se complica?

## Alternativas consideradas
¿Qué otras opciones se evaluaron y por qué se descartaron?
```

---

## 8. Control de Deuda Técnica

### Clasificación

| Prioridad | Descripción | Plazo | Proceso |
|-----------|-------------|-------|---------|
| 🔴 Crítica | Impacta seguridad, disponibilidad o corrupción de datos | Sprint actual | Bloqueante |
| 🟡 Alta | Degrada mantenibilidad o performance significativamente | Próximo sprint | Ticket priorizado |
| 🟢 Normal | Mejoras de calidad, cobertura, refactor | Backlog | 20% de capacidad |

### Proceso

1. **Identificación**: Todo `// TODO` en código lleva ticket obligatorio → `// TODO(ARS-NNN): descripción`
2. **Registro**: Ticket en Jira con label `tech-debt` + estimación de esfuerzo
3. **Revisión mensual**: 15 minutos en planning del sprint — revisar backlog de deuda
4. **Presupuesto**: 20% de la capacidad de cada sprint reservada para deuda técnica
5. **Métrica**: SonarCloud Debt Ratio < 5%; alerta si supera 8%

### Herramientas

- **SonarCloud**: análisis estático en cada PR — code smells, bugs, security hotspots
- **Trivy**: vulnerabilidades en dependencias e imágenes Docker
- **`dotnet outdated`**: revisión mensual de paquetes NuGet desactualizados
- **Codecov**: tendencia de cobertura por PR — alert si cae >2%

---

## 9. Métricas de Calidad del Squad

| Métrica | Target | Alerta | Herramienta |
|---------|--------|--------|-------------|
| Cobertura de tests (nuevas líneas) | ≥ 70% | < 60% | Codecov |
| SonarCloud Debt Ratio | < 5% | > 8% | SonarCloud |
| Vulnerabilidades CRITICAL/HIGH | 0 | > 0 | Trivy |
| Tiempo medio de pipeline CI | < 15 min | > 20 min | GitHub Actions |
| Lead time (commit → producción) | < 2 días | > 5 días | Jira |
| MTTR (tiempo de recuperación) | < 2 horas | > 4 horas | PagerDuty |
| Disponibilidad del servicio | ≥ 99.9% mensual | < 99.5% | CloudWatch |
| P95 latencia /optimization/rank | < 500ms | > 1s | CloudWatch |

---

## 10. Governance Técnica

### Proceso de Decisiones Técnicas

| Impacto | Tipo de decisión | Aprobación requerida |
|---------|-----------------|---------------------|
| Bajo | Librería de utilidad, refactor local | Revisor del PR |
| Medio | Nueva dependencia, cambio de patrón | Tech Lead + 1 Senior Dev |
| Alto | Cambio de arquitectura, nueva tecnología, cambio de BD | Tech Lead + CTO + ADR documentado |
| Crítico | Cambio de proveedor cloud, migración de plataforma | Committee técnico completo |

### Reuniones de Gobernanza

| Reunión | Frecuencia | Participantes | Agenda |
|---------|-----------|--------------|--------|
| Tech Sync | Semanal (30 min) | Todo el squad | Bloqueos técnicos, PRs pendientes, deuda |
| Architecture Review | Quincenal (60 min) | Tech Lead + Seniors | ADRs nuevos, decisiones de diseño |
| Security Review | Mensual (45 min) | Tech Lead + DevOps | Dependencias, secretos, incidentes |
| Tech Debt Sprint | Mensual (sprint parcial) | Todo el squad | Pago de deuda priorizada |

### Onboarding de Nuevos Desarrolladores

1. Leer este handbook completo — primer día
2. Setup del entorno local con `docker-compose up -d` — primer día
3. PR de "hello world" (cambio menor) revisado por Tech Lead — primer día
4. Primer feature asignado con buddy (senior) — primera semana
5. Code review como reviewer solo (no approver) — segunda semana
6. Approver habilitado — primera evaluación de 30 días
