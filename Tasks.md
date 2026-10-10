# Tasks

Estado del proyecto al 2026-10-09. Región de AWS: **us-east-2** (única permitida por el SCP de la cuenta).

## Hecho

### Preparación
- [x] Leer `habit-tracker-sdlc-kickoff.md` y extraer los documentos de la sección 10.
- [x] Verificar herramientas locales: dotnet 10, docker, gh, aws, cfn-lint, git.
- [x] Verificar la cuenta AWS (`habit-admin`, plan Free activo, $100 de crédito, vence 2027-04-09).
- [x] Detectar restricciones: solo us-east-2 permitida (SCP), OIDC denegado.
- [x] Confirmar que existe la VPC predeterminada en us-east-2.

### Paso 1: estructura y documentación (commit `3b68feb`)
- [x] `README.md`, `CONTRIBUTING.md`, `CHANGELOG.md`, `.gitignore`.
- [x] `docs/` por fase del SDLC (requisitos, diseño, pruebas, despliegue, mantenimiento).
- [x] ADR-001 a ADR-006 en `docs/decisiones/`.
- [x] `.github/PULL_REQUEST_TEMPLATE.md`.
- [x] `infra/aws/base.yaml` y `infra/aws/service.yaml` (pasan `cfn-lint`).
- [x] `scripts/setup-aws.sh` y `scripts/cleanup-aws.sh` (región por defecto us-east-2).

### Paso 2: API y pruebas (commit `9d21980`)
- [x] Solución `HabitTracker.sln` con API (.NET 10) y proyecto xUnit.
- [x] `StreakCalculator` con 15 pruebas unitarias.
- [x] EF Core + SQLite, migración `InitialCreate` y seed de ejemplo.
- [x] Endpoints de hábitos, check-ins y `/health`.
- [x] 8 pruebas de integración con SQLite en memoria (23 pruebas en total, todas en verde).
- [x] `dotnet-ef` como herramienta local (`dotnet-tools.json`).

### Paso 3: frontend PWA (commit `1561b12`)
- [x] `index.html`, `app.js`, `styles.css`, `manifest.json`, `sw.js`.
- [x] Íconos 192 y 512 px, e ícono SVG.
- [x] Verificado en local: `/`, `/health`, `/api/habits`, manifest y service worker responden 200.
- [x] Puerto local 5000 alineado con el README.

### Paso 4: contenedor (commit `419e3f9`)
- [x] `Dockerfile` multi-stage y `.dockerignore`.
- [x] Imagen construida y probada en Docker local.

### Pasos 5 y 6: CI/CD y scripts (commit `f5026c6`)
- [x] `.github/workflows/ci.yml` (restore, format, build, test, cfn-lint).
- [x] `.github/workflows/deploy.yml` (ECR + `aws cloudformation deploy` + health check).
- [x] `scripts/seed-issues.sh` (parser de historias probado; falta ejecutarlo con `gh`).

## Pendiente

### Requiere tu confirmación (crea recursos externos o publica)
- [ ] Crear el repo en GitHub y hacer push de `main`.
- [ ] Crear el presupuesto de $5 en AWS Budgets (alertas al 50, 90 y 100 %).
- [ ] Ejecutar `./scripts/setup-aws.sh <usuario>/<repo>` (crea stack base, usuario IAM, llaves y secrets de GitHub).
- [ ] Comprobar que el SCP permita crear usuarios IAM, roles y ECS Express Mode (no verificado).
- [ ] Crear el entorno `production` en GitHub (limitado a `main`) y mover ahí los secrets de AWS.
- [ ] Primer despliegue (merge a `main` o `workflow_dispatch`) y verificar `/health` en el móvil.
- [ ] Ensayar la creación del servicio con tiempo: las plantillas solo pasaron `cfn-lint`.
- [ ] Verificar `Cpu`/`Memory`, comportamiento de actualización y permisos del rol de despliegue en `AWS::ECS::ExpressGatewayService`.

### GitHub
- [ ] Crear el GitHub Project (tablero) y ejecutar `./scripts/seed-issues.sh <usuario>/<repo> [proyecto]` (requiere `gh auth refresh -s project`).
- [ ] Proteger `main`: PR con 1 aprobación, check `ci` en verde, sin push directo.
- [ ] Reemplazar `<usuario>` en el badge del `README.md`.

### Preparación del demo
- [ ] Crear la rama `demo/bug-racha` con el bug intencional en `StreakCalculator`.
- [ ] Probar la PWA instalada en un móvil real.
- [ ] Grabar captura o video de respaldo (plan B).
- [ ] Abrir la app 5 minutos antes del demo.
- [ ] Actualizar `CHANGELOG.md` al cerrar cada iteración (v0.1.0, v0.2.0, v0.3.0).
- [ ] Completar `docs/06-mantenimiento/retrospectiva.md` tras el demo.

### Después del demo
- [ ] `./scripts/cleanup-aws.sh service` (detiene Fargate y el balanceador).
- [ ] `./scripts/cleanup-aws.sh all` si ya no se necesita, y borrar los secrets de AWS en GitHub.
- [ ] Revisar en la consola que no queden balanceadores, servicios ECS, grupos de logs ni imágenes.
- [ ] Revisar Billing y Budgets al día siguiente.

### Documentación por ajustar
- [ ] Actualizar `habit-tracker-sdlc-kickoff.md`: región us-east-2 y restricciones del SCP (sigue diciendo us-east-1).
- [ ] Decidir si se versionan `habit-tracker-sdlc-kickoff.md` y `prompt-init.md` (hoy sin versionar).
- [ ] Pendientes de la sección 11 del kickoff: nombre definitivo de la app y del repo, hábitos de ejemplo definitivos.

### Día 2: migración a PostgreSQL (ADR-004, HU-08)
- [ ] Crear el proyecto en Supabase y obtener la cadena del pooler.
- [ ] Añadir `docker-compose.yml` en la raíz (referencia en `docs/05-despliegue/docker-compose.dia2.yml`).
- [ ] Cambiar el proveedor a Npgsql y regenerar las migraciones.
- [ ] CI: service container de Postgres.
- [ ] CD: paso de migraciones y secretos de conexión (Secrets Manager o Parameter Store).
- [ ] `/health` que compruebe la base de datos.
- [ ] Actualizar README y runbook, y pasar ADR-004 a "Aceptada".

### Opcional
- [ ] Azure como alternativa (ADR-006, Anexo A): requiere cuenta nueva y `deploy-azure.yml`.
