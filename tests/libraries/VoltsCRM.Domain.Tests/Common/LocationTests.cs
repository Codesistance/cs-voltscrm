using VoltsCRM.Domain.Common;

namespace VoltsCRM.Domain.Tests.Common;

public class LocationTests
{
    private static Address SampleAddress() => new("1 Main St", "Lagos", "Lagos", "Nigeria");

    [Fact]
    public void Constructs_with_address_and_no_coordinates()
    {
        var location = new Location(SampleAddress());

        Assert.Equal("Lagos", location.Address.City);
        Assert.Null(location.Coordinates);
    }

    [Fact]
    public void Constructs_with_coordinates()
    {
        var coords = new GpsCoordinates(6.5244, 3.3792);
        var location = new Location(SampleAddress(), coords);

        Assert.NotNull(location.Coordinates);
        Assert.Equal(6.5244, location.Coordinates!.Latitude);
        Assert.Equal(3.3792, location.Coordinates.Longitude);
    }

    [Fact]
    public void Null_address_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Location(null!));
    }

    [Fact]
    public void Value_equality_holds_for_same_components()
    {
        var a = new Location(SampleAddress(), new GpsCoordinates(6.5244, 3.3792));
        var b = new Location(SampleAddress(), new GpsCoordinates(6.5244, 3.3792));

        Assert.Equal(a, b);
    }

    [Fact]
    public void Value_equality_differs_when_coordinates_differ()
    {
        var a = new Location(SampleAddress(), new GpsCoordinates(6.5244, 3.3792));
        var b = new Location(SampleAddress());

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void WithCoordinates_returns_updated_copy_keeping_address()
    {
        var original = new Location(SampleAddress());
        var updated = original.WithCoordinates(new GpsCoordinates(1, 2));

        Assert.Null(original.Coordinates);
        Assert.Equal(1, updated.Coordinates!.Latitude);
        Assert.Equal(original.Address, updated.Address);
    }
}
