# MyApp — Clean Architecture .NET 8

Modelo Enterprise + Features com:
- **ApiControllerBase** → traduz Result em IActionResult via sufixo do Error.Code
- **BaseService** → helpers de Result (Ok/Fail)
- **AuthorizedService** → identidade + guards (EnsureRole, EnsureOwnerOrAdmin), herda BaseService
- **[Authorize(Roles = AppRoles.AdminOrManager)]** → constantes compostas compile-time

## Hierarquia de Services

```
BaseService              (helpers de Result, sem dependências)
   └── AuthorizedService (+ ICurrentUser, guards de autorização)
          ├── OrderService
          ├── CustomerService
          └── EmployeeServices
```

## Mapeamento Error.Code → HTTP (ApiControllerBase)

| Sufixo do Code      | Status HTTP                |
|---------------------|----------------------------|
| `.NotFound`         | 404 Not Found              |
| `.Conflict`         | 409 Conflict               |
| `.Unauthorized`     | 401 Unauthorized           |
| `.Forbidden`        | 403 Forbidden              |
| `.Validation`       | 422 Unprocessable Entity   |
| (outro)             | 400 Bad Request            |
| sucesso com valor   | 200 OK                     |
| sucesso sem valor   | 204 No Content             |

## EmployeeServices — três padrões de uso

1. **GetByIdAsync** → Result<T> tipado + guard de ownership (404/403/200)
2. **DeactivateAsync** → Result comando + guard de role + regra de conflito (403/404/409/204)
3. **GetMyTeamAsync** → filtragem implícita por identidade (Admin vê tudo, Manager vê seu time)

## Estrutura

```
src/
  MyApp.Domain/          → Entities (Order, Customer, Employee), ValueObjects, Interfaces
  MyApp.Application/      → BaseService, AuthorizedService, Services por feature
    Common/Services/      → BaseService.cs, AuthorizedService.cs
    Employees/            → EmployeeServices.cs (3 casos de exemplo)
    Orders/ Customers/ Auth/
  MyApp.Infrastructure/  → EF Core, Identity, UnitOfWork, SimpleMapper (IMapper)
  MyApp.Api/             → ApiControllerBase, Controllers, Program.cs

tests/
  MyApp.Domain.Tests/        → OrderTests
  MyApp.Application.Tests/    → OrderServiceTests, AuthServiceTests, EmployeeServicesTests
```

## Como rodar

1. Ajuste a ConnectionString em `appsettings.json`
2. `dotnet ef database update --project src/MyApp.Infrastructure --startup-project src/MyApp.Api`
3. `dotnet run --project src/MyApp.Api`
4. Swagger: https://localhost:5001/swagger
5. Testes: `dotnet test`

## Login admin padrão

- Email: admin@myapp.com
- Senha: Admin@123!
