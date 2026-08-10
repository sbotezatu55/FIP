using Fip.SharedKernel.Geography;

namespace Fip.Application.Tests;

public sealed class GeoDistanceCalculatorTests
{
    private readonly IGeoDistanceCalculator _calculator = new GeoDistanceCalculator();

    [Fact]
    public void CalculateNauticalMiles_ReturnsZeroForIdenticalCoordinates()
    {
        var distance = _calculator.CalculateNauticalMiles(40.6413, -73.7781, 40.6413, -73.7781);

        Assert.Equal(0, distance, precision: 10);
    }

    [Fact]
    public void CalculateNauticalMiles_ReturnsApproximatelySixtyForOneDegreeOfLatitude()
    {
        var distance = _calculator.CalculateNauticalMiles(0, 0, 1, 0);

        Assert.InRange(distance, 59.9, 60.1);
    }

    [Fact]
    public void CalculateNauticalMiles_CalculatesKnownCityPair()
    {
        // John F. Kennedy International Airport to Los Angeles International Airport.
        var distance = _calculator.CalculateNauticalMiles(40.6413, -73.7781, 33.9416, -118.4085);

        Assert.InRange(distance, 2_135, 2_155);
    }

    [Fact]
    public void CalculateNauticalMiles_CrossesPrimeMeridian()
    {
        var distance = _calculator.CalculateNauticalMiles(51.5, -0.5, 51.5, 0.5);

        Assert.InRange(distance, 37.2, 37.6);
    }

    [Fact]
    public void CalculateNauticalMiles_HandlesOppositeLongitudeSigns()
    {
        var distance = _calculator.CalculateNauticalMiles(35, -10, 35, 10);

        Assert.InRange(distance, 980, 984);
    }

    [Fact]
    public void CalculateNauticalMiles_HandlesInternationalDateLine()
    {
        var distance = _calculator.CalculateNauticalMiles(0, 179.5, 0, -179.5);

        Assert.InRange(distance, 59.9, 60.1);
    }

    [Theory]
    [InlineData(-90.001)]
    [InlineData(90.001)]
    [InlineData(double.NaN)]
    public void CalculateNauticalMiles_RejectsInvalidLatitude(double latitude)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.CalculateNauticalMiles(latitude, 0, 0, 0));

        Assert.Equal("latitude1", exception.ParamName);
    }

    [Theory]
    [InlineData(-180.001)]
    [InlineData(180.001)]
    [InlineData(double.NaN)]
    public void CalculateNauticalMiles_RejectsInvalidLongitude(double longitude)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.CalculateNauticalMiles(0, longitude, 0, 0));

        Assert.Equal("longitude1", exception.ParamName);
    }
}
