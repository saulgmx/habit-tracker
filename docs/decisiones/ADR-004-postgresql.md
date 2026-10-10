# ADR-004: Migrar de SQLite a PostgreSQL (día 2)

- **Estado:** Propuesta (se implementa como tarea de día 2)
- **Fecha:** 2026-10-09

## Contexto
El día 1 se usa SQLite por simplicidad y menor riesgo en el demo. SQLite en un contenedor pierde los datos en cada despliegue o reinicio y no da paridad con entornos reales.

## Decisión
Al migrar:
- **Local y CI:** PostgreSQL en contenedor (`docker-compose` y *service container* de GitHub Actions).
- **Nube:** Supabase como PostgreSQL administrado, usado solo como base de datos (Npgsql + EF Core, sin su SDK).
- **Esquema:** migraciones de EF Core regeneradas para Postgres (las de SQLite no son portables), aplicadas como paso explícito del pipeline de CD.

## Alternativas descartadas
- **SQLite + volumen EFS:** menos piezas, pero sin paridad con entornos reales.
- **Postgres autoalojado en ECS:** requiere volumen persistente y backups propios.
- **Amazon RDS for PostgreSQL:** válido y natural en AWS, pero con más costo y configuración (VPC, subredes privadas) para un demo; opción para una evolución posterior.

## Consecuencias
- Una dependencia externa más (Supabase debe estar activo).
- La cadena de conexión es un secreto en GitHub y en AWS (Secrets Manager o Parameter Store), referenciado desde el servicio.
- Las migraciones deben ser compatibles hacia atrás: el rollback de imagen no revierte el esquema.
- Si se quieren conservar datos, exportarlos de SQLite e importarlos; con datos de ejemplo basta el seed.

## Criterios de aceptación (ver HU-08)
- [ ] La API arranca contra Postgres local con `docker compose`.
- [ ] Las pruebas pasan en CI con service container de Postgres.
- [ ] El CD aplica migraciones y despliega.
- [ ] Los datos sobreviven a un redespliegue.
- [ ] ADR-004 pasa a "Aceptada" y se actualizan README y runbook.
