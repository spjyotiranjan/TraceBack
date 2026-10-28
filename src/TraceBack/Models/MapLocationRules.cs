using System.Globalization;

namespace TraceBack.Models;

public sealed class MapLocationInput
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public static class MapLocationRules
{
    public const string RequiredMessage = "Choose a location on the map or enter latitude and longitude.";
    public const string InvalidMessage = "Latitude must be between -90 and 90, longitude between -180 and 180, with at most six decimal places.";

    public static bool TryNormalize(MapLocationInput? input, out MapLocation? location, out string? error)
    {
        location = null;
        error = null;
        if (input is null)
        {
            return true;
        }

        if (input.Latitude is null && input.Longitude is null)
        {
            return true;
        }

        if (input.Latitude is not double latitude || input.Longitude is not double longitude ||
            !double.IsFinite(latitude) || !double.IsFinite(longitude) ||
            latitude is < -90 or > 90 || longitude is < -180 or > 180 ||
            HasExcessPrecision(latitude) || HasExcessPrecision(longitude))
        {
            error = InvalidMessage;
            return false;
        }

        location = new MapLocation
        {
            Latitude = Math.Round(latitude, 6, MidpointRounding.AwayFromZero),
            Longitude = Math.Round(longitude, 6, MidpointRounding.AwayFromZero)
        };
        return true;
    }

    private static bool HasExcessPrecision(double value) =>
        Math.Abs(value - Math.Round(value, 6, MidpointRounding.AwayFromZero)) > 0.000000001;

    public static MapLocation? SafeStored(MapLocation? location) =>
        location is not null &&
        double.IsFinite(location.Latitude) && double.IsFinite(location.Longitude) &&
        location.Latitude is >= -90 and <= 90 && location.Longitude is >= -180 and <= 180
            ? new MapLocation
            {
                Latitude = Math.Round(location.Latitude, 6, MidpointRounding.AwayFromZero),
                Longitude = Math.Round(location.Longitude, 6, MidpointRounding.AwayFromZero)
            }
            : null;

    public static string? GoogleMapsUrl(MapLocation? location)
    {
        var point = SafeStored(location);
        return point is null ? null :
            $"https://www.google.com/maps/search/?api=1&query={point.Latitude.ToString("F6", CultureInfo.InvariantCulture)}%2C{point.Longitude.ToString("F6", CultureInfo.InvariantCulture)}";
    }
}
