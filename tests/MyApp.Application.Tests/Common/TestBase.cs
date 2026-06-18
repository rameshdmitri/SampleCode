namespace MyApp.Application.Tests.Common;
using Moq;
using MyApp.Application.Common.Constants;
using MyApp.Application.Common.Interfaces;

public abstract class TestBase
{
    protected readonly Mock<ICurrentUser> CurrentUserMock = new();

    protected TestBase()
    {
        // Padrão: Customer autenticado
        CurrentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        CurrentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());
        CurrentUserMock.Setup(u => u.IsInRole(AppRoles.Admin)).Returns(false);
        CurrentUserMock.Setup(u => u.IsInRole(AppRoles.Customer)).Returns(true);
        CurrentUserMock.Setup(u => u.IsInRole(AppRoles.Manager)).Returns(false);
        CurrentUserMock.Setup(u => u.IsInRole(AppRoles.Support)).Returns(false);
        CurrentUserMock
            .Setup(u => u.IsOwnerOrAdmin(It.IsAny<Guid>()))
            .Returns<Guid>(id => id == CurrentUserMock.Object.Id);
    }

    protected void SetupAsAdmin()
    {
        CurrentUserMock.Setup(u => u.IsInRole(AppRoles.Admin)).Returns(true);
        CurrentUserMock.Setup(u => u.IsInRole(AppRoles.Customer)).Returns(false);
        CurrentUserMock.Setup(u => u.IsOwnerOrAdmin(It.IsAny<Guid>())).Returns(true);
    }

    protected void SetupAsManager()
    {
        CurrentUserMock.Setup(u => u.IsInRole(AppRoles.Manager)).Returns(true);
        CurrentUserMock.Setup(u => u.IsInRole(AppRoles.Customer)).Returns(false);
    }
}
