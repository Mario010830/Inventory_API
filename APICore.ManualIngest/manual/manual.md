# Inventory management system — user manual

## 1. Introduction

This system lets you run **multi-location inventory**: products, stock by location, movements (entries, exits, adjustments, transfers), **sales orders**, **contacts**, **leads**, **promotions**, and **administration** (users, roles, settings, logs).

Navigation is mainly from the **left sidebar**; the **top bar** includes currency selection and shortcuts.

**Typical workflow**

1. Define **locations** (warehouses / stores).
2. Create **categories** and **suppliers**.
3. Create **products** (with optional images).
4. Register **inventory movements** to change stock (or use flows that create stock implicitly where the app allows).
5. Use **Ventas** for sale orders; use **Contactos** / **Leads** for CRM.
6. Use **Reportes** for analytics exports/views.

---

## 2. Signing in and session

### 2.1 Login

1. Open the login page (`/login`).
2. Enter **email** and **password**.
3. Submit the form.
4. On success you are redirected to **`/dashboard`**.

**Example**

- Email: `usuario@empresa.com`
- Password: (as provided by your administrator)

### 2.2 Session and logout

- The app keeps your session after reload (token / stored user, depending on deployment).
- To leave the app safely: sidebar **“Cerrar sesión”** (bottom). That clears the local session and returns you to login.

### 2.3 Permissions

Menu items and buttons depend on your **role**. If you do not see a section or an **“Nuevo …”** button is disabled, you lack the corresponding permission (e.g. `product.create`, `inventorymovement.create`). Ask an admin to adjust your role under **Usuarios** / **Roles**.

---

## 3. Main layout

| Area | Purpose |
|------|--------|
| **Sidebar** | Modules: Dashboard, Productos, Categorías, Proveedores, Ubicaciones, Inventario, Movimientos, Ventas, Contactos, Leads, Promociones; **ADMIN**: Usuarios, Roles, Logs, Categorías de negocio, Configuración; **REPORTES** submenu. |
| **Top bar** | Menu toggle (mobile / collapse), **currency** selector, notification/message icons (if enabled). |
| **Main content** | Tables, filters, modals, and **detail drawer** when you open a row. |

**Mobile**

- Open the menu with the **hamburger** icon; **Escape** closes the overlay.

---

## 4. Dashboard

**Path:** `Dashboard` → `/dashboard`

**What you see**

- Summary KPIs (products, inventory value, low stock, orders, trends — depending on API data).
- Charts: inventory flow, category mix, stock status, entries vs exits, value evolution, heatmaps/lists, etc.

**Usage**

- Use **date / range** controls where available to focus the period.
- Click chart/list items if the UI links them to detail (depends on implementation).

---

## 5. Products (Productos)

**Path:** `Productos` → `/dashboard/products`

**Permissions:** typically `product.read` to view; `product.create` / `product.update` / `product.delete` for changes.

### 5.1 List and filters

- **Search** debounced text filter.
- Filters often include: **category**, **availability**, **for sale**, **product type**, **price min/max**.
- **“Nuevo Producto”** opens the create modal (if allowed).

### 5.2 Create a product

1. Click **“Nuevo Producto”**.
2. Fill required fields (name, SKU/code, category, prices, stock-related fields as shown in the form).
3. Set flags such as **available** / **for sale** if present.
4. Optionally assign **tags** and **locations** where the product is offered (if the form includes checkboxes).
5. **Save** / submit the modal.

**Example (conceptual)**

- Name: `Cable USB-C 2m`
- Category: `Accesorios`
- Sale price: `12.99`
- Mark as available for sale if you sell it on the public catalog.

### 5.3 Edit a product

1. Open the row menu or click the row (per table actions).
2. Choose **edit**.
3. Change fields and save.

**Note:** Some fields (e.g. offer locations) may only send to the server if you actually changed them, to avoid overwriting server data.

### 5.4 Delete a product

1. Use the row action **Eliminar** (or equivalent).
2. Confirm in the **delete** dialog.

Errors from the server are shown in the modal or as toasts.

### 5.5 Images

- **Upload** image (drag-and-drop or file picker); supported types typically include JPEG, PNG, GIF, WebP (max size enforced in UI, e.g. 5 MB).
- **Gallery:** set **main** image, **reorder**, **delete** images where the UI exposes actions.

### 5.6 Import

- Use **import wizard** (e.g. Elyerro / CSV flows) when visible in the toolbar — follow on-screen steps: preview → confirm import.

### 5.7 Bulk actions

- Select multiple rows when the table supports it; use the **bulk toolbar** (e.g. bulk delete) if you have rights.

---

## 6. Categories (Categorías)

**Path:** `Categorías` → `/dashboard/categories`

**Permissions:** `productcategory.*`

### Create

1. **“Nueva categoría”**.
2. Enter name (and any other required fields).
3. Save.

### Edit / delete

- Use row actions → **Editar** / **Eliminar**, confirm deletion.
- Extra endpoints in the app also support **stats** / **storage usage** views where implemented in the UI.

---

## 7. Suppliers (Proveedores)

**Path:** `Proveedores` → `/dashboard/suppliers`

**Permissions:** `supplier.*`

### Create

1. **“Nuevo proveedor”** (or equivalent add button).
2. Complete supplier data (name, contact, tax IDs, etc. as per form).
3. Save.

### Edit / delete

- Row actions → edit or delete with confirmation.

---

## 8. Locations (Ubicaciones)

**Path:** `Ubicaciones` → `/dashboard/locations`

**Permissions:** `location.*`

### Create

1. **“Nueva ubicación”**.
2. Fill name, address, organization linkage, **business category**, etc.
3. Configure **business hours** in the editor when present (per weekday).
4. Save.

### Edit / delete

- Open edit from the row; **Eliminar** with confirmation.
- **Image:** upload location image similar to product image rules where available.

### Bulk delete

- If enabled, select rows and run bulk delete from the toolbar.

---

## 9. Inventory (Inventario)

**Path:** `Inventario` → `/dashboard/inventory`

**Permissions:** `inventory.read`

This screen is primarily a **read-only** view of **current stock per product and location**.

### Usage

- Filter by **text**, **location**, **category**, **critical stock only**, **date range** (on created/modified).
- Open a row to see **detail** in the side drawer.

**Important:** To **change** stock, use **Movimientos** (entries, exits, adjustments, transfers), not arbitrary editing on this grid (unless your deployment adds that).

---

## 10. Movements (Movimientos)

**Path:** `Movimientos` → `/dashboard/movements`

**Permissions:** `inventorymovement.read` to list; `inventorymovement.create` to add.

### Movement types

| Type | Typical use |
|------|-------------|
| **Entrada** | Goods received (purchase, return to stock). |
| **Salida** | Goods leaving (sale shipment, consumption, damage). |
| **Ajuste** | Stock correction. |
| **Transferencia** | Between locations (when the form asks for origin/destination). |

### Create a movement

1. Use quick buttons such as **“Entrada”** / **“Salida”** or the main **new movement** action.
2. Choose **location**, **product** (or create a minimal new product from the movement flow if the UI offers it).
3. Set **quantity**, **type**, **reason** (from configured reasons), **date** if applicable.
4. Submit.

**Example**

- **Entrada** of **50** units of product **“SKU-001”** at **“Depósito Central”**, reason **“Compra proveedor”**.

### List / detail

- Filter by type, text, dates, etc.
- Open a row for **movement detail** in the drawer.

---

## 11. Sales (Ventas)

**Path:** `Ventas` → `/dashboard/sales`

**Permissions:** `sale.read` and related for create/update/confirm.

### Order states (UI labels)

- **Pendiente** (`Draft`)
- **Aceptada** (`Confirmed`)
- **Cancelada** (`Cancelled`)

### Typical flow

1. **Create** a sale order (add line items, location, totals as per form).
2. **Confirm** or **Cancel** from row actions when available.
3. Filter list by **status**, **dates**, **amount range**, **seller**, text search.

### Example

- New order at **“Tienda Centro”** with two line items → save as **Pendiente** → **Aceptar** when the sale is finalized.

---

## 12. Contacts (Contactos)

**Path:** `Contactos` → `/dashboard/contacts`

**Permissions:** `contact.create` / `contact.update` / `contact.delete`

### CRUD

- **“Nuevo contacto”** → fill form → save.
- **Edit** / **Delete** from row actions.
- **Bulk delete** when permitted and rows are selected.

---

## 13. Leads (Leads)

**Path:** `Leads` → `/dashboard/leads`

**Permissions:** `lead.*`

### CRUD

- **New lead** → form → save.
- **Edit** / **Delete** per row.
- **Convert** lead (action that calls convert endpoint) when you qualify the lead to customer/opportunity.

### Example

- Lead **“María — consulta mayorista”** → status updated → **Convertir** when closed-won.

---

## 14. Promotions (Promociones)

**Path:** `Promociones` → `/dashboard/promotions`

**Permissions:** tied to **`product.update`** for management in the current app.

### Usage

- Create or edit promotions (discount rules, dates, products/locations — as shown in the form).
- **Activate/deactivate** promotion where toggles exist.
- **Delete** when allowed.

---

## 15. Users (Usuarios)

**Path:** `ADMIN` → `Usuarios` → `/dashboard/users`

**Permissions:** `user.*`

### Create user

1. **“Nuevo usuario”**.
2. Set name, email, password (if applicable), **role**, **location**, organization fields as required.
3. Save.

### Edit / delete

- Edit user from row actions; **delete** with confirmation.
- Some fields may be partial updates (only changed fields sent).

---

## 16. Roles (Roles)

**Path:** `ADMIN` → `Roles` → `/dashboard/roles`

**Permissions:** `role.*`

### Create role

1. **“Nuevo rol”**.
2. Name the role and assign **permissions** (checkbox list from **“Permisos”** / API).
3. Save.

### Edit / delete

- **Edit** role name and permissions.
- **Delete** role if no longer used (watch dependencies on users).

### Useful permission examples

- `product.read`, `product.create`, `inventorymovement.create`, `sale.read`, `setting.read`, etc.

---

## 17. Logs

**Path:** `ADMIN` → `Logs` → `/dashboard/logs`

**Permissions:** `log.read`

- Read-only **audit / event** list with filters (type, date, pagination).
- Use for troubleshooting “who did what”.

---

## 18. Business categories (Categorías de negocio)

**Path:** `ADMIN` → `Categorías de negocio` → `/dashboard/business-categories`

**Permissions:** `setting.update` (for edits in current UI)

- Maintain categories used to classify **locations** (e.g. retail vs warehouse).
- Edit inline or via modal per implementation; **save** persists changes.

---

## 19. Settings (Configuración)

**Path:** `ADMIN` → `Configuración` → `/dashboard/settings`

Sections (hash anchors): **Inventario**, **Monedas y tipo de cambio**, **Notificaciones**, **Seguridad**, **Perfil de cuenta**, **Suscripción**.

### Usage

- **Inventario:** defaults and inventory-related options.
- **Monedas:** manage currencies and default / exchange where exposed.
- **Notificaciones:** preferences.
- **Seguridad:** password / security options.
- **Perfil:** update profile via **“Actualizar perfil”** / `account/update-profile` flow.
- **Suscripción:** view **“Mi suscripción”** information.

Use the **submenu** under Configuración in the sidebar to jump to `#inventario`, `#monedas`, etc.

---

## 20. Reports (Reportes)

**Menu:** `REPORTES` in the sidebar.

Typical entries (as in app):

- Ventas, Inventario, Productos, CRM, Operaciones
- **Métricas** (`/admin/reports/metrics`) — traffic/products/sales/customers metrics by period

**Usage**

- Open the report type, set **filters** and **date range**, **export** (CSV/PDF) where buttons exist.

---

## 21. Public catalog (optional)

If your deployment exposes **`/catalog`**, customers can browse **public locations** and **catalog** by location, tags, and pagination — **without** admin login. Admin still maintains products marked for public sale and locations.

---

## 22. Tips and good practices

1. **Always pick the correct location** on movements and sales — stock is per location.
2. Use **categories** consistently before scaling the product list.
3. Prefer **movements** with clear **reasons** so reports and audits stay understandable.
4. **Currency** in the top bar affects how amounts are **displayed**; ensure base data matches your accounting rules.
5. For large tables, wait for **debounced search** after typing; some grids **load extra pages** automatically for “select all” behavior.
6. If an action fails, read the **toast** or modal message — often it is validation or a business rule from the API.

---

## 23. Quick reference — create / edit / delete

| Entity | Create | Edit | Delete | Notes |
|--------|--------|------|--------|--------|
| Product | **Nuevo Producto** | Row → edit | Confirm modal | Images, import, bulk |
| Category | **Nueva categoría** | Row | Confirm | |
| Supplier | Add button | Row | Confirm | |
| Location | **Nueva ubicación** | Row | Confirm | Hours, image |
| Inventory | — | Detail drawer | — | View only; change via movements |
| Movement | **Entrada** / **Salida** / form | — | — | Per permissions |
| Sale order | New order | Row / actions | — | Confirm / cancel |
| Contact | **Nuevo contacto** | Row | Confirm / bulk | |
| Lead | New | Row | Confirm / bulk | Convert |
| Promotion | New | Row | Confirm | Needs `product.update` |
| User | **Nuevo usuario** | Row | Confirm | |
| Role | **Nuevo rol** | Row | Confirm | Permissions |
| Log | — | — | — | Read only |
| Business category | Per UI | Per UI | — | `setting.update` |

---

*Document generated for the Strova inventory frontend; align with your deployed API and permissions.*
