using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using TraceBack.Models;

namespace TraceBack.Tests;

public sealed class MapLocationTests
{
    [Theory]
    [InlineData(-90.0, -180.0)]
    [InlineData(90.0, 180.0)]
    [InlineData(20.2960584, 85.8245394)]
    public void Valid_coordinates_are_bounded_and_stored_to_six_decimals(double latitude, double longitude)
    {
        Assert.True(MapLocationRules.TryNormalize(new MapLocationInput
        {
            Latitude = latitude,
            Longitude = longitude
        }, out var point, out var error));
        Assert.Null(error);
        Assert.Equal(Math.Round(latitude, 6, MidpointRounding.AwayFromZero), point!.Latitude);
        Assert.Equal(Math.Round(longitude, 6, MidpointRounding.AwayFromZero), point.Longitude);
    }

    [Theory]
    [InlineData(null, 1.0)]
    [InlineData(1.0, null)]
    [InlineData(double.NaN, 1.0)]
    [InlineData(1.0, double.PositiveInfinity)]
    [InlineData(90.000001, 1.0)]
    [InlineData(1.0, -180.000001)]
    public void Partial_nonfinite_and_out_of_range_points_are_rejected(double? latitude, double? longitude)
    {
        Assert.False(MapLocationRules.TryNormalize(new MapLocationInput
        {
            Latitude = latitude,
            Longitude = longitude
        }, out var point, out var error));
        Assert.Null(point);
        Assert.Equal(MapLocationRules.InvalidMessage, error);
    }

    [Fact]
    public void Legacy_report_round_trips_without_a_point_and_keeps_location_text()
    {
        var legacy = new Item { Location = "Library east entrance" };
        var document = legacy.ToBsonDocument();
        Assert.False(document.Contains("mapLocation"));

        var reloaded = BsonSerializer.Deserialize<Item>(document);
        Assert.Null(reloaded.MapLocation);
        Assert.Equal("Library east entrance", reloaded.Location);
        Assert.Null(ItemResponse.From(reloaded).MapLocation);
        Assert.Null(MapLocationRules.GoogleMapsUrl(reloaded.MapLocation));
    }

    [Fact]
    public void Blank_coordinate_fields_on_a_legacy_edit_do_not_require_a_point()
    {
        Assert.True(MapLocationRules.TryNormalize(new MapLocationInput(), out var point, out var error));
        Assert.Null(point);
        Assert.Null(error);
    }

    [Fact]
    public void Point_round_trips_in_bson_and_api_response_exposes_only_safe_coordinates()
    {
        var point = new MapLocation { Latitude = 20.296058, Longitude = 85.824539 };
        var document = new Item { Location = "Library", MapLocation = point }.ToBsonDocument();
        Assert.True(document["mapLocation"].AsBsonDocument.Contains("latitude"));
        Assert.True(document["mapLocation"].AsBsonDocument.Contains("longitude"));

        var reloaded = BsonSerializer.Deserialize<Item>(document);
        Assert.Equal(point.Latitude, ItemResponse.From(reloaded).MapLocation?.Latitude);
        Assert.Equal(point.Longitude, ItemResponse.From(reloaded).MapLocation?.Longitude);
        Assert.Equal("https://www.google.com/maps/search/?api=1&query=20.296058%2C85.824539",
            MapLocationRules.GoogleMapsUrl(reloaded.MapLocation));

        reloaded.MapLocation = new MapLocation { Latitude = double.NaN, Longitude = 1 };
        Assert.Null(ItemResponse.From(reloaded).MapLocation);
        Assert.Null(MapLocationRules.GoogleMapsUrl(reloaded.MapLocation));
    }
}
