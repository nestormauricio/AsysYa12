# Arquitectura C4 Completa — ASISYA: Módulo de Asignación Inteligente de Proveedores

**Versión:** 3.0 | **Repositorio:** github.com/nestormauricio/AsysYa12

---

## NIVEL 1 — Contexto del Sistema (#1)

```
╔═══════════════════════════════════════════════════════════════════════════╗
║                    SISTEMA ASISYA — Contexto Global                       ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                           ║
║  [Usuario Final]  ──HTTPS/REST──►  ┌─────────────────────┐              ║
║  (App Móvil iOS/Android)           │                     │              ║
║                                    │   SISTEMA ASISYA    │              ║
║  [Proveedor]      ──HTTPS/WS───►  │   (Asignación       │              ║
║  (App Web/Móvil)  ◄──Push/WS───   │    Inteligente de   │              ║
║                                    │    Asistencia       │              ║
║  [Administrador]  ──HTTPS/REST──►  │    Vehicular)       │              ║
║  (Backoffice Web)                  └──────────┬──────────┘              ║
║                                               │                          ║
║                    ┌──────────────────────────┼──────────────────┐       ║
║                    │         Sistemas Externos Consumidos         │       ║
║                    │  [Google Maps API]   geocodificación, ETA   │       ║
║                    │  [Firebase FCM]      push notifications     │       ║
║                    │  [Twilio]            SMS / WhatsApp         │       ║
║                    │  [AWS Cognito]       OAuth 2.0 / JWT IdP   │       ║
║                    └─────────────────────────────────────────────┘       ║
╚═══════════════════════════════════════════════════════════════════════════╝
```

### Actores

| Actor | Canal | Descripción |
|-------|-------|-------------|
| Usuario Final | REST + WebSocket | Solicita asistencia vehicular (grúa, batería, cerrajería) |
| Proveedor | REST + WebSocket | Recibe asignaciones; actualiza ubicación y estado |
| Administrador | REST | Gestiona proveedores, consulta métricas, configura el sistema |
| Google Maps API | HTTPS | Geocodificación inversa, distancias reales y ETA en tráfico |
| Firebase FCM | HTTPS | Push notifications a dispositivos móviles |
| Twilio | HTTPS | SMS/WhatsApp como canal secundario de notificación |
| AWS Cognito | OAuth 2.0 | Identity Provider; emite y valida JWT tokens |

---

## NIVEL 2 — Contenedores e Infraestructura AWS (#2)

```
╔══════════════════════════════════════════════════════════════════════════════════════╗
║  AWS CLOUD — Region: us-east-1                                                       ║
║                                                                                      ║
║  ┌─────────────────────────── CAPA PÚBLICA ──────────────────────────────────────┐  ║
║  │                                                                                │  ║
║  │  Internet ──► Route53 (DNS) ──► CloudFront CDN ──► S3 (React SPA bundle)     │  ║
║  │                                       │                                       │  ║
║  │                                  AWS WAF                                      │  ║
║  │                          [OWASP ruleset · Rate limit · IP block]              │  ║
║  │                                       │                                       │  ║
║  │                            API Gateway (HTTP API v2)                          │  ║
║  │                    [JWT auth · Rate limit 1000rpm · Routing · TLS 1.3]       │  ║
║  │                                       │                                       │  ║
║  │                         Application Load Balancer (ALB)                       │  ║
║  │                      [Health checks · SSL termination · AZ routing]           │  ║
║  └───────────────────────────────────────┬───────────────────────────────────────┘  ║
║                                          │                                           ║
║  ┌─────────────────────── CAPA PRIVADA (ECS Fargate) ───────────────────────────┐   ║
║  │                                                                               │   ║
║  │  ┌──────────────────────┐  ┌──────────────────────┐  ┌───────────────────┐   │   ║
║  │  │ AssistanceRequest    │  │ ProviderOptimizer    │  │ Notifications     │   │   ║
║  │  │ Service              │  │ Service ⭐           │  │ Service           │   │   ║
║  │  │ .NET 8 · 2–10 tasks  │  │ .NET 8 · 2–20 tasks  │  │ .NET 8 · 1–5 tasks│   │   ║
║  │  │ RDS PostgreSQL       │  │ RDS PostgreSQL+Redis │  │ RDS PostgreSQL    │   │   ║
║  │  └──────────┬───────────┘  └──────────┬───────────┘  └────────┬──────────┘   │   ║
║  │             │                         │                        │              │   ║
║  │  ┌──────────▼─────────────────────────▼───────────────────────▼─────────┐    │   ║
║  │  │                   Amazon SQS — Message Broker                        │    │   ║
║  │  │  [assistance-requests-queue FIFO] DLQ: assistance-requests-dlq      │    │   ║
║  │  │  [provider-assignments-queue]     DLQ: provider-assignments-dlq     │    │   ║
║  │  │  [notifications-queue]            DLQ: notifications-dlq            │    │   ║
║  │  └──────────────────────────────────────────────────────────────────────┘    │   ║
║  │                                                                               │   ║
║  │  ┌─────────────────────┐  ┌──────────────────────┐                          │   ║
║  │  │ Location Service    │  │   Amazon SNS          │                          │   ║
║  │  │ .NET 8 + SignalR    │  │ [ops-alerts topic]    │                          │   ║
║  │  │ 2–8 tasks · Redis   │  │ → PagerDuty / Slack   │                          │   ║
║  │  └─────────────────────┘  └──────────────────────┘                          │   ║
║  └───────────────────────────────────────────────────────────────────────────── ┘   ║
║                                                                                      ║
║  ┌─────────────────────── CAPA DE DATOS ─────────────────────────────────────────┐  ║
║  │  RDS PostgreSQL 16 Multi-AZ  │  ElastiCache Redis 7  │  S3 (evidencias/docs) │  ║
║  └───────────────────────────────────────────────────────────────────────────────┘  ║
║                                                                                      ║
║  ┌───────────────────── TRANSVERSAL ─────────────────────────────────────────────┐  ║
║  │  AWS Cognito (IdP) │ Secrets Manager │ IAM Roles │ ECR (imágenes Docker)     │  ║
║  │  CloudWatch Logs+Metrics │ X-Ray Traces │ CloudTrail (auditoría)             │  ║
║  └───────────────────────────────────────────────────────────────────────────────┘  ║
╚══════════════════════════════════════════════════════════════════════════════════════╝
```

### Tabla de Contenedores con Tecnologías (#2)

| Contenedor | Tecnología | Puerto | Escala | BD |
|-----------|-----------|--------|--------|----|
| React SPA | React 18 + TypeScript + Vite → Nginx Alpine | 80 | CloudFront CDN | — |
| API Gateway | AWS HTTP API v2 | 443 | Managed | — |
| **AssistanceRequestService** | .NET 8, ECS Fargate, Clean Arch | 8080 | 2–10 tasks | RDS PostgreSQL 16 |
| **ProviderOptimizerService** ⭐ | .NET 8, ECS Fargate, MediatR, Redis | 8080 | 2–20 tasks | RDS + ElastiCache Redis |
| **NotificationsService** | .NET 8, ECS Fargate, FCM, Twilio | 8080 | 1–5 tasks | RDS PostgreSQL 16 |
| **LocationService** | .NET 8, ECS Fargate, SignalR, Redis | 8080 | 2–8 tasks | ElastiCache Redis |

---

## Responsabilidades del API Gateway (#4)

| Responsabilidad | Detalle |
|----------------|---------|
| Autenticación JWT | Valida token Cognito antes de routear al microservicio |
| Rate Limiting | 1 000 req/min global; 100 req/min por usuario JWT |
| Routing | `/api/assistance/*` → AssistanceRequestService; `/api/providers/*` → ProviderOptimizerService |
| TLS Termination | TLS 1.3 en edge; tráfico interno HTTP dentro de VPC privada |
| Request Enrichment | Añade header `X-User-Id` y `X-User-Role` desde JWT claims |
| CORS | Orígenes configurados explícitamente (app, SPA) |

---

## Los 4 Microservicios — Responsabilidades (#5 y #6)

### 1. AssistanceRequestService
- Recibe y valida solicitudes de asistencia del usuario
- Persiste `AssistanceRequest` con estado inicial `Pending`
- Publica `AssistanceRequestCreatedEvent` → SQS FIFO
- Expone endpoints de consulta de estado por `requestId`
- Notifica a Location Service cuando asignación se completa

### 2. ProviderOptimizerService ⭐ (implementado)
- Consume `AssistanceRequestCreatedEvent` de SQS
- Obtiene candidatos disponibles (Redis cache → RDS fallback)
- Ejecuta algoritmo de scoring ponderado por 4 dimensiones
- Aplica bloqueo optimista para evitar asignaciones duplicadas
- Publica `ProviderAssignedEvent` → SQS
- Expone `POST /optimize` y `GET /providers/available`

### 3. NotificationsService
- Consume `ProviderAssignedEvent` de SQS
- Envía push FCM al usuario y SMS/WA Twilio al proveedor
- Retry exponencial: 1s → 2s → 4s; fallback canal a canal
- Registra `Notification` con estado final en BD

### 4. LocationService
- Geocodificación de coordenadas (Google Maps API)
- WebSocket Hub (SignalR): proveedor publica ubicación cada 5s
- Calcula ETA dinámico en tiempo real
- Emite `ProviderArrived` cuando proveedor llega a destino

---

## NIVEL 3 — Componentes: ProviderOptimizerService (#3)

```
╔══════════════════════════════════════════════════════════════════════════╗
║                      ProviderOptimizerService                            ║
║                                                                          ║
║  ┌──────────────── API Layer ──────────────────────────────────────────┐ ║
║  │  AuthController           ProvidersController  OptimizationCtrl    │ ║
║  │  POST /auth/login         GET  /providers       POST /optimize      │ ║
║  │  POST /auth/register      GET  /providers/:id   GET  /providers/    │ ║
║  │                           POST /providers            available      │ ║
║  │                           PUT  /providers/:id                       │ ║
║  │  ExceptionHandlingMiddleware → ProblemDetails RFC 7807              │ ║
║  └─────────────────────────────────┬──────────────────────────────────┘ ║
║                                    │ MediatR Pipeline                    ║
║                       ValidationBehavior → LoggingBehavior              ║
║  ┌──────────────── Application Layer (CQRS via MediatR) ───────────────┐ ║
║  │  Commands                     Queries                               │ ║
║  │  ├─ CreateProviderCommand      ├─ GetProvidersQuery                 │ ║
║  │  ├─ UpdateProviderCommand      ├─ GetProviderByIdQuery              │ ║
║  │  ├─ DeleteProviderCommand      ├─ GetAvailableProvidersQuery  ◄────┤ ║
║  │  ├─ LoginCommand               └─ RankProvidersQuery (optimize) ◄──┤ ║
║  │  └─ RegisterCommand                                                 │ ║
║  │  FluentValidation Validators por cada Command/Query                 │ ║
║  └─────────────────────────────────┬──────────────────────────────────┘ ║
║                                    │                                     ║
║  ┌──────────────── Domain Layer ───────────────────────────────────────┐ ║
║  │  Aggregate Roots        Value Objects         Interfaces (ports)   │ ║
║  │  ├─ Provider (AR)       ├─ GeoCoordinate      IProviderRepository  │ ║
║  │  ├─ AssistanceRequest   ├─ ProviderScore      IOptimizationService │ ║
║  │  └─ ApplicationUser     └─ EtaEstimate        IUnitOfWork          │ ║
║  │                                               IJwtTokenService     │ ║
║  │  Domain Events          Exceptions            ICacheService        │ ║
║  │  ├─ ProviderCreated     DomainException                            │ ║
║  │  └─ AvailabilityChanged ProviderNotFoundException                  │ ║
║  └─────────────────────────────────┬──────────────────────────────────┘ ║
║                                    │                                     ║
║  ┌──────────────── Infrastructure Layer ───────────────────────────────┐ ║
║  │  Data                     Services               Repositories      │ ║
║  │  ├─ AppDbContext (EF8)     ├─ OptimizationService ProviderRepo      │ ║
║  │  ├─ ProviderConfiguration  │  (ScoringStrategy)   UserRepository   │ ║
║  │  ├─ AssistanceConfig       ├─ JwtTokenService                      │ ║
║  │  ├─ Migrations (×2)        ├─ RedisCacheService                    │ ║
║  │  └─ DependencyInjection    └─ PasswordHasher (BCrypt)              │ ║
║  └─────────────────────────────────────────────────────────────────────┘ ║
╚══════════════════════════════════════════════════════════════════════════╝
```

---

## Bounded Contexts (#7)

```
CONTEXT MAP — ASISYA

  ┌─────────────────────┐  Published Language   ┌──────────────────────┐
  │  AssistanceRequest  │──AssistanceCreated──►  │  ProviderOptimizer   │
  │  BC                 │                        │  BC                  │
  └─────────────────────┘                        └──────────┬───────────┘
                                                            │ ProviderAssigned
  ┌──────────────────┐   Conformist (JWT)                   ▼
  │  Identity BC     │──────────────────────────► ┌──────────────────────┐
  │  (Cognito)       │                             │  Notifications BC    │
  └──────────────────┘                             └──────────────────────┘
         │
         └── Shared Kernel (GeoCoordinate VO) ──► LocationBC
```

---

## DDD: Entidades, Agregados y Value Objects (#8)

```
AssistanceRequest (Aggregate Root)  — AssistanceRequestService
  Id: Guid | RequestorName: string | RequestorPhone: string
  Location: GeoCoordinate (VO) | RequiredType: ProviderType (enum)
  Status: RequestStatus (Pending→Assigned→InProgress→Completed|Cancelled)
  AssignedProviderId?: Guid | Notes?: string
  CreatedAt / UpdatedAt: DateTime

Provider (Aggregate Root)  — ProviderOptimizerService
  Id: Guid | Name: string [max 200] | Type: ProviderType (enum)
  Location: GeoCoordinate (VO) | Rating: decimal [0.0–5.0]
  IsAvailable: bool | ActiveAssignments: int | MaxCapacity: int
  RowVersion: byte[]  ← concurrencia optimista

GeoCoordinate (Value Object)
  Latitude: double [-90,90] | Longitude: double [-180,180]
  DistanceTo(other): double  ← Haversine formula

ProviderScore (Value Object)
  Value: double (suma total) | RatingComponent: double
  AvailabilityComponent: double | DistanceComponent: double
  EtaComponent: double

EtaEstimate (Value Object)
  DistanceKm: double | EstimatedMinutes: double | EstimatedArrival: DateTime

Notification (Aggregate Root)  — NotificationsService
  Id: Guid | RecipientId: Guid | Channel: NotificationChannel (enum)
  Title: string | Body: string | Status: NotificationStatus (enum)
  RetryCount: int [max 3] | SentAt?: DateTime
```

---

## Eventos de Dominio con Payload (#9)

```json
// AssistanceRequestCreatedEvent
{
  "eventId": "uuid", "occurredAt": "2024-01-15T10:30:00Z",
  "eventType": "AssistanceRequestCreated", "version": "1.0",
  "payload": {
    "requestId": "uuid", "requestorName": "Juan Pérez",
    "requestorPhone": "+527771234567",
    "location": { "latitude": 19.4326, "longitude": -99.1332 },
    "requiredType": "Towing", "notes": "Llanta ponchada en autopista"
  }
}

// ProviderAssignedEvent
{
  "eventId": "uuid", "occurredAt": "2024-01-15T10:30:05Z",
  "eventType": "ProviderAssigned", "version": "1.0",
  "payload": {
    "requestId": "uuid", "providerId": "uuid",
    "providerName": "Grúas Rápido SA", "providerPhone": "+527771234001",
    "etaMinutes": 12, "distanceKm": 3.4, "score": 0.847
  }
}

// ProviderAvailabilityChangedEvent
{
  "eventId": "uuid", "occurredAt": "2024-01-15T10:30:05Z",
  "eventType": "ProviderAvailabilityChanged", "version": "1.0",
  "payload": { "providerId": "uuid", "isAvailable": false, "reason": "AtCapacity" }
}

// NotificationSentEvent
{
  "eventId": "uuid", "occurredAt": "2024-01-15T10:30:06Z",
  "eventType": "NotificationSent", "version": "1.0",
  "payload": {
    "notificationId": "uuid", "recipientId": "uuid",
    "channel": "Push", "deliveredAt": "2024-01-15T10:30:06Z"
  }
}
```

---

## Contratos entre Servicios — DTOs / Schemas (#10)

### POST /api/optimization/rank (alias: POST /optimize)
```json
// Request
{ "latitude": 19.4326, "longitude": -99.1332, "requiredType": 3,
  "weights": { "rating": 0.30, "availability": 0.25, "distance": 0.25, "eta": 0.20 } }

// Response 200
[{ "provider": { "id": "uuid", "name": "Grúas Rápido SA", "type": 3,
     "latitude": 19.44, "longitude": -99.12, "rating": 4.8,
     "isAvailable": true, "activeAssignments": 1, "maxCapacity": 5,
     "phoneNumber": "+527771234001" },
   "score": 0.847, "ratingComponent": 0.288, "availabilityComponent": 0.200,
   "distanceComponent": 0.198, "etaComponent": 0.161,
   "distanceKm": 3.4, "estimatedMinutes": 12.1,
   "estimatedArrival": "2024-01-15T10:42:00Z" }]

// Response 400
{ "status": 400, "message": "Validation failed.",
  "errors": { "Latitude": ["Latitude must be between -90 and 90."] } }
```

### GET /api/providers/available
```json
// Response 200
{ "providers": [{ "id": "uuid", "name": "Grúas Rápido SA", "type": 3,
    "latitude": 19.44, "longitude": -99.12, "rating": 4.8,
    "isAvailable": true, "activeAssignments": 1, "maxCapacity": 5 }],
  "total": 4 }
```

### Mensaje SQS — sobre el contrato
```json
{
  "MessageId": "uuid",
  "Body": "{\"eventType\":\"AssistanceRequestCreated\",\"version\":\"1.0\",\"payload\":{...}}",
  "MessageAttributes": {
    "EventType": { "DataType": "String", "StringValue": "AssistanceRequestCreated" },
    "Version":   { "DataType": "String", "StringValue": "1.0" },
    "TraceId":   { "DataType": "String", "StringValue": "1-5f84a3c0-abc123" }
  }
}
```

---

## Message Broker: SQS + SNS (#11)

```
SQS Queues:
  assistance-requests-queue (FIFO)     → DLQ: assistance-requests-dlq
  provider-assignments-queue (Standard)→ DLQ: provider-assignments-dlq
  notifications-queue (Standard)       → DLQ: notifications-dlq

Configuración:
  Visibility timeout: 30s
  Max receive count: 3 → DLQ
  Retention: 4 días
  FIFO: deduplicación por requestId (MessageDeduplicationId)

SNS Topics:
  ops-alerts-topic → PagerDuty (CRITICAL), Slack #alertas-ops
  (subscripción Lambda que filtra por severidad)
```

---

## Flujo Síncrono vs Asíncrono (#12)

```
SÍNCRONO (respuesta inmediata < 200ms):
  POST /api/assistance  →  validar  →  persistir (Pending)  →  202 Accepted {requestId}

ASÍNCRONO (background):
  AssistanceRequestService → SQS → ProviderOptimizerService (scoring + asignación)
    → SQS → NotificationsService (push + SMS)
    → SQS → LocationService (inicia tracking WebSocket)

SÍNCRONO (consulta de estado):
  GET /api/assistance/{id} → leer BD → estado actual + provider asignado

TIEMPO REAL (WebSocket):
  Usuario ←──── SignalR Hub ←──── proveedor publica ubicación cada 5s
```

---

## Estrategia de Autoscaling (#13)

| Servicio | Min | Max | Scale-Out trigger | Cooldown |
|----------|-----|-----|------------------|----------|
| AssistanceRequestService | 2 | 10 | CPU>70% OR SQS>50 | 60s |
| **ProviderOptimizerService** | 2 | 20 | CPU>70% OR SQS>100 | 60s |
| NotificationsService | 1 | 5 | SQS>200 | 30s |
| LocationService | 2 | 8 | WS connections>500/task | 90s |

Scheduled: Peak 18:00–22:00 → min×2. Off-peak 02:00–06:00 → min=1.

---

## Procesamiento Asíncrono (#14)

```
ProviderOptimizerService consumer:
  Long Polling (20s) | Batch size: 10 | Concurrencia: 5 threads/task
  Idempotencia: check requestId en BD antes de procesar
  Heartbeat: ExtendVisibilityTimeout si procesamiento > 20s
  DLQ alert: SNS → Slack si mensaje llega a DLQ

NotificationsService consumer:
  Retry: 1s → 2s → 4s (exponencial + jitter)
  Fallback de canal: Push → SMS → WhatsApp
```

---

## Rate Limiting (#15)

| Capa | Límite | Respuesta |
|------|--------|-----------|
| AWS WAF | 1 000 req/5min por IP | 429 Block |
| API Gateway | 1 000 req/min global | 429 Throttle |
| API Gateway | 100 req/min por JWT sub | 429 Throttle |
| POST /optimize | 10 req/min por usuario | 429 + Retry-After header |
| POST /api/assistance | 5 req/min por usuario | 429 |

---

## Retry + Fallback (#16)

```csharp
// Polly — política para clients externos (Google Maps, Twilio, FCM)
RetryPolicy:     WaitAndRetryAsync(3, attempt => 2^attempt seconds + jitter)
CircuitBreaker:  CircuitBreakerAsync(5 faults, 30s break)
Timeout:         TimeoutAsync(5s)
Fallback:        Si Google Maps no responde → usar Haversine directo + log degradación
```

---

## Redis en el Flujo (#17)

```
Cache key: "providers:available:{type}" — TTL 60s

GET /providers/available: Redis HIT → retornar / MISS → RDS → guardar → retornar
POST /optimize:           usar lista cacheada; NO cachear resultado de ranking
Invalidación:             al crear/actualizar/eliminar provider → RemoveAsync

Config ElastiCache: Cluster Multi-AZ | eviction: allkeys-lru | max-memory: 1GB/node
```

---

## RDS PostgreSQL (#18)

```
Engine: PostgreSQL 16 Multi-AZ | Instance: db.r6g.large
Storage: GP3 100GB auto-scaling | Backups: 7 días retención
Failover: < 60s (standby segunda AZ) | Encryption: KMS AES-256
Connection: RDS Proxy (connection pooling, failover transparente)

Índices:
  idx_providers_status       ON providers(is_available)
  idx_providers_type_status  ON providers(type, is_available)
  idx_providers_location     ON providers(latitude, longitude)
```

---

## S3 (#19)

```
asisya-spa-{env}:       React SPA bundle → CloudFront → usuarios
asisya-evidencias-{env}: Fotos de evidencia → Lifecycle 90d → Glacier
                         URL pre-firmada 15min para upload desde app móvil
```

---

## CDN (#20)

```
CloudFront:
  /assets/* → S3, cache 1 año (immutable assets con hash en nombre)
  /         → S3, cache 0 (index.html siempre fresco)
  /api/*    → API Gateway, sin cache, forward header Authorization

Security headers: HSTS, X-Frame-Options: DENY, X-Content-Type-Options: nosniff
```

---

## Seguridad Completa (#21)

### JWT / OAuth2
```
Flujo: Authorization Code + PKCE (app móvil)
Token: JWT RS256 por Cognito | Access: 15min | Refresh: 7 días
API Gateway valida signature + expiry antes de routear
```

### IAM Roles por servicio (least privilege)
```
ProviderOptimizer-TaskRole:
  secretsmanager:GetSecretValue (arn:...:asisya/prod/provider-optimizer)
  sqs:SendMessage, ReceiveMessage, DeleteMessage (solo sus queues)
  rds-db:connect (solo su DB)

CICD-DeployRole (OIDC — sin access keys):
  ecr:*, ecs:UpdateService, ecs:RegisterTaskDefinition
```

### WAF
```
P1: AWS-AWSManagedRulesCommonRuleSet   (OWASP Top 10)  → Block
P2: AWS-AWSManagedRulesSQLiRuleSet     (SQL Injection)  → Block
P3: AWS-AWSManagedRulesKnownBadInputs  (XSS/traversal) → Block
P4: RateLimitByIP (1000/5min)                          → Block
```

### Secrets Manager + Auditoría
```
Secretos rotados cada 90 días (Lambda automática)
CloudTrail: todas las llamadas AWS API
CloudWatch Logs: logs de aplicación JSON (90 días)
```

### OWASP Mitigado
```
A03 Injection           → EF Core parametrizado (nunca SQL concatenado)
A02 Crypto Failures     → TLS 1.3, AES-256 en reposo, BCrypt passwords
A01 Broken Access Ctrl  → RBAC [Authorize(Roles="Admin")], JWT claims
A07 Auth Failures       → JWT expiración corta + refresh rotation
A06 Vulnerable Deps     → Trivy scan en CI/CD (CRITICAL bloquea pipeline)
```

---

## Observabilidad (#22)

### Logs estructurados (Serilog JSON → CloudWatch)
```json
{ "timestamp":"2024-01-15T10:30:05Z", "level":"Information",
  "service":"ProviderOptimizerService", "traceId":"1-5f84a3c0-abc123",
  "message":"RankProviders completed",
  "properties":{ "candidates":5, "bestScore":0.847, "durationMs":45, "cacheHit":true } }
```

### Métricas y Alarmas
```
Métricas custom: OptimizationDurationMs (P50/P95/P99), AvailableProviders,
                 CacheHitRate, SQSMessagesProcessed, Errors

Alarmas: P99 > 1s → PagerDuty | ErrorRate > 5% → PagerDuty + Slack
         DLQ depth > 0 → Slack
```

### Tracing (X-Ray) + SLOs
```
X-Ray: 5% sampling prod, 100% staging — Service Map visual

SLOs:
  Disponibilidad:      99.9% mensual (≤ 43 min downtime)
  POST /optimize:      P95 < 500ms, P99 < 1s
  GET /available:      P95 < 200ms
  Notificación push:   < 3s end-to-end
```

---

## Load Balancers (#23)

```
ALB:
  Listener: HTTPS:443 (cert ACM) | Health: GET /health (30s interval)
  Routing:
    /api/assistance/*   → AssistanceRequestService Target Group
    /api/providers/*    → ProviderOptimizerService Target Group
    /api/optimization/* → ProviderOptimizerService Target Group
    /api/notifications/ → NotificationsService Target Group
    /ws/*               → LocationService Target Group (WebSocket upgrade)
  Access Logs: S3 asisya-alb-logs-{env}
```
