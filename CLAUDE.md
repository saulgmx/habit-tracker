# Habit Tracker

PWA para crear hábitos y ver rachas. Proyecto de demostración del SDLC y CI/CD con GitHub Actions y AWS.
Estado y pendientes en `Tasks.md`; documentación por fase del SDLC en `docs/` (ADRs en `docs/decisiones/`).

## Comandos
- Correr: `docker compose up -d --wait db` y `dotnet run --project src/HabitTracker.Api` → http://localhost:5000 (migra y siembra datos al arrancar).
- Pruebas: `docker compose up -d --wait db` y `dotnet test` (unitarias de rachas e integración con una base PostgreSQL por ejecución; el servidor sale de `TEST_POSTGRES`).
- Antes de un PR: `dotnet format --verify-no-changes`, `dotnet build -c Release`, `dotnet test` y `cfn-lint infra/aws/*.yaml`.
- Contenedor: `docker compose up --build` → http://localhost:8080 (la imagen sola necesita `ConnectionStrings__Default`).

## Estructura
- `src/HabitTracker.Api`: Minimal API (.NET 10, EF Core + PostgreSQL con Npgsql). La lógica de rachas está en `Domain/StreakCalculator.cs`, pura y sin EF ni HTTP.
- `src/HabitTracker.Api/wwwroot`: frontend JS vanilla, manifest y service worker (solo cachea la interfaz, no la API).
- `tests/HabitTracker.Tests`: xUnit.
- `infra/aws`: CloudFormation. `base.yaml` (ECR, cluster, roles, usuario de despliegue) y `service.yaml` (servicio ECS Express Mode).
- `scripts/`: `setup-aws.sh`, `cleanup-aws.sh` y `seed-issues.sh`.

## Reglas de trabajo
1. **`main` está protegida:** sin push directo. Siempre rama + PR (`tipo/descripcion`) y commits `tipo: mensaje (#issue)`. El merge lo hace el dueño del repo con bypass de administrador cuando el CI está en verde.
2. **Antes de borrar una rama, comprobar que su PR esté `MERGED`:** borrar la rama de un PR abierto lo cierra.
3. **AWS: pedir permiso antes de cualquier acción que cree, cambie o borre recursos** (CloudFormation, ECS, IAM, ECR, GitHub secrets). Decir qué se va a hacer, el costo y si es reversible. Las consultas de solo lectura no lo necesitan.
4. **Nunca imprimir ni escribir en el repo secretos, llaves, IDs de cuenta de AWS ni correos.** El repositorio es público. No tocar los secrets de GitHub sin permiso.
5. **No documentar lo que no se haya verificado.** Si algo es una suposición, decirlo.
6. Los archivos locales sin versionar (`habit-tracker-sdlc-kickoff.md`, `prompt-*.md`) no se suben.

## AWS (datos estables)
- **Solo se puede usar la región `us-east-2`:** un SCP de la organización deniega EC2, ECS, ECR, CloudFormation y ELB en las demás, y también OIDC de IAM (ADR-005).
- La cuenta usa un usuario IAM (no IAM Identity Center): autenticación con `aws configure`.
- Despliegue con ECS Express Mode (Fargate + balanceador administrado) y plantillas de CloudFormation. Arquitectura x86_64.
- `deploy.yml` es **solo manual** (Actions → Deploy → Run workflow, sobre `main`). Los secrets de AWS viven en el entorno `production`, limitado a `main`.
- **Cada despliegue crea Fargate y un balanceador que cobran por hora.** Borrar el servicio al terminar: `./scripts/cleanup-aws.sh service us-east-2`. Tras borrarlo quedan grupos de logs y revisiones de la definición de tarea que hay que borrar a mano.
- Si el despliegue falla, diagnosticar con los eventos del stack, `aws ecs describe-services` y CloudTrail (ver «Si el despliegue falla» en `docs/05-despliegue/runbook.md`). Un stack en `ROLLBACK_COMPLETE` hay que borrarlo antes de reintentar.

## Convenciones
- Idioma: español en documentación, mensajes de commit y textos de la UI.
- Evitar SQL crudo y funciones propias de un proveedor de base de datos (solo se usa SQL crudo en la preparación de las pruebas).
- Migraciones de EF Core: una por cambio de modelo (se regeneran al cambiar de proveedor).
- CloudFormation: parámetros con descripción y valores por defecto razonables; validar con `cfn-lint`.
