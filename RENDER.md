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
| `ConnectionStrings__ApiConnection` | Cadena de conexión a **PostgreSQL** (Npgsql), p. ej. `Host=...;Port=5432;Database=...;Username=...;Password=...` |
| `BearerTokens__Key` | Clave secreta para firmar JWT (mín. 32 caracteres) |
| `BearerTokens__Issuer` | Emisor del token (ej: `https://tu-api.onrender.com`) |
| `BearerTokens__Audience` | Audiencia del token (ej: `https://tu-api.onrender.com`) |

`PORT` la asigna Render automáticamente; la API ya la usa.

## 3. Base de datos

Pega la connection string de tu instancia (Render PostgreSQL, Neon, Supabase, Azure Database for PostgreSQL, etc.) en `ConnectionStrings__ApiConnection`.  
Para el plan gratuito de Render no incluye base de datos; necesitas una externa.

## 4. Deploy

Tras subir los cambios a GitHub (rama `main`), Render hará el build con Docker y desplegará. Revisa los logs si falla el arranque (suele ser por variables de entorno faltantes).

---

## 5. VPS / Linux manual (`/var/www/.../publish`)

Si publicas desde **Windows** y subes la carpeta a un servidor **Linux**, debes generar el output para **linux-x64**. Si usas el publish por defecto (Windows), los `.dll` no son válidos en Linux y verás errores como:

- `Illegal or unimplemented ELEM_TYPE in signature` / *format of the file '...APICore.Services.dll' is invalid*
- `Could not load type '...'` con nombres **truncados o raros** (p. ej. `arameter.goryResponse`) → suele ser **mezcla de DLLs** de otro build o **archivos corruptos** al copiar.

### Publicar correctamente para Linux

Desde la carpeta de la solución (donde está `APICore.sln`):

```bash
dotnet publish APICore.API/APICore.API.csproj -c Release -o ./publish-linux -r linux-x64 --self-contained false
```

En el servidor debe estar instalado el **ASP.NET Core Runtime 9.0** (alineado con `TargetFramework` `net9.0`).

Alternativa (no depende del runtime en el servidor, carpeta más grande):

```bash
dotnet publish APICore.API/APICore.API.csproj -c Release -o ./publish-linux-sc -r linux-x64 --self-contained true
```

### Checklist de despliegue

1. **Borra** la carpeta antigua en el servidor y **sube todo** el contenido de `publish-linux` de una vez (no mezcles DLLs viejos con nuevos).
2. Sube en **modo binario** (SFTP/SCP; evita FTP en ASCII).
3. Verifica que el proceso que ejecuta la app use exactamente esa carpeta y no otra copia en caché.

### Recomendación

Donde puedas, usa el **`Dockerfile`** del repo: el build ocurre ya en imagen Linux y evita estos problemas.
