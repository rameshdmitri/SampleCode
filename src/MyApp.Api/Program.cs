using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MyApp.Api.Middleware;
using MyApp.Application.Auth.Interfaces;
using MyApp.Application.Common.Constants;
using MyApp.Application.Customers.Interfaces;
using MyApp.Application.Customers.Services;
using MyApp.Application.Employees.Interfaces;
using MyApp.Application.Employees.Services;
using MyApp.Application.Orders.Interfaces;
using MyApp.Application.Orders.Services;
using MyApp.Application.Orders.Validators;
using MyApp.Infrastructure;
using MyApp.Infrastructure.Configuration;
using MyApp.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;

// Infrastructure (Identity + EF Core + UoW + Repositórios + Mapper)
builder.Services.AddInfrastructure(builder.Configuration);

// Application Services
builder.Services.AddScoped<IAuthService,      AuthService>();
builder.Services.AddScoped<IOrderService,     OrderService>();
builder.Services.AddScoped<ICustomerService,  CustomerService>();
builder.Services.AddScoped<IEmployeeServices, EmployeeServices>();
builder.Services.AddScoped<CreateOrderValidator>();

// JWT Bearer
builder.Services
    .AddAuthentication(opts =>
    {
        opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opts.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer      = jwtSettings.Issuer,
            ValidAudience    = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            // CRÍTICO: mapeia o claim 'role' para [Authorize(Roles = ...)]
            RoleClaimType = AppClaimTypes.Role,
            NameClaimType = AppClaimTypes.Name,
            ClockSkew     = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new OpenApiInfo { Title = "MyApp API", Version = "v1" });
    opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header,
        Description = "Informe: Bearer {token}"
    });
    opts.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            []
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
