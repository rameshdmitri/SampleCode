namespace MyApp.Application.Common.Constants;

public static class AppRoles
{
    // ── Papéis individuais ────────────────────────────────────
    public const string Admin    = "Admin";
    public const string Manager  = "Manager";
    public const string Customer = "Customer";
    public const string Support  = "Support";
    public const string Operator = "Operator";

    // ── Combinações pré-computadas (compile-time) ─────────────
    public const string AdminOrManager  = Admin + "," + Manager;
    public const string AdminOrOperator = Admin + "," + Operator;
    public const string AdminOrSupport  = Admin + "," + Support;
    public const string All             = Admin + "," + Manager + "," + Support + "," + Customer;
}
