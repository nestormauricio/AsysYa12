# ProviderOptimizerService — ASISYA

Microservicio crítico del sistema de asistencias vehiculares ASISYA.  
Implementa la selección óptima de proveedores mediante un algoritmo de scoring ponderado (ETA, distancia, rating, disponibilidad).

---

## Arquitectura

```
ProviderOptimizerService/
├── src/
│   ├── API/           # Capa de presentación (Controllers, Middleware)
│   ├── Application/   # Casos de uso (CQRS — MediatR, FluentValidation)
│   ├── Domain/        # Entidades, ValueObjects, Interfaces (DDD)
│   └── Infrastructure/# EF Core + PostgreSQL, Redis, JWT, BCrypt
├── tests/
│   ├── UnitTests/     # Tests de dominio y servicios
│   └── IntegrationTests/ # Tests de API con WebApplicationFactory
├── frontend/          # React + TypeScript (Vite)
├── docs/              # C4, Engineering Handbook, Code Review, ADRs
├── Dockerfile
├── docker-compose.yml
└── .github/workflows/ # CI/CD con GitHub Actions
```

**Patrón:** Clean Architecture · DDD · CQRS (MediatR) · Repository + UoW  
**Stack:** .NET 8 · PostgreSQL 16 · Redis 7 · React 18 · Docker

---

## Inicio Rápido

### Requisitos
- Docker ≥ 24 y Docker Compose v2
- .NET SDK 8 (solo para desarrollo local)
- Node.js 20+ (solo para frontend local)

### Con Docker Compose (recomendado)

```bash
# 1. Clonar y entrar al directorio
git clone https://github.com/nestormauricio/AsysYa12
cd AsysYa12/ProviderOptimizerService

# 2. Configurar variables de entorno
cp .env.example .env
# Editar .env y cambiar JWT_SECRET y POSTGRES_PASSWORD

# 3. Levantar todos los servicios
docker compose up --build

# 4. Acceder a Swagger
open http://localhost:8080/swagger
```

### Desarrollo local (sin Docker)

```bash
cd src/API
dotnet run
# API en http://localhost:5000
# Swagger en http://localhost:5000/swagger
```

```bash
cd frontend
npm install
npm run dev
# Frontend en http://localhost:3000
```

---

## Endpoints principales

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | `/api/auth/register` | No | Registrar nuevo usuario |
| POST | `/api/auth/login` | No | Autenticarse y obtener JWT |
| GET | `/api/providers` | JWT | Listar todos los proveedores |
| GET | `/api/providers/available` | JWT | Listar proveedores disponibles (filtrar por `?type=`) |
| POST | `/optimize` | JWT | Ranking óptimo de proveedores para una ubicación |
| POST | `/api/providers` | JWT+Admin | Crear proveedor |
| PUT | `/api/providers/{id}` | JWT+Admin | Actualizar proveedor |
| DELETE | `/api/providers/{id}` | JWT+Admin | Eliminar proveedor |

### Ejemplo: Obtener token

```bash
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","email":"admin@asisya.com","password":"SecurePass123!"}'
```

### Ejemplo: Optimizar proveedor

```bash
TOKEN="<tu_jwt_aquí>"
curl -X POST http://localhost:8080/optimize \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "latitude": -12.046374,
    "longitude": -77.042793,
    "requiredType": 1,
    "weights": { "rating": 0.30, "availability": 0.25, "distance": 0.25, "eta": 0.20 }
  }'
```

---

## Algoritmo de Selección

El `WeightedScoringOptimizationService` calcula un score para cada proveedor disponible:

```
Score = w_rating × rating_normalizado
      + w_availability × (1 - activeAssignments/5)
      + w_distance × (1 - distance_normalizada)
      + w_eta × (1 - eta_normalizada)
```

- Todos los componentes están normalizados a [0,1]
- Los pesos son configurables por request (defecto: 0.30 / 0.25 / 0.25 / 0.20)
- La distancia usa la fórmula de Haversine sobre las coordenadas GPS reales
- El ETA se calcula asumiendo velocidad promedio de 60 km/h

---

## Tests

```bash
# Todos los tests
dotnet test ProviderOptimizerService.sln

# Solo unitarios
dotnet test tests/UnitTests/UnitTests.csproj

# Solo integración
dotnet test tests/IntegrationTests/IntegrationTests.csproj
```

---

## CI/CD

Pipeline en `.github/workflows/ci.yml`:

1. **lint** — Verificación de formato
2. **build** — Compilación en Release
3. **unit-tests** — Tests unitarios con reporte TRX
4. **integration-tests** — Tests de API en memoria
5. **docker** — Build de imagen Docker (+ configuración comentada para push a ECR)

---

## Documentación adicional

- [`docs/C4_Architecture.md`](docs/C4_Architecture.md) — Diagramas C4 Nivel 1–3
- [`docs/EngineeringHandbook.md`](docs/EngineeringHandbook.md) — Estándares técnicos del squad
- [`docs/CodeReview.md`](docs/CodeReview.md) — Code review del snippet defectuoso
- [`docs/adr/`](docs/adr/) — Architecture Decision Records
