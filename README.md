# Habit Tracker

Aplicación web progresiva (PWA) para crear hábitos, marcarlos cada día y ver tus rachas. Proyecto de ejemplo para demostrar el SDLC y CI/CD con GitHub y AWS.

![CI](https://github.com/saulgmx/habit-tracker/actions/workflows/ci.yml/badge.svg)

## Qué hace

- Crear y eliminar hábitos.
- Marcar un hábito como hecho hoy.
- Ver la racha de días consecutivos.
- Instalarse en el móvil como app.

## Cómo correrla

Necesita PostgreSQL (ADR-004). En local se levanta con Docker:

```bash
docker compose up -d --wait db                       # PostgreSQL en localhost:5432 (credenciales de ejemplo)
dotnet run --project src/HabitTracker.Api            # migra y siembra datos al arrancar
# abrir http://localhost:5000
```

Para correr todo en contenedores: `docker compose up --build` y abrir http://localhost:8080.

## Pruebas

Las pruebas de integración usan una base PostgreSQL propia por ejecución, en el servidor de `docker compose up -d --wait db` (o el de la variable `TEST_POSTGRES`).

```bash
docker compose up -d --wait db
dotnet test
```

## Contenedor

```bash
docker build -t habit-tracker .
docker run -p 8080:8080 -e ConnectionStrings__Default="Host=<servidor>;Database=habits;Username=habits;Password=<clave>" habit-tracker
```

## Infraestructura (AWS)

```bash
aws configure                                          # usuario IAM (esta cuenta no usa IAM Identity Center)
aws sts get-caller-identity
./scripts/setup-aws.sh saulgmx/habit-tracker us-east-2   # una sola vez: ECR, cluster, roles, usuario IAM y secrets de GitHub
# Desplegar: Actions -> Deploy -> Run workflow (solo manual; crea el servicio en unos 6 minutos).
./scripts/cleanup-aws.sh service                    # al terminar: borrar el servicio (Fargate y balanceador cobran por hora)
```

La infraestructura está definida en `infra/aws/` con CloudFormation (ver ADR-005). La cuenta solo permite la región `us-east-2`. Controles de costo en `docs/05-despliegue/costos.md`.

## Documentación

Ver la carpeta [`docs/`](docs/) organizada por fase del SDLC.
