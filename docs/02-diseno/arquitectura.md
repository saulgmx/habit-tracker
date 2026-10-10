# Arquitectura

```mermaid
flowchart LR
    U[Móvil / PWA] -->|HTTPS| ALB[Balanceador administrado<br/>ECS Express Mode]
    subgraph Fargate
        subgraph Contenedor
            API[ASP.NET Core Minimal API]
            WWW[wwwroot: HTML, JS, CSS, SW]
            DB[(SQLite)]
            API --- WWW
            API --> DB
        end
    end
    ALB --> API
    GH[GitHub Actions] -->|push imagen| ECR[Amazon ECR]
    GH -->|aws cloudformation deploy| CFN[Stack del servicio]
    CFN --> ALB
    ECR -->|pull| API
```

## Componentes
- **PWA (wwwroot):** interfaz en JS vanilla; el service worker cachea los archivos estáticos.
- **API:** expone los endpoints y contiene la lógica de rachas (`StreakCalculator`, función pura).
- **SQLite (día 1):** persistencia en archivo local del contenedor. En el día 2 se reemplaza por PostgreSQL (Supabase en la nube), ver ADR-004.
- **Contenedor:** una sola imagen con API y PWA.
- **Infraestructura:** definida como código en `infra/aws/` con CloudFormation: stack base y stack del servicio (ver ADR-005).

## Decisiones relacionadas
Ver [ADRs](../decisiones/).
