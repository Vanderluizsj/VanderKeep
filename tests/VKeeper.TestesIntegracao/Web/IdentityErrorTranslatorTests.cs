using Microsoft.AspNetCore.Identity;
using VKeeper.Web.Servicos;

namespace VKeeper.TestesIntegracao.Web;

public sealed class IdentityErrorTranslatorTests
{
    [Fact]
    public void Translate_WhenDuplicateUserNameAndDuplicateEmailArePresent_ShouldEmitSingleEmailError()
    {
        var errors = new[]
        {
            new IdentityError { Code = nameof(IdentityErrorDescriber.DuplicateUserName), Description = "dup user" },
            new IdentityError { Code = nameof(IdentityErrorDescriber.DuplicateEmail), Description = "dup email" }
        };

        var translated = IdentityErrorTranslator.Translate(errors);

        Assert.Single(translated);
        Assert.Equal("Já existe uma conta cadastrada com este e-mail.", translated[0]);
    }

    [Fact]
    public void Translate_WithPasswordPolicyErrors_ShouldReturnPortugueseMessages()
    {
        var errors = new[]
        {
            new IdentityError { Code = nameof(IdentityErrorDescriber.PasswordTooShort) },
            new IdentityError { Code = nameof(IdentityErrorDescriber.PasswordRequiresDigit) },
            new IdentityError { Code = nameof(IdentityErrorDescriber.PasswordRequiresUpper) },
            new IdentityError { Code = nameof(IdentityErrorDescriber.PasswordMismatch) }
        };

        var translated = IdentityErrorTranslator.Translate(errors);

        Assert.Equal(4, translated.Count);
        Assert.Contains("A senha deve ter pelo menos 8 caracteres.", translated);
        Assert.Contains("A senha deve conter pelo menos um número.", translated);
        Assert.Contains("A senha deve conter pelo menos uma letra maiúscula.", translated);
        Assert.Contains("A senha atual informada está incorreta.", translated);
    }

    [Fact]
    public void Translate_WithUnknownErrorCode_ShouldReturnGenericFriendlyMessage()
    {
        var errors = new[]
        {
            new IdentityError { Code = "UnknownCustomCode", Description = "internal detail" }
        };

        var translated = IdentityErrorTranslator.Translate(errors);

        Assert.Single(translated);
        Assert.DoesNotContain("internal detail", translated[0]);
    }
}
