using MediatR;
using FluentValidation;
using AutoMapper;
using Microsoft.OpenApi.Models;
using Serilog;
using VirtualMed.Api.Middleware;
using VirtualMed.Application.Commands.Patients;
using VirtualMed.Application.Common.Behaviors;
using Microsoft.EntityFrameworkCore;
using VirtualMed.Application.Interfaces;
using VirtualMed.Application.Interfaces.Services;
using VirtualMed.Infrastructure.Persistence;
using VirtualMed.Infrastructure.Repositories;
using VirtualMed.Infrastructure.Services;
using VirtualMed.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "VirtualMed API",
        Version = "v1"
    });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Reference = new OpenApiReference
        {
            Id = "Bearer",
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition("Bearer", jwtSecurityScheme);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            jwtSecurityScheme,
            Array.Empty<string>()
        }
    });
});

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreatePatientCommand).Assembly));

builder.Services.AddValidatorsFromAssembly(typeof(CreatePatientCommand).Assembly);

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddAutoMapper(typeof(VirtualMed.Application.Common.Mappings.PatientProfile).Assembly);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Configurar MinIO
builder.Services.Configure<MinioSettings>(builder.Configuration.GetSection("Minio"));

builder.Services.AddScoped<IApplicationDbContext>(provider => 
    provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IMinioService, MinioService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
