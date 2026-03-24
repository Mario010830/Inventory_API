# Cambios en el front por actualizaciones de la API

Resumen de lo que el front debe hacer para alinearse con los cambios recientes en la API.

---

## 1. Actualizar usuario (incluido el rol)

**Antes:** Podía usarse `PUT api/user` con `id` en query o en body.  
**Ahora:** La API espera **`PUT api/user/{id}`** con el `id` en la **URL**.

- **URL:** `PUT /api/user/{id}` (ej: `PUT /api/user/5`).
- **Body (JSON):** Incluir siempre `roleId` cuando se quiera cambiar el rol:
  - `roleId: number` → asigna ese rol.
  - `roleId: 0` o no enviar → mantiene el rol actual (0 se interpreta como “sin rol” si se envía explícito).
- El resto del body igual: `fullName`, `email`, `phone`, `locationId`, `organizationId`, etc.

**Ejemplo body para editar usuario (incluyendo rol y fecha de nacimiento):**
```json
{
  "fullName": "Juan Pérez",
  "email": "juan@example.com",
  "phone": "+34 600 000 000",
  "birthDate": "1990-05-15",
  "roleId": 2,
  "locationId": 1,
  "organizationId": 1
}
```
Los campos opcionales que no se envían se mantienen; si se envían (p. ej. `birthDate`, `roleId`), se actualizan.

---

## 2. Permisos y listas/desplegables (sin cambio de URLs)

La API relajó permisos para que, si el usuario puede **crear/editar** algo, también pueda **listar** los datos de referencia (categorías, ubicaciones, productos, proveedores).

- **No hace falta cambiar URLs** de los GET de categorías, ubicaciones, productos o proveedores.
- Si antes el front recibía **403** al cargar categorías en el formulario de producto (con un usuario que solo tenía “crear producto”), ahora debería poder llamar a los mismos endpoints sin cambio en el front.
- Si tenías workarounds (ocultar formularios, mensajes “sin permiso”), se pueden revisar; en muchos casos ya no serán necesarios.

---

## 3. Formulario de crear movimiento de inventario (ubicación fija)

Cuando el usuario tiene **ubicación asignada**, el movimiento debe crearse siempre en esa ubicación y el campo **no debe ser editable**.

### Nuevo endpoint: contexto del formulario

- **Método y ruta:** `GET /api/inventory-movement/form-context`
- **Permiso:** `InventoryMovementCreate`
- **Respuesta (ejemplo):**
```json
{
  "locationId": 3,
  "locationName": "Sucursal Centro",
  "isLocationLocked": true
}
```
- Si el usuario **no** tiene ubicación: `locationId` y `locationName` pueden ser `null` y `isLocationLocked: false`.

### Flujo recomendado en el front

1. **Al abrir el formulario de “Crear movimiento”:**
   - Llamar a `GET /api/inventory-movement/form-context`.

2. **Si `isLocationLocked === true`:**
   - Mostrar la ubicación como texto (o campo deshabilitado) con `locationName`.
   - No mostrar desplegable de ubicaciones (o mostrarlo deshabilitado con ese valor).
   - Al enviar el POST, incluir en el body `locationId` con el valor recibido (opcional: el backend usa la del usuario si tiene ubicación).

3. **Si `isLocationLocked === false`:**
   - Mostrar el desplegable de ubicaciones como hasta ahora (ej. `GET /api/location`).
   - El usuario elige la ubicación y se envía en el body del POST.

4. **POST crear movimiento:**  
   Sigue siendo `POST /api/inventory-movement` con el mismo body. Si el usuario tiene ubicación, la API ignora el `locationId` del body y usa la del usuario.

---

## 4. Catálogo público por ubicación (tipo de producto)

La API ya devuelve correctamente el **tipo** del producto (`"inventariable"` o `"elaborado"`) en la respuesta del catálogo por ubicación. No es obligatorio cambiar nada en el front; si ya usabas el campo `tipo` de cada ítem, debería verse bien. Si antes lo hardcodeabas o no lo mostrabas, puedes usar ahora el valor que viene en la respuesta.

---

## Resumen rápido

| Área              | Acción en el front |
|-------------------|--------------------|
| Editar usuario    | Usar `PUT /api/user/{id}` y enviar `roleId` en el body cuando se cambie el rol. |
| Listas/permisos   | Sin cambios de URLs; los 403 en desplegables pueden desaparecer. |
| Crear movimiento | Llamar `GET /api/inventory-movement/form-context` al abrir el formulario; si `isLocationLocked` es true, fijar y bloquear la ubicación. |
| Catálogo público  | Opcional: usar el campo `tipo` que ya viene correcto en la API. |
