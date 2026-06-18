# Constitution — MyApp

Manual técnico de regras permanentes extraído do código de produção desta solução. Este documento é a fonte de verdade arquitetural para todas as tarefas futuras neste repositório. As regras descrevem o que **já existe** em `D:\Lab\MyApp` — não um estado desejado.

> Stack: **.NET 8 / ASP.NET Core**, EF Core 8 + SQL Server, ASP.NET Core Identity + JWT Bearer, FluentValidation, xUnit. Domínio de exemplo: pedidos (`Order`), clientes (`Customer`) e funcionários (`Employee`).

---

## 1. Arquitetura & Camadas

### Estrutura da Solução (4 projetos + 2 de teste)

```
MyApp.Api             → Host / composition root da API (ASP.NET Core 8). Refere Application + Infrastructure.
  ├── MyApp.Application    → Serviços, DTOs, Result/Error, validators, catálogos de erro. Refere Domain.
  │     └── MyApp.Domain   → Entidades, value objects, interfaces de repositório, eventos (zero dependências externas).
  └── MyApp.Infrastructure → EF Core, Identity, UnitOfWork, repositórios, JWT, mapper. Refere Application + Domain.

tests/MyApp.Domain.Tests       → Testes de domínio puro (xUnit).
tests/MyApp.Application.Tests   → Testes de serviço com mocks (xUnit + Moq + FluentAssertions; refere Application + Infrastructure).
```

### Regra de Dependência (INVIOLÁVEL)

- `MyApp.Domain` não tem dependências externas (sem EF Core, sem Identity, sem pacotes de infraestrutura).
- `MyApp.Application` refere **somente** `MyApp.Domain`. **Nunca** refere `MyApp.Infrastructure`.
- `MyApp.Infrastructure` refere `Application` + `Domain` e implementa as interfaces declaradas na Application.
- `MyApp.Api` é o único ponto que conhece todas as camadas (composição/DI).
- **Consequência prática:** toda implementação que depende de tipos de infraestrutura (Identity `UserManager`/`SignInManager`, `HttpContext`, EF) mora na Infrastructure, com a **interface** declarada na Application. Exemplos: `IJwtTokenService`→`JwtTokenService`, `ICurrentUser`→`CurrentUserService`, `IAuthService`→`AuthService`, `IMapper`→`SimpleMapper`.

---

## 2. Entidades de Domínio

### Regras Obrigatórias

- Toda entidade herda de `BaseEntity` (`Domain/Common/BaseEntity.cs`): `Guid Id` (default `Guid.NewGuid()`), lista de domain events (`RaiseDomainEvent`, `DomainEvents`, `ClearDomainEvents`).
- Raízes de agregação implementam o marcador `IAggregateRoot` (`Domain/Common/IAggregateRoot.cs`). Hoje: `Order`, `Customer`, `Employee`.
- **Domínio rico:** propriedades com `private set`, construtor privado sem parâmetros (para o EF) + construtor de fábrica que valida invariantes, e métodos que expressam comportamento (`Order.Confirm()`, `Order.Cancel()`, `Customer.Update()`, `Customer.Deactivate()`).
- Invariantes violadas lançam **`DomainException`** (`Domain/Exceptions/DomainException.cs`) — nunca `Exception` genérica.
- Eventos de domínio são `record` em `Domain/Events/` (`OrderCreated`, `OrderCancelled`), disparados via `RaiseDomainEvent` dentro da entidade.

### Value Objects

- Herdam de `ValueObject` (`Domain/Common/ValueObject.cs`) e implementam `GetEqualityComponents()`; igualdade estrutural e operadores `==`/`!=` já vêm da base.
- `Money` (`Domain/ValueObjects/Money.cs`): `Amount` + `Currency` (default `"BRL"`), imutável, com `Add`/`Multiply`; rejeita valor negativo e soma de moedas diferentes lançando exceção. Possui ctor privado para o EF.

### Convenções de Chave

| Uso | Tipo |
|-----|------|
| Chave primária de entidade de domínio | `Guid` (em `BaseEntity.Id`) |
| Chave do Identity (`AppUser`, `IdentityRole`) | `Guid` (`IdentityUser<Guid>` / `IdentityRole<Guid>`) |
| Identificador de negócio sequencial | `int` (`Employee.EmployeeNumber`, `ValueGeneratedOnAdd` + índice único) |

### Enums

- Enums de domínio em `Domain/Enums/` (`OrderStatus`: `Pending, Confirmed, Shipped, Delivered, Cancelled`). Persistidos **como string** no EF (`HasConversion<string>()`).

---

## 3. Repositórios e Unit of Work

### Hierarquia

```
Domain/Interfaces/I*Repository           ← contrato por entidade (CRUD + métodos customizados)
  └── Infrastructure/Repositories/BaseRepository<T>   ← CRUD genérico (where T : BaseEntity)
        └── Infrastructure/Repositories/*Repository   ← implementação concreta : BaseRepository<T>, I*Repository
```

- As interfaces de repositório ficam em `MyApp.Domain/Interfaces/` (`IOrderRepository`, `ICustomerRepository`, `IEmployeeRepository`). Cada uma declara seu próprio CRUD assíncrono + métodos específicos (ex.: `IOrderRepository.GetByCustomerIdAsync`, `IEmployeeRepository.GetByManagerUserIdAsync`).
- `BaseRepository<T>` (Infrastructure) provê `GetByIdAsync/GetAllAsync/AddAsync/UpdateAsync/DeleteAsync`, todos `async` com `CancellationToken`. As implementações concretas herdam dela e adicionam as queries customizadas.
- **Nomenclatura de métodos customizados:** `GetBy[Critério]Async` (ex.: `GetByCustomerIdAsync`, `GetByManagerUserIdAsync`).
- Nunca instanciar repositório diretamente em serviço de produção — sempre injetar via interface.

### Unit of Work

- `IUnitOfWork` (`Application/Common/Interfaces/IUnitOfWork.cs`) expõe os repositórios como propriedades (`Employees`, `Orders`, `Customers`) + `CommitAsync(ct)` e `SaveAsync(ct)`.
- Implementação `UnitOfWork` (`Infrastructure/Persistence/UnitOfWork.cs`) instancia os repositórios no construtor e delega a `DbContext.SaveChangesAsync`.
- Em serviços, **nunca** acesse `AppDbContext` diretamente — use `IUnitOfWork` ou a interface de repositório injetada.

```csharp
// CORRETO
public sealed class OrderService(IOrderRepository repository, IUnitOfWork unitOfWork, ...) { ... }
await unitOfWork.CommitAsync(ct);
```

---

## 4. Camada de Aplicação — Serviços

### Regras Estruturais

- Cada serviço tem **interface** em `Application/<Feature>/Interfaces/` + **implementação** em `Application/<Feature>/Services/`. (Exceção: `IAuthService` tem interface na Application e implementação na Infrastructure, por depender do Identity.)
- **Toda implementação herda `BaseService`** (`Application/Common/Services/BaseService.cs`) — diretamente, ou via `AuthorizedService` quando precisa da identidade do usuário corrente.
- Operações assíncronas que podem falhar retornam `Result` / `Result<T>` — **nunca lançam exceptions para falhas esperadas**.
- Registro em `Program.cs` como `AddScoped` (ver seção 14).

### BaseService

Helpers de criação de `Result`, sem dependências:

```csharp
protected static Result    Ok();
protected static Result<T> Ok<T>(T value);
protected static Result    Fail(Error error);
protected static Result    Fail(string code, string msg);
protected static Result<T> Fail<T>(Error error);
protected static Result<T> Fail<T>(string code, string msg);
```

### AuthorizedService (identidade do usuário corrente)

Base **opt-in** (`Application/Common/Services/AuthorizedService.cs`) que estende `BaseService` e recebe `ICurrentUser` por construtor primário:

```csharp
public abstract class AuthorizedService(ICurrentUser currentUser) : BaseService
{
    protected Guid   CurrentUserId   => currentUser.Id;     // Guid (não string)
    protected string CurrentUserEmail => currentUser.Email;
    protected bool   IsAuthenticated  => currentUser.IsAuthenticated;

    protected bool IsAdmin, IsManager, IsSupport, IsCustomer, IsAdminOrManager;  // via IsInRole

    protected Result EnsureAuthenticated();         // → AuthErrors.Unauthenticated (401)
    protected Result EnsureRole(string role);       // → AuthErrors.Forbidden (403)
    protected Result EnsureOwnerOrAdmin(Guid ownerId); // → AuthErrors.Forbidden (403)
}
```

Regras:

- Serviços que não precisam de identidade (não há, hoje, mas o padrão existe) herdam `BaseService` direto.
- `CurrentUserId` substitui parâmetros de identidade vindos da controller — a controller **não** extrai claims.
- Guards devolvem um `Result` pronto. Padrão de propagação:

```csharp
var guard = EnsureRole(AppRoles.Admin);
if (guard.IsFailure) return guard;                  // comando → propaga o Result
// ou, em Result<T>:
if (guard.IsFailure) return Fail<OrderDto>(guard.Error);
```

- Coleção filtrada por identidade que resulta vazia retorna `Ok([])` → `200 []`, nunca `Fail`. O 404 é reservado a recurso individual inexistente (ver `OrderService.GetMyOrdersAsync` vs `GetByIdAsync`).

### Três padrões de autorização no serviço (referência: `EmployeeServices`)

1. **`Result<T>` + guard de ownership** — `GetByIdAsync`: 404 se não existe, 403 se não é dono nem Admin/Manager, senão 200.
2. **`Result` (comando) + guard de role + regra de negócio** — `DeactivateAsync`: `EnsureRole(Admin)` (403), 404, conflito de regra (409), senão 204; auditoria via `entity.UpdatedBy = CurrentUserId`.
3. **Filtragem implícita por identidade** — `GetMyTeamAsync`: sem guard; a identidade muda **o que** retorna (Admin vê tudo, Manager vê o time), não **se** é permitido.

### Validação de Entrada

- Regras de input usam **FluentValidation** (`AbstractValidator<TRequest>` em `Application/<Feature>/Validators/`, ex.: `CreateOrderValidator`).
- O validator é **injetado e chamado explicitamente** dentro do serviço; a falha vira `Fail("<Feature>.Validation", mensagensConcatenadas)` → 422.

```csharp
var validation = await validator.ValidateAsync(request, ct);
if (!validation.IsValid)
    return Fail<Guid>("Order.Validation", string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage)));
```

- `ValidationException` (`Application/Common/Exceptions/`) existe (agrupa falhas em dicionário) mas o fluxo padrão é via `Result` — exceptions ficam para erros realmente inesperados.

---

## 5. Result Pattern (Railway-Oriented)

**Localização:** `Application/Common/Models/` (`Result.cs`, `Error.cs`).

```csharp
// Error é sealed record; o Code segue "Dominio.TipoErro"
public sealed record Error(string Code, string Description)
{
    public static readonly Error None      = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.");
}

// No serviço — sempre via helpers do BaseService
return Ok(dto);                                 // Result<T> sucesso
return Fail<OrderDto>(OrderErrors.NotFound);    // Result<T> falha do catálogo
return Ok();                                    // Result (comando) sucesso

// Result<T> tem conversão implícita de TValue, mas prefira Ok()/Fail()
```

- `IsSuccess` / `IsFailure` para ramificar fluxo. Acessar `.Value` só após confirmar `IsSuccess` (lança `InvalidOperationException` caso contrário).
- **Nunca** chamar `Result.Success()`/`Result.Failure()` diretamente no serviço — use os helpers de `BaseService`.

---

## 6. Catálogo de Erros & Mapeamento HTTP

- Cada feature tem um arquivo de erros estáticos em `Application/<Feature>/Errors/` (`AuthErrors`, `OrderErrors`, `CustomerErrors`, `EmployeeErrors`).
- Erros são `static readonly Error` nomeados **ou** factories quando precisam de parâmetro (`EmployeeErrors.NotFound(int id)`), reutilizáveis entre serviços e testes.
- **O sufixo do `Error.Code` é load-bearing:** `ApiControllerBase` o traduz em status HTTP. Definir o código com o sufixo correto não é opcional.

```csharp
public static class OrderErrors
{
    public static readonly Error NotFound     = new("Order.NotFound",   "Order not found.");      // → 404
    public static readonly Error CannotCancel = new("Order.Conflict",   "Order cannot be ...");   // → 409
    public static readonly Error EmptyItems   = new("Order.Validation", "Order must have ...");   // → 422
}
```

| Sufixo do `Code` | HTTP | Caso de sucesso |
|---|---|---|
| `.NotFound` | 404 | `Result<T>` → **200 OK** (com value) |
| `.Conflict` | 409 | `Result` (sem value) → **204 No Content** |
| `.Unauthorized` | 401 | |
| `.Forbidden` | 403 | |
| `.Validation` | 422 | |
| (qualquer outro) | 400 | |

---

## 7. DTOs

### Estrutura

- DTOs ficam por feature em `Application/<Feature>/DTOs/`.
- São `record` imutáveis. DTOs de resposta expõem uma factory estática `FromEntity(...)` para projeção (ex.: `OrderDto.FromEntity`, `EmployeeResponseDto.FromEntity`).
- Convenção de nome atual (mista, preservar o existente): respostas usam sufixo `Dto` (`OrderDto`, `OrderItemDto`, `CustomerDto`, `EmployeeResponseDto`); requests usam sufixo de intenção (`LoginRequest`, `RegisterRequest`, `CreateOrderRequest`, `UpdateOrderRequest`, `UpdateCustomerRequest`, `CreateEmployeeRequest`); `LoginResponse` é resposta sem `Dto`.

```csharp
public sealed record OrderDto(Guid Id, Guid CustomerId, OrderStatus Status, decimal Total,
    DateTime CreatedAt, IReadOnlyList<OrderItemDto> Items)
{
    public static OrderDto FromEntity(Order o) => new(o.Id, o.CustomerId, o.Status, o.Total.Amount, o.CreatedAt, ...);
}
```

### Paginação

`PagedResult<T>` (`Common/Models/`): `Items`, `Page`, `PageSize`, `TotalCount`, `TotalPages` (computado), `HasNext`/`HasPrev` (computados).

---

## 8. Mapeamento de Objetos

- Contrato `IMapper` (`Application/Common/Interfaces/IMapper.cs`) injetado por construtor; implementação `SimpleMapper` (`Infrastructure/Services/SimpleMapper.cs`).
- `SimpleMapper` é manual (baseado em `switch` por tipo) e hoje cobre apenas `Employee`/`IEnumerable<Employee>`. **Ao adicionar um novo mapeamento, adicione um `arm` ao `switch`** — caso contrário lança `NotSupportedException`.
- Estado atual: AutoMapper/Mapster **não** são usados (o próprio `SimpleMapper` documenta a intenção de substituí-lo futuramente). DTOs de resposta também oferecem `FromEntity`, usado diretamente em vários serviços sem passar pelo `IMapper`.

---

## 9. Autenticação & JWT

### Identity

- `AppUser : IdentityUser<Guid>` (`Infrastructure/Identity/AppUser.cs`) com `FullName`, `CreatedAt`, `IsActive`.
- `AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>` — **chaves `Guid`**.
- **Política de senha** (em `AddInfrastructure`): `RequiredLength = 8`, `RequireDigit`, `RequireUppercase`, `RequireNonAlphanumeric` = true; lockout 5 tentativas / 15 min; `RequireUniqueEmail`. (Mais forte que o mínimo do ASP.NET — não enfraquecer.)
- **Seed** (`IdentitySeeder`, roda no startup): cria roles `Admin/Manager/Support/Customer` e o admin padrão `admin@myapp.com` / `Admin@123!`.

### Token

- `IJwtTokenService.GenerateAsync(Guid userId, string email, string name, IEnumerable<string> roles)` (interface na Application) — impl `JwtTokenService` (Infrastructure): HS256, claims `sub`/`email`/`name`/`jti` + um claim `role` por papel, expiração de `JwtSettings.ExpirationHours` (8h).
- `JwtSettings` (`Infrastructure/Configuration/JwtSettings.cs`, seção `"JwtSettings"`): `SecretKey`, `Issuer`, `Audience`, `ExpirationHours`. Vinculado via `Configure<JwtSettings>`.

### Validação do token (Program.cs) — CRÍTICO

- `ValidateIssuer/Audience/Lifetime/IssuerSigningKey = true`; `ClockSkew = TimeSpan.Zero`.
- `RoleClaimType = AppClaimTypes.Role` ("role") e `NameClaimType = AppClaimTypes.Name` ("name") — **sem isso, `[Authorize(Roles = ...)]` falha silenciosamente** ao casar papéis.

### ICurrentUser (identidade da requisição)

- Interface `ICurrentUser` (`Application/Common/Interfaces/`) — somente leitura: `Id` (**`Guid`**), `Email`, `Name`, `UserName`, `IsAuthenticated`, `Roles`, `IsInRole(role)`, `IsOwnerOrAdmin(Guid)`.
- Implementação `CurrentUserService` (`Infrastructure/Services/`) via `IHttpContextAccessor`, lendo claims por `AppClaimTypes`.
- A Application consome identidade **somente** via `ICurrentUser` (normalmente herdando `AuthorizedService`). Nunca injetar `IHttpContextAccessor`/`HttpContext` em serviços.

### Constantes (`Application/Common/Constants/`)

```csharp
public static class AppRoles      // Admin, Manager, Customer, Support, Operator
{                                 // + compostos: AdminOrManager, AdminOrOperator, AdminOrSupport, All
}
public static class AppClaimTypes // UserId="sub", Email="email", Name="name", Role="role"
{
}
```

---

## 10. Autorização (dois níveis)

A autorização é enforced em duas camadas que **coexistem**:

1. **Grossa — atributo na controller/action:** `[Authorize]` (todas exigem auth por fallback policy) + `[Authorize(Roles = AppRoles.X)]` usando as constantes compostas compile-time (`AppRoles.AdminOrManager`, `AppRoles.All`). 403 da middleware, sem body. **Nunca** hardcode strings de papel no atributo.
2. **Fina — no serviço:** ownership, filtragem por usuário e auditoria via `AuthorizedService`/`ICurrentUser`, retornando `Fail(AuthErrors.Forbidden)` → 403 com `{ code, description }`.

---

## 11. Convenções da API (MyApp.Api)

### ApiControllerBase

**Toda controller herda `ApiControllerBase`** (`Api/Controllers/ApiControllerBase.cs`), não `ControllerBase`. Ela provê:

- `[ApiController]` + rota `api/[controller]` — não repetir nos herdeiros.
- `HandleResult<T>(Result<T>)` → `200 Ok(value)` ou erro mapeado pelo sufixo do `Error.Code`.
- `HandleResult(Result)` → `204 NoContent` ou erro mapeado.

```csharp
// CORRETO — controller fina, sem try/catch e sem switch de status
[Authorize]
public sealed class OrdersController(IOrderService orderService) : ApiControllerBase
{
    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => HandleResult(await orderService.GetByIdAsync(id, ct));
}
```

- Endpoints públicos (login/register) usam `[AllowAnonymous]` explicitamente (`AuthController`).
- Toda action recebe `CancellationToken ct` e o repassa adiante.

### Pipeline (Program.cs)

`ExceptionMiddleware` → `UseHttpsRedirection` → `UseAuthentication` → `UseAuthorization` → `MapControllers`. Swagger (com Bearer JWT) **somente** em `Development`. `AddAuthorization` define `FallbackPolicy = RequireAuthenticatedUser`.

### Tratamento global de erro

`ExceptionMiddleware` (`Api/Middleware/`) captura exceptions não tratadas, loga via `ILogger` e devolve `500` com corpo JSON genérico. **Falhas esperadas não chegam aqui** — fluem por `Result`.

---

## 12. Entity Framework Core

### AppDbContext

- `IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>`; `DbSet` para `Order`, `Customer`, `Employee`.
- Configurações **auto-descobertas por reflexão**: `builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly)`. Não registrar `IEntityTypeConfiguration<T>` manualmente.

### Padrão de Configuration (`Infrastructure/Persistence/Configurations/`)

- Uma classe `*Configuration : IEntityTypeConfiguration<T>` por entidade.
- Padrões em uso:
  - Enum como string: `builder.Property(o => o.Status).HasConversion<string>()`.
  - Value object owned: `OwnsMany(o => o.Items, ...)` + `OwnsOne(i => i.UnitPrice, price => { price.Property(p => p.Amount)... })`.
  - Coleção encapsulada com field-backing: `Navigation(o => o.Items).Metadata.SetField("_items")` + `UsePropertyAccessMode(PropertyAccessMode.Field)`; propriedade computada ignorada (`builder.Ignore(o => o.Total)`).
  - Identificador sequencial: `Property(e => e.EmployeeNumber).ValueGeneratedOnAdd()` + `HasIndex(...).IsUnique()`; `HasMaxLength` em strings.

### Connection String

- Seção `ConnectionStrings:Default`. Padrão de dev (`appsettings.json`): `Server=(localdb)\mssqllocaldb;Database=MyAppDb;Trusted_Connection=True;`.

---

## 13. Injeção de Dependência por Camada

### Infrastructure (`DependencyInjection.AddInfrastructure`)

Registra `AppDbContext` (SqlServer, conn `Default`), Identity (+ política de senha/lockout), `IHttpContextAccessor`, e os scoped: `ICurrentUser→CurrentUserService`, `IJwtTokenService→JwtTokenService`, `IMapper→SimpleMapper`, `IUnitOfWork→UnitOfWork`, e os repositórios (`IOrderRepository`, `ICustomerRepository`, `IEmployeeRepository`).

### Api (`Program.cs`)

- `Configure<JwtSettings>` + `AddInfrastructure(configuration)`.
- Serviços de aplicação como `AddScoped`: `IAuthService→AuthService` (impl na Infrastructure), `IOrderService→OrderService`, `ICustomerService→CustomerService`, `IEmployeeServices→EmployeeServices`, e o `CreateOrderValidator`.
- JWT Bearer + `AddAuthorization` (fallback policy) + `AddControllers` + Swagger.
- **Regra:** a Infrastructure expõe `AddInfrastructure`. Os serviços de aplicação são registrados no `Program.cs` (não há, hoje, um `AddApplicationServices` próprio).

---

## 14. Testes

- **Frameworks:** xUnit (`[Fact]`/`[Theory]`) + **Moq** (mocks) + **FluentAssertions** (`result.IsSuccess.Should().BeTrue()`).
- Dois projetos: `MyApp.Domain.Tests` (domínio puro, ex.: `OrderTests`) e `MyApp.Application.Tests` (serviços com dependências mockadas).
- O `using Xunit` é global, declarado via `<Using Include="Xunit" />` no `.csproj` de cada projeto de teste — os arquivos de teste **não** importam `Xunit` individualmente.
- `MyApp.Application.Tests` referencia Application **e** Infrastructure (precisa de `AppUser`, `UserManager`, etc. para testar `AuthService`).
- `TestBase` (`Application.Tests/Common/`) centraliza o `Mock<ICurrentUser>`: default = Customer autenticado; helpers `SetupAsAdmin()` / `SetupAsManager()`.

```csharp
public sealed class OrderServiceTests : TestBase
{
    private readonly Mock<IOrderRepository> _repoMock = new();
    [Fact] public async Task ... { ... result.IsFailure.Should().BeTrue(); }
}
```

---

## 15. Domínio do Negócio (glossário)

| Termo | Significado |
|-------|-------------|
| **Order** | Pedido (raiz de agregação). Contém `OrderItem`s; `Total` é `Money` computado; transita por `OrderStatus` via `Confirm()`/`Cancel()`. |
| **OrderItem** | Item do pedido (owned entity), com `UnitPrice : Money` e `TotalPrice`. |
| **OrderStatus** | `Pending → Confirmed → Shipped → Delivered`, ou `Cancelled`. |
| **Money** | Value object `Amount` + `Currency` (default `BRL`), imutável e com aritmética validada. |
| **Customer** | Cliente, vinculado a um `UserId` (Identity). |
| **Employee** | Funcionário, com `EmployeeNumber` sequencial, `UserId` e `ManagerUserId` (hierarquia para "meu time"). |

---

## 16. Estrutura de Pastas de Referência

```
src/
  MyApp.Domain/
    Common/        BaseEntity, IAggregateRoot, ValueObject
    Entities/      Order, OrderItem, Customer, Employee
    Enums/         OrderStatus
    Events/        OrderCreated, OrderCancelled
    Exceptions/    DomainException
    Interfaces/    IOrderRepository, ICustomerRepository, IEmployeeRepository
    ValueObjects/  Money

  MyApp.Application/
    Common/
      Constants/   AppRoles, AppClaimTypes
      Exceptions/  ValidationException
      Interfaces/  ICurrentUser, IJwtTokenService, IUnitOfWork, IMapper
      Models/      Result, Error, PagedResult
      Services/    BaseService, AuthorizedService
    Auth/          DTOs, Errors (AuthErrors), Interfaces (IAuthService)
    Orders/        DTOs, Errors, Interfaces, Services, Validators
    Customers/     DTOs, Errors, Interfaces, Services
    Employees/     DTOs, Errors, Interfaces, Services

  MyApp.Infrastructure/
    Configuration/ JwtSettings
    Identity/      AppUser, JwtTokenService, AuthService, IdentitySeeder
    Persistence/
      Configurations/  OrderConfiguration, CustomerConfiguration, EmployeeConfiguration
      AppDbContext, UnitOfWork
    Repositories/  BaseRepository<T>, OrderRepository, CustomerRepository, EmployeeRepository
    Services/      CurrentUserService, SimpleMapper
    DependencyInjection.cs  (AddInfrastructure)

  MyApp.Api/
    Controllers/   ApiControllerBase, AuthController, OrdersController, CustomersController, EmployeesController
    Middleware/    ExceptionMiddleware
    Program.cs

tests/
  MyApp.Domain.Tests/        OrderTests
  MyApp.Application.Tests/    Common/TestBase, Auth, Orders, Employees
```

---

## 17. Como Rodar

1. Ajuste `ConnectionStrings:Default` em `src/MyApp.Api/appsettings.json` se necessário.
2. `dotnet ef database update --project src/MyApp.Infrastructure --startup-project src/MyApp.Api`
3. `dotnet run --project src/MyApp.Api` → Swagger em `https://localhost:5001/swagger` (Development).
4. Login admin padrão: `admin@myapp.com` / `Admin@123!`.
5. Testes: `dotnet test`.
