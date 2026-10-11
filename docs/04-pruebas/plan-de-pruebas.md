# Plan de pruebas

## Estrategia
| Nivel | Herramienta | Qué cubre |
|---|---|---|
| Unitarias | xUnit | `StreakCalculator` |
| Integración | `WebApplicationFactory` | Endpoints contra PostgreSQL real, con una base propia por ejecución (`docker compose up -d --wait db` en local; service container en CI) |
| Manual | Móvil real | Instalación PWA, uso táctil |
| Manual en navegador | Brave o Chrome | Marcar un hábito con la red lenta o fallando (HU-10): el cambio es inmediato, se revierte con error si el servidor falla y los toques seguidos salen en orden. Hoy es una prueba manual; automatizarla es la tarea opcional de Playwright |

## Casos de la racha (unitarias)
| # | Escenario | Resultado esperado |
|---|---|---|
| 1 | Sin check-ins | Racha 0 |
| 2 | Solo hoy | Racha 1 |
| 3 | Hoy, ayer y anteayer | Racha 3 |
| 4 | Ayer y anteayer, hoy sin marcar | Racha 2 (sigue viva) |
| 5 | Hoy y hace 2 días (falta ayer) | Racha 1 |
| 6 | Último check-in hace 2 días o más | Racha 0 |
| 7 | Cruce de mes (31 → 1) | Cuenta continua |
| 8 | Cruce de año (31 dic → 1 ene) | Cuenta continua |
| 9 | Año bisiesto (28 → 29 feb → 1 mar) | Cuenta continua |
| 10 | Fechas duplicadas | No suman dos veces |
| 11 | Fechas desordenadas | Mismo resultado que ordenadas |
| 12 | Racha máxima con varias rachas históricas | Devuelve la mayor |

## Bug intencional para el demo
Cambiar la comparación de días consecutivos (por ejemplo `== 1` por `<= 1` o romper el caso de "ayer sin hoy") para que fallen los casos 4, 5 o 10. Dejar el commit listo en una rama `demo/bug-racha`.
