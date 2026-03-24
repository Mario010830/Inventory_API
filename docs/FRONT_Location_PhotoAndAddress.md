# Ubicación: foto y dirección por partes (Front)

## Resumen

La API de ubicaciones ahora soporta:
- **Foto:** campo `photoUrl` (URL de imagen).
- **Dirección por partes:** `province`, `municipality`, `street` (todos opcionales).

## Base de datos

Asegurar que la tabla `Location` tenga las columnas (si no usáis migraciones de EF, ejecutar el script en `APICore.Data/Scripts/Location_AddPhotoAndAddressColumns.sql`):

- `PhotoUrl` (nvarchar max, nullable)
- `Province` (nvarchar, nullable)
- `Municipality` (nvarchar, nullable)
- `Street` (nvarchar, nullable)

## API

### Crear ubicación – POST `/api/location`

Body (JSON). Los nuevos campos son opcionales:

```json
{
  "organizationId": 1,
  "name": "Sucursal Centro",
  "code": "SUC-01",
  "description": "...",
  "whatsAppContact": "+34...",
  "photoUrl": "https://...",
  "province": "Madrid",
  "municipality": "Madrid",
  "street": "Calle Mayor 1"
}
```

### Actualizar ubicación – PUT `/api/location/{id}`

Mismo body; incluir `photoUrl`, `province`, `municipality`, `street` solo si queréis actualizarlos.

### Respuestas de ubicación

En **GET** por id, listado de ubicaciones y en el **catálogo público**, cada ubicación incluye:

- `photoUrl` (string | null)
- `province` (string | null)
- `municipality` (string | null)
- `street` (string | null)

## Subir foto de ubicación

**Opción recomendada:** usar el endpoint de la API:

- **POST** `/api/location/image`
- **Content-Type:** `multipart/form-data`
- **Campo del formulario:** `file` (archivo de imagen: JPEG, PNG, GIF o WebP; máx. 5 MB)
- **Respuesta 200:** `{ "data": { "photoUrl": "https://..." } }`

El front debe:

1. Llamar a **POST** `/api/location/image` con el archivo.
2. Tomar `data.photoUrl` de la respuesta.
3. Enviar ese valor en `photoUrl` al **crear** (POST `/api/location`) o **editar** (PUT `/api/location/{id}`) la ubicación.

**Opción alternativa:** si ya tenéis un flujo genérico de subida de imágenes (por ejemplo **POST** `/api/product/image`), podéis usar la URL devuelta como `photoUrl` en crear/editar ubicación.

## Qué debe hacer el front

1. **Formulario crear/editar ubicación**
   - Añadir campos opcionales: **Provincia**, **Municipality**, **Calle** (y si aplica, selector o input de **Foto**).
   - Al guardar, enviar en el body: `photoUrl`, `province`, `municipality`, `street`.

2. **Subida de foto**
   - En crear/editar ubicación: botón “Subir foto” que llame a **POST** `/api/location/image` con el archivo, reciba `photoUrl` y lo asigne al modelo (y lo envíe en el body al guardar).

3. **Visualización**
   - En listados y detalle de ubicación: mostrar imagen desde `photoUrl` (si existe) y dirección formateada con `province`, `municipality`, `street` (por ejemplo: “Calle, Municipio, Provincia” o el orden que prefieras).

4. **Catálogo público**
   - Si el catálogo público muestra ubicaciones, usar los mismos campos `photoUrl`, `province`, `municipality`, `street` de la respuesta para foto y dirección.
