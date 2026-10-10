# Requisitos

## Funcionales
| ID | Requisito |
|---|---|
| RF-01 | El usuario puede crear un hábito con nombre. |
| RF-02 | El usuario puede eliminar un hábito. |
| RF-03 | El usuario puede marcar un hábito como hecho en el día actual. |
| RF-04 | El usuario puede desmarcar un hábito del día actual. |
| RF-05 | La app muestra la racha actual de cada hábito. |
| RF-06 | La app muestra la racha máxima de cada hábito. |
| RF-07 | La app muestra una vista semanal con los días cumplidos. |

## No funcionales
| ID | Requisito |
|---|---|
| RNF-01 | La app es instalable como PWA en iOS y Android. |
| RNF-02 | La interfaz carga sin conexión (caché del service worker). |
| RNF-03 | Tiempo de respuesta de la API menor a 500 ms en uso normal. |
| RNF-04 | La lógica de rachas tiene cobertura de pruebas unitarias. |
| RNF-05 | El despliegue es automático al hacer merge a `main`. |
| RNF-06 | Se sirve exclusivamente por HTTPS. |
| RNF-07 | Los datos persisten entre despliegues y reinicios (día 2, con PostgreSQL; el día 1 usa seed). |
| RNF-08 | Los cambios del esquema se aplican mediante migraciones versionadas. |
| RNF-09 | La infraestructura se define como código y puede recrearse con un comando. |
