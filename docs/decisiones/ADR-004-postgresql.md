# ADR-004: Migrar de SQLite a PostgreSQL (día 2)

- **Estado:** Propuesta (se implementa como tarea de día 2)
- **Fecha:** 2026-10-09
- **Revisada:** 2026-10-10 (la base de datos en la nube pasa de Supabase a Amazon RDS for PostgreSQL)

## Contexto
El día 1 se usa SQLite por simplicidad y menor riesgo en el demo. SQLite en un contenedor pierde los datos en cada despliegue o reinicio y no da paridad con entornos reales.

La cuenta de AWS es nueva y está en el plan Free. Según la página de AWS Free Tier de RDS, quien se une después del 15 de julio de 2025 recibe hasta 100 USD de crédito al registrarse y puede ganar hasta 100 USD más por activar servicios básicos, y RDS y Aurora son elegibles. Hay que confirmar en la consola (widget «Explore AWS») que usar RDS suma los 20 USD adicionales; ver «Verificado» y «Por verificar».

## Decisión
Al migrar:
- **Local y CI:** PostgreSQL en contenedor (`docker-compose` y *service container* de GitHub Actions).
- **Nube:** Amazon RDS for PostgreSQL (instancia única, `db.t4g.micro`, Single-AZ) en `us-east-2`, usado con Npgsql + EF Core.
- **Esquema:** migraciones de EF Core regeneradas para Postgres (las de SQLite no son portables), aplicadas como paso explícito del pipeline de CD.
- **Credenciales:** la cadena de conexión vive en AWS Secrets Manager o Parameter Store, referenciada desde el servicio.

## Alternativas descartadas
- **Supabase (decisión original):** PostgreSQL administrado fuera de AWS, sin costo directo, pero es una dependencia externa que puede pausarse y no aprovecha el crédito de AWS. Se cambió a RDS para mantener todo en una sola cuenta y usar el crédito.
- **Aurora PostgreSQL:** también es elegible para el crédito, pero cuesta más y su capacidad mínima es mayor que la de una instancia pequeña de RDS; sobra para este demo.
- **SQLite + volumen EFS:** menos piezas, pero sin paridad con entornos reales.
- **Postgres autoalojado en ECS:** requiere volumen persistente y backups propios.

## Verificado (2026-10-10, solo lectura, `us-east-2`)
- La cuenta está en el plan **Free**, activo, con crédito restante y fecha de expiración del plan en abril de 2027.
- RDS responde en `us-east-2`: `db.t4g.micro` con PostgreSQL está disponible como opción de instancia, la cuota de instancias es 40 y no hay instancias ni clústeres creados.
- Existe una VPC por defecto con 3 subredes por defecto en `us-east-2`.
- El usuario IAM de la sesión tiene `AdministratorAccess`. La simulación de `rds:CreateDBInstance` da «denegado» sin contexto de región y «permitido» con `aws:RequestedRegion=us-east-2`, es decir, hay una condición por región; coincide con la restricción documentada en ADR-005.

## Por verificar
- Que crear la instancia realmente funcione: el SCP de la organización no se puede simular, así que la prueba definitiva es crearla.
- Que el uso de RDS otorgue el crédito adicional de 20 USD (se ve en la consola, «Explore AWS»).
- Costo real de la instancia y del almacenamiento: no hay cifra verificada; revisar la calculadora de precios y Billing.
- Cómo se conecta el servicio de ECS Express Mode a RDS (VPC, subredes y grupo de seguridad que usa el servicio). Es la pieza con más incertidumbre.
- Qué pasa con la base de datos cuando expira el plan Free (abril de 2027) si no se pasa al plan de pago.

## Consecuencias
- Todo queda en AWS, pero una instancia de RDS **cobra por hora aunque esté inactiva** (contra el crédito): hay que detenerla o borrarla al terminar, igual que el servicio de Fargate. Una instancia detenida se reinicia sola a los 7 días.
- Hay que definir red y seguridad: el grupo de seguridad de RDS solo debe aceptar el puerto 5432 desde el servicio. Evitar el acceso público; si hace falta para correr migraciones desde GitHub Actions, es una excepción temporal que se documenta.
- La infraestructura de la base de datos se agrega a `infra/aws/` como plantilla de CloudFormation y al script `cleanup-aws.sh`.
- Las migraciones deben ser compatibles hacia atrás: el rollback de imagen no revierte el esquema.
- Si se quieren conservar datos, exportarlos de SQLite e importarlos; con datos de ejemplo basta el seed.

## Criterios de aceptación (ver HU-08)
- [ ] La API arranca contra Postgres local con `docker compose`.
- [ ] Las pruebas pasan en CI con service container de Postgres.
- [ ] El CD aplica migraciones y despliega contra RDS.
- [ ] Los datos sobreviven a un redespliegue.
- [ ] ADR-004 pasa a "Aceptada" y se actualizan README y runbook.
