namespace MyApp.Application.Common.Services;
using MyApp.Application.Common.Models;

/// <summary>
/// Classe base para todos os Application Services.
/// Contém apenas helpers de criação de Result — nenhuma dependência.
/// </summary>
public abstract class BaseService
{
    protected static Result    Ok()                          => Result.Success();
    protected static Result<T> Ok<T>(T value)                => Result.Success(value);

    protected static Result    Fail(Error error)             => Result.Failure(error);
    protected static Result    Fail(string code, string msg) => Result.Failure(new Error(code, msg));

    protected static Result<T> Fail<T>(Error error)          => Result.Failure<T>(error);
    protected static Result<T> Fail<T>(string code, string msg) =>
        Result.Failure<T>(new Error(code, msg));
}
