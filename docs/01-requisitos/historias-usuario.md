# Historias de usuario

> Cada historia se replica como issue en GitHub (`scripts/seed-issues.sh`) y se organiza en un tablero de GitHub Projects.

## Épica 1: Gestión de hábitos

### HU-01 Crear un hábito
**Como** usuario **quiero** crear un hábito **para** empezar a darle seguimiento.
- [ ] Puedo escribir un nombre y guardarlo.
- [ ] No se permite un nombre vacío.
- [ ] El hábito aparece en la lista de inmediato.

### HU-02 Eliminar un hábito
**Como** usuario **quiero** eliminar un hábito **para** mantener mi lista limpia.
- [ ] Se pide confirmación antes de eliminar.
- [ ] Se eliminan también sus registros.

## Épica 2: Seguimiento diario

### HU-03 Marcar hábito como hecho hoy
**Como** usuario **quiero** marcar un hábito como hecho **para** registrar mi avance.
- [ ] Un toque marca o desmarca el hábito de hoy.
- [ ] El cambio se guarda y se conserva al recargar.

### HU-10 Marcar un hábito responde al instante
**Como** persona que usa la app **quiero** que al marcar o desmarcar un hábito la pantalla cambie al instante **para** que se sienta fluida aunque la conexión con el servidor tenga latencia.
- [x] Al tocar un hábito, la casilla y la racha se actualizan sin esperar al servidor.
- [x] La petición se confirma en segundo plano; si falla, se revierte el cambio y se muestra el mensaje de error.
- [x] No se pierde ni se duplica un check-in si se toca varias veces seguidas.
- [x] `PUT` y `DELETE` de `/api/habits/{id}/checkins/{date}` devuelven el hábito actualizado, para evitar recargar la lista.
- [x] Hay pruebas que cubren el cambio (la API; la interfaz se prueba a mano o en un navegador).

## Épica 3: Rachas

### HU-04 Ver mi racha actual
**Como** usuario **quiero** ver mis días consecutivos **para** motivarme a no romperla.
- [ ] La racha cuenta días consecutivos hasta hoy.
- [ ] Si hoy aún no está marcado pero ayer sí, la racha sigue viva.
- [ ] Si se salta un día completo, la racha vuelve a 0.
- [ ] Funciona correctamente al cruzar de mes y de año.

### HU-05 Ver mi racha máxima
**Como** usuario **quiero** ver mi mejor racha **para** compararme conmigo mismo.

### HU-06 Ver la semana
**Como** usuario **quiero** ver los últimos 7 días **para** detectar mi constancia.

## Épica 4: PWA

### HU-07 Instalar la app en el móvil
**Como** usuario **quiero** instalar la app **para** abrirla como cualquier otra.
- [ ] Tiene manifest e íconos.
- [ ] Muestra pantalla de inicio e interfaz sin conexión.

## Épica 5: Evolución técnica (día 2)

### HU-08 Migrar la base de datos a PostgreSQL
**Como** equipo **quiero** migrar de SQLite a PostgreSQL **para** conservar los datos entre despliegues.
- [x] La API arranca contra Postgres local con `docker compose`.
- [x] Las pruebas pasan en CI con un service container de Postgres.
- [x] El despliegue aplica las migraciones (la API las ejecuta al arrancar) contra RDS y despliega.
- [x] Los datos sobreviven a un redespliegue.

## Épica 6: Infraestructura

### HU-09 Crear y borrar el entorno con comandos
**Como** equipo **quiero** crear y borrar la infraestructura con comandos y plantillas **para** montar y desmontar el entorno de forma repetible y controlar el costo.
- [ ] `scripts/setup-aws.sh` crea el stack base (ECR, cluster, roles, usuario IAM de despliegue), crea la llave de acceso y guarda los secrets y variables en GitHub.
- [ ] El primer despliegue crea el stack del servicio con `aws cloudformation deploy`.
- [ ] El escalado está acotado (máximo 2 tareas, 0.25 vCPU / 0.5 GiB).
- [ ] El workflow imprime la URL del servicio.
- [ ] Las plantillas se validan en CI con `cfn-lint`.
- [ ] `scripts/cleanup-aws.sh` borra el servicio, o todo el entorno.
