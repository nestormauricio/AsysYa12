# ADR-001: Adopción de Clean Architecture

**Estado:** Aceptado  
**Fecha:** 2024-01-01  
**Decisores:** Líder Técnico, CTO

## Contexto

El equipo necesita una arquitectura que permita:
- Testear la lógica de negocio de forma aislada (sin BD real)
- Cambiar detalles de infraestructura (ORM, cache, mensajería) sin impactar el dominio
- Incorporar nuevos desarrolladores con un patrón mental claro y consistente
- Prepararse para escalar hacia múltiples microservicios con el mismo estándar

## Decisión

Adoptar **Clean Architecture** (Robert C. Martin) con capas: Domain → Application → Infrastructure → API, donde las dependencias solo fluyen hacia adentro (Dependency Inversion).

Complementado con:
- **CQRS** via MediatR para separar lecturas de escrituras
- **Domain Events** para comunicación entre bounded contexts
- **Unit of Work + Repository** para abstraer persistencia

## Consecuencias

**Positivo:**
- Domain Layer 100% testeable sin infraestructura
- Cambio de PostgreSQL a otro motor de BD requiere solo modificar Infrastructure
- Nuevos features siguen el mismo patrón: Command → Handler → Repository
- Validación centralizada en FluentValidation antes de llegar al handler

**Negativo/Tradeoffs:**
- Mayor cantidad de archivos y clases vs. un controller directo a BD
- Curva de aprendizaje inicial para desarrolladores no familiarizados con CQRS
- Overhead de MediatR en operaciones simples (aceptable dado los beneficios)

## Alternativas Consideradas

- **MVC tradicional (Controller → Service → Repository):** Más simple pero acopla casos de uso con HTTP; dificulta testing.
- **Vertical Slice Architecture:** Considerado pero descartado por menor claridad en proyectos medianos con múltiples bounded contexts.
- **CQRS con event sourcing completo:** Demasiada complejidad para el estado actual del producto.
