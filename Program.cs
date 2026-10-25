using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PuntoVenta.Components;
using PuntoVenta.Data;
using PuntoVenta.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();   // necesario para servir blazor.web.js fuera del modo Development

// ---------- Base de datos (MySQL + EF Core) ----------
var cs = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDb>(o => o.UseMySql(cs, new MySqlServerVersion(new Version(8, 0, 36))));

// ---------- JWT ----------
var jwtKey = builder.Configuration["Jwt:Key"];
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// ---------- API REST ----------
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// ---------- Blazor (vistas) ----------
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
// Blazor consume la API REST con HttpClient. Si "ApiBaseUrl" esta vacio, usa la misma direccion con la que
// se abrio el sitio (IP o nombre del servidor, ej. http://10.0.0.4), asi no depende de localhost.
builder.Services.AddScoped(sp =>
{
    var configurada = builder.Configuration["ApiBaseUrl"];
    var baseUrl = string.IsNullOrWhiteSpace(configurada)
        ? sp.GetRequiredService<NavigationManager>().BaseUri
        : configurada;
    return new HttpClient { BaseAddress = new Uri(baseUrl) };
});
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ApiClient>();

var app = builder.Build();

// Usuario administrador inicial (solo si la tabla esta vacia y existe el rol Administrador)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var rolAdmin = db.Roles.FirstOrDefault(r => r.Nombre == "Administrador");
        if (rolAdmin != null && !db.Usuarios.Any())
        {
            db.Usuarios.Add(new PuntoVenta.Models.Usuario
            {
                NombreUsuario = "admin",
                Nombre = "Administrador",
                PasswordHash = PasswordHasher.Hash("Admin123*"),
                Activo = true,
                IdRol = rolAdmin.Id
            });
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("ATENCION: no se pudo verificar la base de datos. Ejecute database/puntoventa.sql (o database/actualizacion_roles.sql) y revise la cadena de conexion.");
        Console.WriteLine(ex.Message);
    }
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();   // sirve _framework/blazor.web.js tambien en modo Production
app.MapControllers();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// Direcciones para abrir el sistema (util para la demostracion en red local)
Console.WriteLine();
Console.WriteLine("=== PUNTO DE VENTA ===");
Console.WriteLine("En este equipo:        http://localhost:5000");
foreach (var ip in System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName())
             .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
    Console.WriteLine($"Desde otra PC de red:  http://{ip}:5000");
Console.WriteLine();

app.Run();
