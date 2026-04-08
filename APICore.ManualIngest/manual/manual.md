# Sistema de gestión de inventario — manual de usuario

## 1. Introducción

Este sistema permite gestionar inventario multi-ubicación: productos, stock por ubicación, movimientos (entradas, salidas, ajustes, transferencias), pedidos de venta, contactos, leads, promociones y administración (usuarios, roles, configuración, registros).

La navegación principal es la barra lateral izquierda; la barra superior incluye selector de moneda y accesos directos.

Flujo de trabajo típico

1. Definir ubicaciones (almacenes / tiendas).
2. Crear categorías y proveedores.
3. Crear productos (con imágenes opcionales).
4. Registrar movimientos de inventario para cambiar stock (o usar flujos que crean stock de forma implícita donde la app lo permita).
5. Usar Ventas para pedidos; Contactos / Leads para CRM.
6. Usar Reportes para exportaciones y vistas analíticas.

---

## 2. Inicio de sesión y sesión

### 2.1 Inicio de sesión

1. Abre la página de login (`/login`).
2. Introduce correo y contraseña.
3. Envía el formulario.
4. Si es correcto, te redirige a `/dashboard`.

Ejemplo

- Correo: `usuario@empresa.com`
- Contraseña: (la que te facilite tu administrador)

### 2.2 Sesión y cierre

- La app mantiene la sesión al recargar (token / usuario guardado, según despliegue).
- Para salir con seguridad: en la barra lateral, Cerrar sesión (abajo). Borra la sesión local y vuelve al login.

### 2.3 Permisos

Los ítems del menú y los botones dependen de tu rol. Si no ves una sección o el botón Nuevo … está desactivado, te falta el permiso correspondiente (por ejemplo `product.create`, `inventorymovement.create`). Pide a un administrador que ajuste tu rol en Usuarios / Roles.

---

## 3. Diseño general

| Zona | Función |
|------|--------|
| Barra lateral | Módulos: Dashboard, Productos, Categorías, Proveedores, Ubicaciones, Inventario, Movimientos, Ventas, Contactos, Leads, Promociones; ADMIN: Usuarios, Roles, Logs, Categorías de negocio, Configuración; submenú REPORTES. |
| Barra superior | Menú (móvil / colapsar), selector de moneda, iconos de notificaciones o mensajes (si están activos). |
| Contenido principal | Tablas, filtros, modales y panel lateral de detalle al abrir una fila. |

En móvil

- Abre el menú con el icono de menú hamburguesa; Escape cierra la capa superpuesta.

---

## 4. Dashboard

Ruta: Dashboard → `/dashboard`

Qué verás

- Resumen de KPI (productos, valor de inventario, stock bajo, pedidos, tendencias — según datos de la API).
- Gráficos: flujo de inventario, mix por categoría, estado del stock, entradas vs salidas, evolución de valor, mapas de calor o listas, etc.

Uso

- Usa los controles de fecha o rango cuando existan para acotar el periodo.
- Pulsa elementos del gráfico o lista si la interfaz enlaza al detalle (depende de la implementación).

---

## 5. Productos

Ruta: Productos → `/dashboard/products`

Permisos: normalmente `product.read` para ver; `product.create`, `product.update`, `product.delete` para cambios.

### 5.1 Listado y filtros

- Búsqueda por texto con retardo (debounce).
- Filtros habituales: categoría, disponibilidad, a la venta, tipo de producto, precio mínimo/máximo.
- Nuevo Producto abre el modal de alta (si tienes permiso).

### 5.2 Crear un producto

1. Pulsa Nuevo Producto.
2. Rellena los campos obligatorios (nombre, SKU o código, categoría, precios y campos de stock que muestre el formulario).
3. Activa las opciones Disponible y A la venta si el formulario las incluye.
4. Opcionalmente asigna etiquetas y ubicaciones donde se ofrece el producto (por ejemplo con casillas).
5. Guarda o envía el modal.

Ejemplo (orientativo)

- Nombre: Cable USB-C 2 m
- Categoría: Accesorios
- Precio de venta: 12,99
- Márcalo disponible y a la venta si lo vendes en el catálogo público.

### 5.3 Editar un producto

1. Abre el menú de la fila o pulsa la fila (según acciones de la tabla).
2. Elige editar.
3. Modifica los campos y guarda.

Nota: algunos campos (por ejemplo ubicaciones de oferta) solo se envían al servidor si los cambias, para no sobrescribir datos en el servidor.

### 5.4 Eliminar un producto

1. Usa la acción Eliminar en la fila (o equivalente).
2. Confirma en el diálogo de borrado.

Los errores del servidor se muestran en el modal o como notificaciones tipo toast.

### 5.5 Imágenes

- Sube imagen (arrastrar y soltar o selector de archivo); suelen admitirse JPEG, PNG, GIF, WebP (tamaño máximo en la interfaz, por ejemplo 5 MB).
- Galería: define imagen principal, reordena y elimina imágenes donde la interfaz lo permita.

### 5.6 Importación

- Usa el asistente de importación (por ejemplo CSV) cuando aparezca en la barra de herramientas: previsualiza y confirma los pasos en pantalla.

### 5.7 Acciones masivas

- Selecciona varias filas si la tabla lo permite; usa la barra de acciones masivas (por ejemplo borrado masivo) si tienes permisos.

---

## 6. Categorías

Ruta: Categorías → `/dashboard/categories`

Permisos: `productcategory.*`

### Crear

1. Nueva categoría.
2. Introduce el nombre (y demás campos obligatorios).
3. Guarda.

### Editar / eliminar

- Acciones de fila → Editar / Eliminar; confirma el borrado.
- La aplicación puede ofrecer también vistas de estadísticas o uso de almacenamiento donde estén implementadas.

---

## 7. Proveedores

Ruta: Proveedores → `/dashboard/suppliers`

Permisos: `supplier.*`

### Crear

1. Nuevo proveedor (o botón equivalente).
2. Completa datos (nombre, contacto, identificadores fiscales, etc. según el formulario).
3. Guarda.

### Editar / eliminar

- Acciones de fila → editar o eliminar con confirmación.

---

## 8. Ubicaciones

Ruta: Ubicaciones → `/dashboard/locations`

Permisos: `location.*`

### Crear

1. Nueva ubicación.
2. Rellena nombre, dirección, vínculo con organización, categoría de negocio, etc.
3. Configura horario comercial en el editor cuando exista (por día de la semana).
4. Guarda.

### Editar / eliminar

- Abre edición desde la fila; Eliminar con confirmación.
- Imagen: sube imagen de la ubicación con reglas similares a las de producto cuando esté disponible.

### Borrado masivo

- Si está activo, selecciona filas y ejecuta borrado masivo desde la barra.

---

## 9. Inventario

Ruta: Inventario → `/dashboard/inventory`

Permisos: `inventory.read`

Esta pantalla muestra sobre todo el stock actual por producto y ubicación de forma consultiva.

### Uso

- Filtra por texto, ubicación, categoría, solo stock crítico, rango de fechas (creación o modificación).
- Abre una fila para ver el detalle en el panel lateral.

Importante: para cambiar cantidades usa Movimientos (entradas, salidas, ajustes, transferencias), no edición arbitraria en esta cuadrícula (salvo que tu despliegue añada esa función).

---

## 10. Movimientos

Ruta: Movimientos → `/dashboard/movements`

Permisos: `inventorymovement.read` para listar; `inventorymovement.create` para crear.

### Tipos de movimiento

| Tipo | Uso habitual |
|------|--------------|
| Entrada | Mercancía recibida (compra, devolución a stock). |
| Salida | Mercancía que sale (envío de venta, consumo, merma). |
| Ajuste | Corrección de stock. |
| Transferencia | Entre ubicaciones (cuando el formulario pida origen y destino). |

### Crear un movimiento

1. Usa botones rápidos como Entrada / Salida o la acción principal de nuevo movimiento.
2. Elige ubicación y producto (o crea un producto mínimo desde el flujo si la interfaz lo ofrece).
3. Indica cantidad, tipo, motivo (de la lista configurada), fecha si aplica.
4. Envía.

Ejemplo

- Entrada de 50 unidades del producto SKU-001 en Depósito Central, motivo Compra proveedor.

### Listado / detalle

- Filtra por tipo, texto, fechas, etc.
- Abre una fila para el detalle del movimiento en el panel lateral.

---

## 11. Ventas

Ruta: Ventas → `/dashboard/sales`

Permisos: `sale.read` y los relacionados para crear, actualizar o confirmar.

### Estados del pedido (etiquetas en pantalla)

- Pendiente (borrador)
- Aceptada (confirmada)
- Cancelada

### Flujo habitual

1. Crea un pedido (líneas, ubicación, totales según el formulario).
2. Confirma o cancela desde acciones de fila cuando existan.
3. Filtra el listado por estado, fechas, importe, vendedor o búsqueda de texto.

### Ejemplo

- Nuevo pedido en Tienda Centro con dos líneas → guardar como Pendiente → Aceptar cuando la venta quede cerrada.

---

## 12. Contactos

Ruta: Contactos → `/dashboard/contacts`

Permisos: `contact.create`, `contact.update`, `contact.delete`

### Altas, bajas y cambios

- Nuevo contacto → formulario → guardar.
- Editar / Eliminar desde acciones de fila.
- Borrado masivo cuando esté permitido y haya filas seleccionadas.

---

## 13. Leads

Ruta: Leads → `/dashboard/leads`

Permisos: `lead.*`

### Altas, bajas y cambios

- Nuevo lead → formulario → guardar.
- Editar / Eliminar por fila.
- Convertir lead (acción que llama al endpoint de conversión) cuando lo pases a cliente u oportunidad.

### Ejemplo

- Lead María — consulta mayorista → actualizar estado → Convertir cuando cierre favorable.

---

## 14. Promociones

Ruta: Promociones → `/dashboard/promotions`

Permisos: en la app actual suelen ligarse a `product.update` para la gestión.

### Uso

- Crea o edita promociones (reglas de descuento, fechas, productos y ubicaciones según el formulario).
- Activa o desactiva la promoción donde haya interruptores.
- Elimina cuando esté permitido.

---

## 15. Usuarios

Ruta: ADMIN → Usuarios → `/dashboard/users`

Permisos: `user.*`

### Crear usuario

1. Nuevo usuario.
2. Nombre, correo, contraseña (si aplica), rol, ubicación y campos de organización según requisitos.
3. Guarda.

### Editar / eliminar

- Edita desde acciones de fila; elimina con confirmación.
- Algunos campos pueden ser actualización parcial (solo lo modificado).

---

## 16. Roles

Ruta: ADMIN → Roles → `/dashboard/roles`

Permisos: `role.*`

### Crear rol

1. Nuevo rol.
2. Pon nombre al rol y asigna permisos (lista de casillas en Permisos / API).
3. Guarda.

### Editar / eliminar

- Edita nombre y permisos.
- Elimina el rol si ya no se usa (revisa usuarios que lo tengan asignado).

### Ejemplos de permisos útiles

- `product.read`, `product.create`, `inventorymovement.create`, `sale.read`, `setting.read`, etc.

---

## 17. Registros (Logs)

Ruta: ADMIN → Logs → `/dashboard/logs`

Permisos: `log.read`

- Listado de auditoría o eventos solo lectura, con filtros (tipo, fecha, paginación).
- Sirve para ver quién hizo qué al depurar incidencias.

---

## 18. Categorías de negocio

Ruta: ADMIN → Categorías de negocio → `/dashboard/business-categories`

Permisos: `setting.update` para ediciones en la interfaz actual

- Mantén las categorías que clasifican ubicaciones (por ejemplo tienda frente a almacén).
- Edición en línea o por modal según implementación; guardar persiste los cambios.

---

## 19. Configuración

Ruta: ADMIN → Configuración → `/dashboard/settings`

Secciones (anclas en URL): Inventario, Monedas y tipo de cambio, Notificaciones, Seguridad, Perfil de cuenta, Suscripción.

### Uso

- Inventario: valores por defecto y opciones relacionadas con inventario.
- Monedas: gestión de monedas y tipo de cambio o moneda por defecto donde exista.
- Notificaciones: preferencias.
- Seguridad: contraseña y opciones de seguridad.
- Perfil: actualizar perfil con Actualizar perfil o flujo `account/update-profile`.
- Suscripción: información de Mi suscripción.

Usa el submenú de Configuración en la barra lateral para ir a `#inventario`, `#monedas`, etc.

---

## 20. Reportes

Menú: REPORTES en la barra lateral.

Entradas habituales (según la app):

- Ventas, Inventario, Productos, CRM, Operaciones
- Métricas (`/admin/reports/metrics`): tráfico, productos, ventas, clientes por periodo

### Uso

- Abre el tipo de informe, define filtros y rango de fechas, exporta (CSV/PDF) donde haya botones.

---

## 21. Catálogo público (opcional)

Si el despliegue expone `/catalog`, los clientes pueden ver ubicaciones públicas y catálogo por ubicación, etiquetas y paginación sin iniciar sesión de administración. El administrador sigue manteniendo productos marcados para venta pública y ubicaciones.

---

## 22. Consejos y buenas prácticas

1. Elige siempre la ubicación correcta en movimientos y ventas: el stock es por ubicación.
2. Usa categorías de forma coherente antes de escalar el catálogo de productos.
3. Prefiere movimientos con motivos claros para que informes y auditoría se entiendan bien.
4. La moneda de la barra superior afecta cómo se muestran los importes; alinea los datos con tus normas contables.
5. En tablas grandes, espera a que termine la búsqueda con retardo tras escribir; algunas rejillas cargan páginas extra para comportamientos de seleccionar todo.
6. Si una acción falla, lee el mensaje del toast o del modal: suele ser validación o una regla de negocio de la API.

---

## 23. Referencia rápida: crear / editar / eliminar

| Entidad | Crear | Editar | Eliminar | Notas |
|--------|--------|--------|----------|--------|
| Producto | Nuevo Producto | Fila → editar | Modal de confirmación | Imágenes, importación, masivo |
| Categoría | Nueva categoría | Fila | Confirmar | |
| Proveedor | Botón de alta | Fila | Confirmar | |
| Ubicación | Nueva ubicación | Fila | Confirmar | Horario, imagen |
| Inventario | — | Panel de detalle | — | Solo consulta; cambios vía movimientos |
| Movimiento | Entrada / Salida / formulario | — | — | Según permisos |
| Pedido de venta | Nuevo pedido | Fila / acciones | — | Confirmar / cancelar |
| Contacto | Nuevo contacto | Fila | Confirmar / masivo | |
| Lead | Nuevo | Fila | Confirmar / masivo | Convertir |
| Promoción | Nueva | Fila | Confirmar | Requiere `product.update` |
| Usuario | Nuevo usuario | Fila | Confirmar | |
| Rol | Nuevo rol | Fila | Confirmar | Permisos |
| Log | — | — | — | Solo lectura |
| Categoría de negocio | Según interfaz | Según interfaz | — | `setting.update` |

---

Documento orientado al frontend de inventario Strova; alinéalo con la API desplegada y los permisos reales.
