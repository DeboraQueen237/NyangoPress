namespace NyangoPress.Application.Common;

/// <summary>
/// Résultat d'un cas d'usage : succès ou <see cref="Error"/>. Les erreurs prévisibles passent par ici,
/// les exceptions sont réservées aux cas inattendus.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null && error != Error.None)
        {
            throw new InvalidOperationException("Un résultat réussi ne peut pas porter d'erreur.");
        }

        if (!isSuccess && (error is null || error == Error.None))
        {
            throw new InvalidOperationException("Un résultat en échec doit porter une erreur.");
        }

        IsSuccess = isSuccess;
        Error = error ?? Error.None;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, null);

    public static Result<T> Failure<T>(Error error) => new(default, false, error);

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, Error? error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>Valeur du résultat. Lève une exception si le résultat est en échec : testez IsSuccess d'abord.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Impossible de lire la valeur d'un résultat en échec.");

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure<T>(error);
}
