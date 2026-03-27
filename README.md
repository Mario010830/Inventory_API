---

# APICore

> API backend multi-tenant para inventario, ventas, suscripciones y notificaciones push web.

## Tabla de Contenidos
- [Visión General](#visión-general)
- [Modelos de Datos](#modelos-de-datos)
- [API / Rutas](#api--rutas)
- [Flujos Principales](#flujos-principales)

## Visión General
- **Qué problema resuelve**: centraliza operación de comercios/sucursales con control de inventario, ventas, catálogo público, CRM básico, suscripciones por plan y promociones con push notifications.
- **Arquitectura general**: monolito en capas (API + Services + Data + Common), con persistencia PostgreSQL vía EF Core y filtros globales de multi-tenant.
- **Stack tecnológico completo con versiones**:
  - .NET `9.0` (`net9.0`)
  - ASP.NET Core + Swagger (`Swashbuckle.AspNetCore 9.0.6`)
  - Entity Framework Core + PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4`)
  - JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer 9.x`)
  - AutoMapper (`15.1.0`)
  - Serilog (`4.3.0`, `Serilog.AspNetCore 9.0.0`, `Serilog.Sinks.File 7.0.0`)
  - Web Push (`WebPush 1.0.12`)
  - SendGrid (`9.28.1`)
  - Reportes PDF (`QuestPDF 2024.12.3`)
  - Testing: xUnit `2.7.0`, Moq, EF InMemory/SQLite
- **Estructura de solución**:
  - `APICore.API`: host HTTP, controladores, middlewares, DI y configuración
  - `APICore.Services`: lógica de negocio
  - `APICore.Data`: entidades, `CoreDbContext`, migraciones, UoW/Repository
  - `APICore.Common`: DTOs, constantes, helpers
  - `APICore.Test`: pruebas de integración
- **Configuración y ejecución**:
  - Variables relevantes: `ConnectionStrings__ApiConnection`, `BearerTokens__Key`, `BearerTokens__Issuer`, `BearerTokens__Audience`, `PORT`, `PushNotifications__*`, `LocalStorage__BaseUrl`
  - Build: `dotnet build APICore.sln -c Release`
  - Publish API: `dotnet publish APICore.API/APICore.API.csproj -c Release -o ./publish`
  - Run API (dev): `dotnet run --project APICore.API/APICore.API.csproj`

# Modelos de Datos

> Nota: se documentan las entidades principales del dominio. Tipos/nullable provienen de `APICore.Data/Entities` y migraciones.

## Organization
- **Propósito**: tenant principal de negocio.

| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| Id | int | Sí | PK |
| Name | string | Sí | Nombre organización |
| Code | string | Sí | Código |
| Description | string? | No | Descripción |
| IsActive | bool | Sí | Estado activo/inactivo |
| SubscriptionId | int? | No | Suscripción actual |

- **Relaciones**: 1:N con `Locations`, `Users`, `Products`, `Suppliers`, `Roles`, `Currencies`; 1:N con `Subscriptions`.

## Location
- **Propósito**: sucursal/localización operativa.

| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| Id | int | Sí | PK |
| OrganizationId | int | Sí | FK organización |
| Name | string | Sí | Nombre tienda/sucursal |
| Code | string | Sí | Código interno |
| Description | string? | No | Descripción |
| WhatsAppContact | string? | No | Teléfono WhatsApp |
| PhotoUrl | string? | No | Imagen tienda |
| Province/Municipality/Street | string? | No | Dirección |
| BusinessHoursJson | string? | No | Horario serializado |
| Latitude/Longitude | double? | No | Geolocalización |
| BusinessCategoryId | int? | No | Rubro |

- **Relaciones**: N:1 `Organization`; 1:N `Users`, `Inventories`, `InventoryMovements`.

## User
- **Propósito**: cuenta autenticable del sistema.

| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| Id | int | Sí | PK |
| FullName | string | Sí | Nombre completo |
| Email | string | Sí | Login principal |
| Phone | string | Sí | Teléfono |
| Password | string | Sí | Hash password |
| GoogleId | string? | No | Vinculación Google |
| Status | enum | Sí | Estado usuario |
| OrganizationId | int? | No | Organización |
| LocationId | int? | No | Ubicación |
| RoleId | int? | No | Rol |

- **Relaciones**: N:1 `Organization`, `Location`, `Role`; 1:N `UserTokens`.

## Role / Permission / RolePermission
- **Propósito**: autorización basada en permisos.

| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| Role.Id | int | Sí | PK |
| Role.OrganizationId | int? | No | Rol global o por org |
| Role.Name | string | Sí | Nombre rol |
| Role.IsSystem | bool | Sí | Rol del sistema |
| Permission.Id | int | Sí | PK |
| Permission.Code | string | Sí | Código único permiso |
| RolePermission.RoleId | int | Sí | FK rol |
| RolePermission.PermissionId | int | Sí | FK permiso |

- **Relaciones**: N:M `Role` ↔ `Permission`.

## ProductCategory
- **Propósito**: clasificación de productos por organización.

| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| Id | int | Sí | PK |
| OrganizationId | int | Sí | FK organización |
| Name | string | Sí | Nombre categoría |
| Description | string? | No | Descripción |
| Color | string | Sí | Color UI |
| Icon | string | Sí | Ícono UI |

- **Relaciones**: 1:N `Products`.

## Product / ProductImage / Tag / ProductTag
- **Propósito**: catálogo y media de productos.

| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| Product.Id | int | Sí | PK |
| Product.OrganizationId | int | Sí | FK organización |
| Product.Code | string | Sí | Código producto |
| Product.Name | string | Sí | Nombre |
| Product.CategoryId | int? | No | FK categoría |
| Product.Precio | decimal | Sí | Precio venta |
| Product.Costo | decimal | Sí | Costo |
| Product.ImagenUrl | string | Sí* | URL imagen principal |
| Product.IsAvailable | bool | Sí | Disponible |
| Product.IsForSale | bool | Sí | Visible/venible |
| Product.Tipo | enum string | Sí | `inventariable` / `elaborado` |
| ProductImage.ImageUrl | string | Sí | URL imagen |
| ProductImage.SortOrder | int | Sí | Orden |
| ProductImage.IsMain | bool | Sí | Imagen principal |
| Tag.Name/Slug | string | Sí | Etiqueta global |

- **Relaciones**: `Product` 1:N `ProductImage`, N:M con `Tag` vía `ProductTag`, 1:N `ProductLocationOffer`.

## ProductLocationOffer (disponibilidad por tienda)
- **Propósito**: para productos `elaborado`, indica en qué tiendas (`Location`) se ofrece el producto sin depender de filas de inventario.
- **Tabla**: `ProductLocationOffers` con `ProductId`, `LocationId`, `OrganizationId`; índice único `(ProductId, LocationId)`.

### Contrato API y front (admin / importación)
- **JSON** (camelCase típico en ASP.NET): `offerLocationIds` en `CreateProductRequest` y `UpdateProductRequest`; `ProductResponse` devuelve la misma lista (ids de ubicación, ordenadas).
- **Solo `tipo: "elaborado"`** usa la tabla. Para `inventariable` no se guardan ofertas (y al cambiar de elaborado a inventariable se eliminan).
- **PUT producto**: si `offerLocationIds` **no** se envía (`null`/omitido), las ofertas existentes no se modifican. Si se envía **`[]`**, se eliminan todas las tiendas asignadas.
- Los ids deben ser ubicaciones de la organización del usuario; si no, respuesta de negocio con código **`400030`**.
- **Catálogo público** (`GET /api/public/catalog`): un elaborado solo se lista en una tienda si tiene fila de oferta y `IsForSale`.
- **Ventas** (`POST` pedido): un elaborado sin oferta en la `locationId` del pedido devuelve **`400044`** (“Este producto no está ofertado en la tienda seleccionada.”).
- **Importación CSV / plantilla**: puede mapearse una columna con lista de ids separados por coma o un archivo auxiliar de pares producto–ubicación; el cliente debe validar y enviar `offerLocationIds` en el cuerpo del create/update o en lotes dedicados.

## Promotion
- **Propósito**: descuentos por producto.

| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| Id | int | Sí | PK |
| OrganizationId | int | Sí | FK organización |
| ProductId | int | Sí | FK producto |
| Type | enum string | Sí | `percentage` / `fixed` |
| Value | decimal | Sí | Valor descuento |
| StartsAt/EndsAt | DateTime? | No | Ventana de vigencia |
| IsActive | bool | Sí | Estado activo |
| MinQuantity | decimal | Sí | Cantidad mínima |

- **Relaciones**: N:1 `Organization`, N:1 `Product`; 0..N referenciada por `SaleOrderItem.PromotionId`.

## Inventory / InventoryMovement / Supplier
- **Propósito**: stock actual y trazabilidad de movimientos.

| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| Inventory.ProductId | int | Sí | FK producto |
| Inventory.LocationId | int | Sí | FK ubicación |
| Inventory.CurrentStock | decimal | Sí | Stock actual |
| Inventory.MinimumStock | decimal | Sí | Stock mínimo |
| Inventory.UnitOfMeasure | string | Sí | Unidad |
| InventoryMovement.Type | enum string | Sí | Entrada/Salida/Ajuste |
| InventoryMovement.Reason (Cause) | string? | No | Motivo |
| InventoryMovement.Quantity | decimal | Sí | Cantidad movida |
| InventoryMovement.SupplierId | int? | No | Proveedor |
| Supplier.Name | string | Sí | Nombre proveedor |
| Supplier.Phone/Email | string? | No | Contacto |

- **Relaciones**: `Inventory` N:1 `Product`/`Location`; `InventoryMovement` N:1 `Product`/`Location`/`Supplier?`.

## SaleOrder / SaleOrderItem / SaleReturn / SaleReturnItem
- **Propósito**: ventas, detalle de líneas y devoluciones.

| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| SaleOrder.OrganizationId | int | Sí | FK organización |
| SaleOrder.LocationId | int | Sí | FK ubicación |
| SaleOrder.ContactId | int? | No | Contacto cliente |
| SaleOrder.Status | enum string | Sí | Estado orden |
| SaleOrder.Subtotal/Total | decimal | Sí | Importes |
| SaleOrderItem.ProductId | int | Sí | FK producto |
| SaleOrderItem.Quantity | decimal | Sí | Cantidad |
| SaleOrderItem.UnitPrice | decimal | Sí | Precio aplicado |
| SaleOrderItem.OriginalUnitPrice | decimal | Sí | Precio original |
| SaleOrderItem.PromotionId | int? | No | Promo aplicada |
| SaleReturn.SaleOrderId | int | Sí | Orden origen |
| SaleReturn.Status | enum string | Sí | Estado devolución |

- **Relaciones**: `SaleOrder` 1:N `Items` y `Returns`; `SaleReturn` 1:N `SaleReturnItem`.

## Plan / Subscription / SubscriptionRequest
- **Propósito**: control de planes, vigencias y solicitudes de aprobación.

| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| Plan.Name | string | Sí | Nombre plan (`free`, etc.) |
| Plan.MaxProducts/MaxUsers/MaxLocations | int | Sí | Límites |
| Plan.MonthlyPrice/AnnualPrice | decimal | Sí | Precio |
| Subscription.OrganizationId | int | Sí | FK organización |
| Subscription.PlanId | int | Sí | FK plan |
| Subscription.BillingCycle | string | Sí | `monthly` / `annual` |
| Subscription.Status | string | Sí | `active/pending/expired` |
| Subscription.StartDate/EndDate | DateTime | Sí | Vigencia |
| SubscriptionRequest.SubscriptionId | int | Sí | FK suscripción |
| SubscriptionRequest.Type | string | Sí | alta/renovación/cambio |
| SubscriptionRequest.Status | string | Sí | pending/approved/rejected |

- **Relaciones**: `Plan` 1:N `Subscriptions`; `Subscription` 1:N `SubscriptionRequest`.

## WebPushSubscription
- **Propósito**: dispositivos/sesiones suscritas a push web.

| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| Id | int | Sí | PK |
| Endpoint | string | Sí | Endpoint navegador (único) |
| P256DH | string | Sí | Clave pública cliente |
| Auth | string | Sí | Auth secret |
| ExpirationTime | long? | No | Expiración endpoint |
| LocationId | int | Sí | Segmentación por tienda |
| OrganizationId | int | Sí | Organización |
| IsActive | bool | Sí | Estado suscripción |

- **Relaciones**: N:1 `Location`, N:1 `Organization`.

# API / Rutas

> Base principal: `api/*`.  
> Auth/permisos por `[Authorize]` y `[RequirePermission(...)]`.  
> Respuestas envueltas principalmente con `ApiOkResponse`, `ApiOkPaginatedResponse`, `ApiCreatedResponse`.

## Formato de respuesta (ejemplos reales)

```json
{
  "statusCode": 200,
  "message": "OK",
  "result": {}
}
```

```json
{
  "statusCode": 200,
  "message": "OK",
  "result": [],
  "pagination": {
    "totalItems": 120,
    "page": 1,
    "perPage": 10,
    "totalPages": 12
  }
}
```

## Endpoints

### Account (`/api/account`)

| Método + Path | Descripción | Request (params/body) | Response exitosa (ejemplo) | Posibles errores |
|---|---|---|---|---|
| `POST /api/account/register` | Registro básico | Body `SignUpRequest` | `201 Created` (`true`) | `400`, `409` |
| `POST /api/account/register-with-organization` | Registro + creación org | Body `RegisterWithOrganizationRequest` | `201 Created` (`true`) | `400`, `409` |
| `POST /api/account/login` | Login email/password | Body `LoginRequest` | `{"result":{"token":"...","refreshToken":"..."}}` | `401`, `400` |
| `POST /api/account/login-google` | Login Google | Body `GoogleLoginRequest` | `{"result":{"token":"...","refreshToken":"..."}}` | `401`, `400` |
| `POST /api/account/logout` | Cierra sesión actual | Header JWT | `200 OK` | `401` |
| `POST /api/account/global-logout` | Cierra todas las sesiones | Header JWT | `200 OK` | `401` |
| `POST /api/account/refresh-token` | Renueva token | Body `RefreshTokenRequest` | `200 OK` | `401`, `400` |
| `POST /api/account/change-password` | Cambio de contraseña | Body `ChangePasswordRequest` | `200 OK` | `401`, `400` |
| `POST /api/account/update-profile` | Actualiza perfil | Body `UpdateProfileRequest` | `{"result":{"id":1,"fullName":"..."}}` | `401`, `400` |
| `POST /api/account/validate-token` | Valida JWT/refresh | Body `ValidateTokenRequest` | `{"result":{"isValid":true}}` | `401` |
| `POST /api/account/change-status` | Cambia status usuario | Body `ChangeAccountStatusRequest` | `200 OK` | `401`, `403`, `404` |
| `POST /api/account/forgot-password?email=` | Recuperación password | Query `email` | `200 OK` | `400`, `404` |

### BusinessCategory (`/api/business-category`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `GET /api/business-category` | Lista rubros | - | `{"result":[{"id":1,"name":"..." }]}` | `500` |
| `GET /api/business-category/{id}` | Detalle rubro | Path `id` | `{"result":{"id":1}}` | `404` |
| `POST /api/business-category` | Crear rubro | Body `CreateBusinessCategoryRequest` | `201 {"result":{"id":1}}` | `401`, `403`, `400` |
| `PUT /api/business-category/{id}` | Actualizar rubro | Path `id`, Body `UpdateBusinessCategoryRequest` | `200 {"result":true}` | `401`, `403`, `404` |
| `DELETE /api/business-category/{id}` | Eliminar rubro | Path `id` | `204 No Content` | `401`, `403`, `404` |

### Contact (`/api/contact`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `POST /api/contact` | Crear contacto | Body `CreateContactRequest` | `201 {"result":{"id":1}}` | `401`, `403`, `400` |
| `GET /api/contact` | Listar contactos | Query `page,perPage,sortOrder` | `{"result":[...],"pagination":{...}}` | `401`, `403` |
| `GET /api/contact/id?id=` | Obtener contacto por id | Query `id` | `{"result":{"id":1}}` | `404` |
| `PUT /api/contact?id=` | Actualizar contacto | Query `id`, Body `UpdateContactRequest` | `204` | `404`, `400` |
| `DELETE /api/contact?id=` | Eliminar contacto | Query `id` | `204` | `404` |

### Currency (`/api/currency`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `GET /api/currency` | Lista monedas | - | `{"result":[{"code":"USD"}]}` | `401`, `403` |
| `GET /api/currency/{id}` | Moneda por id | Path `id` | `{"result":{"id":1}}` | `404` |
| `POST /api/currency` | Crear moneda | Body `CreateCurrencyRequest` | `201 {"result":{"id":1}}` | `400`, `409` |
| `PUT /api/currency/{id}` | Actualizar moneda | Path `id`, Body `UpdateCurrencyRequest` | `200 {"result":true}` | `404`, `400` |
| `DELETE /api/currency/{id}` | Eliminar moneda | Path `id` | `204` | `404`, `409` |
| `PUT /api/currency/default` | Define moneda base | Body `SetDefaultCurrencyRequest` | `200 {"result":true}` | `400`, `404` |

### Dashboard (`/api/dashboard`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `GET /summary` | Resumen KPIs | Query `from,to` | `{"result":{"sales":...}}` | `401` |
| `GET /inventory-flow` | Flujo inventario | Query `days,from,to` | `{"result":[...]}` | `401` |
| `GET /category-distribution` | Distribución por categoría | - | `{"result":[...]}` | `401` |
| `GET /inventory-value-evolution` | Evolución valor inventario | Query `months,from,to` | `{"result":[...]}` | `401` |
| `GET /stock-status` | Estado stock | - | `{"result":{"low":...}}` | `401` |
| `GET /list-top-movements` | Top movimientos | Query `days,limit` | `{"result":[...]}` | `401` |
| `GET /list-low-stock` | Productos bajo stock | Query `limit` | `{"result":[...]}` | `401` |
| `GET /list-latest-movements` | Últimos movimientos | Query `limit` | `{"result":[...]}` | `401` |
| `GET /list-value-by-location` | Valor por ubicación | Query `limit` | `{"result":[...]}` | `401` |
| `GET /list-recent-products` | Productos recientes | Query `limit,days` | `{"result":[...]}` | `401` |
| `GET /entries-vs-exits` | Entradas vs salidas | Query `days,from,to` | `{"result":[...]}` | `401` |
| `GET /low-stock-alerts-by-day` | Alertas diarias de stock | Query `days` | `{"result":[...]}` | `401` |

### Diagnostics / Health

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `GET /health-check` | Health de servicios (controller) | - | `200 [{"serviceStatus":200}]` | `503` |
| `GET /health` | Healthcheck pipeline | - | `200/503` | - |

### Inventory (`/api/inventory`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `POST /api/inventory` | Crear inventario | Body `CreateInventoryRequest` | `201 {"result":{"id":1}}` | `400`, `401`, `403` |
| `GET /api/inventory` | Listado inventario | Query `page,perPage,sortOrder` | `{"result":[...],"pagination":{...}}` | `401`, `403` |
| `GET /api/inventory/by-product` | Stock por producto | Query `locationId?` | `{"result":[...]}` | `400` |
| `GET /api/inventory/id?id=` | Inventario por id | Query `id` | `{"result":{"id":1}}` | `404` |
| `PUT /api/inventory?id=` | Actualizar inventario | Query `id`, Body `UpdateInventoryRequest` | `204` | `404`, `400` |
| `DELETE /api/inventory?id=` | Eliminar inventario | Query `id` | `204` | `404` |
| `GET /api/inventory/stats` | Estadísticas inventario | - | `{"result":{...}}` | `401` |
| `GET /api/inventory/flow` | Flujo de inventario | Query `days` | `{"result":[...]}` | `401` |
| `GET /api/inventory/stock-by-location` | Stock por ubicación | - | `{"result":[...]}` | `401` |
| `GET /api/inventory/category-distribution` | Distribución por categoría | - | `{"result":[...]}` | `401` |

### InventoryMovement (`/api/inventory-movement`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `GET /form-context` | Contexto para formularios | - | `{"result":{"products":[...]}}` | `401`, `403` |
| `POST /` | Crear movimiento | Body `CreateInventoryMovementRequest` | `201 {"result":{"id":1}}` | `400`, `409` |
| `GET /` | Listado movimientos | Query `page,perPage,sortOrder` | `{"result":[...],"pagination":{...}}` | `401`, `403` |
| `GET /id?id=` | Movimiento por id | Query `id` | `{"result":{"id":1}}` | `404` |
| `GET /product/{productId}` | Historial por producto | Path `productId`, Query `locationId,page,perPage` | `{"result":[...],"pagination":{...}}` | `404` |
| `GET /stats` | Stats movimientos | Query `from,to,today` | `{"result":{...}}` | `401` |
| `GET /flow-with-cumulative` | Flujo acumulado | Query `days` | `{"result":[...]}` | `401` |
| `GET /distribution-by-type` | Distribución por tipo | - | `{"result":[...]}` | `401` |

### Lead (`/api/lead`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `POST /api/lead` | Crear lead | Body `CreateLeadRequest` | `201 {"result":{"id":1}}` | `400` |
| `GET /api/lead` | Listar leads | Query `page,perPage,status,sortOrder` | `{"result":[...],"pagination":{...}}` | `401`, `403` |
| `GET /api/lead/id?id=` | Lead por id | Query `id` | `{"result":{"id":1}}` | `404` |
| `PUT /api/lead?id=` | Actualizar lead | Query `id`, Body `UpdateLeadRequest` | `204` | `404`, `400` |
| `DELETE /api/lead?id=` | Eliminar lead | Query `id` | `204` | `404` |
| `POST /api/lead/{id}/convert` | Convertir a contacto | Path `id` | `201 {"result":{"id":10,"name":"..."}}` | `404`, `400` |

### Location (`/api/location`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `POST /api/location/image` | Subir imagen ubicación | form-data `file` | `{"result":{"photoUrl":"http..."}}` | `400` |
| `GET /api/location` | Listar ubicaciones | Query `page,perPage,organizationId,sortOrder` | `{"result":[...],"pagination":{...}}` | `401`, `403` |
| `GET /api/location/id?id=` | Ubicación por id | Query `id` | `{"result":{"id":1}}` | `404` |
| `POST /api/location` | Crear ubicación | Body `CreateLocationRequest` | `201 {"result":{"id":1}}` | `400` |
| `PUT /api/location?id=` | Actualizar ubicación | Query `id`, Body `UpdateLocationRequest` | `204` | `404`, `400` |
| `DELETE /api/location?id=` | Eliminar ubicación | Query `id` | `204` | `404` |

### Log (`/api/log`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `GET /api/log` | Logs globales | Query `page,perPage,sortOrder,logType,eventTypeLog` | `{"result":[...],"pagination":{...}}` | `401`, `403` |
| `GET /api/log/current-user-logs` | Logs del usuario actual | Query `logType,eventTypeLog,page,perPage,sortOrder` | `{"result":[...],"pagination":{...}}` | `401` |
| `GET /api/log/logs-by-user` | Logs por serial user | Query `serialUser,...` | `{"result":[...],"pagination":{...}}` | `401`, `403` |

### Organization (`/api/organization`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `GET /api/organization` | Lista organizaciones | Query `page,perPage,sortOrder` | `{"result":[...],"pagination":{...}}` | `401`, `403` |
| `GET /api/organization/id?id=` | Organización por id | Query `id` | `{"result":{"id":1}}` | `404` |
| `POST /api/organization` | Crear organización | Body `CreateOrganizationRequest` | `201 {"result":{"id":1}}` | `400`, `409` |
| `PUT /api/organization?id=` | Actualizar organización | Query `id`, Body `UpdateOrganizationRequest` | `204` | `401`, `403`, `404` |
| `DELETE /api/organization?id=` | Eliminar organización | Query `id` | `204` | `401`, `403`, `404` |

### Plan (`/api/plan`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `GET /api/plan` | Listado público de planes | - | `{"result":[{"id":1,"name":"free"}]}` | `500` |
| `GET /api/plan/{id}` | Plan por id | Path `id` | `{"result":{"id":1}}` | `404` |
| `POST /api/plan` | Crear plan | Body `CreateOrUpdatePlanRequest` | `201 {"result":{"id":1}}` | `401`, `403`, `400` |
| `PUT /api/plan/{id}` | Actualizar plan | Path `id`, Body `CreateOrUpdatePlanRequest` | `204` | `404`, `400` |
| `DELETE /api/plan/{id}` | Eliminar plan | Path `id` | `204` | `404`, `409` |

### ProductCategory (`/api/product-category`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `POST /api/product-category` | Crear categoría | Body `CreateProductCategoryRequest` | `201 {"result":{"id":1}}` | `400` |
| `GET /api/product-category` | Listar categorías | Query `page,perPage,sortOrder` | `{"result":[...],"pagination":{...}}` | `401`, `403` |
| `GET /api/product-category/id?id=` | Categoría por id | Query `id` | `{"result":{"id":1}}` | `404` |
| `PUT /api/product-category?id=` | Actualizar categoría | Query `id`, Body `UpdateProductCategoryRequest` | `204` | `404`, `400` |
| `DELETE /api/product-category?id=` | Eliminar categoría | Query `id` | `204` | `404`, `409` |
| `GET /api/product-category/stats` | Estadísticas | - | `{"result":{...}}` | `401` |
| `GET /api/product-category/item-distribution` | Distribución ítems | Query `period,days` | `{"result":[...]}` | `401` |
| `GET /api/product-category/storage-usage` | Uso almacenamiento | - | `{"result":[...]}` | `401` |

### Product (`/api/product`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `POST /api/product/image` | Subir imagen producto | form-data `file` | `{"result":{"imagenUrl":"http..."}}` | `400` |
| `POST /api/product` | Crear producto | Body `CreateProductRequest` | `201 {"result":{"id":1}}` | `400`, `409` |
| `GET /api/product` | Listar productos | Query `page,perPage,sortOrder,onlyForSale` | `{"result":[...],"pagination":{...}}` | `401`, `403` |
| `GET /api/product/catalog` | Catálogo interno para venta | Query `page,perPage` | `{"result":[...],"pagination":{...}}` | `401`, `403` |
| `GET /api/product/id?id=` | Producto por id | Query `id` | `{"result":{"id":1}}` | `404` |
| `PUT /api/product?id=` | Actualizar producto | Query `id`, Body `UpdateProductRequest` | `204` | `404`, `400` |
| `DELETE /api/product?id=` | Eliminar producto | Query `id` | `204` | `404`, `409` |
| `GET /api/product/{id}/images` | Imágenes del producto (público) | Path `id` | `{"result":[{"imageUrl":"..."}]}` | `404` |
| `POST /api/product/{id}/images` | Subir múltiples imágenes | Path `id`, form-data `files[]` | `{"result":[... ]}` | `400`, `404` |
| `PUT /api/product/{id}/images/{imageId}/main` | Definir imagen principal | Path `id,imageId` | `204` | `404` |
| `PUT /api/product/{id}/images/reorder` | Reordenar imágenes | Path `id`, Body `ReorderProductImageItemRequest[]` | `204` | `400`, `404` |
| `DELETE /api/product/{id}/images/{imageId}` | Eliminar imagen | Path `id,imageId` | `204` | `404` |
| `GET /api/product/stats` | Stats de productos | Query `from,to` | `{"result":{...}}` | `401` |
| `GET /api/product/performance` | Rendimiento productos | Query `days,from,to` | `{"result":[...]}` | `401` |
| `GET /api/product/stock-by-category` | Stock agrupado | - | `{"result":[...]}` | `401` |

### Promotion (`/api/promotion`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `POST /api/promotion` | Crear promoción (+push) | Body `CreatePromotionRequest` | `201 {"result":{"promotion":{...},"push":{...}}}` | `400`, `404` |
| `GET /api/promotion` | Listar promociones | Query `page,perPage,productId,activeOnly` | `{"result":[...],"pagination":{...}}` | `401`, `403` |
| `GET /api/promotion/id?id=` | Promo por id | Query `id` | `{"result":{"id":1}}` | `404` |
| `PUT /api/promotion?id=` | Actualizar promo | Query `id`, Body `UpdatePromotionRequest` | `204` | `404`, `400` |
| `PATCH /api/promotion/{id}/active?isActive=` | Activar/desactivar | Path `id`, Query `isActive` | `204` | `404` |
| `DELETE /api/promotion?id=` | Eliminar promo | Query `id` | `204` | `409`, `404` |

### Public (`/api/public`) (anónimo)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `GET /api/public/locations` | Ubicaciones públicas | - | `{"result":[... ]}` | `500` |
| `GET /api/public/tags` | Tags públicas | - | `{"result":[... ]}` | `500` |
| `GET /api/public/catalog` | Catálogo público | Query `locationId,all,page,pageSize` | `{"result":[...]}` o `{"data":[...],"pagination":{...}}` | `400` |

### Push (`/api/push`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `POST /api/push/subscribe` | Registrar/actualizar suscripción push | Body `PushSubscribeRequest` | `{"result":{"success":true}}` | `400` |
| `POST /api/push/send` | Envío manual push por location | Body `PushSendRequest` | `{"result":{"sent":n,"failed":m}}` | `401`, `403`, `400` |

### Reports (`/api/reports`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `GET /api/reports/sales/export/pdf` | Export ventas PDF | Query `dateFrom,dateTo,locationId` | `File(application/pdf)` | `401`, `403` |
| `GET /api/reports/sales/export` | Export ventas CSV | Query `dateFrom,dateTo,locationId` | `File(text/csv)` | `401`, `403` |
| `GET /api/reports/sales` | Reporte ventas | Query `dateFrom,dateTo,locationId,page,pageSize` | `{"result":{...}}` | `401`, `403` |
| `GET /api/reports/inventory` | Reporte inventario | Query `dateFrom,dateTo,locationId` | `{"result":{...}}` | `401`, `403` |
| `GET /api/reports/products` | Reporte productos | Query `dateFrom,dateTo,locationId` | `{"result":{...}}` | `401`, `403` |
| `GET /api/reports/crm` | Reporte CRM | Query `dateFrom,dateTo,locationId` | `{"result":{...}}` | `401`, `403` |
| `GET /api/reports/operations` | Reporte operaciones | Query `dateFrom,dateTo,locationId` | `{"result":{...}}` | `401`, `403` |

### Role (`/api/role`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `GET /api/role/permissions` | Lista permisos | - | `{"result":[{"code":"..."}]}` | `401`, `403` |
| `GET /api/role/my-role` | Rol del usuario actual | - | `{"result":{"id":1,"name":"Admin"}}` | `404`, `401` |
| `GET /api/role` | Listar roles | Query `page,perPage,sortOrder` | `{"result":[...],"pagination":{...}}` | `401`, `403` |
| `GET /api/role/id?id=` | Rol por id | Query `id` | `{"result":{"id":1}}` | `404` |
| `POST /api/role` | Crear rol | Body `CreateRoleRequest` | `201 {"result":{"id":1}}` | `400`, `409` |
| `PUT /api/role?id=` | Actualizar rol | Query `id`, Body `UpdateRoleRequest` | `204` | `404`, `400` |
| `DELETE /api/role?id=` | Eliminar rol | Query `id` | `204` | `404`, `409` |

### SaleOrder (`/api/sale-order`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `POST /api/sale-order` | Crear orden (anónimo permitido) | Body `CreateSaleOrderRequest` | `201 {"result":{"id":1,"status":"pending"}}` | `400`, `404` |
| `GET /api/sale-order` | Listar órdenes | Query `page,perPage,status,sortOrder` | `{"result":[...],"pagination":{...}}` | `401`, `403` |
| `GET /api/sale-order/id?id=` | Orden por id | Query `id` | `{"result":{"id":1}}` | `404` |
| `PUT /api/sale-order?id=` | Actualizar orden | Query `id`, Body `UpdateSaleOrderRequest` | `204` | `404`, `400` |
| `POST /api/sale-order/{id}/confirm` | Confirmar orden | Path `id` | `{"result":{"status":"confirmed"}}` | `404`, `409` |
| `POST /api/sale-order/{id}/cancel` | Cancelar orden | Path `id` | `{"result":{"status":"cancelled"}}` | `404`, `409` |
| `GET /api/sale-order/stats` | Stats ventas | Query `days` | `{"result":{...}}` | `401`, `403` |

### SaleReturn (`/api/sale-return`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `POST /api/sale-return` | Crear devolución | Body `CreateSaleReturnRequest` | `201 {"result":{"id":1}}` | `400`, `404`, `409` |
| `GET /api/sale-return` | Listar devoluciones | Query `page,perPage,sortOrder` | `{"result":[...],"pagination":{...}}` | `401`, `403` |
| `GET /api/sale-return/id?id=` | Devolución por id | Query `id` | `{"result":{"id":1}}` | `404` |
| `GET /api/sale-return/by-sale-order?saleOrderId=` | Devoluciones por orden | Query `saleOrderId,page,perPage` | `{"result":[...],"pagination":{...}}` | `404` |

### Setting (`/api/setting`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `POST /api/setting/set-setting` | Upsert setting simple | Body `SettingRequest` | `{"result":true}` | `401`, `403`, `400` |
| `GET /api/setting?key=` | Obtener setting por key | Query `key` | `{"result":{"key":"...","value":"..."}}` | `404` |
| `GET /api/setting/grouped` | Obtener settings agrupadas | - | `{"result":{"general":{...}}}` | `401`, `403` |
| `PUT /api/setting/grouped` | Actualizar settings agrupadas | Body `UpdateGroupedSettingsRequest` | `{"result":{"updated":true}}` | `400`, `401`, `403` |

### Subscription (`/api/subscription`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `GET /api/subscription` | Lista suscripciones (admin) | Query `page,perPage,status,planId` | `{"result":[...],"pagination":{...}}` | `401`, `403` |
| `GET /api/subscription/{id}` | Detalle suscripción | Path `id` | `{"result":{"id":1,"adminContact":{"phone":"..."}}}` | `404` |
| `GET /api/subscription/my-subscription` | Suscripción de mi org | - | `{"result":{"id":1}}` | `404`, `401` |
| `GET /api/subscription/requests` | Lista solicitudes | Query `page,perPage,status` | `{"result":[...],"pagination":{...}}` | `401`, `403` |
| `GET /api/subscription/requests/{id}` | Detalle solicitud | Path `id` | `{"result":{"id":10,"subscription":{...}}}` | `404` |
| `POST /api/subscription/requests/{id}/approve` | Aprobar solicitud | Path `id`, Body `ApproveSubscriptionRequestDto` | `{"result":{"status":"approved"}}` | `404`, `409` |
| `POST /api/subscription/requests/{id}/reject` | Rechazar solicitud | Path `id`, Body `RejectSubscriptionRequestDto` | `{"result":{"status":"rejected"}}` | `404`, `409` |
| `POST /api/subscription/{id}/renew` | Renovar suscripción | Path `id`, Body `RenewSubscriptionRequest` | `{"result":{"status":"active"}}` | `404`, `400` |
| `PUT /api/subscription/{id}/change-plan` | Cambiar plan | Path `id`, Body `ChangePlanRequest` | `{"result":{"plan":{"id":2}}}` | `404`, `400` |

### Supplier (`/api/supplier`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `POST /api/supplier` | Crear proveedor | Body `CreateSupplierRequest` | `201 {"result":{"id":1}}` | `400` |
| `GET /api/supplier` | Listar proveedores | Query `page,perPage,sortOrder` | `{"result":[...],"pagination":{...}}` | `401`, `403` |
| `GET /api/supplier/id?id=` | Proveedor por id | Query `id` | `{"result":{"id":1}}` | `404` |
| `PUT /api/supplier?id=` | Actualizar proveedor | Query `id`, Body `UpdateSupplierRequest` | `204` | `404`, `400` |
| `DELETE /api/supplier?id=` | Eliminar proveedor | Query `id` | `204` | `404` |
| `GET /api/supplier/stats` | Stats proveedores | Query `from,to` | `{"result":{...}}` | `401` |
| `GET /api/supplier/delivery-timeline` | Timeline entregas | Query `days,from,to` | `{"result":[...]}` | `401` |
| `GET /api/supplier/category-distribution` | Distribución por categoría | - | `{"result":[...]}` | `401` |

### Tags (`/api/tags`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `GET /api/tags` | Listar tags | - | `{"result":[{"name":"..." }]}` | `401`, `403` |
| `GET /api/tags/{id}` | Tag por id | Path `id` | `{"result":{"id":1}}` | `404` |
| `POST /api/tags` | Crear tag | Body `CreateTagRequest` | `201 {"result":{"id":1}}` | `400`, `409` |
| `PUT /api/tags/{id}` | Actualizar tag | Path `id`, Body `UpdateTagRequest` | `200 {"result":true}` | `404`, `400` |
| `DELETE /api/tags/{id}` | Eliminar tag | Path `id` | `204` | `404` |

### User (`/api/user`)

| Método + Path | Descripción | Request | Response | Errores |
|---|---|---|---|---|
| `POST /api/user` | Crear usuario | Body `CreateUserRequest` | `201 {"result":{"id":1}}` | `400`, `409` |
| `GET /api/user` | Listar usuarios | Query `page,perPage,sortOrder` | `{"result":[...],"pagination":{...}}` | `401`, `403` |
| `GET /api/user/id?id=` | Usuario por id | Query `id` | `{"result":{"id":1}}` | `404` |
| `PUT /api/user/{id}` | Actualizar usuario | Path `id`, Body `UpdateUserRequest` | `204` | `404`, `400` |
| `DELETE /api/user?id=` | Eliminar usuario | Query `id` | `204` | `404` |

## Flujos Principales

### 1) Autenticación JWT
```text
Cliente
  -> POST /api/account/login (email/password o Google)
      -> AccountService valida credenciales
      -> genera AccessToken + RefreshToken
      -> persiste UserToken
  <- 200 token/refresh

Requests protegidas
  -> JWT Bearer
      -> CurrentUserContextMiddleware setea OrganizationId/LocationId en DbContext
      -> filtros de tenant aplican automáticamente
```

### 2) Venta con descuento promocional
```text
Cliente (checkout)
  -> POST /api/sale-order
      -> SaleOrderService carga producto + inventario + promociones activas
      -> aplica Promotion si cumple vigencia/minQuantity
      -> guarda SaleOrder + SaleOrderItem (UnitPrice/OriginalUnitPrice/PromotionId)
  <- 201 orden pendiente

Operador
  -> POST /api/sale-order/{id}/confirm
      -> crea InventoryMovement tipo salida
      -> descuenta stock
  <- 200 confirmada
```

### 3) Promoción activa + Push Web
```text
Cliente PWA
  -> POST /api/push/subscribe (endpoint, keys, locationId)
      -> upsert en WebPushSubscriptions

Admin
  -> POST /api/promotion
      -> PromotionService crea promo
      -> PromotionPushService resuelve locations de la org
      -> PushNotificationService envía WebPush (VAPID) por location
         payload incluye: title, body, url, image/imageUrl, icon, badge, storeName
```

### 4) Ciclo de suscripción
```text
Registro organización
  -> create org + admin + currency base
  -> si plan free: Subscription activa inmediata
  -> si plan pago: Subscription pending + SubscriptionRequest pending

Backoffice
  -> POST /api/subscription/requests/{id}/approve|reject
      -> actualiza estado de request y subscription
      -> activa/inactiva organización según resultado
```

