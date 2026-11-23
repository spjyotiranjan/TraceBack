using TraceBack.Models;

namespace TraceBack.Controllers;

public sealed record BrowsePage(
    ItemSearch Search,
    List<Item> Items,
    bool HasNext,
    bool LoadFailed = false)
{
    public RouteValueDictionary PageRoute(int page) => new(new
    {
        Search.Query,
        Search.Type,
        Search.Category,
        Search.Location,
        Search.Status,
        Page = page
    });
}

public sealed record HomePage(List<Item> RecentItems, bool RecentLoadFailed = false);

public sealed record FilterChip(string Label, string Value, string Url);

public sealed record ItemDetailPage(
    Item Item,
    bool IsOwner,
    bool IsAdmin,
    bool CanClaim);

public sealed record ItemEditorPage(
    ItemForm Form,
    string? Id,
    string? CurrentImageUrl = null,
    bool HasSavedPin = false,
    string? LockedType = null,
    bool ReselectImage = false,
    bool PendingImageRemoval = false,
    long? EditRevision = null);

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
