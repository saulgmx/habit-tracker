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

### GitHub
- [x] Crear el repo `saulgmx/habit-tracker` y hacer push de `main`.
- [x] Proteger `main`: PR con 1 aprobación y check `ci` en verde, sin force push ni borrado. `enforce_admins` apagado: el administrador puede saltarse la aprobación (eres el único colaborador).
- [x] Crear el GitHub Project https://github.com/users/saulgmx/projects/1 (privado, enlazado al repo).
- [x] Crear los labels y los issues #1 a #9 con `seed-issues.sh`, ya dentro del tablero.
- [x] Mover las tarjetas: #1 a #6 en Done, #7 y #9 en In Progress, #8 en Todo.
- [x] Probar el flujo de rama protegida con el PR #10 (CI en verde y merge con bypass de administrador).
- [x] Reemplazar `<usuario>` en el badge y el ejemplo de `setup-aws.sh` del `README.md`.
- [x] Cambiar `aws sso login` por `aws configure` en README y runbook (la cuenta usa un usuario IAM, no IAM Identity Center).

### AWS
- [x] Crear el presupuesto `habit-tracker-5usd` ($5 al mes, alertas por correo al 50, 90 y 100 %).
- [x] Verificar en solo lectura que ECS, ECR, CloudFormation, ELB, EC2, Logs e IAM responden en us-east-2.
- [x] Ejecutar `./scripts/setup-aws.sh saulgmx/habit-tracker us-east-2`: stack `habit-tracker-base` creado (ECR, cluster ECS, 2 roles IAM y usuario `habit-tracker-github-deploy` con 1 llave).
- [x] Guardar en GitHub (repositorio) los secrets `AWS_ACCESS_KEY_ID` y `AWS_SECRET_ACCESS_KEY` y las variables `AWS_REGION` y `ECR_REPOSITORY`.
- [x] Confirmar que el SCP permite crear usuario y roles IAM.

### Verificación del frontend
- [x] Probar la UI en Brave headless con vista de móvil: lista, marcar y desmarcar, crear, nombre vacío, eliminar, service worker y modo offline (15 chequeos en verde).
- [x] Corregir la fecha ("9 De Octubre" a "9 de Octubre") con `::first-letter`.

## Pendiente

### Requiere tu confirmación (crea recursos externos o publica)
- [ ] Comprobar que el SCP permita crear el servicio de ECS Express Mode y los roles vinculados de ECS y ELB (solo se sabe al crearlo).
- [ ] Crear el entorno `production` en GitHub (limitado a `main`) y mover ahí los secrets de AWS.
- [ ] Primer despliegue (merge a `main` o `workflow_dispatch`) y verificar `/health` en el móvil.
- [ ] Ensayar la creación del servicio con tiempo: las plantillas solo pasaron `cfn-lint`.
- [ ] Verificar `Cpu`/`Memory`, comportamiento de actualización y permisos del rol de despliegue en `AWS::ECS::ExpressGatewayService`.

### GitHub
- [ ] Decidir qué hacer con `deploy.yml`: falla en rojo en cada push a `main` mientras no existan `AWS_REGION` ni los secrets (hacerlo solo manual con `workflow_dispatch` hasta entonces).
- [ ] Decidir si desactivar el workflow "Auto-close issue" del proyecto (hoy cierra solos los issues al pasar a Done) para cerrarlos con `Closes #N` en los PRs.
- [ ] Decidir si se quita la regla de aprobación o se invita a un segundo colaborador (hoy el merge exige bypass de administrador).

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
- [ ] Actualizar `habit-tracker-sdlc-kickoff.md`: región us-east-2, restricciones del SCP y `aws configure` en lugar de `aws configure sso` (sigue diciendo us-east-1 y SSO).
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
- [ ] Pasar el servicio a ARM64 (Graviton) en Express Mode: hasta 40 % mejor relación precio-rendimiento según AWS. Hoy se usa x86_64 (valor por defecto) y el ahorro con 0.25 vCPU y 0.5 GiB sería de centavos.
  - Bloqueo: la CLI de ECS ya tiene `--cpu-architecture` (`X86_64` o `ARM64`), pero el esquema de `AWS::ECS::ExpressGatewayService` en CloudFormation (consultado el 2026-10-09 en us-east-2) no tiene `CpuArchitecture`. Volver a consultarlo con `aws cloudformation describe-type --region us-east-2 --type RESOURCE --type-name AWS::ECS::ExpressGatewayService`.
  - Cuando exista la propiedad: añadir `CpuArchitecture: ARM64` en `infra/aws/service.yaml`.
  - Construir la imagen para `linux/arm64` en `deploy.yml` (runner `ubuntu-24.04-arm` o buildx con QEMU). Si no coincide con la arquitectura del servicio, el contenedor falla al arrancar con *exec format error*.
  - Si CloudFormation tarda, crear el servicio con la CLI cambiaría ADR-005 y reabre la deriva entre plantilla y servicio: no recomendado antes del demo.
- [ ] Pruebas E2E de la UI con Playwright para .NET (`Microsoft.Playwright`) en `tests/HabitTracker.Tests`, sin añadir Node. Hacerlo después del demo.
  - Job aparte y no obligatorio en `ci.yml`, para que no bloquee el check `ci` que protege `main`.
  - 3 o 4 pruebas de humo: cargar la lista, marcar un hábito, crear uno y comprobar que el service worker se registra.
  - Ya se probó a mano en Brave (15 chequeos en verde, incluido offline); el script no está en el repo.
- [ ] Azure como alternativa (ADR-006, Anexo A): requiere cuenta nueva y `deploy-azure.yml`.
