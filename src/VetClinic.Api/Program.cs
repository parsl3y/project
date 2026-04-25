using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using VetClinic.Api.Data;
using VetClinic.Api.Exceptions;
using VetClinic.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()));

builder.Services.AddDbContext<VetClinicDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<OwnerService>();
builder.Services.AddScoped<PetService>();
builder.Services.AddScoped<AppointmentService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "VetClinic API",
        Version     = "v1",
        Description = "API для ветеринарної клініки"
    });
});

var app = builder.Build();

// ── 1. Exception handler — ПЕРШИМ ──
app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    var ex = ctx.Features
        .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;

    ctx.Response.ContentType = "application/json";
    ctx.Response.StatusCode = ex switch
    {
        NotFoundException => StatusCodes.Status404NotFound,
        BusinessException => StatusCodes.Status422UnprocessableEntity,
        _                 => StatusCodes.Status500InternalServerError
    };

    // ✅ WriteAsync замість WriteAsJsonAsync — виправляє PipeWriter помилку
    var json = System.Text.Json.JsonSerializer.Serialize(
        new { error = ex?.Message });
    await ctx.Response.WriteAsync(json);
}));

// ── 2. Swagger — тільки не в Testing ──
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ── 3. Міграція — пропускаємо у Testing ──
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<VetClinicDbContext>();
    db.Database.Migrate();
    
    // ── 4. Seeding даних для розробки ──
    if (app.Environment.IsDevelopment())
    {
        await DataSeeder.SeedAsync(db);
    }
}

// ── 4. Контролери — ОСТАННІМИ ──
app.MapControllers();
app.Run();

public partial class Program { }
