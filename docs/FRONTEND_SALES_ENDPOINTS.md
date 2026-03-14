# Cómo manejar ventas desde el front (endpoints)

**Importante:** En esta app **no hay pago**. El usuario arma un **carrito** en la app, esa **orden se registra** en la app, y la forma de cerrar el trato es un **enlace a WhatsApp** del dueño de la organización/ubicación con un mensaje que lista los productos que se quieren. El cobro y la entrega se acuerdan por WhatsApp.

Flujo resumido: **usuario** elige **ubicación** → ve **catálogo** → arma **carrito** → (opcional) **registra la orden** en la app → el front muestra un **link a WhatsApp** del dueño de esa ubicación con un mensaje con todos los productos. El dueño recibe el mensaje, confirma y luego un admin puede confirmar la venta en la app para descontar stock.

---

## 1. Catálogo y ubicaciones (público)

Base: `GET /api/public/...` — no requieren login.

### 1.1 Listar ubicaciones (negocios)

Para que el usuario elija **desde qué negocio/ubicación** quiere comprar. Cada ubicación puede tener un **WhatsApp de contacto** para el enlace “Enviar pedido por WhatsApp”.

```
GET /api/public/locations
```

**Respuesta (200):** `result` es un array de:

```json
{
  "id": 1,
  "name": "Tienda Centro",
  "description": "Sucursal centro",
  "organizationId": 1,
  "organizationName": "Mi Empresa",
  "whatsAppContact": "5215512345678"
}
```

- **`locationId`** = `id` de la ubicación (para catálogo y para crear la orden).
- **`whatsAppContact`**: teléfono para WhatsApp (código de país sin `+`). Ej: `5215512345678` (México). Si viene, el front arma el enlace: `https://wa.me/{whatsAppContact}?text={mensajeCodificado}`. Si no viene, no mostrar botón “Enviar por WhatsApp” o pedir al admin que configure el número en la ubicación.

### 1.2 Catálogo de productos por ubicación

Productos que se pueden vender en esa ubicación, con precio y stock. El usuario usa esto para armar el carrito.

```
GET /api/public/catalog?locationId=1
```

**Query:** `locationId` (número) — obligatorio.

**Respuesta (200):** `result` es un array de:

```json
{
  "id": 10,
  "code": "PROD-001",
  "name": "Producto A",
  "description": "...",
  "imagenUrl": "https://...",
  "precio": 99.50,
  "categoryId": 2,
  "categoryName": "Categoría X",
  "categoryColor": "#abc",
  "stockAtLocation": 25
}
```

- Usa `id` como **productId** al armar los ítems de la venta.
- Usa `precio` como precio unitario (o deja que la API use el del producto si no envías otro).
- Valida contra `stockAtLocation` que no pidas más de lo disponible.

---

## 2. Enlace WhatsApp con el pedido

Cuando el usuario tiene el carrito listo (ubicación + ítems con producto, cantidad, precio), el front debe:

1. **Opcional:** registrar la orden en la app (`POST /api/sale-order` con login) para guardarla como borrador.
2. **Mostrar el enlace a WhatsApp** del dueño de la **ubicación** elegida, con un mensaje que liste los productos.

**Formato del enlace:**  
`https://wa.me/{whatsAppContact}?text={textoCodificado}`  

El número `whatsAppContact` sale de `GET /api/public/locations` (campo `whatsAppContact` de la ubicación elegida). Debe ser número con código de país **sin** `+`, por ejemplo: `5215512345678`.

**Ejemplo de mensaje (para codificar en UTF-8 y poner en `text=`):**

```
Hola, quiero solicitar:

- Producto A x 2 — $99.50 c/u
- Producto B x 1 — $150.00 c/u

Total: $349.00
Ubicación: Tienda Centro
```

El front arma este texto a partir del carrito (nombres, cantidades, precios) y de la ubicación elegida. Si la orden se registró en la app, puede añadir algo como “Ref. orden #5” en el mensaje.

---

## 3. Órdenes de venta en la API (con login)

Para **registrar** la orden en la app y/o para que un **admin** liste, confirme o cancele ventas.

Base: `POST/GET/PUT ... /api/sale-order/...` — requieren `Authorization: Bearer <token>`.

### 3.1 Crear orden de venta (registrar venta)

```
POST /api/sale-order
Content-Type: application/json
```

**Body:**

```json
{
  "locationId": 1,
  "contactId": null,
  "notes": "Cliente preferencial",
  "discountAmount": 0,
  "items": [
    {
      "productId": 10,
      "quantity": 2,
      "unitPrice": 99.50,
      "discount": 0
    },
    {
      "productId": 11,
      "quantity": 1,
      "unitPrice": null,
      "discount": 0
    }
  ]
}
```

- `locationId`: la ubicación elegida (misma del catálogo).
- `contactId`: opcional (cliente asociado).
- `items`: al menos uno. `unitPrice` opcional; si no va, se usa el precio actual del producto.
- `quantity` debe ser &gt; 0.

**Respuesta (201):** `result` es la orden creada (misma estructura que en 3.3). La orden nace en estado **Draft**.

### 3.2 Listar órdenes de venta

```
GET /api/sale-order?page=1&perPage=10&status=&sortOrder=
```

**Query (todos opcionales):**

- `page`, `perPage`: paginación.
- `status`: filtrar por estado (ej. `Draft`, `Confirmed`, `Cancelled`).
- `sortOrder`: orden (depende de la API, ej. `createdAtDesc`).

**Respuesta (200):** lista paginada; cada elemento como en 3.3.

### 3.3 Obtener una orden por ID

```
GET /api/sale-order/id?id=5
```

**Respuesta (200):** `result`:

```json
{
  "id": 5,
  "folio": "SO-001",
  "organizationId": 1,
  "locationId": 1,
  "locationName": "Tienda Centro",
  "contactId": null,
  "contactName": null,
  "status": "Draft",
  "notes": "...",
  "subtotal": 299.00,
  "discountAmount": 0,
  "total": 299.00,
  "userId": 1,
  "createdAt": "...",
  "modifiedAt": "...",
  "items": [
    {
      "id": 1,
      "saleOrderId": 5,
      "productId": 10,
      "productName": "Producto A",
      "quantity": 2,
      "unitPrice": 99.50,
      "unitCost": 50.00,
      "discount": 0,
      "lineTotal": 199.00,
      "grossMargin": 99.00
    }
  ]
}
```

### 3.4 Actualizar orden (solo Draft)

```
PUT /api/sale-order?id=5
Content-Type: application/json
```

**Body:** solo campos a cambiar (orden en Draft):

```json
{
  "contactId": 3,
  "notes": "Nueva nota",
  "discountAmount": 10.00
}
```

- No se editan ítems por aquí; solo contacto, notas y descuento global.
- **Respuesta (204)** No Content.

### 3.5 Confirmar orden (Draft → Confirmada, descuenta stock)

```
POST /api/sale-order/5/confirm
```

Sin body. **Respuesta (200):** `result` = orden actualizada (con `status` Confirmada).

### 3.6 Cancelar orden

```
POST /api/sale-order/5/cancel
```

Sin body. **Respuesta (200):** `result` = orden actualizada (Cancelada).

### 3.7 Estadísticas de ventas (reportes)

```
GET /api/sale-order/stats?days=30
```

**Query:** `days` opcional.  
**Respuesta (200):** `result` con estadísticas (totales, por período, etc., según la API).

---

## 4. Flujo recomendado en el front (usuario arma carrito → WhatsApp)

1. **Pantalla inicial: elegir ubicación**
   - `GET /api/public/locations` → mostrar negocios. Guardar `locationId` y `whatsAppContact` de la elegida.

2. **Catálogo / carrito**
   - `GET /api/public/catalog?locationId=<id>` → listar productos con precio y `stockAtLocation`.
   - El usuario agrega productos al carrito (en memoria: productId, nombre, cantidad, precio).

3. **Resumen y “Enviar por WhatsApp”**
   - Mostrar resumen del carrito (ítems, total).
   - Si hay `whatsAppContact`, mostrar botón que abre:  
     `https://wa.me/{whatsAppContact}?text={encodeURIComponent(mensajeConProductosYTotal)}`.
   - Opcional (si el usuario tiene cuenta): antes de eso llamar a `POST /api/sale-order` para registrar la orden en la app y poner en el mensaje “Ref. orden #X”.

4. **Admin (por otro flujo)**
   - Cuando el dueño confirma por WhatsApp, un admin puede crear/confirmar la venta en la app (`POST /api/sale-order` + `POST /api/sale-order/<id>/confirm`) para descontar stock y dejar registro.

---

## 5. Resumen de endpoints

| Acción | Método | URL | Auth |
|--------|--------|-----|------|
| Listar ubicaciones | GET | `/api/public/locations` | No |
| Catálogo por ubicación | GET | `/api/public/catalog?locationId=<id>` | No |
| Crear orden | POST | `/api/sale-order` | Sí |
| Listar órdenes | GET | `/api/sale-order?page=&perPage=&status=&sortOrder=` | Sí |
| Ver orden | GET | `/api/sale-order/id?id=<id>` | Sí |
| Actualizar orden (draft) | PUT | `/api/sale-order?id=<id>` | Sí |
| Confirmar orden | POST | `/api/sale-order/<id>/confirm` | Sí |
| Cancelar orden | POST | `/api/sale-order/<id>/cancel` | Sí |
| Stats ventas | GET | `/api/sale-order/stats?days=` | Sí |

Con esto el front puede manejar todo el flujo de ventas usando solo estos endpoints.
