using VKeeper.Aplicacao.Comum;

namespace VKeeper.Infraestrutura.Servicos;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
