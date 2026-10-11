# ADR-002: Contenedores y hosting en AWS

- **Estado:** Aceptada (reemplaza la versión con Azure Container Apps; ver ADR-006)
- **Fecha:** 2026-10-09

## Contexto
Se quiere mostrar un flujo de CD moderno con construcción de imagen, registro y despliegue. El hosting pasa a una cuenta nueva de AWS (plan Free) porque la cuenta de Azure tuvo problemas.

## Decisión
Dockerfile multi-stage, imagen en Amazon ECR y despliegue en Amazon ECS Express Mode (Fargate con balanceador HTTPS, autoescalado y monitoreo administrados), con escalado acotado (1 a 2 tareas, 0.25 vCPU / 0.5 GiB).

## Alternativas descartadas
- **AWS App Runner:** más simple, pero AWS recomienda ECS Express Mode para servicios nuevos (verificar el estado vigente de App Runner).
- **ECS/Fargate con balanceador definido a mano:** más control, pero más recursos de red que explicar en un demo.
- **Lambda con contenedor:** cambia el modelo de ejecución de la API.
- **GHCR como registro:** válido, pero ECR integra el rol de ejecución de ECS sin credenciales adicionales.

## Consecuencias
- No hay escalado a cero (no encontré soporte en Express Mode): el servicio y su balanceador cobran por hora hasta borrarlos.
- La infraestructura se define como código con CloudFormation (ver ADR-005) y los costos se controlan según `docs/05-despliegue/costos.md`.
- SQLite perdía los datos al redesplegar. Con PostgreSQL en RDS (ADR-004) los datos sobreviven; el seed al arrancar solo carga los datos de ejemplo cuando la base está vacía.
- **Verificado:** ECS Express Mode está disponible y funciona en `us-east-2`, que es la única región permitida por los SCP de la cuenta (ver ADR-005). El servicio se desplegó con crédito del plan Free; no hay una cifra de costo verificada.
- **Arquitectura x86_64:** es el valor por defecto de Express Mode y coincide con la imagen que construye el CI (`ubuntu-latest`). ARM64 (Graviton) ya existe en la CLI de ECS (`--cpu-architecture`), pero `AWS::ECS::ExpressGatewayService` en CloudFormation no tenía la propiedad `CpuArchitecture` el 2026-10-09; se deja como mejora opcional (ver `Tasks.md`). Si se activa, la imagen debe construirse también para `linux/arm64`, o el contenedor falla al arrancar con *exec format error*.
