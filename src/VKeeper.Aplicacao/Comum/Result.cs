namespace VKeeper.Aplicacao.Comum;

public class Result
{
    protected Result(bool isSuccess, ErrorType errorType, IReadOnlyList<string> errors)
    {
        if (isSuccess && (errorType != ErrorType.None || errors.Count > 0))
        {
            throw new InvalidOperationException("A successful result cannot contain errors.");
        }

        if (!isSuccess && (errorType == ErrorType.None || errors.Count == 0))
        {
            throw new InvalidOperationException("A failed result must contain an error type and at least one error message.");
        }

        IsSuccess = isSuccess;
        ErrorType = errorType;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public ErrorType ErrorType { get; }

    public IReadOnlyList<string> Errors { get; }

    public string Error => Errors.Count > 0 ? Errors[0] : string.Empty;

    public static Result Success() => new(true, ErrorType.None, Array.Empty<string>());

    public static Result Failure(string error, ErrorType errorType = ErrorType.Failure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        return new Result(false, errorType, new[] { error.Trim() });
    }

    public static Result ValidationFailure(IEnumerable<string> errors)
    {
        var errorList = NormalizeValidationErrors(errors);
        return new Result(false, ErrorType.Validation, errorList);
    }

    public static Result NotFound(string error) => Failure(error, ErrorType.NotFound);

    public static Result Conflict(string error) => Failure(error, ErrorType.Conflict);

    public static Result Forbidden(string error) => Failure(error, ErrorType.Forbidden);

    protected static string[] NormalizeValidationErrors(IEnumerable<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var errorList = errors
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (errorList.Length == 0)
        {
            throw new ArgumentException("At least one validation error must be provided.", nameof(errors));
        }

        return errorList;
    }
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value)
        : base(true, ErrorType.None, Array.Empty<string>())
    {
        _value = value;
    }

    private Result(ErrorType errorType, IReadOnlyList<string> errors)
        : base(false, errorType, errors)
    {
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed result.");

    public static Result<T> Success(T value) => new(value);

    public new static Result<T> Failure(string error, ErrorType errorType = ErrorType.Failure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        return new Result<T>(errorType, new[] { error.Trim() });
    }

    public new static Result<T> ValidationFailure(IEnumerable<string> errors)
    {
        var errorList = NormalizeValidationErrors(errors);
        return new Result<T>(ErrorType.Validation, errorList);
    }

    public new static Result<T> NotFound(string error) => Failure(error, ErrorType.NotFound);

    public new static Result<T> Conflict(string error) => Failure(error, ErrorType.Conflict);

    public new static Result<T> Forbidden(string error) => Failure(error, ErrorType.Forbidden);
}
