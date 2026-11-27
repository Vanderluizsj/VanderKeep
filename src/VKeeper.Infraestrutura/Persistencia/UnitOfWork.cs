using VKeeper.Dominio.Interfaces;

namespace VKeeper.Infraestrutura.Persistencia;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly VKeeperDbContext _dbContext;

    public UnitOfWork(VKeeperDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
