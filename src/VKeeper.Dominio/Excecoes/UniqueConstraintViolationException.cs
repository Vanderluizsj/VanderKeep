namespace VKeeper.Dominio.Excecoes;

public sealed class UniqueConstraintViolationException : Exception
{
    public UniqueConstraintViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
