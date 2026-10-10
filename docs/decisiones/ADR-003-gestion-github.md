# ADR-003: Gestión con GitHub Issues y Projects

- **Estado:** Aceptada (reemplaza la gestión con Azure DevOps)
- **Fecha:** 2026-10-09

## Contexto
Se pasó a AWS y se quiere mostrar el ciclo completo en una sola plataforma.

## Decisión
Backlog y tablero en GitHub Issues y GitHub Projects; repositorio y pipelines en GitHub Actions. Los PRs enlazan issues con `Closes #123`. La documentación vive en `docs/`, sin Wiki externa.

## Alternativas descartadas
- **Azure DevOps (Boards + Wiki):** funciona con GitHub, pero añade una plataforma, una autenticación y una integración sin aportar al mensaje del demo.

## Consecuencias
- Los issues se cierran solos al hacer merge de PRs enlazados.
- Todo el flujo se ve en GitHub (issue, rama, PR, CI, CD, despliegue).
- `scripts/seed-issues.sh` crea labels e issues a partir de las historias; requiere `gh` con el permiso `project`.
