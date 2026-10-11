# API

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/habits` | Lista hábitos con racha actual, máxima y últimos 7 días |
| POST | `/api/habits` | Crea un hábito. Body: `{ "name": "Beber agua" }` |
| DELETE | `/api/habits/{id}` | Elimina un hábito |
| PUT | `/api/habits/{id}/checkins/{date}` | Marca el día como hecho y devuelve el hábito actualizado (`200`) |
| DELETE | `/api/habits/{id}/checkins/{date}` | Desmarca el día y devuelve el hábito actualizado (`200`) |
| GET | `/health` | Verificación de salud para el despliegue |

## Ejemplo de respuesta de `GET /api/habits`
```json
[
  {
    "id": 1,
    "name": "Beber agua",
    "currentStreak": 4,
    "bestStreak": 9,
    "last7Days": [true, true, false, true, true, true, true]
  }
]
```

`PUT` y `DELETE` de un check-in devuelven el mismo objeto que un elemento de `GET /api/habits`, con las rachas ya recalculadas, para que el cliente no tenga que recargar la lista. Son idempotentes: marcar un día ya marcado, o desmarcar uno sin marcar, responde `200` igual; peticiones simultáneas para el mismo día no duplican el check-in.

Todas las rutas que calculan rachas aceptan `?today=yyyy-MM-dd` (la fecha local del cliente); sin él, se usa la fecha del servidor.

## Errores
- `400` si el nombre está vacío o la fecha es inválida.
- `404` si el hábito no existe.
