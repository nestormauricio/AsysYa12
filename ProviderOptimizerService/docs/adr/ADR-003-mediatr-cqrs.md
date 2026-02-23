# ADR-003: MediatR para implementación de CQRS

**Estado:** Aceptado  
**Fecha:** 2024-01-01

## Contexto

Con Clean Architecture adoptada (ADR-001), necesitamos un mecanismo para implementar el patrón CQRS (Commands y Queries separados) y pipeline behaviors transversales (logging, validación) sin acoplar directamente los handlers a los controladores.

## Decisión

Usar **MediatR 12** para despacho de Commands y Queries, con pipeline behaviors para:
- `ValidationBehavior<TRequest, TResponse>` — FluentValidation automático antes de cada handler
- `LoggingBehavior<TRequest, TResponse>` — logging de entrada/salida sin código en cada handler

## Consecuencias

**Positivo:**
- Controllers delgados: solo mapean HTTP → Command/Query → Response
- Validación y logging automáticos sin duplicar código
- Fácil extensión: agregar behavior de caching, métricas, retry sin tocar handlers
- Testing de handlers sin instanciar controladores

**Negativo/Tradeoffs:**
- Overhead de reflexión en resolución de handlers (imperceptible en comparación con I/O)
- Debugging menos directo (no hay call stack lineal obvio)

## Alternativas Consideradas

- **Implementación manual de Command Bus:** Más control pero requiere mucho boilerplate.
- **Sin CQRS, Services directos:** Más simple inicialmente pero mezcla lecturas y escrituras, complica testing.
