# UX

## Principios
- Una mano, pulgar: acciones principales abajo.
- Un toque para lo más frecuente (marcar hábito).
- Contraste alto y botones grandes.

## Pantallas
1. **Hoy:** lista de hábitos con check grande, racha 🔥 y mini-semana.
2. **Nuevo hábito:** campo de nombre y botón guardar.
3. **Detalle (opcional v2):** vista semanal y racha máxima.

## Flujo
```mermaid
flowchart TD
    A[Abrir app] --> B[Pantalla Hoy]
    B -->|Tocar check| C[Hábito marcado, racha se actualiza]
    B -->|Botón +| D[Nuevo hábito]
    D -->|Guardar| B
```

## Pendiente
- Wireframes (boceto a mano o Figma).
- Paleta y tipografía.
