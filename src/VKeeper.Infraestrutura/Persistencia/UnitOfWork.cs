using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VKeeper.Dominio.Excecoes;
using VKeeper.Dominio.Interfaces;

namespace VKeeper.Infraestrutura.Persistencia;

public sealed class UnitOfWork : IUnitOfWork
{
    private const int SqlServerUniqueIndexViolation = 2601;
    private const int SqlServerUniqueConstraintViolation = 2627;

    private readonly VKeeperDbContext _dbContext;

    public UnitOfWork(VKeeperDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new UniqueConstraintViolationException(
                "A unique constraint violation occurred while saving changes.",
                ex);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException &&
               (sqlException.Number == SqlServerUniqueIndexViolation ||
                sqlException.Number == SqlServerUniqueConstraintViolation);
    }
}
