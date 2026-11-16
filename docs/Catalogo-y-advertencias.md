# Catálogo y advertencias alimentarias

Desde `/catalog` se puede buscar por texto o código, consultar el detalle, guardar el producto y agregarlo a la despensa. La búsqueda y el detalle usan la API propia; consultar no guarda ni crea ítems. Guardar un código existente recupera el producto interno sin duplicarlo. Cada ítem de despensa pertenece al usuario de la sesión.

Al agregar se puede indicar cantidad, unidad y fecha de vencimiento. Si falla el agregado después de guardar el producto, el formulario conserva los datos y el producto no se elimina. Cuando la respuesta de creación es incierta, conviene revisar Mi despensa antes de reintentar para evitar duplicar el ítem.

Las advertencias se calculan en el servidor con las preferencias persistidas del usuario autenticado. No se guardan en el producto compartido. Al consultar se usan los datos externos recibidos; al guardar o agregar se utilizan los datos del producto interno. Como pueden diferir, la interfaz conserva ambas evaluaciones y las identifica por su fuente.

La comparación normaliza mayúsculas, acentos y espacios, elimina prefijos de idioma y usa secuencias de palabras completas. Conserva letras y números Unicode; no traduce ingredientes ni infiere equivalencias o plurales. Los campos ausentes o ilegibles se informan como no verificables. Si una preferencia no contiene letras ni números, tampoco se presenta la comparación como completa. Una coincidencia y un campo no verificable pueden mostrarse juntos.

La opción de alertas oculta las coincidencias personalizadas; no oculta la fuente, la aclaración informativa ni los campos no verificables. La información del proveedor puede estar incompleta y no constituye consejo médico.

La suite de componentes usa servicios sustituidos y no depende de Internet. Para comprobar el conjunto es necesario además iniciar backend y Angular, usar una base de pruebas y verificar la sesión, el guardado, el agregado, las advertencias y el aislamiento entre usuarios. Las pruebas con SQLite no comprueban por sí solas SQL Server ni la autorización HTTP real.
