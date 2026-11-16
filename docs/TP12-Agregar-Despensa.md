# TP12 — Guardar y agregar a Mi despensa

Desde el detalle del catálogo, «Guardar producto» reutiliza el servicio de guardado por código. «Agregar a mi despensa» abre un formulario con cantidad, unidad y vencimiento opcionales. Al confirmar, guarda primero el producto si aún no se obtuvo su identificador y usa ese identificador para crear un ítem de la persona autenticada.

La cantidad usa las mismas validaciones de la edición de despensa: no negativa, hasta dos decimales y como máximo 999.999.999.999,99. La unidad admite 50 caracteres; la fecha se envía como fecha, sin conversión horaria. Cancelar no crea un ítem y el formulario conserva sus datos si falla una solicitud.

Si se guarda el producto y falla el agregado, se conserva el identificador para un reintento explícito. Cuando no se puede confirmar el resultado de la creación, la interfaz indica que se revise Mi despensa antes de intentar nuevamente: la solicitud podría haberse completado aunque su respuesta no haya llegado. Agregar deliberadamente otra vez puede crear otro ítem.

Las advertencias devueltas por el servidor al guardar o crear se muestran en el componente del detalle. No se envían preferencias ni propietario desde el formulario; el servidor utiliza la sesión.

## Verificación

En la copia de trabajo se ejecutaron las pruebas de Angular y la compilación de producción. La implementación incluye pruebas con servicios simulados para el guardado previo, la creación, entradas inválidas, cancelación y error con reintento explícito. Estas pruebas no reemplazan el recorrido real de Angular, API y base de datos, ni la comprobación con dos usuarios, que quedan pendientes de registrar para el cierre del bloque.


## Comprobaciones reales del 16/11/2026

En SQL Server Express, con la base aislada `LosTresMosqueteros_TP12_Jesyk_20261116`, se ejecutó el recorrido mediante Angular y API local. Producto consultado: `3017620422003` (Open Food Facts; datos externos variables).

- Rechazo de cantidad negativa; agregado con cantidad y unidad y persistencia tras recarga; agregado con campos vacíos.
- Cancelar no creó otro ítem; fecha `2026-12-15` se mostró como 15/12/2026.
- Edición de cantidad, marcado como consumido y eliminación de un ítem de prueba correctos.
- Dos usuarios locales: el segundo vio su despensa vacía, creó un ítem propio y recibió sus propias preferencias. La lectura directa de un ítem del primero desde el segundo respondió con error de entidad inexistente. La modificación ajena por HTTP queda pendiente de comprobación manual; las pruebas de integración existentes la cubren a nivel de servicio.
- Con el servidor detenido después de guardar el producto, la interfaz conservó el formulario y advirtió sobre el resultado incierto. La base confirmó que no se creó el ítem; tras reiniciar, el reintento explícito creó uno. El producto se mantuvo único.
- Una preferencia `milk` produjo coincidencia visible al abrir, guardar y agregar; desactivar alertas ocultó la coincidencia manteniendo visibles fuente y aclaración.
- Solicitud anónima a la API de despensa: HTTP 401.

Las suites locales registraron 67 pruebas Angular y 186 backend correctas; `yarn build` terminó con advertencia previa de tamaño de estilos de Home. Cobertura unitaria consolidada por archivo y línea con `CodeCoverage.runsettings`, Application y Domain: **190/442 líneas (42,99 %)**. El mínimo de 70 % corresponde al cierre del trabajo final, no al cierre de este bloque.

## Comprobaciones adicionales del 16/11/2026

- Inglés: catálogo, formulario, validación de cantidad y confirmación del agregado se mostraron traducidos; el agregado se realizó en la base aislada.
- Ancho móvil: se solicitó una ventana de 375 px y el navegador informó 312 px efectivos. El formulario no presentó desbordamiento horizontal en ese ancho. Al desplazarse, el pie de página se superpuso parcialmente al contenido; queda pendiente revisar su presentación.
- Con una sesión autenticada del segundo usuario, GET y PUT del ítem del primero respondieron HTTP 404. Para PUT se utilizó la protección habitual de solicitudes; SQL Server confirmó que cantidad y unidad siguieron vacías y el vencimiento siguió en 2026-12-15.

Pendientes para cerrar TP12: resolver o evaluar la superposición del pie de página, actualizar la documentación común, integrar el código y obtener revisión de otra persona y CI de backend/frontend en verde. No se ha acreditado todavía el cierre del bloque completo.
