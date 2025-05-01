using BuzzWatch.Domain.Common;

namespace BuzzWatch.Domain.Devices
{
    public record HiveLocation(string Address, double? Latitude, double? Longitude) : ValueObject
    {
        public override string ToString() => Address;
    }
} 