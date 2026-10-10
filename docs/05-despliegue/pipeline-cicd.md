# Pipeline CI/CD

## CI (`ci.yml`) — en cada Pull Request y push a `main`
1. Checkout.
2. `actions/setup-dotnet`.
3. `dotnet restore`.
4. `dotnet format --verify-no-changes`.
5. `dotnet build --no-restore -c Release`.
6. `dotnet test --no-build -c Release`.
7. Validar la infraestructura: `pip install cfn-lint` y `cfn-lint infra/aws/*.yaml`.

## CD (`deploy.yml`) — al hacer merge a `main` (y a mano con `workflow_dispatch`)
Entorno de GitHub: `production`.
1. Checkout.
2. Login a AWS con las llaves del usuario de despliegue (`aws-actions/configure-aws-credentials`, `secrets.AWS_ACCESS_KEY_ID` y `secrets.AWS_SECRET_ACCESS_KEY`, región `vars.AWS_REGION`).
3. Login a ECR (`aws-actions/amazon-ecr-login`).
4. Construir la imagen Docker y subirla a ECR con las etiquetas `<sha>` y `latest`.
5. Desplegar el servicio: `aws cloudformation deploy --stack-name habit-tracker-service --template-file infra/aws/service.yaml --parameter-overrides ImageUri=<registro>/<repositorio>:<sha>`.
6. Leer el output `Endpoint` del stack.
7. Verificar `GET /health` con reintentos (el servicio puede tardar en estar listo).
8. Publicar la URL en el resumen del job y en el deployment del entorno.

> El primer merge crea el stack del servicio; los siguientes lo actualizan. La infraestructura base no se toca en cada merge: la crea `scripts/setup-aws.sh` una sola vez (ADR-005), que además guarda las variables en GitHub si `gh` está autenticado.

## Secretos y variables (Settings → Secrets and variables → Actions)
Las llaves son de larga duración: guardarlas como secrets del entorno `production`, nunca en el repo ni en los logs.

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
- Sin pushes directos.

## Entornos
- `production` (único), con URL visible en el deployment de GitHub. Los secrets de AWS se guardan en este entorno; conviene exigir aprobación o limitarlo a la rama `main` para que no los use otra rama.

## Cambios del día 2 (PostgreSQL)
- **CI:** añadir un *service container* de Postgres (por ejemplo `postgres:16`) con healthcheck y `ConnectionStrings__Default` apuntando a `localhost:5432`.
- **CD:** antes de desplegar el servicio, generar un script idempotente (`dotnet ef migrations script --idempotent`) y aplicarlo a Supabase con `psql`. Desde GitHub Actions usar el pooler en modo sesión (la conexión directa puede ser solo IPv6; verificar en la documentación de Supabase).
- **Secretos:** `SUPABASE_CONNECTION_STRING` (secret de GitHub, para migraciones) y la cadena de la app en AWS Secrets Manager o Parameter Store, referenciada desde `service.yaml`.
- **`/health`:** que también compruebe la conexión a la base de datos.
