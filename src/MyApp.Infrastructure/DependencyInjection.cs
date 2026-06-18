namespace MyApp.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Application.Common.Interfaces;
using MyApp.Domain.Interfaces;
using MyApp.Infrastructure.Identity;
using MyApp.Infrastructure.Persistence;
using MyApp.Infrastructure.Repositories;
using MyApp.Infrastructure.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("Default")));

        services.AddIdentity<AppUser, IdentityRole<Guid>>(opts =>
            {
                opts.Password.RequiredLength         = 8;
                opts.Password.RequireDigit           = true;
                opts.Password.RequireUppercase       = true;
                opts.Password.RequireNonAlphanumeric = true;
                opts.Lockout.MaxFailedAccessAttempts = 5;
                opts.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(15);
                opts.User.RequireUniqueEmail         = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser,     CurrentUserService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IMapper,          SimpleMapper>();

        // UnitOfWork expõe os repositórios — registramos só o UoW
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositórios individuais (para serviços que os injetam direto)
        services.AddScoped<IOrderRepository,    OrderRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();

        return services;
    }
}
