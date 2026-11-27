using VKeeper.Dominio.Enums;

namespace VKeeper.Web.Servicos;

public static class NotePriorityDisplay
{
    public static string GetLabel(NotePriority priority) => priority switch
    {
        NotePriority.Low => "Baixa",
        NotePriority.Medium => "Média",
        NotePriority.High => "Alta",
        _ => priority.ToString()
    };

    public static string GetBadgeClass(NotePriority priority) => priority switch
    {
        NotePriority.Low => "text-bg-secondary",
        NotePriority.Medium => "text-bg-warning",
        NotePriority.High => "text-bg-danger",
        _ => "text-bg-secondary"
    };
}
