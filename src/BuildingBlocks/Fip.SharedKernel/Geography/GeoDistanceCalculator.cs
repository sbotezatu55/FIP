namespace Fip.SharedKernel.Geography;

/// <summary>
/// Calculates great-circle distances between geographic coordinates.
/// </summary>
public sealed class GeoDistanceCalculator : IGeoDistanceCalculator
{
    private const double MeanEarthRadiusNauticalMiles = 3440.065;
    private const double DegreesToRadians = Math.PI / 180;

    public double CalculateNauticalMiles(
        double latitude1,
        double longitude1,
        double latitude2,
        double longitude2)
    {
        ValidateLatitude(latitude1, nameof(latitude1));
        ValidateLongitude(longitude1, nameof(longitude1));
        ValidateLatitude(latitude2, nameof(latitude2));
        ValidateLongitude(longitude2, nameof(longitude2));

        var latitude1Radians = latitude1 * DegreesToRadians;
        var latitude2Radians = latitude2 * DegreesToRadians;
        var deltaLatitudeRadians = (latitude2 - latitude1) * DegreesToRadians;
        var deltaLongitudeRadians = (longitude2 - longitude1) * DegreesToRadians;

        var haversine =
            Math.Pow(Math.Sin(deltaLatitudeRadians / 2), 2) +
            Math.Cos(latitude1Radians) *
            Math.Cos(latitude2Radians) *
            Math.Pow(Math.Sin(deltaLongitudeRadians / 2), 2);

        var centralAngle = 2 * Math.Atan2(
            Math.Sqrt(Math.Min(1, haversine)),
            Math.Sqrt(Math.Max(0, 1 - haversine)));

        return MeanEarthRadiusNauticalMiles * centralAngle;
    }

    private static void ValidateLatitude(double latitude, string parameterName)
    {
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                latitude,
                "Latitude must be between -90 and 90 degrees.");
        }
    }

    private static void ValidateLongitude(double longitude, string parameterName)
    {
        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                longitude,
                "Longitude must be between -180 and 180 degrees.");
        }
    }
}
