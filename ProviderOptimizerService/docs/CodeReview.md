# Code Review — Snippet Defectuoso
## Revisión técnica formal al estilo Pull Request profesional

**Revisado por:** Líder Técnico | **Severidad total:** CRÍTICA | **Veredicto:** REQUEST CHANGES

---

## Código Bajo Revisión

```csharp
public class ProviderService
{
    private List<Provider> providers = new List<Provider>();

    public Provider AssignProvider(Request request)
    {
        var availableProviders = providers.Where(p => p.IsAvailable == true).ToList();
        var provider = availableProviders[0];

        provider.IsAvailable = false;
        providers.Add(provider);

        var db = new SqlConnection("Server=myserver;Database=mydb;User Id=admin;Password=admin123;");
        db.Open();
        var cmd = new SqlCommand("INSERT INTO assignments VALUES ('" + request.Id + "','" + provider.Id + "')", db);
        cmd.ExecuteNonQuery();

        return provider;
    }
}
```

---

## Resumen Ejecutivo

Este código presenta **4 vulnerabilidades críticas de seguridad**, **1 error de concurrencia garantizado en producción**, y **6 violaciones adicionales** de calidad, mantenibilidad y arquitectura. **No puede ser mergeado en ningún estado**.

| Categoría | Hallazgos | Severidad más alta |
|-----------|-----------|-------------------|
| Seguridad | 2 | 🔴 CRÍTICA |
| Concurrencia | 1 | 🔴 CRÍTICA |
| Transacciones | 1 | 🔴 CRÍTICA |
| Asincronía | 1 | 🔴 ALTA |
| Lógica de negocio | 2 | 🟡 ALTA |
| Clean Architecture / SOLID | 4 | 🟡 ALTA |
| Recursos / Calidad | 2 | 🟢 NORMAL |

---

## Hallazgo 1 — [BLOCKER] SQL Injection

**Línea:** `"INSERT INTO assignments VALUES ('" + request.Id + "','" + provider.Id + "')"`

**Tipo:** Inyección SQL (OWASP A03:2021 — Injection)  
**CVSS v3.1:** 9.8 (CRÍTICO) — Attack Vector: Network, Privileges Required: None

**Descripción:**  
La query construye SQL por concatenación directa de parámetros del request. Un `request.Id` con valor `'; DROP TABLE assignments; --` ejecutaría DDL arbitrario. Un atacante con acceso al endpoint puede leer, modificar o destruir toda la base de datos.

**Impacto en ASISYA:**  
Exfiltración de datos de todos los proveedores, solicitudes y usuarios. Destrucción de datos operativos. Violación de GDPR/LGPD con multas de hasta el 4% de ingresos anuales.

**Corrección requerida:**
```csharp
// ✅ Opción 1: Parámetros tipados con ADO.NET
var cmd = new SqlCommand(
    "INSERT INTO assignments (request_id, provider_id) VALUES (@requestId, @providerId)",
    connection);
cmd.Parameters.Add("@requestId", SqlDbType.UniqueIdentifier).Value = request.Id;
cmd.Parameters.Add("@providerId", SqlDbType.UniqueIdentifier).Value = provider.Id;

// ✅ Opción 2 (preferida): EF Core — parametriza automáticamente
await _context.Assignments.AddAsync(new Assignment(request.Id, provider.Id), ct);
await _context.SaveChangesAsync(ct);
```

---

## Hallazgo 2 — [BLOCKER] Credenciales Hardcodeadas

**Línea:** `new SqlConnection("Server=myserver;Database=mydb;User Id=admin;Password=admin123;")`

**Tipo:** Secretos en código fuente (OWASP A02:2021 — Cryptographic Failures)

**Descripción:**  
Las credenciales de base de datos con usuario `admin` y password `admin123` están literalmente en el código fuente. Cualquier persona con acceso al repositorio (colaboradores, ex-empleados, bots que indexan GitHub) tiene acceso total a la base de datos de producción. El usuario `admin` sugiere privilegios máximos sobre toda la BD.

**Impacto adicional:**  
- Cualquier log de excepciones o trace puede revelar el connection string completo
- Si el repo es público, los secretos son inmediatamente explotables
- Viola PCI-DSS, SOC 2, ISO 27001 y cualquier normativa de seguridad de datos

**Corrección requerida:**
```csharp
// ✅ Inyectar IConfiguration — leer de AWS Secrets Manager / variables de entorno
public class ProviderService
{
    private readonly IConfiguration _configuration;

    public ProviderService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // En startup: la connection string se carga desde AWS Secrets Manager
    // NUNCA en código, NUNCA en appsettings.json commitado con valores reales
    var connectionString = _configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string not configured.");
}
```

---

## Hallazgo 3 — [BLOCKER] Race Condition — Asignación Doble Garantizada

**Líneas:** `var provider = availableProviders[0]; provider.IsAvailable = false;`

**Tipo:** Concurrencia / Thread Safety

**Descripción:**  
En cualquier servidor web con concurrencia (Kestrel procesa múltiples requests simultáneos), dos threads pueden ejecutar `availableProviders[0]` y leer el mismo proveedor al mismo tiempo. Ambos lo marcan como no disponible y ambos lo asignan. Resultado: dos usuarios con el mismo proveedor.

**Escenario concreto:**  
```
Thread A: lee providers[0] = "Grúas Rápido SA" (available)
Thread B: lee providers[0] = "Grúas Rápido SA" (available) ← mismo instante
Thread A: provider.IsAvailable = false
Thread B: provider.IsAvailable = false  ← ya fue cambiado por A, pero B ya lo tiene
Thread A: INSERT assignment (request_1, gruas_rapido)
Thread B: INSERT assignment (request_2, gruas_rapido) ← mismo proveedor, dos solicitudes
```

**Corrección requerida (opción recomendada — Bloqueo Optimista en BD):**
```csharp
// En SQL/EF Core: SELECT FOR UPDATE + versión de concurrencia
// UPDATE providers SET is_available = false, version = version + 1
// WHERE id = @id AND version = @expectedVersion AND is_available = true
// Si affectedRows == 0 → otro thread ganó, reintento con el siguiente candidato

// Con EF Core (RowVersion / Timestamp):
[Timestamp]
public byte[] RowVersion { get; private set; } = null!;
// EF lanza DbUpdateConcurrencyException si version no coincide → retry
```

---

## Hallazgo 4 — [BLOCKER] Ausencia de Transacciones — Inconsistencia de Estado

**Líneas:** Toda la función `AssignProvider`

**Tipo:** ACID / Integridad de datos

**Descripción:**  
La operación compuesta (marcar proveedor como no disponible + insertar en tabla assignments) no está dentro de una transacción. Si el INSERT falla (BD caída, timeout, constraint violation), el proveedor queda marcado como no disponible en memoria pero sin registro en la BD. Al reiniciar el servicio, el proveedor aparece como disponible nuevamente, generando un estado inconsistente irrecuperable sin intervención manual.

**Corrección requerida:**
```csharp
// ✅ Operación atómica usando Unit of Work
await using var transaction = await _context.Database.BeginTransactionAsync(
    IsolationLevel.ReadCommitted, ct);
try
{
    provider.AssignRequest();              // Domain: invariante de negocio
    _context.Providers.Update(provider);
    await _context.Assignments.AddAsync(new Assignment(requestId, provider.Id), ct);
    await _context.SaveChangesAsync(ct);
    await transaction.CommitAsync(ct);
}
catch
{
    await transaction.RollbackAsync(ct);
    throw; // Re-throw para que el caller maneje el error apropiadamente
}
```

---

## Hallazgo 5 — [BLOCKER] Código Síncrono Bloquea Thread Pool

**Líneas:** `db.Open()`, `cmd.ExecuteNonQuery()`

**Tipo:** Performance / Escalabilidad

**Descripción:**  
Las operaciones de I/O a base de datos son síncronas y bloquean el thread de ASP.NET Core mientras esperan respuesta. Con 100 requests concurrentes y cada operación BD tomando 50ms, se bloquean 100 threads simultáneamente. Kestrel tiene un thread pool limitado — este patrón bajo carga media lleva al **thread pool starvation**, que manifiesta como timeouts y 503s en cascada.

**Corrección requerida:**
```csharp
// ✅ Método asíncrono completo con CancellationToken
public async Task<ProviderDto> AssignProviderAsync(
    AssignProviderCommand command,
    CancellationToken ct = default)
{
    // ...
    await connection.OpenAsync(ct);
    await command.ExecuteNonQueryAsync(ct);
    // ✅ Mejor aún: delegar a EF Core que gestiona async internamente
}
```

---

## Hallazgo 6 — [BUG] Algoritmo de Selección Incorrecto (Siempre el Primero)

**Línea:** `var provider = availableProviders[0];`

**Descripción:**  
Siempre se selecciona el primer proveedor de la lista sin criterio alguno. En producción, el primer proveedor registrado en la BD recibirá el 100% de las solicitudes hasta quedarse sin capacidad. Los demás proveedores nunca serán asignados. Esto viola el objetivo de negocio principal del módulo.

**Corrección requerida:**
```csharp
// ✅ Algoritmo de scoring ponderado (ver OptimizationService.cs)
var ranked = _optimizationService.RankProviders(
    availableProviders,
    new GeoCoordinate(request.Latitude, request.Longitude),
    weights: null); // usa pesos por defecto

if (!ranked.Any())
    throw new NoProvidesAvailableException(request.RequiredType, request.Location);

var best = ranked[0]; // el de mayor score
```

---

## Hallazgo 7 — [BUG] IndexOutOfRangeException sin Manejo

**Línea:** `var provider = availableProviders[0];`

**Descripción:**  
Si no hay proveedores disponibles, `availableProviders` es una lista vacía y `[0]` lanza `IndexOutOfRangeException`. El caller recibe un 500 Internal Server Error sin información útil para distinguir "no hay proveedores" de un error del sistema.

**Corrección requerida:**
```csharp
// ✅ Caso sin proveedores manejado explícitamente con excepción de dominio
if (!ranked.Any())
    throw new NoProvidesAvailableException(request.RequiredType);
// NoProvidesAvailableException : DomainException
// ExceptionHandlingMiddleware la mapea a HTTP 422 Unprocessable Entity con mensaje descriptivo
```

---

## Hallazgo 8 — [BUG] Violaciones de SOLID

**Tipo:** Diseño / Mantenibilidad

| Principio | Violación | Consecuencia |
|-----------|-----------|-------------|
| **SRP** | `ProviderService` hace: consultar BD, asignar proveedor, conectar SQL, ejecutar SQL, gestionar estado en memoria | Imposible probar una responsabilidad sin todas las demás |
| **OCP** | Para cambiar el algoritmo de selección hay que modificar la clase | Riesgo de regresiones en cada cambio |
| **DIP** | Dependencia directa de `SqlConnection` (clase concreta) | Imposible testear sin una BD real |
| **ISP** | La clase hace demasiado — si hubiera interfaces, serían demasiado grandes | Clientes forzados a depender de métodos que no usan |

**Corrección — Separación de responsabilidades:**
```
Domain:          Provider.AssignRequest()         ← invariante de negocio
Domain Service:  OptimizationService.RankProviders ← algoritmo de selección
Application:     AssignProviderCommandHandler      ← orquestación del flujo
Infrastructure:  ProviderRepository.UpdateAsync    ← persistencia
API:             ProvidersController               ← HTTP + mapeo de DTOs
```

---

## Hallazgo 9 — [SUGGESTION] Sin DTOs — Exposición de Entidades de Dominio

**Tipo:** Arquitectura / Seguridad

**Descripción:**  
Retornar `Provider` (entidad de dominio) como respuesta acopla la API pública con el modelo interno. Cambios en el dominio rompen automáticamente los contratos de API. Además puede exponer campos sensibles o internos (como `DomainEvents`, `RowVersion`, etc.).

**Corrección:**
```csharp
// ✅ Siempre retornar DTOs desde la capa de Application
public record AssignProviderResponse(
    Guid ProviderId,
    string ProviderName,
    string ProviderPhone,
    double Score,
    int EtaMinutes,
    double DistanceKm);
```

---

## Hallazgo 10 — [SUGGESTION] Resource Leak — Sin `using`

**Líneas:** `var db = new SqlConnection(...)`, `var cmd = new SqlCommand(...)`

**Descripción:**  
`SqlConnection` y `SqlCommand` implementan `IDisposable`. Sin `using`, las conexiones no se devuelven al pool cuando ocurre una excepción. Bajo carga media-alta, el connection pool se agota y todas las requests nuevas esperan hasta timeout.

**Corrección:**
```csharp
// ✅ using statement garantiza Dispose() incluso si hay excepciones
await using var connection = new SqlConnection(connectionString);
await using var command = new SqlCommand(sql, connection);
// ✅ Aún mejor: dejar que EF Core gestione el ciclo de vida de conexiones
```

---

## Hallazgo 11 — [BLOCKER] Falta de Validaciones de Entrada (#56)

**Categoría:** Correctness / Security | **Severidad:** ALTA

### Problema
El código no valida ningún parámetro de entrada. Se asume que `latitude`, `longitude` y `type` son siempre válidos.

```csharp
// ❌ SIN VALIDACIÓN — ningún parámetro se verifica antes de usarse
public Provider GetBestProvider(double latitude, double longitude, string type)
{
    // latitude podría ser 999, NaN, o estar fuera de rango
    // type podría ser null, vacío, o inyección SQL
    var providers = db.ExecuteQuery("SELECT * ...");
}
```

### Impacto
- Coordenadas inválidas → scores sin sentido (distancias negativas o ∞)
- Tipo sin validar → abre vector de SQL Injection (Hallazgo 1)
- Sin mensajes de error claros → debugging en producción imposible

### Corrección propuesta
```csharp
// ✅ RankProvidersQueryValidator.cs — FluentValidation
public class RankProvidersQueryValidator : AbstractValidator<RankProvidersQuery>
{
    public RankProvidersQueryValidator()
    {
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Longitude must be between -180 and 180.");

        When(x => x.Weights is not null, () =>
        {
            RuleFor(x => x.Weights!.Rating + x.Weights!.Availability
                       + x.Weights!.Distance + x.Weights!.Eta)
                .Equal(1.0)
                .WithMessage("Scoring weights must sum to 1.0.");
        });
    }
}
// ValidationBehavior (MediatR pipeline) ejecuta esto antes del handler.
// Si falla → 400 Bad Request con lista de errores. Handler nunca recibe datos inválidos.
```

---

## Hallazgo 12 — [BUG] Sin Política de Selección — Algoritmo No Intercambiable (#59)

**Categoría:** Design / SOLID / Extensibilidad | **Severidad:** MEDIA

### Problema
El criterio de "mejor proveedor" está hardcodeado dentro de `GetBestProvider`. No existe abstracción que permita cambiar el algoritmo sin modificar la clase. Viola **OCP**: para usar scoring por rating, por ETA puro, o por cliente específico, hay que reescribir la clase.

```csharp
// ❌ SIN POLÍTICA DE SELECCIÓN — lógica de scoring pegada al método
public Provider GetBestProvider(double lat, double lon, string type)
{
    // Criterio hardcodeado: solo ETA, sin rating ni disponibilidad
    // Cambiar esto = modificar esta clase = riesgo de regresión
    return providers.OrderBy(p => p.ETA).First();
}
```

### Impacto
- Imposible A/B testear dos estrategias de scoring simultáneamente
- Cambios de negocio ("dar más peso al rating") requieren tocar código core
- No hay contrato formal; cualquiera puede cambiar la lógica sin revisión

### Corrección propuesta — Strategy Pattern
```csharp
// ✅ IScoringStrategy.cs (Domain/Interfaces) — contrato formal
public interface IScoringStrategy
{
    string Name { get; }
    ProviderScore ComputeScore(
        Provider provider, double distanceKm, double etaMinutes,
        double maxDist, double minDist, double maxEta, double minEta,
        ScoringWeights weights);
}

// ✅ WeightedScoringStrategy.cs — implementación concreta inyectable
public class WeightedScoringStrategy : IScoringStrategy
{
    public string Name => "WeightedMultiFactor";
    // Score = W_rating*(r/5) + W_avail*(1-assign/cap) + W_dist*(1-Nd) + W_eta*(1-Ne)
    public ProviderScore ComputeScore(...) { /* ver implementación completa en repo */ }
}

// ✅ OptimizationService — recibe estrategia por DI (no la hardcodea)
public class OptimizationService : IOptimizationService
{
    private readonly IScoringStrategy _strategy;
    public OptimizationService(IScoringStrategy strategy) => _strategy = strategy;

    public IReadOnlyList<...> RankProviders(IEnumerable<Provider> providers, ...)
    {
        // pre-compute metrics, then delegate scoring:
        var score = _strategy.ComputeScore(provider, dist, eta, ...);
    }
}

// ✅ DI — swap de estrategia sin tocar lógica:
services.AddScoped<IScoringStrategy, WeightedScoringStrategy>();
// A/B test: services.AddScoped<IScoringStrategy, EtaOnlyScoringStrategy>();
```

---

## Código Refactorizado Completo

El refactor completo de este snippet está implementado en los archivos del repositorio:

- `Domain/Entities/Provider.cs` → `AssignRequest()` con invariantes
- `Domain/Interfaces/IOptimizationService.cs` → contrato del algoritmo
- `Infrastructure/Services/OptimizationService.cs` → algoritmo completo
- `Application/Features/Providers/Commands/CreateProvider/` → Command + Handler + Validator
- `Infrastructure/Repositories/ProviderRepository.cs` → persistencia async + Unit of Work
- `API/Controllers/ProvidersController.cs` → HTTP layer limpio

---

## Veredicto Final

```
❌ REQUEST CHANGES — No mergeable

BLOCKER (5): SQL Injection, Credenciales hardcodeadas, Race condition,
             Sin transacciones, Sin validaciones de entrada
HIGH    (5): Código síncrono, Algoritmo incorrecto, IndexOutOfRange,
             Violaciones SOLID, Sin política de selección
MEDIUM  (2): Sin DTOs, Resource leak

12 hallazgos en total. El código debe ser completamente reescrito
siguiendo Clean Architecture + Strategy Pattern para scoring.
Ver implementación de referencia en ProviderOptimizerService/src/.
```
