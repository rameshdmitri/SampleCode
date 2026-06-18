namespace MyApp.Application.Common.Models;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)  throw new InvalidOperationException();
        if (!isSuccess && error == Error.None) throw new InvalidOperationException();
        IsSuccess = isSuccess;
        Error     = error;
    }

    public bool  IsSuccess { get; }
    public bool  IsFailure => !IsSuccess;
    public Error Error     { get; }

    public static Result         Success()                 => new(true, Error.None);
    public static Result         Failure(Error error)      => new(false, error);
    public static Result<TValue> Success<TValue>(TValue v) => new(v, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error e)  => new(default, false, e);
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error)
        => _value = value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failure result.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
