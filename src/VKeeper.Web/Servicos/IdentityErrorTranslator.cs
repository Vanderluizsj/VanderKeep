using Microsoft.AspNetCore.Identity;

namespace VKeeper.Web.Servicos;

public static class IdentityErrorTranslator
{
    public static IReadOnlyList<string> Translate(IEnumerable<IdentityError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var errorList = errors.ToList();
        var hasDuplicateEmail = errorList.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateEmail));

        var translated = new List<string>();
        foreach (var error in errorList)
        {
            // Since UserName mirrors Email, avoid showing two duplicate-account messages.
            if (error.Code == nameof(IdentityErrorDescriber.DuplicateUserName) && hasDuplicateEmail)
            {
                continue;
            }

            var message = error.Code switch
            {
                nameof(IdentityErrorDescriber.DuplicateEmail) => "Já existe uma conta cadastrada com este e-mail.",
                nameof(IdentityErrorDescriber.DuplicateUserName) => "Já existe uma conta cadastrada com este e-mail.",
                nameof(IdentityErrorDescriber.InvalidEmail) => "Informe um endereço de e-mail válido.",
                nameof(IdentityErrorDescriber.PasswordTooShort) => "A senha deve ter pelo menos 8 caracteres.",
                nameof(IdentityErrorDescriber.PasswordRequiresDigit) => "A senha deve conter pelo menos um número.",
                nameof(IdentityErrorDescriber.PasswordRequiresLower) => "A senha deve conter pelo menos uma letra minúscula.",
                nameof(IdentityErrorDescriber.PasswordRequiresUpper) => "A senha deve conter pelo menos uma letra maiúscula.",
                nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric) => "A senha deve conter pelo menos um caractere especial.",
                nameof(IdentityErrorDescriber.PasswordMismatch) => "A senha atual informada está incorreta.",
                _ => "Não foi possível concluir a operação. Verifique os dados informados."
            };

            if (!translated.Contains(message, StringComparer.Ordinal))
            {
                translated.Add(message);
            }
        }

        return translated;
    }
}
