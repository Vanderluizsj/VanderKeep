namespace VKeeper.Web.ViewModels;

public sealed class ErrorViewModel
{
    public string? RequestId { get; init; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public int StatusCode { get; init; } = 500;

    public string Title { get; init; } = "Ocorreu um erro inesperado";

    public string Message { get; init; } = "Não foi possível processar sua solicitação no momento. Tente novamente em instantes.";
}
