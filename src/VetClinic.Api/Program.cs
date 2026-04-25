using Microsoft.EntityFrameworkCore;
using VetClinic.Api.Data;
using VetClinic.Api.Exceptions;
using VetClinic.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddDbContext<VetClinicDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<OwnerService>();
builder.Services.AddScoped<PetService>();
builder.Services.AddScoped<AppointmentService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "VetClinic API", 
        Version = "v1",
        Description = "API для ветеринарної клініки"
    });
});

var app = builder.Build();

app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    var ex = ctx.Features
        .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;

    ctx.Response.ContentType = "application/json";
    ctx.Response.StatusCode = ex switch
    {
        NotFoundException    => StatusCodes.Status404NotFound,
        BusinessException    => StatusCodes.Status422UnprocessableEntity,
        _                    => StatusCodes.Status500InternalServerError
    };

    await ctx.Response.WriteAsJsonAsync(new { error = ex?.Message });
}));

app.UseHttpsRedirection();

app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.Run();
