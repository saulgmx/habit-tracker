#!/usr/bin/env bash
# Uso: ./scripts/setup-aws.sh <usuario/repo> [region]
# Se ejecuta UNA vez con tus credenciales de AWS (no uses el usuario raíz) y con `gh` autenticado.
# Crea el stack base (ECR, cluster, roles y usuario IAM de despliegue), crea una llave de
# acceso para ese usuario y la guarda como secrets de GitHub sin mostrarla en pantalla.
set -euo pipefail

REPO="${1:?Falta usuario/repo de GitHub (por ejemplo saul/habit-tracker)}"
REGION="${2:-us-east-2}"
PROJECT="${PROJECT:-habit-tracker}"
STACK="${PROJECT}-base"
export AWS_DEFAULT_REGION="$REGION"

command -v gh >/dev/null 2>&1 && gh auth status >/dev/null 2>&1 \
  || { echo "Necesitas gh instalado y autenticado (gh auth login)."; exit 1; }

ACCOUNT_ID="$(aws sts get-caller-identity --query Account --output text)"
echo "Cuenta: $ACCOUNT_ID | Región: $REGION | Stack: $STACK"

echo "Validando plantilla..."
aws cloudformation validate-template --template-body file://infra/aws/base.yaml > /dev/null

echo "Desplegando stack base (puede tardar unos minutos)..."
aws cloudformation deploy \
  --stack-name "$STACK" \
  --template-file infra/aws/base.yaml \
  --capabilities CAPABILITY_NAMED_IAM \
  --parameter-overrides ProjectName="$PROJECT"

out() {
  aws cloudformation describe-stacks --stack-name "$STACK" \
    --query "Stacks[0].Outputs[?OutputKey=='$1'].OutputValue" --output text
}
DEPLOY_USER="$(out GitHubDeployUserName)"
REPO_NAME="$(out RepositoryName)"

# Variables (no son secretas).
gh variable set AWS_REGION --repo "$REPO" --body "$REGION"
gh variable set ECR_REPOSITORY --repo "$REPO" --body "$REPO_NAME"

# Llave de acceso: IAM permite máximo 2 por usuario. No se reemplaza una existente sin avisar.
EXISTING="$(aws iam list-access-keys --user-name "$DEPLOY_USER" --query 'length(AccessKeyMetadata)' --output text)"
if [ "$EXISTING" != "0" ]; then
  echo "El usuario $DEPLOY_USER ya tiene $EXISTING llave(s) de acceso."
  echo "Para rotarla: crea una nueva, actualiza los secrets y borra la anterior (ver runbook)."
  echo "No se creó otra llave."
else
  KEY_JSON="$(aws iam create-access-key --user-name "$DEPLOY_USER" --output json)"
  # El secreto pasa por stdin y no se imprime.
  printf '%s' "$KEY_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["AccessKey"]["AccessKeyId"], end="")' \
    | gh secret set AWS_ACCESS_KEY_ID --repo "$REPO"
  printf '%s' "$KEY_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["AccessKey"]["SecretAccessKey"], end="")' \
    | gh secret set AWS_SECRET_ACCESS_KEY --repo "$REPO"
  unset KEY_JSON
  echo "Llave de acceso creada y guardada como secrets de GitHub."
fi

echo "Listo. Falta: crear el entorno 'production' en GitHub y el presupuesto (ver docs/05-despliegue/costos.md)."
