# ADR-002: PostgreSQL como BD principal + Redis para caché

**Estado:** Aceptado  
**Fecha:** 2024-01-01  
**Decisores:** Líder Técnico, DevOps

## Contexto

El sistema requiere persistencia transaccional ACID para entidades de dominio (Providers, AssistanceRequests) y caché de alta velocidad para resultados de optimización que son costosos de recalcular y cambian con baja frecuencia (TTL ~60 segundos).

## Decisión

- **PostgreSQL 16** como base de datos principal (RDS Multi-AZ en producción)
- **Redis 7** vía ElastiCache como caché distribuido y eventual session store para WebSocket (LocationService)
- **EF Core 8** con migrations versionadas como ORM; sin raw SQL salvo casos de performance justificados

## Consecuencias

**Positivo:**
- PostgreSQL soporta JSONB (service_types de Provider), índices GiST para geodatos, y extensión PostGIS si se requiere en futuro
- Redis ElastiCache Multi-AZ con replicación automática; fallo de Redis no es catastrófico (fallback a BD)
- Migrations EF Core versionadas en código = evolución del schema trazable en Git

**Negativo/Tradeoffs:**
- Dos sistemas de persistencia que operar y monitorear
- Inconsistencia eventual posible entre cache Redis y estado en PostgreSQL (aceptable por TTL corto)

## Alternativas Consideradas

- **MySQL:** Descartado por menor soporte de tipos avanzados y menor comunidad .NET.
- **MongoDB:** Descartado; el modelo de datos es relacional con JOINs necesarios entre Providers y AssistanceRequests.
- **Memcached:** Descartado; Redis ofrece persistencia opcional, pub/sub y estructuras de datos más ricas.
