namespace MyApp.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Common.Models;

/// <summary>
/// Controller base que traduz Result/Result&lt;T&gt; em IActionResult.
/// O sufixo do Error.Code determina o status HTTP devolvido.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : MapFailure(result.Error);

    protected IActionResult HandleResult(Result result) =>
        result.IsSuccess ? NoContent() : MapFailure(result.Error);

    private IActionResult MapFailure(Error error) => error.Code switch
    {
        var c when c.EndsWith(".NotFound")     => NotFound(new { error.Code, error.Description }),
        var c when c.EndsWith(".Conflict")     => Conflict(new { error.Code, error.Description }),
        var c when c.EndsWith(".Unauthorized") => Unauthorized(new { error.Code, error.Description }),
        var c when c.EndsWith(".Forbidden")    => StatusCode(StatusCodes.Status403Forbidden, new { error.Code, error.Description }),
        var c when c.EndsWith(".Validation")   => UnprocessableEntity(new { error.Code, error.Description }),
        _                                      => BadRequest(new { error.Code, error.Description })
    };
}
