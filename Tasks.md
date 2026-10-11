# Tasks

Estado del proyecto al 2026-10-10. Región de AWS: **us-east-2** (única permitida por el SCP de la cuenta).

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
- [x] Limitar el entorno `production` a la rama `main`, rotar la llave de acceso y mover los secrets al entorno (los del repositorio se borraron).
- [x] Cambiar `deploy.yml` a solo manual (`workflow_dispatch`) para no crear recursos de pago por accidente.

### Primer despliegue en AWS (2026-10-10)
Tres intentos de `deploy.yml` a mano; los dos primeros fallaron por permisos que faltaban y se diagnosticaron con los eventos del stack y CloudTrail.
- [x] **Intento 1:** falló en `AWS::ECS::ExpressGatewayService`. El rol de infraestructura no tenía `ec2:DescribeAccountAttributes` y ELB devolvió `AccessDenied` al crear el balanceador. Arreglo: PR #14. Se actualizó el stack base y se limpió el servicio ECS huérfano y el stack en `ROLLBACK_COMPLETE`.
- [x] **Intento 2:** el servicio se creó, pero CloudFormation falló al leer `Endpoint`: al usuario de despliegue le faltaba `ecs:DescribeServiceRevisions`. Arreglo: PR #15 (también `ecs:DescribeTaskDefinition` para el usuario, y `ec2:DescribeInternetGateways`, `ec2:GetSecurityGroupsForVpc`, `ecs:DescribeServices` y `ecs:UpdateService` para el rol de infraestructura).
- [x] **Intento 3:** terminó sin errores en 6 min 29 s. Stack `habit-tracker-service` en `CREATE_COMPLETE`, 1 tarea corriendo.
- [x] Verificar desde fuera por HTTPS: `/health` 200, `/api/habits` con los datos sembrados, y `/`, `manifest.json`, `sw.js` e íconos con 200.
- [x] Confirmar que el SCP permite crear el servicio de Express Mode y los roles vinculados de ECS, ELB y Application Auto Scaling.
- [x] Confirmar que `Cpu` 256 y `Memory` 512 son un formato válido para `AWS::ECS::ExpressGatewayService`.

- [x] Probar la PWA en un iPhone real (Safari) con la URL HTTPS del servicio: se instala con Añadir a pantalla de inicio y funciona sin conexión. Cierra la HU-07.
- [x] Comprobar con Chrome (CDP) que la app cumple los criterios de instalabilidad (`installabilityErrors` vacío).
- [x] Borrar el servicio con `./scripts/cleanup-aws.sh service us-east-2` y comprobar que no queda servicio ECS, balanceador, target group, security group propio ni alarma. El stack base sigue intacto.
- [x] Actualizar la documentación con lo aprendido (PR #18): runbook (sección «Si el despliegue falla», stack en `ROLLBACK_COMPLETE`, servicio ECS huérfano, rotación de llaves y limpieza), ADR-005 (política `express-extra`, permisos del usuario de despliegue, SCP de región y OIDC), ADR-002, pipeline CI/CD, costos, README, CONTRIBUTING y retrospectiva.

### Verificación del frontend
- [x] Probar la UI en Brave headless con vista de móvil: lista, marcar y desmarcar, crear, nombre vacío, eliminar, service worker y modo offline (15 chequeos en verde).
- [x] Corregir la fecha ("9 De Octubre" a "9 de Octubre") con `::first-letter`.

### Ensayo del bug del demo (2026-10-10)
- [x] Crear la rama `demo/bug-racha` con el bug intencional en `StreakCalculator` (commit `645dea9`): `Current` empieza siempre en hoy, así que rompe el caso 4 del plan de pruebas (ayer y anteayer sin hoy dan 0 en vez de 2).
- [x] Abrir el PR #22 en borrador («NO integrar») y comprobar el CI en rojo: falla `Current_AyerYAnteayerSinHoy_SigueViva`, 22 de 23 pasan.
- [x] Corregir y comprobar el CI en verde (23 de 23).
- [x] Dejar la rama con el bug puesto y el CI en rojo (`git reset --hard 645dea9` y `push --force-with-lease`). Para el demo: abrir el CI del PR #22, mostrar la prueba que falla, corregir `StreakCalculator.cs` (`var cursor = set.Contains(today) ? today : today.AddDays(-1);`) y empujar para ver el verde. La rama no se integra en `main`.

### Segundo despliegue: actualización de imagen (2026-10-10)
- [x] Crear el servicio con `deploy.yml` sobre `79cd55d` (run `38074116647`): verde en 7 min 6 s; `/health`, `/api/habits`, `/`, `manifest.json` y `sw.js` dieron 200.
- [x] Integrar el PR #24 para tener un SHA nuevo en `main` (`33b709b`). El workflow etiqueta la imagen con el SHA: con el mismo commit el changeset sería vacío y no probaría la actualización.
- [x] Lanzar `deploy.yml` otra vez (run `38074956407`): verde en 9 min 58 s. El stack pasó de `CREATE_COMPLETE` a `UPDATE_COMPLETE` (el recurso `Service` tardó unos 9 min), sin errores ni rollback y sin permisos que faltaran. La tarea en ejecución usa la imagen `33b709b` (definición de tarea `habit-tracker-habit-tracker:5`) y `/health`, `/api/habits` y `/` dieron 200.
- [x] Borrar el servicio con `./scripts/cleanup-aws.sh service us-east-2` y comprobar en solo lectura: queda solo `habit-tracker-base`, sin servicios, tareas, balanceadores ni target groups.
- No verificado: si hubo caída del servicio durante la actualización (no se sondeó mientras duraba).

### Kickoff y ADR (2026-10-10)
- [x] Actualizar `habit-tracker-sdlc-kickoff.md` en local: nota de «Revisión 8», región `us-east-2`, restricciones del SCP y `aws configure` en lugar de `aws configure sso` y `aws sso login`. Las plantillas incrustadas y lo de Supabase se dejan como contenido inicial (la nota avisa del desfase).
- [x] Decidir que el kickoff **no se versiona** (queda solo local, como dice `CLAUDE.md`; `prompt-*.md` entra en la misma regla).
- [x] ADR-005: quitar la referencia a «la sección de riesgos del kickoff» y apuntar a `pipeline-cicd.md` y al runbook, que sí están en el repo.

### Día 2: migración a PostgreSQL (ADR-004, HU-08), 2026-10-10
Fases A y B en PR #28 y #29, fase C probada en AWS y fase D (documentación) en este PR. HU-08 cumplida.
- [x] Proveedor Npgsql y migración `InitialCreate` regenerada; `docker-compose.yml` con `db` y `api`; CI con service container de Postgres y 28 pruebas contra Postgres real (PR #28).
- [x] `/health` que comprueba la base de datos (200, o 503 si no responde).
- [x] Cadena de conexión por partes (`Database__Host`, etc.) y cifrado GSS desactivado en Npgsql (quita el aviso de `libgssapi_krb5`); puerto local 5050 porque el 5000 lo ocupa AirPlay en macOS; mensaje claro con reintento si la API no responde (en vez de «Failed to fetch»).
- [x] Plantillas: `infra/aws/db.yaml` (RDS PostgreSQL 16, `db.t4g.micro`, 20 GiB gp3 cifrado, privada, contraseña administrada por RDS en Secrets Manager), `service.yaml` con grupo de seguridad adicional y secretos, `scripts/setup-db.sh` y modo `db` en `cleanup-aws.sh` (PR #29).
- [x] Crear la base con `setup-db.sh`: sin errores y en menos de 10 minutos; el SCP no la bloqueó. RDS creó solo el rol vinculado `AWSServiceRoleForRDS`.
- [x] Desplegar contra RDS (run `38091875241`, 6 min 19 s): `/health` con `"database":"ok"`, `/api/habits` con los datos sembrados, `/`, `manifest.json` y `sw.js` con 200. Sin permisos faltantes.
- [x] Persistencia: un hábito marcador sobrevivió, con su check-in, a un redespliegue de 10 min que reemplazó la tarea (`:6` a `:7`, imagen `fc10a5a` a `d571d2d`); el seed no se duplicó.
- [x] Medir el retraso al marcar un hábito: es de red (~60 ms por viaje, 1 a 5 ms de procesamiento) y viene de las dos peticiones seguidas (marcar y recargar la lista) sin actualizar la pantalla hasta recibir ambas.
- [x] Borrar servicio y base con `./scripts/cleanup-aws.sh db us-east-2` y comprobar en solo lectura: queda solo `habit-tracker-base`, sin instancias RDS, secretos, servicios, tareas, balanceadores, target groups ni security groups del proyecto. Quedan 5 grupos de logs y 7 revisiones de la definición de tarea (decidido dejarlos hasta el final del demo).
- [x] Documentación: ADR-004 a «Aceptada», ADR-001, ADR-002 y ADR-005, arquitectura, modelo de datos, convenciones, plan de pruebas, historias de usuario (HU-08), pipeline, costos, runbook y README.
- [x] Crédito adicional: se aplicaron 20 USD por usar RDS y 20 USD por usar AWS Budgets (confirmado por el dueño en la consola). El plan Free queda con 139.94 USD de crédito (antes 100) hasta el 2027-04-09.
- No verificado: gasto real en Billing, cargo del secreto de Secrets Manager, TLS en la conexión a RDS y arranque simultáneo de dos tareas con `Migrate()` (detalle en ADR-004, «Por verificar»).

### Marcar un hábito responde al instante (HU-10, issue #32), 2026-10-10
- [x] Documentar la HU-10 en `docs/01-requisitos/historias-usuario.md`.
- [x] API: `PUT` y `DELETE` de check-ins devuelven el hábito actualizado (`200`) y aceptan `?today=`; un `PUT` simultáneo del mismo día no falla ni duplica. 4 pruebas nuevas y 3 ajustadas (32 en total, contra Postgres real), `docs/02-diseno/api.md` actualizado.
- [x] Interfaz: estado en memoria; al tocar, la casilla, el punto de hoy y la racha cambian al instante (la racha actual sube o baja 1; la máxima solo sube hasta que el servidor confirme). Las peticiones salen en orden por hábito y al terminar la última se usa la respuesta del servidor, sin recargar la lista. Si falla, se revierte y el error sigue visible. El botón conserva el foco.
- [x] Verificado en Brave: 13 comprobaciones con un servidor de prueba (latencia de 1.5 s, fallo 500, tres toques seguidos con peticiones en orden y nunca dos a la vez, foco) y una prueba de humo con la API real y Postgres. Esos guiones no están en el repo; la tarea opcional de Playwright los reemplazaría.
- [x] Probado en un iPhone real contra AWS (servicio y base recreados con `setup-db.sh` y un Deploy sobre `9135813`; run `38100356701`, 6 min 33 s): el dueño del proyecto confirmó que se ve bien. Desde el equipo de pruebas, un `PUT` con conexión nueva tardó de 180 a 264 ms (6 medidas), igual que antes: la mejora es que la pantalla ya no espera esa respuesta. No se midió la latencia percibida en el móvil, solo la impresión del dueño. Servicio y base borrados después con `cleanup-aws.sh db`.

## Pendiente

### Requiere tu confirmación (crea recursos externos o publica)
- [ ] El servicio y la base de datos están borrados (no hay costo de Fargate, balanceador ni RDS). Cada nuevo despliegue necesita `./scripts/setup-db.sh` y un Deploy, y vuelve a cobrar por hora: borrarlos con `./scripts/cleanup-aws.sh db` al terminar.

### GitHub
- [ ] Volver a activar el despliegue automático en `deploy.yml` (hoy solo manual con `workflow_dispatch`) cuando el entorno esté ensayado.
- [ ] Decidir si desactivar el workflow "Auto-close issue" del proyecto (hoy cierra solos los issues al pasar a Done) para cerrarlos con `Closes #N` en los PRs.
- [ ] Decidir si se quita la regla de aprobación o se invita a un segundo colaborador (hoy el merge exige bypass de administrador).

### Preparación del demo
- [ ] Grabar captura o video de respaldo (plan B).
- [ ] Abrir la app 5 minutos antes del demo.
- [ ] Actualizar `CHANGELOG.md` al cerrar cada iteración (v0.1.0, v0.2.0, v0.3.0).
- [ ] Completar `docs/06-mantenimiento/retrospectiva.md` tras el demo.

### Después del demo
- [ ] `./scripts/cleanup-aws.sh db` (detiene Fargate y el balanceador y borra la base de datos) si siguen creados.
- [ ] `./scripts/cleanup-aws.sh all` si ya no se necesita, y borrar los secrets de AWS en GitHub.
- [ ] Revisar en la consola que no queden balanceadores, servicios ECS, grupos de logs ni imágenes.
- [ ] Borrar los restos de los despliegues que CloudFormation no gestiona. Decidido (2026-10-10): se dejan hasta terminar el demo y se borran entonces, juntos; sin costo relevante.
  - Grupos de logs `/aws/ecs/habit-tracker/habit-tracker-` con sufijo `5c44`, `97b8`, `d4e5` y `e908` (0 bytes, sin expiración al 2026-10-10; cada despliegue crea uno nuevo).
  - Definiciones de tarea `habit-tracker-habit-tracker`, revisiones 1 a 5 en `ACTIVE` al 2026-10-10 (más las que cree cada despliegue). Hay que darlas de baja (`deregister-task-definition`) y luego eliminarlas (`delete-task-definitions`). Irreversible: pedir permiso antes.
  - Valorar añadir este borrado a `cleanup-aws.sh`.
- [ ] Revisar Billing y Budgets al día siguiente.

### Documentación por ajustar
- [ ] Pendientes de la sección 11 del kickoff: nombre definitivo de la app y del repo, hábitos de ejemplo definitivos.

### Día 2: pendientes
- [ ] Un usuario de aplicación con menos privilegios que el administrador de RDS (hoy la API usa el administrador; suficiente para el demo).

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
