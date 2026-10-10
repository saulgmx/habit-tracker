# ADR-006: Azure como alternativa de despliegue (pospuesta)

- **Estado:** Propuesta
- **Fecha:** 2026-10-09

## Contexto
La primera versión del plan usaba una cuenta de Azure (Container Apps + Bicep). Esa cuenta tuvo problemas; se eliminará y se creará otra (la suscripción tiene un periodo de espera de 3 días). Mientras tanto, el despliegue de referencia es AWS. Se quiere probar Azure después.

## Decisión
- AWS es el destino del día 1 (ADR-002 y ADR-005).
- Los archivos de Azure se conservan sin cambios de fondo en el Anexo A: `infra/azure/main.bicep`, `scripts/azure/setup-azure.sh` y `scripts/azure/bootstrap.sh`.
- Para probar Azure se creará un workflow aparte (`deploy-azure.yml`, por definir) que no sustituya al de AWS.

## Consecuencias
- El Bicep y los scripts de Azure no se han ejecutado ni validado (no hay `az` en el entorno donde se escribieron).
- Usan la imagen en ghcr.io (paquete público); habrá que decidir si el workflow de Azure publica también allí o usa Azure Container Registry.
- La gestión ya no usa Azure Boards (ADR-003): el Anexo A no depende de ella.
- Mantener dos destinos implica dos juegos de secretos y variables; documentar sus nombres para que no se mezclen.

## Criterios para activarla
- [ ] Nueva cuenta de Azure operativa y presupuesto creado.
- [ ] Anexo A revisado y probado con `az bicep build` y `what-if`.
- [ ] Workflow `deploy-azure.yml` definido.
