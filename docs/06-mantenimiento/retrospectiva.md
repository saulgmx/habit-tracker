# Retrospectiva

*Completar al terminar el demo.*

## Qué salió bien
-

## Qué mejorar
-

## Bugs encontrados
| Bug | Cómo se detectó | Corrección |
|---|---|---|
| Fecha mostrada como «9 De Octubre» | Revisión visual de la UI en el navegador | `::first-letter` en lugar de `text-transform: capitalize` |
| Despliegue 1: el balanceador no se crea (`AccessDenied`) | Eventos del stack, `describe-services` de ECS y CloudTrail | Permiso `ec2:DescribeAccountAttributes` en el rol de infraestructura (PR #14) |
| Despliegue 2: falla al leer el `Endpoint` | Evento `Service` del stack y CloudTrail | `ecs:DescribeServiceRevisions` y otros permisos (PR #15), tomados de la lista oficial del recurso |
| Borrar una rama cerró un PR abierto (#13) | Estado del PR en GitHub | Restaurar la rama desde `refs/pull/13/head` y reabrir; comprobar que el PR esté integrado antes de borrar su rama |

## Acciones para la próxima iteración
-
