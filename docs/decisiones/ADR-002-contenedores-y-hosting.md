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
- SQLite pierde los datos al redesplegar: usar seed al arrancar. La migración a PostgreSQL (ADR-004) elimina esta limitación.
- Se debe verificar que ECS Express Mode esté disponible en la región elegida y cubierto por el plan Free.
