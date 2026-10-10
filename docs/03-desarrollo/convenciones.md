# Convenciones de desarrollo

## Ramas
`main` (protegida) · `feature/123-nombre` · `fix/123-nombre` (el número es el del issue)

## Commits
`tipo: mensaje (#123)` — tipos: `feat`, `fix`, `test`, `docs`, `ci`, `refactor`.

## Código
- C#: `dotnet format` y analizadores por defecto.
- La lógica de negocio vive en clases puras, sin dependencias de EF ni HTTP.
- JS: módulos ES, sin librerías externas.
- Migraciones: una por cambio de modelo, con nombre descriptivo y revisadas en el PR (se regeneran al migrar a PostgreSQL).
- Evitar SQL crudo y funciones específicas de SQLite para facilitar la migración del día 2.
- Secretos: nunca en el repo; usar variables de entorno.
- CloudFormation: parámetros con descripción y valores por defecto razonables; validar con `cfn-lint` antes del PR.

## Definición de "terminado"
- [ ] Cumple los criterios de aceptación de la historia.
- [ ] Pruebas nuevas o actualizadas y en verde.
- [ ] CI en verde.
- [ ] PR revisado y aprobado.
- [ ] Documentación actualizada si aplica.
- [ ] Probado en móvil.
