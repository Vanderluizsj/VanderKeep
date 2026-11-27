using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using VKeeper.Aplicacao.Categorias;
using VKeeper.Aplicacao.Notas;

namespace VKeeper.Aplicacao;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<INoteService, NoteService>();
        return services;
    }
}
