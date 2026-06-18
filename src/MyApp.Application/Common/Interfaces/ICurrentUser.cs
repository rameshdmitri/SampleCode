namespace MyApp.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid                  Id              { get; }
    string                Email           { get; }
    string                Name            { get; }
    string                UserName        { get; }
    bool                  IsAuthenticated { get; }
    IReadOnlyList<string> Roles           { get; }

    bool IsInRole(string role);
    bool IsOwnerOrAdmin(Guid resourceOwnerId);
}
