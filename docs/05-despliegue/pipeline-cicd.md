# Pipeline CI/CD

## CI (`ci.yml`) — en cada Pull Request y push a `main`
1. Checkout.
2. `actions/setup-dotnet`.
3. `dotnet restore`.
4. `dotnet format --verify-no-changes`.
5. `dotnet build --no-restore -c Release`.
6. `dotnet test --no-build -c Release`.
7. Validar la infraestructura: `pip install cfn-lint` y `cfn-lint infra/aws/*.yaml`.

## CD (`deploy.yml`) — solo a mano (`workflow_dispatch`)
Se ejecuta desde Actions → Deploy → Run workflow, sobre `main`. Cada despliegue crea Fargate y un balanceador que cobran por hora, por eso no se dispara con el merge. Para volver al despliegue automático, añadir de nuevo `push: branches: [main]` en `on:`.

Entorno de GitHub: `production`.
1. Checkout.
2. Login a AWS con las llaves del usuario de despliegue (`aws-actions/configure-aws-credentials`, `secrets.AWS_ACCESS_KEY_ID` y `secrets.AWS_SECRET_ACCESS_KEY`, región `vars.AWS_REGION`).
3. Login a ECR (`aws-actions/amazon-ecr-login`).
4. Construir la imagen Docker y subirla a ECR con las etiquetas `<sha>` y `latest`.
5. Desplegar el servicio: `aws cloudformation deploy --stack-name habit-tracker-service --template-file infra/aws/service.yaml --parameter-overrides ImageUri=<registro>/<repositorio>:<sha>`.
6. Leer el output `Endpoint` del stack.
7. Verificar `GET /health` con reintentos (el servicio puede tardar en estar listo).
8. Publicar la URL en el resumen del job y en el deployment del entorno.

> El primer despliegue crea el stack del servicio (unos 6 minutos); los siguientes lo actualizan (la actualización se ensayó: unos 10 minutos y sin errores). Una actualización solo cambia algo si la imagen es nueva: la etiqueta es el SHA del commit, y con el mismo commit el conjunto de cambios queda vacío. La infraestructura base no se toca en cada despliegue: la crea `scripts/setup-aws.sh` una sola vez (ADR-005), que además guarda las variables en GitHub si `gh` está autenticado.
>
> Para que el despliegue funcione, el usuario de despliegue y el rol de infraestructura necesitan los permisos de `infra/aws/base.yaml` (ver ADR-005): sin ellos falla en la creación del servicio.

## Secretos y variables (Settings → Secrets and variables → Actions)
Las llaves son de larga duración: guardarlas como secrets del entorno `production` (no del repositorio), nunca en el repo ni en los logs. `setup-aws.sh` las guarda a nivel de repositorio; hay que rotarlas al entorno (ver runbook).

| Nombre | Tipo | Uso |
|---|---|---|
| `AWS_ACCESS_KEY_ID` | Secret | Llave del usuario IAM de despliegue (la crea `setup-aws.sh`) |
| `AWS_SECRET_ACCESS_KEY` | Secret | Secreto de esa llave |
| `AWS_REGION` | Variable | Región (por ejemplo `us-east-2`) |
| `ECR_REPOSITORY` | Variable | Nombre del repositorio ECR (por defecto `habit-tracker`) |

## Permisos del workflow
`permissions: { contents: read }`

## Protección de `main`
- Requiere PR con 1 aprobación.
- Requiere que el check `ci` esté en verde.
- Sin pushes directos, force push ni borrado de la rama.
- `enforce_admins` apagado: con un solo colaborador nadie puede aprobar su propio PR, así que el administrador hace el merge con *bypass* una vez que el CI está en verde. Con un segundo colaborador se puede exigir la aprobación a todos.

## Entornos
- `production` (único), con URL visible en el deployment de GitHub. Está limitado a la rama `main` y los secrets de AWS viven aquí, así que ninguna otra rama puede usarlos.

## PostgreSQL (día 2)
- **CI:** el job `ci` levanta un *service container* `postgres:16` con healthcheck y define `TEST_POSTGRES` apuntando a `localhost:5432`. Cada ejecución de pruebas crea y borra su propia base en ese servidor.
- **CD:** no hay paso de migraciones. La API aplica las migraciones al arrancar (`Migrate()`) porque RDS es privada y un runner de GitHub Actions no está dentro de la VPC (ver ADR-004).
- **Requisito:** el stack `habit-tracker-db` debe existir antes de desplegar (`./scripts/setup-db.sh`): el servicio importa su host, secreto y grupo de seguridad, y `deploy.yml` falla en el paso «Desplegar el servicio» si no están.
- **Secretos:** no hay secretos de base de datos en GitHub. RDS administra la contraseña en Secrets Manager y ECS la inyecta en la tarea (`Database__User` y `Database__Password`).
- **`/health`:** comprueba la conexión a la base de datos y responde 503 si no responde.
