using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vanderkeep.Aplicacao.Comum;
using Vanderkeep.Dominio.Interfaces;
using Vanderkeep.Infraestrutura.Persistencia;
using Vanderkeep.Infraestrutura.Repositorios;
using Vanderkeep.Infraestrutura.Servicos;

namespace Vanderkeep.Infraestrutura;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

        services.AddDbContext<VanderkeepDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<INoteRepository, NoteRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
