using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using VKeeper.Aplicacao.Categorias;

namespace VKeeper.Aplicacao;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<ICategoryService, CategoryService>();
        return services;
    }
}
