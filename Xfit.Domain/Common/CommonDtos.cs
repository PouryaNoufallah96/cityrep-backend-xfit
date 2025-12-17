namespace Xfit.Domain.Common
{
    public enum Gender { Male, Female }

    public class AddressInfo
    {
        public GeoLocation? Location { get; set; } = null;
        public string Province { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
    }

    public class GeoLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }


}
