# Administración de la despensa

Para administrar usuarios y roles usamos Gestión de identidad de ABP en `/identity/users` y `/identity/roles`. Desde la edición de un usuario, el administrador puede asignar roles y cambiar el estado de la cuenta.

## Permisos del incremento

- `LosTresMosqueteros.Administration.Products` protege el CRUD de productos internos en `/api/app/producto` y se reutiliza para el listado administrativo.
- `LosTresMosqueteros.Administration.Audit` se usa para la consulta de auditoría.
- `LosTresMosqueteros.Administration.IntegrationMetrics` se usa para la consulta de métricas.

Al incorporar una pantalla administrativa, aplicar el mismo permiso al endpoint, la ruta y el menú. Ejecutar DbMigrator sobre la base de desarrollo para sembrar los permisos del incremento. El rol `admin` recibe los permisos iniciales mediante ABP.

## Verificación

- Con un administrador, listar usuarios, cambiar el rol de una cuenta de prueba y deshabilitarla. Volver a iniciar sesión con esa cuenta y comprobar el rechazo.
- Comparar `GET /api/identity/users` y `GET /api/app/producto` con administrador, con usuario común y sin sesión. Las respuestas esperadas son 200, 403 y 401, respectivamente.
- Verificar que el usuario común pueda seguir consultando el catálogo y usar Mi despensa.
