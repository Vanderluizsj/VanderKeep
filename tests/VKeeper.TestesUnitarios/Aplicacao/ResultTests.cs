using VKeeper.Aplicacao.Comum;

namespace VKeeper.TestesUnitarios.Aplicacao;

public sealed class ResultTests
{
    [Fact]
    public void Success_ShouldSetSuccessFlagsAndNoErrors()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Empty(result.Errors);
        Assert.Equal(string.Empty, result.Error);
    }

    [Fact]
    public void GenericSuccess_ShouldExposeValue()
    {
        var result = Result<string>.Success("VKeeper");

        Assert.True(result.IsSuccess);
        Assert.Equal("VKeeper", result.Value);
    }

    [Fact]
    public void GenericFailure_WhenAccessingValue_ShouldThrowInvalidOperationException()
    {
        var result = Result<string>.NotFound("Nota não encontrada.");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Nota não encontrada.", result.Error);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void ValidationFailure_ShouldTrimDeduplicateAndIgnoreBlankErrors()
    {
        var result = Result.ValidationFailure(new[] { " Título obrigatório. ", " ", "Título obrigatório.", "Prioridade inválida." });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("Título obrigatório.", result.Errors);
        Assert.Contains("Prioridade inválida.", result.Errors);
    }

    [Fact]
    public void ConflictAndForbidden_ShouldPreserveExpectedErrorType()
    {
        var conflict = Result.Conflict("Categoria duplicada.");
        var forbidden = Result<int>.Forbidden("Acesso negado.");

        Assert.Equal(ErrorType.Conflict, conflict.ErrorType);
        Assert.Equal(ErrorType.Forbidden, forbidden.ErrorType);
    }
}
