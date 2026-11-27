namespace VKeeper.Aplicacao.Comum;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
