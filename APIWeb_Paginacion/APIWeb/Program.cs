using Microsoft.EntityFrameworkCore;
using APIWeb.Data;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ContextoBD>(options => options.UseMySQL(builder.Configuration.GetConnectionString("ConexionMySQL")));



// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


//seguridad
builder.Services.AddCors(options =>
{
    options.AddPolicy("PoliticaSegura", policy =>
    {
        //solo acepta la direccion origins
        policy.WithOrigins("http://127.0.0.1:5500")
                //permite POST, GET..
              .AllowAnyMethod()
              //permite jwt autorizacion 
              .AllowAnyHeader();
    });
});


// Configuración de autenticación JWT
//busca la clave en .json
var claveSecreta = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("No se encontró la clave JWT.");


builder.Services.AddAuthentication(options =>
{
    //indica utilizar JWT para identificar
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    //indica JWT seera elmecanismo
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})

    //valida el token 
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        //quien emitio
        ValidateIssuer = true,
        //para quien
        ValidateAudience = true,
        //que no expire token
        ValidateLifetime = true,
        //que sea valido
        ValidateIssuerSigningKey = true,

        //validar que si coincidan 
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],

        //comprobacion
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(claveSecreta)
        ),
        //evita que tenga tiempo demas despues de que expire
        ClockSkew = TimeSpan.Zero,

        //que tipo de claim tiene el rol
        RoleClaimType = ClaimTypes.Role,
    };
});


builder.Services.AddMemoryCache();

var app = builder.Build();

//capturar excepciones no controladas
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Ocurrió un error interno en la API.");

        if (context.Response.HasStarted)
        {
            throw;
        }

        //limpiar, devolver JSON
        context.Response.Clear();
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            mensaje = "Ocurrió un error interno. Intenta nuevamente."
        });
    }
});



//evitar otras paginas entren a la API o engaños
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append(
        "Content-Security-Policy",
        "frame-ancestors 'none';"
    );

    await next();
});



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) 
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI( c =>
    {        
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mi API");
        //Indicar la raiz como URL que responderá con la documentación de swagger
        c.RoutePrefix = "";
    });
}
app.UseHttpsRedirection();

app.UseCors("PoliticaSegura");

app.UseAuthentication(); //identifica quien esta realizando peticion

app.UseAuthorization(); //comprueba si el usuario tiene permiso

app.MapControllers();
app.Run();