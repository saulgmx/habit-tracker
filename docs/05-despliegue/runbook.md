# Runbook

## Preparar el entorno (una sola vez)
1. Entrar a la consola con el usuario raíz solo para crear un usuario administrador (IAM; esta cuenta no usa IAM Identity Center) y activar MFA; trabajar después con ese usuario.
2. `aws configure` con las llaves del usuario IAM y `aws sts get-caller-identity` para confirmar la cuenta.
3. Crear un presupuesto de $5 en AWS Budgets (alertas al 50, 90 y 100 %).
4. Verificar la VPC predeterminada: `aws ec2 describe-vpcs --filters Name=isDefault,Values=true`.
5. Confirmar en la consola qué servicios cubre el plan Free (ECS/Fargate, balanceador, ECR) y que Express Mode está disponible en la región.
6. `./scripts/setup-aws.sh <usuario>/<repo> us-east-2`: crea el stack base, genera la llave de acceso del usuario de despliegue y guarda secrets y variables en GitHub (sin mostrar la llave).
7. En GitHub, crear el entorno `production` (Settings → Environments), limitado a la rama `main`. Si `setup-aws.sh` guardó los secrets a nivel de repositorio, moverlos al entorno.
8. Primer despliegue: merge a `main` o ejecutar `deploy.yml` a mano. Crea el servicio.
9. Obtener la URL y verificar `GET /health` y la app en el móvil.

Controles de costo en `docs/05-despliegue/costos.md`.

## Desplegar
Automático al hacer merge a `main`. Revisar la pestaña *Actions*.

## Verificar
```bash
URL=$(aws cloudformation describe-stacks --stack-name habit-tracker-service \
  --query "Stacks[0].Outputs[?OutputKey=='Endpoint'].OutputValue" --output text)
curl "https://${URL#https://}/health"
```
Abrir la app en el móvil y marcar un hábito.

## Rollback
Volver a ejecutar el workflow de un commit anterior, o desplegar a mano la imagen previa:
```bash
aws cloudformation deploy --stack-name habit-tracker-service \
  --template-file infra/aws/service.yaml \
  --parameter-overrides ImageUri=<registro>/habit-tracker:<sha-anterior>
```

## Rotar o revocar las llaves de acceso
```bash
U=habit-tracker-github-deploy
aws iam list-access-keys --user-name $U
aws iam create-access-key --user-name $U      # guardar en los secrets de GitHub y probar el workflow
aws iam delete-access-key --user-name $U --access-key-id <ID-anterior>
```
Si hay sospecha de filtración: desactivar la llave de inmediato (`aws iam update-access-key --status Inactive ...`) y revisar CloudTrail.

## Antes del demo
- [ ] Servicio creado y respondiendo `/health` (ensayar con tiempo: la creación tarda varios minutos).
- [ ] Presupuesto de $5 con alertas activas.
- [ ] Abrir la app 5 minutos antes.
- [ ] Datos de ejemplo cargados (seed al arrancar, porque SQLite es efímero).
- [ ] Rama `demo/bug-racha` lista.
- [ ] Captura o video de respaldo.

## Después del demo
- [ ] `./scripts/cleanup-aws.sh service` para detener el cómputo y el balanceador.
- [ ] Rotar o borrar las llaves de acceso si ya no se usan.
- [ ] Si ya no se necesita: `./scripts/cleanup-aws.sh all` (borra también las llaves y el usuario) y eliminar los secrets de AWS en GitHub.
- [ ] Revisar en la consola que no queden balanceadores, servicios ECS, grupos de logs ni imágenes.
- [ ] Revisar Billing y Budgets al día siguiente (los datos tardan en reflejarse).

## Día 2 (tras migrar a PostgreSQL)
- [ ] Proyecto de Supabase activo (despertarlo si estaba pausado).
- [ ] El rollback de imagen no revierte migraciones: mantenerlas compatibles hacia atrás.
