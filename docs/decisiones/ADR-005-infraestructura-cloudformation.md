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
- **Verificado el 2026-10-10:** el stack base y el servicio se crearon en AWS tras tres intentos de despliegue (el tercero, sin errores, en 6 min 29 s). `Cpu` 256 y `Memory` 512 son valores válidos. Sigue sin ensayarse la actualización del servicio con una imagen nueva.
- **La política administrada del rol de infraestructura no basta.** `AmazonECSInfrastructureRoleforExpressGatewayServices` no incluye `ec2:DescribeAccountAttributes`, y sin él ELB falla al crear el balanceador con `AccessDenied` ("An unknown error occurred"). Se complementa con la política en línea `express-extra` de `base.yaml` (`ec2:DescribeAccountAttributes`, `ec2:DescribeInternetGateways`, `ec2:GetSecurityGroupsForVpc`, y `ecs:DescribeServices` y `ecs:UpdateService` acotados al cluster del proyecto). Las acciones de CloudWatch de la administrada llevan una condición de etiqueta `AmazonECSManaged` y se dejaron como están.
- **El usuario de despliegue necesita también permisos de lectura** que CloudFormation usa con sus credenciales para leer el atributo `Endpoint`: `ecs:DescribeServiceRevisions` y `ecs:DescribeTaskDefinition`. La lista oficial de permisos del recurso se obtiene con `aws cloudformation describe-type --type RESOURCE --type-name AWS::ECS::ExpressGatewayService` (sección `handlers`).
- **Un fallo de permisos en el rol de infraestructura es engañoso:** CloudFormation muestra `No rollback candidate was found to run the rollback`, y el servicio de ECS puede quedar huérfano aunque el stack se revierta. La causa real está en el deployment del servicio (`aws ecs describe-services`) y en CloudTrail.
- **Un stack en `ROLLBACK_COMPLETE` no se puede actualizar:** hay que borrarlo antes de reintentar (y borrar antes el servicio ECS huérfano si existe).
- **CloudFormation no gestiona todo lo que crea Express Mode:** tras borrar el servicio quedan el grupo de logs (`/aws/ecs/<proyecto>/<proyecto>-xxxx`, uno por despliegue) y las revisiones de la definición de tarea. No tienen costo relevante, pero hay que borrarlos a mano.
- No hay tope automático de gasto: se mitiga con presupuesto, escalado acotado y borrando el servicio al terminar.
- **Se descartó OIDC:** un SCP de la organización deniega las acciones de proveedores OIDC de IAM en esta cuenta (`iam:ListOpenIDConnectProviders`, "explicit deny in a service control policy"). Las llaves de larga duración son menos seguras: mitigaciones en la sección de riesgos del kickoff y rotación en el runbook.
- **Solo se puede usar la región `us-east-2`:** otro SCP deniega EC2, ECS, ECR, CloudFormation y ELB en las demás regiones probadas (us-east-1, us-west-1, us-west-2, eu-west-1, eu-central-1, ca-central-1 y sa-east-1). Todos los valores por defecto del repo usan us-east-2.
- La cuenta usa un usuario IAM, no IAM Identity Center: se autentica con `aws configure`, no con `aws sso login`.
- Las llaves se guardan como secrets del entorno `production` (limitado a `main`) y se rotan sin mostrarlas en pantalla.
