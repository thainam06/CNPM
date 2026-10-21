using ShortVideoTranslator.Business;
using ShortVideoTranslator.Data;

namespace ShortVideoTranslator.Tests;

public class PlaceServiceTests
{
    private readonly PlaceService _places = new(new DemoPlaceRepository());

    [Fact]
    public void DemoCatalogHasEightDistrictOnePlacesAndMarksUnverifiedLocationData()
    {
        var places = _places.GetAll();
        Assert.Equal(8, places.Count);
        Assert.Equal(8, places.Select(place => place.Id).Distinct().Count());
        Assert.All(places, place => {
            Assert.True(place.IsDemo);
            Assert.Equal("district1", place.Region);
            Assert.NotNull(place.Address);
            Assert.NotNull(place.AddressSourceUrl);
            if (place.LocationVerified) { Assert.NotNull(place.Latitude); Assert.NotNull(place.Longitude); Assert.NotNull(place.LocationSourceUrl); }
            else { Assert.Null(place.Latitude); Assert.Null(place.Longitude); }
            Assert.False(string.IsNullOrWhiteSpace(place.Description));
        });
    }

    [Theory]
    [InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)][InlineData(7)][InlineData(8)]
    public void DetailMatchesCatalog(int id) => Assert.Equal(_places.GetAll().Single(place => place.Id == id), _places.GetById(id));

    [Fact]
    public void UnknownIdHasNoPlace() => Assert.Null(_places.GetById(999));
}
