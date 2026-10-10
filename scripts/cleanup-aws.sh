#!/usr/bin/env bash
# Uso: ./scripts/cleanup-aws.sh service|all [region]
#   service: borra el servicio (detiene Fargate y el balanceador, que es lo que cuesta).
#   all:     además borra llaves de acceso, imágenes, ECR, cluster, roles y el usuario de despliegue.
set -euo pipefail

MODE="${1:?Uso: cleanup-aws.sh service|all [region]}"
REGION="${2:-us-east-2}"
PROJECT="${PROJECT:-habit-tracker}"
export AWS_DEFAULT_REGION="$REGION"

case "$MODE" in
  service|all) ;;
  *) echo "Modo inválido: $MODE (usa service o all)"; exit 1 ;;
esac

echo "Borrando el stack del servicio..."
aws cloudformation delete-stack --stack-name "${PROJECT}-service"
aws cloudformation wait stack-delete-complete --stack-name "${PROJECT}-service"

if [ "$MODE" = "all" ]; then
  echo "Borrando las llaves de acceso del usuario de despliegue (CloudFormation no puede borrar un usuario con llaves)..."
  DEPLOY_USER="${PROJECT}-github-deploy"
  for KEY_ID in $(aws iam list-access-keys --user-name "$DEPLOY_USER" --query 'AccessKeyMetadata[].AccessKeyId' --output text 2>/dev/null || true); do
    aws iam delete-access-key --user-name "$DEPLOY_USER" --access-key-id "$KEY_ID"
  done
  echo "Vaciando el repositorio ECR..."
  IDS="$(aws ecr list-images --repository-name "$PROJECT" --query 'imageIds[*]' --output json 2>/dev/null || echo '[]')"
  if [ "$IDS" != "[]" ]; then
    aws ecr batch-delete-image --repository-name "$PROJECT" --image-ids "$IDS" > /dev/null
  fi
  echo "Borrando el stack base..."
  aws cloudformation delete-stack --stack-name "${PROJECT}-base"
  aws cloudformation wait stack-delete-complete --stack-name "${PROJECT}-base"
  echo "Recuerda borrar los secrets AWS_ACCESS_KEY_ID y AWS_SECRET_ACCESS_KEY en GitHub."
fi

echo "Listo. Revisa en la consola que no queden balanceadores, servicios ECS ni grupos de logs del servicio."
