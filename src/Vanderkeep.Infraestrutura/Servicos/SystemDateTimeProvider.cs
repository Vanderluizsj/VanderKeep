using Vanderkeep.Aplicacao.Comum;

namespace Vanderkeep.Infraestrutura.Servicos;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
