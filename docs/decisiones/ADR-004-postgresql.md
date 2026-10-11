# ADR-004: Migrar de SQLite a PostgreSQL (día 2)

- **Estado:** Aceptada (implementada y probada en AWS el 2026-10-10)
- **Fecha:** 2026-10-09
- **Revisada:** 2026-10-10 (la base de datos en la nube pasa de Supabase a Amazon RDS for PostgreSQL; se aplica y se verifica)

## Contexto
El día 1 se usa SQLite por simplicidad y menor riesgo en el demo. SQLite en un contenedor pierde los datos en cada despliegue o reinicio y no da paridad con entornos reales.

La cuenta de AWS es nueva y está en el plan Free. Según la página de AWS Free Tier de RDS, quien se une después del 15 de julio de 2025 recibe hasta 100 USD de crédito al registrarse y puede ganar hasta 100 USD más por activar servicios básicos, y RDS y Aurora son elegibles. Se confirmó en la consola (widget «Explore AWS») que usar RDS sumó los 20 USD adicionales; ver «Verificado».

## Decisión
- **Local y CI:** PostgreSQL 16 en contenedor (`docker-compose.yml` y *service container* de GitHub Actions).
- **Nube:** Amazon RDS for PostgreSQL (instancia única, `db.t4g.micro`, Single-AZ, 20 GiB gp3 cifrado, sin acceso público) en `us-east-2`, definida en `infra/aws/db.yaml` y creada con `scripts/setup-db.sh`. Se usa con Npgsql + EF Core.
- **Esquema:** migraciones de EF Core regeneradas para Postgres (las de SQLite no son portables). **Se aplican al arrancar la API (`Migrate()`), no como paso del CD:** RDS es privada y un runner de GitHub Actions no está dentro de la VPC, así que no puede alcanzarla sin abrir el acceso público. Esto cambia lo que decía la primera versión de este ADR.
- **Credenciales:** RDS administra la contraseña del usuario administrador en AWS Secrets Manager (`ManageMasterUserPassword`). La tarea recibe `Database__User` y `Database__Password` como secretos de ECS (claves `username` y `password` del secreto) y `Database__Host`, `Database__Port` y `Database__Name` como variables. No hay contraseñas en plantillas, en el repo ni en secrets de GitHub. La API arma la cadena de conexión con esas partes (`DatabaseConfig`).
- **Red:** `db.yaml` crea un grupo de seguridad para las tareas (`AppSecurityGroup`, sin entradas) que se pasa a Express Mode en `NetworkConfiguration`, y otro para la base que solo acepta el 5432 desde ese grupo. Según la documentación de ECS, un grupo propio se suma a los que Express Mode crea; no los reemplaza.
- **Ciclo de vida:** el stack de la base se crea y se borra aparte del servicio (`setup-db.sh` y `cleanup-aws.sh db`). Se borra sin instantánea final: los datos son de demostración.

## Alternativas descartadas
- **Supabase (decisión original):** PostgreSQL administrado fuera de AWS, sin costo directo, pero es una dependencia externa que puede pausarse y no aprovecha el crédito de AWS. Se cambió a RDS para mantener todo en una sola cuenta y usar el crédito.
- **Aurora PostgreSQL:** también es elegible para el crédito, pero cuesta más y su capacidad mínima es mayor que la de una instancia pequeña de RDS; sobra para este demo.
- **SQLite + volumen EFS:** menos piezas, pero sin paridad con entornos reales.
- **Postgres autoalojado en ECS:** requiere volumen persistente y backups propios.
- **Migraciones como paso del CD o como tarea de ECS aparte:** más fieles al ideal de separar migración y arranque, pero necesitan acceso de red a la base o más piezas. Para el demo se prefirió `Migrate()` al arrancar.
- **Contraseña generada por CloudFormation:** descartada porque RDS puede administrar el secreto sin que la contraseña pase por la plantilla.

## Verificado (2026-10-10)
- **Disponibilidad:** `db.t4g.micro` con PostgreSQL 16 está disponible en `us-east-2` (gp3 desde 20 GiB). La VPC predeterminada tiene 3 subredes. `EngineVersion: '16'` (solo la versión mayor) fue aceptado y RDS eligió 16.13.
- **Creación:** el stack `habit-tracker-db` se creó sin errores con `setup-db.sh` en menos de 10 minutos. El SCP de la organización no bloqueó la instancia ni la creación automática del rol vinculado `AWSServiceRoleForRDS`, que no existía antes. La instancia quedó `available`, privada, cifrada, Single-AZ, con un solo grupo de seguridad que acepta el 5432 desde el grupo de las tareas.
- **Conexión:** el primer despliegue contra RDS (6 min 19 s) funcionó sin ajustar permisos: el rol de ejecución lee el secreto con la política `read-database-secret` que `db.yaml` le añade, y Npgsql conectó. `/health` devolvió `"database":"ok"` y `/api/habits` los datos sembrados. La tarea tiene dos grupos de seguridad (el de Express Mode y el nuestro).
- **Persistencia:** un hábito creado en el servicio en vivo siguió ahí, con su check-in, después de un redespliegue (10 min) que reemplazó la tarea (definición `:6` a `:7`, otra imagen) con la misma instancia de RDS. El seed no se duplicó: solo siembra con la base vacía.
- **Aviso de Kerberos:** Npgsql intentaba cifrado GSS y registraba `libgssapi_krb5.so.2` al arrancar en la imagen. Se desactiva en la cadena de conexión (`GSS Encryption Mode=Disable`); la autenticación es por contraseña y no se vio ningún efecto en RDS.
- **Crédito:** el dueño de la cuenta confirmó en la consola que se aplicaron 20 USD por usar RDS y otros 20 USD por usar AWS Budgets. El plan Free sigue activo y, según la API de AWS (2026-10-10), quedan 139.94 USD con vencimiento el 2027-04-09, frente a los 100 USD de antes de usar RDS. El desglose por actividad viene de la consola, no de la API.
- **Precios de lista** (API de precios de AWS, `us-east-2`): 0.016 USD por hora la instancia y 0.115 USD por GB-mes el almacenamiento gp3, unos 2.30 USD al mes por 20 GiB.
- **Latencia:** con la base en RDS, cada llamada a la API medida desde fuera de AWS cuesta aproximadamente un viaje de red (~60 ms desde el equipo de pruebas, con 1 a 5 ms de procesamiento que no se distinguen del ruido). El retraso al marcar un hábito viene de que la interfaz hace dos peticiones seguidas y no actualiza la pantalla hasta recibir ambas, no de la base.

## Por verificar
- Costo real: se verificaron los precios de lista, no el gasto. Falta revisar Billing y el cargo del secreto de Secrets Manager.
- Cómo se comporta `Migrate()` si dos tareas arrancan a la vez: EF Core adquiere un bloqueo exclusivo al migrar (aparece en los logs), pero no se probó con dos tareas.
- Qué pasa con la base de datos cuando expira el plan Free (abril de 2027) si no se pasa al plan de pago.
- Que la conexión use TLS: la API no lo fuerza (`SslMode` por defecto es `Prefer`) y no se comprobó si la sesión quedó cifrada. Se cree que RDS para PostgreSQL 15 o superior exige TLS por defecto, pero no se verificó.

## Consecuencias
- Todo queda en AWS, pero una instancia de RDS **cobra por hora aunque esté inactiva** (contra el crédito): hay que borrarla al terminar, igual que el servicio de Fargate. `cleanup-aws.sh service` deja la base creada y lo avisa; `cleanup-aws.sh db` borra servicio y base. Una instancia detenida se reiniciaría sola a los 7 días, por eso se borra en lugar de detenerla.
- El servicio importa valores del stack de la base: **`deploy.yml` falla si el stack `habit-tracker-db` no existe**, y CloudFormation no deja borrar la base mientras el servicio la importe (por eso el servicio se borra primero).
- Sin acceso público a la base: no se puede abrir con un cliente SQL desde fuera de la VPC.
- La API usa el usuario administrador de RDS (suficiente para el demo; un usuario de aplicación con menos privilegios sería una mejora).
- Las migraciones deben ser compatibles hacia atrás: el rollback de imagen no revierte el esquema, y como se aplican al arrancar, una versión nueva migra la base al desplegarse.
- Si se quieren conservar datos al borrar la base, hay que cambiar `DeletionPolicy` a `Snapshot` en `db.yaml` o exportarlos antes.

## Criterios de aceptación (ver HU-08)
- [x] La API arranca contra Postgres local con `docker compose`.
- [x] Las pruebas pasan en CI con service container de Postgres.
- [x] El despliegue aplica las migraciones (al arrancar la API) contra RDS.
- [x] Los datos sobreviven a un redespliegue.
- [x] ADR-004 pasa a "Aceptada" y se actualizan README y runbook.
