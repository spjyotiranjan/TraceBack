using TraceBack.Models;

namespace TraceBack.Controllers;

public sealed record BrowsePage(
    ItemSearch Search,
    List<Item> Items,
    bool HasNext);

public sealed record ItemDetailPage(
    Item Item,
    bool IsOwner,
    bool IsAdmin,
    bool CanClaim);

public sealed record ItemEditorPage(
    ItemForm Form,
    string? Id);

public sealed record ClaimPage(
    Item Item,
    ClaimForm Form);

public sealed record ClaimRow(
    Claim Claim,
    Item? Item);

public sealed record DashboardPage(
    List<Item> Items,
    List<ClaimRow> Claims);

public sealed record AdminPage(
    List<Item> Items,
    List<ClaimRow> Claims,
    long ItemCount,
    long ClaimCount);
