# Habit Tracker

Aplicación web progresiva (PWA) para crear hábitos, marcarlos cada día y ver tus rachas. Proyecto de ejemplo para demostrar el SDLC y CI/CD con GitHub y AWS.

![CI](https://github.com/<usuario>/habit-tracker/actions/workflows/ci.yml/badge.svg)

## Qué hace

- Crear y eliminar hábitos.
- Marcar un hábito como hecho hoy.
- Ver la racha de días consecutivos.
- Instalarse en el móvil como app.

## Cómo correrla

```bash
dotnet tool restore                                  # una vez: instala dotnet-ef (manifiesto local)
dotnet ef database update --project src/HabitTracker.Api   # opcional: la app también migra al arrancar
dotnet run --project src/HabitTracker.Api
# abrir http://localhost:5000
```

## Pruebas

```bash
dotnet test
```

## Contenedor

```bash
docker build -t habit-tracker .
docker run -p 8080:8080 habit-tracker
```

## Infraestructura (AWS)

```bash
aws sso login
./scripts/setup-aws.sh <usuario>/<repo> us-east-2   # una sola vez: ECR, cluster, roles, usuario IAM y secrets de GitHub
# El primer merge a main (o ejecutar el workflow a mano) crea el servicio.
./scripts/cleanup-aws.sh service                    # al terminar: borrar el servicio
```

La infraestructura está definida en `infra/aws/` con CloudFormation (ver ADR-005). Controles de costo en `docs/05-despliegue/costos.md`.

## Documentación

Ver la carpeta [`docs/`](docs/) organizada por fase del SDLC.

---

### 📄 `docker-compose.yml` (día 2)

Se añade al migrar a PostgreSQL. Solo para desarrollo local (credenciales de ejemplo).

```yaml
services:
  db:
    image: postgres:16
    environment:
      POSTGRES_USER: habits
      POSTGRES_PASSWORD: habits
      POSTGRES_DB: habits
    ports:
      - "5432:5432"
    volumes:
      - dbdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U habits"]
      interval: 5s
      timeout: 3s
      retries: 10

  api:
    build: .
    ports:
      - "8080:8080"
    environment:
      ConnectionStrings__Default: "Host=db;Port=5432;Database=habits;Username=habits;Password=habits"
    depends_on:
      db:
        condition: service_healthy

volumes:
  dbdata:
```
