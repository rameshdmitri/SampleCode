namespace MyApp.Application.Auth.Errors;
using MyApp.Application.Common.Models;

public static class AuthErrors
{
    // Os sufixos (.Unauthorized, .Forbidden, .Conflict, .NotFound)
    // são interpretados pelo ApiControllerBase para mapear o status HTTP.
    public static readonly Error Unauthenticated    = new("Auth.Unauthorized", "User is not authenticated.");
    public static readonly Error Forbidden          = new("Auth.Forbidden",    "You do not have permission to perform this action.");
    public static readonly Error InvalidCredentials = new("Auth.Unauthorized", "Invalid email or password.");
    public static readonly Error EmailAlreadyInUse  = new("Auth.Conflict",     "This email is already registered.");
    public static readonly Error RoleNotFound       = new("Auth.NotFound",     "The specified role does not exist.");
    public static readonly Error AccountLocked      = new("Auth.Forbidden",    "Account locked. Try again later.");
}
