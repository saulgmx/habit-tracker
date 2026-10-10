# API

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/habits` | Lista hábitos con racha actual, máxima y últimos 7 días |
| POST | `/api/habits` | Crea un hábito. Body: `{ "name": "Beber agua" }` |
| DELETE | `/api/habits/{id}` | Elimina un hábito |
| PUT | `/api/habits/{id}/checkins/{date}` | Marca el día como hecho |
| DELETE | `/api/habits/{id}/checkins/{date}` | Desmarca el día |
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

## Errores
- `400` si el nombre está vacío o la fecha es inválida.
- `404` si el hábito no existe.
