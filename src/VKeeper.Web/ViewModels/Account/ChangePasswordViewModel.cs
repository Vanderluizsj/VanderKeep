using System.ComponentModel.DataAnnotations;

namespace VKeeper.Web.ViewModels.Account;

public sealed class ChangePasswordViewModel
{
    [Required(ErrorMessage = "A senha atual é obrigatória.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha atual")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "A nova senha é obrigatória.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "A nova senha deve ter entre {2} e {1} caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nova senha")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "A confirmação da nova senha é obrigatória.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "A nova senha e a confirmação não conferem.")]
    [Display(Name = "Confirmar nova senha")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
