# ADR-001: Stack tecnológico

- **Estado:** Aceptada
- **Fecha:** 2026-10-09

## Contexto
Demo de un día para público poco técnico, con foco en CI/CD. El responsable domina .NET.

## Decisión
Backend con ASP.NET Core Minimal API y SQLite en el día 1 (migración a PostgreSQL planeada en ADR-004); frontend en JavaScript vanilla servido desde la misma API.

## Alternativas descartadas
- **React/Vue + build:** añade herramientas y ruido al pipeline.
- **Solo frontend con localStorage:** no muestra un backend real en CI/CD.

## Consecuencias
- Un solo proyecto desplegable.
- Sin CORS.
- Sin GitHub Pages: se requiere hosting con soporte para .NET.
