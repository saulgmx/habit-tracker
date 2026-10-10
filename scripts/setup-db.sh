#!/usr/bin/env bash
# Uso: ./scripts/setup-db.sh [region]
# Crea el stack de la base de datos (infra/aws/db.yaml): Amazon RDS for PostgreSQL privada
# (db.t4g.micro, Single-AZ, 20 GiB gp3), su secreto en Secrets Manager y los grupos de seguridad.
# Requiere el stack base (setup-aws.sh) y se ejecuta ANTES del primer despliegue del servicio.
#
# COSTO: la instancia cobra por hora mientras exista, aunque no se use (contra el crédito de la
# cuenta). Tarda unos 10 minutos en crearse. Borrarla al terminar: ./scripts/cleanup-aws.sh db
set -euo pipefail

REGION="${1:-us-east-2}"
PROJECT="${PROJECT:-habit-tracker}"
STACK="${PROJECT}-db"
export AWS_DEFAULT_REGION="$REGION"

aws cloudformation describe-stacks --stack-name "${PROJECT}-base" > /dev/null 2>&1 \
  || { echo "Falta el stack ${PROJECT}-base: ejecuta primero ./scripts/setup-aws.sh"; exit 1; }

VPC_ID="$(aws ec2 describe-vpcs --filters Name=isDefault,Values=true --query 'Vpcs[0].VpcId' --output text)"
[ "$VPC_ID" != "None" ] || { echo "No hay VPC predeterminada en $REGION."; exit 1; }
SUBNETS="$(aws ec2 describe-subnets \
  --filters "Name=vpc-id,Values=$VPC_ID" Name=default-for-az,Values=true \
  --query 'Subnets[].SubnetId' --output text | tr '\t' ',')"
echo "Región: $REGION | Stack: $STACK | Subredes: $(echo "$SUBNETS" | tr ',' '\n' | wc -l | tr -d ' ')"

echo "Validando plantilla..."
aws cloudformation validate-template --template-body file://infra/aws/db.yaml > /dev/null

echo "Creando la base de datos (unos 10 minutos; cobra por hora mientras exista)..."
aws cloudformation deploy \
  --stack-name "$STACK" \
  --template-file infra/aws/db.yaml \
  --capabilities CAPABILITY_IAM \
  --parameter-overrides ProjectName="$PROJECT" VpcId="$VPC_ID" SubnetIds="$SUBNETS"

echo "Listo. El siguiente despliegue del servicio (Actions -> Deploy) ya se conecta a esta base."
echo "Al terminar: ./scripts/cleanup-aws.sh db   (borra el servicio y la base)"
