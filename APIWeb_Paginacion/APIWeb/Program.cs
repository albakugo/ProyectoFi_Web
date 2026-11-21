using Microsoft.EntityFrameworkCore;
using APIWeb.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ContextoBD>(options => options.UseMySQL(builder.Configuration.GetConnectionString("ConexionMySQL")));



// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        //builder.WithOrigins("http://127.0.0.1:5501")
        builder.AllowAnyOrigin() //sugerida para que cualquier IP consuma los servicios
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

var app = builder.Build();

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

app.UseAuthorization();

app.MapControllers();
app.UseCors();
app.Run();
