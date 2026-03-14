# Módulo de Catálogo Público — Prompt para generación de vistas

## Contexto general

Esta es una API REST de sistema de control de inventario y ventas. Se necesita construir un módulo de **catálogo público de ventas** (punto de venta / menú) que funciona **sin autenticación**. El usuario entra, elige en qué local/negocio está y ve los productos disponibles para comprar.

---

## Flujo de navegación

```
/catalog                   → Pantalla de selección de local (locations)
/catalog/:locationId        → Catálogo de productos de ese local
```

---

## Pantalla 1: Selección de local — `/catalog`

### Qué hace
Muestra una lista de todos los negocios/ubicaciones disponibles. El usuario toca o hace clic en uno para ver su catálogo.

### Endpoint
```
GET /api/public/locations
Authorization: No requerida
```

### Respuesta del endpoint
```json
{
  "data": [
    {
      "id": 1,
      "name": "Sucursal Centro",
      "description": "Abierto de 9am a 9pm",
      "organizationId": 1,
      "organizationName": "Café Aroma"
    },
    {
      "id": 2,
      "name": "Sucursal Norte",
      "description": null,
      "organizationId": 1,
      "organizationName": "Café Aroma"
    }
  ]
}
```

### Diseño esperado de la UI
- Header con el nombre de la app / logo
- Título: **"¿En qué local estás?"** o similar
- Lista de tarjetas (cards), una por cada location
- Cada card muestra:
  - Nombre del local (`name`) — texto grande y prominente
  - Nombre del negocio/organización (`organizationName`) — texto secundario
  - Descripción (`description`) — si existe, texto pequeño en gris
  - Un ícono de tienda o ubicación decorativo
- Al hacer clic en una card → navegar a `/catalog/:locationId`
- Si la lista está vacía → mostrar estado vacío: *"No hay locales disponibles por el momento"*
- Si está cargando → mostrar skeleton loaders (3-4 tarjetas en placeholder)
- Si hay error de red → mostrar mensaje de error con botón de reintentar

### Consideraciones UX
- Diseño limpio, accesible y responsivo (mobile-first)
- Las cards deben tener hover/tap visual feedback
- No hay buscador en esta pantalla (la lista de locales se espera corta)

---

## Pantalla 2: Catálogo del local — `/catalog/:locationId`

### Qué hace
Muestra todos los productos con `isForSale = true` disponibles en el local seleccionado, con su precio y stock actual en ese local.

### Endpoint
```
GET /api/public/catalog?locationId={locationId}
Authorization: No requerida
```

### Respuesta del endpoint
```json
{
  "data": [
    {
      "id": 5,
      "code": "CAF-001",
      "name": "Café Americano",
      "description": "Espresso con agua caliente",
      "imagenUrl": "https://cdn.example.com/cafe.jpg",
      "precio": 35.00,
      "categoryId": 2,
      "categoryName": "Bebidas Calientes",
      "categoryColor": "#6366f1",
      "stockAtLocation": 100
    },
    {
      "id": 6,
      "code": "CAF-002",
      "name": "Capuchino",
      "description": null,
      "imagenUrl": null,
      "precio": 45.00,
      "categoryId": 2,
      "categoryName": "Bebidas Calientes",
      "categoryColor": "#6366f1",
      "stockAtLocation": 0
    }
  ]
}
```

> ⚠️ El campo `stockAtLocation` es el stock disponible en ese local específico.
> Cuando es `0` el producto debe verse como **"Agotado"** y no ser seleccionable.

### Diseño esperado de la UI

#### Estructura de la página
```
[Header: nombre del local + botón "Cambiar local"]
[Barra de búsqueda por nombre]
[Filtro horizontal de categorías (chips/tabs)]
[Grid de tarjetas de producto]
```

#### Header
- Muestra el nombre del local activo (obtenido de la pantalla anterior o re-fetched)
- Botón `← Cambiar local` que regresa a `/catalog`

#### Barra de búsqueda
- Input de texto simple para filtrar productos por nombre en el cliente (sin llamada extra a la API)
- Placeholder: *"Buscar producto..."*

#### Filtro de categorías
- Chips/tabs horizontales con scroll
- Primera opción: **"Todos"** (seleccionada por defecto)
- Luego una opción por cada categoría única presente en los productos
- El color del chip puede usar `categoryColor` del producto
- Al seleccionar una categoría, el grid se filtra en el cliente (sin llamada extra)

#### Grid de tarjetas de producto
- Grid responsivo: 2 columnas en móvil, 3-4 en tablet/desktop
- **Cada card muestra:**
  - Imagen del producto (`imagenUrl`) — si es null mostrar placeholder con ícono de producto
  - Nombre (`name`)
  - Categoría (`categoryName`) — badge con el color (`categoryColor`)
  - Precio (`precio`) — formateado como moneda, ej: `$35.00`
  - Estado de stock:
    - `stockAtLocation > 0` → mostrar badge verde **"Disponible"** (no mostrar número de stock al cliente)
    - `stockAtLocation == 0` → overlay gris + badge rojo **"Agotado"** + card no clickeable / opacidad reducida

#### Estado vacío
- Si no hay productos en el catálogo: *"Este local no tiene productos disponibles por el momento"*
- Si el filtro de búsqueda no da resultados: *"No se encontraron productos"*

#### Carga
- Skeleton loaders mientras llega la respuesta (grid de 6-8 cards en placeholder)

#### Error de red
- Mensaje de error centrado con botón de reintentar

---

## Comportamiento de filtros (lado cliente)

Todos los filtros operan sobre los datos ya cargados, **sin llamadas adicionales a la API**:

```
datos_completos
  → filtrar por categoría seleccionada (si no es "Todos")
  → filtrar por texto de búsqueda (nombre contains, case-insensitive)
  → mostrar resultado en el grid
```

---

## Tipos TypeScript sugeridos

```typescript
interface PublicLocation {
  id: number;
  name: string;
  description: string | null;
  organizationId: number;
  organizationName: string;
}

interface PublicCatalogItem {
  id: number;
  code: string;
  name: string;
  description: string | null;
  imagenUrl: string | null;
  precio: number;
  categoryId: number;
  categoryName: string | null;
  categoryColor: string | null;
  stockAtLocation: number;
}
```

---

## Endpoints completos de referencia

| Método | URL | Auth | Descripción |
|--------|-----|------|-------------|
| `GET` | `/api/public/locations` | ❌ No requerida | Lista de locales/negocios |
| `GET` | `/api/public/catalog?locationId={id}` | ❌ No requerida | Catálogo del local |

---

## Notas importantes para el desarrollador front

1. **Sin token** — estos endpoints son 100% públicos, no enviar `Authorization` header
2. **El costo no se expone** — la API solo devuelve `precio` (precio de venta), nunca el costo interno del negocio
3. **Stock** — `stockAtLocation` es el stock real en ESA ubicación, no el stock total del producto en todos los locales
4. **Agotado** — productos con `stockAtLocation == 0` deben verse como no disponibles pero seguir apareciendo en el catálogo (no ocultarlos)
5. **Filtros en cliente** — búsqueda y filtro por categoría se hacen sobre los datos en memoria, no hay endpoint de búsqueda separado
6. **locationId en URL** — guardar el `locationId` en la URL (`/catalog/1`) para que el link sea compartible y funcione el botón de atrás del browser
