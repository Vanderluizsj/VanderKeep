namespace Vanderkeep.Aplicacao.Comum;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }
}
