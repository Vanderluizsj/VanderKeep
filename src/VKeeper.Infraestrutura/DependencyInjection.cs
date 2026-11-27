using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VKeeper.Aplicacao.Comum;
using VKeeper.Dominio.Interfaces;
using VKeeper.Infraestrutura.Persistencia;
using VKeeper.Infraestrutura.Repositorios;
using VKeeper.Infraestrutura.Servicos;

namespace VKeeper.Infraestrutura;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<VKeeperDbContext>((serviceProvider, options) =>
        {
            var resolvedConfiguration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = resolvedConfiguration.GetConnectionString("DefaultConnection") ?? string.Empty;
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<INoteRepository, NoteRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
