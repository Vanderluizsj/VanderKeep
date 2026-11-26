using Vanderkeep.Dominio.Interfaces;

namespace Vanderkeep.Infraestrutura.Persistencia;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly VanderkeepDbContext _dbContext;

    public UnitOfWork(VanderkeepDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
