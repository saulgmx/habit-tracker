# Runbook

## Preparar el entorno (una sola vez)
1. Entrar a la consola con el usuario raíz solo para crear un usuario administrador (IAM; esta cuenta no usa IAM Identity Center) y activar MFA; trabajar después con ese usuario.
2. `aws configure` con las llaves del usuario IAM y `aws sts get-caller-identity` para confirmar la cuenta.
3. Crear un presupuesto de $5 en AWS Budgets (alertas al 50, 90 y 100 %).
4. Usar siempre la región `us-east-2`: un SCP deniega EC2, ECS, ECR, CloudFormation y ELB en las demás (ver ADR-005). Verificar la VPC predeterminada: `aws ec2 describe-vpcs --region us-east-2 --filters Name=isDefault,Values=true`.
5. Confirmar qué servicios cubre el plan Free (ECS/Fargate, balanceador, ECR) y que Express Mode está disponible en la región (lo está en us-east-2).
6. `./scripts/setup-aws.sh <usuario>/<repo> us-east-2`: crea el stack base, genera la llave de acceso del usuario de despliegue y guarda secrets y variables en GitHub (sin mostrar la llave).
7. En GitHub, limitar el entorno `production` a la rama `main` (Settings → Environments; GitHub lo crea solo con el primer workflow). `setup-aws.sh` guarda los secrets a nivel de repositorio: pasarlos al entorno rotando la llave (ver más abajo).
8. Crear la base de datos: `./scripts/setup-db.sh us-east-2`. Crea el stack `habit-tracker-db` (RDS PostgreSQL privada, secreto y grupos de seguridad) en menos de 10 minutos. **Cobra por hora desde que se crea** (precios de lista en `docs/05-despliegue/costos.md`). Debe existir antes del primer despliegue: el servicio importa sus valores.
9. Primer despliegue: ejecutar `deploy.yml` a mano (Actions → Deploy → Run workflow). Crea el servicio y tarda unos 6 minutos.
10. Obtener la URL y verificar `GET /health` (debe incluir `"database":"ok"`) y la app en el móvil.

Si el despliegue falla, ver «Si el despliegue falla».

Controles de costo en `docs/05-despliegue/costos.md`.

## Desplegar
A mano: Actions → Deploy → Run workflow, sobre `main` (o `gh workflow run deploy.yml --ref main`). Requiere que el stack `habit-tracker-db` exista (paso 8 de «Preparar el entorno»). El despliegue crea Fargate y un balanceador que cobran por hora: borrar el servicio al terminar (ver «Después del demo»). Una actualización tarda unos 10 minutos y solo cambia algo si el commit es nuevo (la imagen se etiqueta con el SHA).

## Verificar
```bash
URL=$(aws cloudformation describe-stacks --region us-east-2 --stack-name habit-tracker-service \
  --query "Stacks[0].Outputs[?OutputKey=='Endpoint'].OutputValue" --output text)
curl "https://${URL#https://}/health"
```
Abrir la app en el móvil y marcar un hábito.

## Rollback
El rollback de imagen no revierte el esquema de la base de datos, y las migraciones se aplican al arrancar la API: mantenerlas compatibles hacia atrás. Volver a ejecutar el workflow de un commit anterior, o desplegar a mano la imagen previa:
```bash
aws cloudformation deploy --stack-name habit-tracker-service \
  --template-file infra/aws/service.yaml \
  --parameter-overrides ImageUri=<registro>/habit-tracker:<sha-anterior>
```

## Si el despliegue falla
1. Ver el paso que falló en Actions. Si fue «Desplegar el servicio», revisar los eventos del stack:
```bash
aws cloudformation describe-stack-events --region us-east-2 --stack-name habit-tracker-service \
  --query 'StackEvents[?contains(ResourceStatus,`FAILED`)].[LogicalResourceId,ResourceStatusReason]' --output text
```
2. Si el mensaje es genérico (`No rollback candidate was found…`), mirar la causa en ECS y en CloudTrail:
```bash
aws ecs describe-services --region us-east-2 --cluster habit-tracker --services habit-tracker \
  --query 'services[0].deployments[].rolloutStateReason'
aws cloudtrail lookup-events --region us-east-2 --max-items 500 --page-size 50   # buscar errorCode AccessDenied
```
   Un mensaje «no identity-based policy allows…» es un permiso que falta (rol de infraestructura o usuario de despliegue): añadirlo en `infra/aws/base.yaml` por PR y actualizar el stack base con `setup-aws.sh`. «explicit deny in a service control policy» es un SCP y no se arregla desde la plantilla.
3. Antes de reintentar: si el stack quedó en `ROLLBACK_COMPLETE`, borrarlo (`aws cloudformation delete-stack`). Si quedó un servicio ECS huérfano (`aws ecs list-services --cluster habit-tracker`), borrarlo antes con `aws ecs delete-express-gateway-service --service-arn <arn>` y esperar a que pase a `INACTIVE`.
4. Si el paso «Desplegar el servicio» falla porque no encuentra un valor exportado (CloudFormation suele indicar `No export named habit-tracker-db-…`; no se observó este error), el stack de la base de datos no existe: crearlo con `./scripts/setup-db.sh us-east-2`.

### Si `/health` responde 503 (`"database":"unreachable"`)
La API arrancó pero no llega a la base. Son hipótesis a comprobar, en este orden (no hay un caso real documentado):
1. La instancia no está `available`: `aws rds describe-db-instances --region us-east-2 --query 'DBInstances[].DBInstanceStatus'`.
2. La tarea no lleva el grupo de seguridad `AppSecurityGroup` del stack de la base (la base solo acepta el 5432 desde él): revisar las interfaces de red de la tarea.
3. Los logs de la tarea (grupo `/aws/ecs/habit-tracker/…`) muestran el error de Npgsql. Si la API ni siquiera arranca, falta la configuración de conexión (`Database__Host`, etc.) o el rol de ejecución no puede leer el secreto.

## Rotar o revocar las llaves de acceso
IAM permite máximo 2 llaves por usuario. Rotación sin mostrar el secreto (guardarlo directo en el entorno `production`):
```bash
U=habit-tracker-github-deploy
aws iam list-access-keys --user-name $U
KEY=$(aws iam create-access-key --user-name $U --output json)
printf '%s' "$KEY" | python3 -c 'import json,sys;print(json.load(sys.stdin)["AccessKey"]["AccessKeyId"],end="")' | gh secret set AWS_ACCESS_KEY_ID --env production
printf '%s' "$KEY" | python3 -c 'import json,sys;print(json.load(sys.stdin)["AccessKey"]["SecretAccessKey"],end="")' | gh secret set AWS_SECRET_ACCESS_KEY --env production
unset KEY
aws iam delete-access-key --user-name $U --access-key-id <ID-anterior>   # tras comprobar que la nueva funciona
gh secret delete AWS_ACCESS_KEY_ID; gh secret delete AWS_SECRET_ACCESS_KEY  # los del repositorio, si existen
```
Si hay sospecha de filtración: desactivar la llave de inmediato (`aws iam update-access-key --status Inactive ...`) y revisar CloudTrail.

## Antes del demo
- [ ] Base de datos creada (`habit-tracker-db` en `CREATE_COMPLETE` e instancia `available`; si se borró, `./scripts/setup-db.sh`, menos de 10 minutos).
- [ ] Servicio creado y respondiendo `/health` con `"database":"ok"` (ensayar con tiempo: la creación tarda unos 6 minutos; si el servicio se borró tras las pruebas, hay que recrearlo con un despliegue).
- [ ] Presupuesto de $5 con alertas activas.
- [ ] Abrir la app 5 minutos antes.
- [ ] Datos de ejemplo cargados (la API los siembra al arrancar solo si la base está vacía; los demás datos persisten en RDS).
- [ ] Rama `demo/bug-racha` lista.
- [ ] Captura o video de respaldo.

## Después del demo
- [ ] `./scripts/cleanup-aws.sh db` para detener el cómputo y el balanceador **y borrar la base de datos** (los datos se pierden; sin instantánea). `./scripts/cleanup-aws.sh service` borra solo el servicio y deja la base cobrando.
- [ ] Rotar o borrar las llaves de acceso si ya no se usan.
- [ ] Si ya no se necesita: `./scripts/cleanup-aws.sh all` (borra también las llaves y el usuario) y eliminar los secrets de AWS en GitHub.
- [ ] Revisar en la consola que no queden balanceadores, instancias RDS, secretos, servicios ECS, target groups, security groups ni alarmas. Los grupos de logs (`/aws/ecs/habit-tracker/…`, uno por despliegue) y las revisiones de la definición de tarea no se borran con el stack: eliminarlos a mano (`aws logs delete-log-group`, `aws ecs deregister-task-definition`).
- [ ] Revisar Billing y Budgets al día siguiente (los datos tardan en reflejarse).

## Base de datos (PostgreSQL en RDS)
- Se crea con `./scripts/setup-db.sh` y se borra con `./scripts/cleanup-aws.sh db`. No se detiene: una instancia detenida se reiniciaría sola a los 7 días, y borrarla y recrearla tarda menos de 10 minutos.
- Es privada: no se puede abrir con un cliente SQL desde fuera de la VPC. Para ver datos, usar la API.
- La contraseña está en Secrets Manager (la administra RDS) y nunca se imprime ni se guarda en el repo.
- Borrar la base destruye los datos (sin instantánea final). Para conservarlos, cambiar `DeletionPolicy` a `Snapshot` en `infra/aws/db.yaml`.
- Probar la persistencia: crear un hábito en el servicio, redesplegar con un commit nuevo y comprobar que sigue ahí (se hizo el 2026-10-10 con éxito).
