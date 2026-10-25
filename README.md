# Punto de Venta – .NET 10 + ASP.NET Core Web API + Blazor + MySQL

Proyecto de Programación 2. Un solo proyecto (`PuntoVenta`) que aloja:

- **Modelo**: `Models/` (entidades) y `Data/AppDb.cs` (Entity Framework Core → MySQL)
- **Controlador**: `Controllers/` (API REST con GET, POST, PUT, DELETE protegida con JWT)
- **Vista**: `Components/` (Blazor Server + Bootstrap 5). Blazor consume la API con `HttpClient` (`Services/ApiClient.cs`)

## 1. Requisitos
- .NET 10 SDK
- MySQL 8 + MySQL Workbench
- Visual Studio 2022/2026 o VS Code

## 2. Base de datos
1. Abrir `database/puntoventa.sql` en MySQL Workbench y ejecutarlo completo. Crea la base `punto_venta`, todas las tablas (incluye `usuarios` y `menu`) y datos de prueba en todas las tablas.
2. En `appsettings.json` verificar que `Password=` coincida con la contraseña de su MySQL (ojo: no suba su contraseña real a GitHub público).

## 3. Ejecutar
```
cd PuntoVenta
dotnet run
```
Abrir `http://localhost:5000`. Usuario: **admin** – Contraseña: **Admin123***

## 4. Funcionamiento en red local (obligatorio en la presentación)
1. En la PC servidor ejecutar `ipconfig` y anotar la IPv4 (ej. `192.168.1.10`).
2. Abrir el puerto en el firewall (CMD como administrador):
   `netsh advfirewall firewall add rule name="PuntoVenta 5000" dir=in action=allow protocol=TCP localport=5000`
3. La app ya escucha en todas las interfaces (`"Urls": "http://0.0.0.0:5000"` en `appsettings.json`).
4. Desde otra PC de la misma red: `http://192.168.1.10:5000`

Las imágenes de productos se guardan en `wwwroot/images/productos/` del servidor y en la BD solo se guarda la ruta (`/images/productos/xxxx.jpg`).

## 5. Flujo para la presentación
Login (JWT) → menú dinámico → CRUD → registrar producto con imagen → registrar compra (sube existencia, actualiza costo, venta = costo + 25%) → registrar venta (valida existencia y la descuenta) → reportes → acceso desde otra PC.

## 6. Cómo cumple cada punto
| Requisito | Dónde |
|---|---|
| JWT | `Controllers/AuthController.cs`, `Program.cs`, `Services/AuthService.cs` |
| Endpoints protegidos | `[Authorize]` en todos los controladores (menos login) |
| Menú dinámico | tabla `menu` → `api/menu` → `Components/Layout/MenuNodo.razor` (recursivo) |
| CRUD Puestos, Empleados, Clientes, Proveedores, Marcas, Productos | `Components/Pages/*.razor` + `Components/Shared/CrudPage.razor` |
| Maestro/Detalle | `Compras.razor`, `Ventas.razor`, `Controllers/ComprasVentasController.cs` |
| Reglas de compras y ventas | `ComprasVentasController.cs` (con transacción) |
| Reportes | `Reportes.razor`, `api/reportes/*` |
| Validaciones | DataAnnotations en `Models/Entidades.cs` (Blazor y API) + reglas en controladores |

## 7. Detalles a tener en cuenta
- `genero` se guarda como `TINYINT(1)` (equivale a BIT del diagrama, más compatible con EF Core) y `imagen` como `VARCHAR(150)` para que quepa la ruta.
- El paquete `Microting.EntityFrameworkCore.MySql` es el proveedor MySQL compatible con EF Core 10 (misma API que Pomelo).
- **Bootstrap sin internet**: la página carga Bootstrap desde CDN. Si el laboratorio no tiene internet, descargue `bootstrap.min.css` a `wwwroot/lib/` y cambie el `<link>` en `Components/App.razor`.

## 8. GitHub (trabajo colaborativo)
```
git init
git add .
git commit -m "Proyecto inicial"
git branch -M main
git remote add origin https://github.com/USUARIO/punto-venta.git
git push -u origin main
```
Cada integrante debe clonar el repositorio, trabajar en su rama (`git checkout -b nombre-modulo`) y hacer sus propios commits/pull requests para evidenciar la colaboración.

## 9. Modificaciones solicitadas por el ingeniero (version 2)
| Cambio | Donde |
|---|---|
| Ventas: cliente por NIT/nombre con lista de sugerencias; si el NIT no existe se registra ahi mismo | `Components/Pages/Ventas.razor` + `Components/Shared/Buscador.razor` |
| Producto con filtro (escribir para buscar) en Ventas y Compras | `Buscador.razor` |
| Vista previa de factura antes de confirmar venta y compra (Confirmar / Cancelar) | `Ventas.razor`, `Compras.razor` |
| No. de orden de compra y No. de factura automaticos | `GET api/compras/siguiente-orden`, `GET api/ventas/siguiente-factura` |
| Fecha de la venta automatica (hoy, no editable) | `Ventas.razor` y `ComprasVentasController.cs` |
| Reportes: filtro por texto y por fecha (compras/ventas), totales, exportar CSV e imprimir | `Components/Pages/Reportes.razor`, `wwwroot/app.js` |
| JWT guardado en el navegador (localStorage): no pide login al recargar o abrir otra pestana hasta que expire | `Services/AuthService.cs` |
| Menu administrable | `Components/Pages/MenuAdmin.razor` (`api/menuadmin`) |
| Usuarios con roles y permisos: el menu y las pantallas cambian segun el rol | `Usuarios.razor`, `Roles.razor`, `Controllers/SeguridadControllers.cs`, tablas `roles` y `rol_menu` |

### Base de datos: cual script ejecutar
- **Base nueva o quiere empezar de cero:** `database/puntoventa.sql` (borra y recrea `punto_venta`).
- **Ya tiene la base con datos y no quiere perderlos:** `database/actualizacion_roles.sql` (una sola vez).

### Roles de ejemplo
- **Administrador**: todo, incluida la seccion Seguridad (Usuarios, Roles y permisos, Opciones del menu).
- **Vendedor**: Productos, Ventas, Clientes y reporte de ventas.
- **Encargado de compras**: Productos, Marcas, Compras, Proveedores y reportes de productos, existencias y compras.

Los permisos del menu se cargan al iniciar sesion: si cambia los permisos de un rol, los usuarios de ese rol deben volver a iniciar sesion. Las pantallas de Seguridad tambien estan protegidas en la API (solo rol Administrador).

## 10. Publicar en IIS (maquina virtual de Azure UMG)
1. En la VM: Panel de control > Programas > Activar o desactivar caracteristicas de Windows > **Internet Information Services**. Dentro de "Desarrollo de aplicaciones" marcar **Protocolo WebSocket** (Blazor lo necesita).
2. Instalar el **.NET 10 Hosting Bundle** (dotnet.microsoft.com > Descargas > .NET 10 > Hosting Bundle) y reiniciar IIS con `iisreset`.
3. Instalar **MySQL Server** en la VM y ejecutar `database/puntoventa.sql` (crea `punto_venta`).
4. Generar el release: `dotnet publish -c Release -o C:\inetpub\PuntoVenta` (si la VM no tiene el SDK de .NET 10, publicar en su PC y copiar la carpeta a la VM).
5. En el `appsettings.json` de la carpeta publicada: poner la cadena de conexion de MySQL de la VM y dejar `"ApiBaseUrl": ""` (vacio = usa la direccion con la que se abre el sitio, no localhost).
6. IIS Manager > Application Pools > Add: nombre `PuntoVenta`, ".NET CLR version" = **No Managed Code**.
7. Sites > Add Website: nombre `PuntoVenta`, Application pool `PuntoVenta`, Physical path `C:\inetpub\PuntoVenta`, puerto **80** (si el "Default Web Site" ya usa el 80, detenerlo o usar el puerto 8080).
8. Permisos de imagenes: clic derecho en `C:\inetpub\PuntoVenta\wwwroot\images\productos` > Properties > Security > Edit > Add > `IIS AppPool\PuntoVenta` > marcar **Modify**.
9. Firewall de Windows (CMD como administrador): `netsh advfirewall firewall add rule name="PuntoVenta HTTP" dir=in action=allow protocol=TCP localport=80` (cambiar el puerto si usa 8080).
10. Ver la IP privada de la VM con `ipconfig` (ej. 10.0.0.4) y probar desde otra VM del laboratorio: `http://10.0.0.4` (o `http://10.0.0.4:8080`).

Nota: en IIS el sitio usa el puerto que se configura en IIS; el aviso de "Overriding address(es)" en el log por la clave `Urls` de appsettings es normal y se puede ignorar.
