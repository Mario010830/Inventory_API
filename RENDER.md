# Desplegar en Render

## 1. Docker

El proyecto incluye un `Dockerfile` en la raíz. En Render:

- **Environment**: Docker  
- **Dockerfile Path**: vacío (usa `./Dockerfile`)  
- **Root Directory**: vacío (si el repo tiene esta carpeta como raíz)

Si tu repositorio en GitHub tiene la solución dentro de una subcarpeta (por ejemplo solo la carpeta `APICore`), en Render configura:

- **Root Directory**: `APICore` (o la carpeta donde esté el `Dockerfile`)

## 2. Variables de entorno en Render

En el servicio → **Environment** → **Environment Variables**, define al menos:

| Variable | Descripción |
|----------|-------------|
| `ConnectionStrings__ApiConnection` | Cadena de conexión a SQL Server |
| `BearerTokens__Key` | Clave secreta para firmar JWT (mín. 32 caracteres) |
| `BearerTokens__Issuer` | Emisor del token (ej: `https://tu-api.onrender.com`) |
| `BearerTokens__Audience` | Audiencia del token (ej: `https://tu-api.onrender.com`) |

`PORT` la asigna Render automáticamente; la API ya la usa.

## 3. Base de datos

Si usas SQL Server en la nube (Azure SQL, etc.), pega la connection string en `ConnectionStrings__ApiConnection`.  
Para el plan gratuito de Render no incluye base de datos; necesitas una externa.

## 4. Deploy

Tras subir los cambios a GitHub (rama `main`), Render hará el build con Docker y desplegará. Revisa los logs si falla el arranque (suele ser por variables de entorno faltantes).
