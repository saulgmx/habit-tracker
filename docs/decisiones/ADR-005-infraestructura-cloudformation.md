# ADR-005: Infraestructura como código con CloudFormation en una cuenta nueva de AWS

- **Estado:** Aceptada (reemplaza la versión con Bicep; ver ADR-006)
- **Fecha:** 2026-10-09

## Contexto
Se necesita un entorno en AWS con CD automático desde GitHub y control de costos. Se usa una cuenta nueva con el plan Free. Los problemas con la cuenta de Azure llevaron al cambio.

## Decisión
- Dos stacks de CloudFormation (YAML):
  - `infra/aws/base.yaml` (una vez): repositorio ECR con política de ciclo de vida, cluster ECS, rol de ejecución, rol de infraestructura de Express Mode, y un usuario IAM de despliegue con política mínima (sin llaves en la plantilla).
  - `infra/aws/service.yaml`: el servicio `AWS::ECS::ExpressGatewayService`, que importa los valores del stack base.
- `scripts/setup-aws.sh` crea el stack base y crea una llave de acceso para el usuario de despliegue y la guarda como secrets de GitHub (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`) sin mostrarla, además de las variables `AWS_REGION` y `ECR_REPOSITORY`. Las llaves no se definen en CloudFormation porque `AWS::IAM::AccessKey` dejaría el secreto en las salidas del stack.
- El CD ejecuta `aws cloudformation deploy` del stack del servicio con la imagen nueva: así la plantilla sigue siendo la fuente de verdad y no hay deriva con cambios hechos fuera de CloudFormation.
- `scripts/cleanup-aws.sh` borra el servicio (`service`) o todo el entorno (`all`).
- El CI valida las plantillas con `cfn-lint`.

## Alternativas descartadas
- **Un solo stack:** obligaría al CD a tocar roles y usuarios IAM en cada despliegue, o a crear el servicio antes de tener imagen.
- **La acción de GitHub de ECS Express Mode actualizando el servicio directamente:** más corta, pero el servicio creado por CloudFormation quedaría desfasado de la plantilla.
- **Terraform o CDK:** válidos, pero añaden otra herramienta; CloudFormation es nativo de AWS.
- **Crear recursos desde la consola:** no es reproducible.

## Consecuencias
- Plantillas validadas solo con `cfn-lint`; falta confirmar el despliegue real (formato de `Cpu`/`Memory`, comportamiento de actualización del servicio, permisos del rol de despliegue).
- No hay tope automático de gasto: se mitiga con presupuesto, escalado acotado y borrando el servicio al terminar.
- **Se descartó OIDC** porque en la cuenta nueva no se pudo usar (causa sin confirmar). Las llaves de larga duración son menos seguras: mitigaciones en la sección de riesgos; migrar a OIDC cuando la cuenta lo permita sería una mejora.
- El grupo de logs que crea Express Mode puede no borrarse con el stack: revisarlo en la limpieza.
