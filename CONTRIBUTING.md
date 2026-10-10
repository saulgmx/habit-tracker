# Cómo contribuir

1. Toma un issue del tablero de GitHub Projects (`#123`).
2. Crea una rama desde `main`: `feature/123-descripcion-corta` o `fix/123-descripcion-corta`.
3. Haz commits pequeños con el formato: `tipo: mensaje (#123)`. Tipos: `feat`, `fix`, `test`, `docs`, `ci`, `refactor`.
4. Abre un Pull Request con la plantilla y enlaza el issue (`Closes #123`).
5. El PR requiere **CI en verde** y **1 aprobación** antes de hacer merge. Con un solo colaborador nadie puede aprobar su propio PR: el administrador hace el merge con *bypass* cuando el CI está en verde.
6. Se hace *squash merge* a `main`.
