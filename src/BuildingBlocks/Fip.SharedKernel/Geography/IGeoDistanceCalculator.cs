namespace Fip.SharedKernel.Geography;

public interface IGeoDistanceCalculator
{
    double CalculateNauticalMiles(
        double latitude1,
        double longitude1,
        double latitude2,
        double longitude2);
}
