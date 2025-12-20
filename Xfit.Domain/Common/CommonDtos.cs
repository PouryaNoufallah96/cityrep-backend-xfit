namespace Xfit.Domain.Common
{
    public enum Gender { Male, Female }

    public class AddressInfo
    {
        public GeoLocation? Location { get; set; } = null; //[ Longitude , Latitude ]
        public string Province { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
    }

    public class GeoLocation
    {
        public string Type { get; set; } = "Point";
        public double[] Coordinates { get; set; } // [Longitude, Latitude]
    }

    //    db.Gyms.createIndex({
    //  "Address.Location": "2dsphere"
    //})

//    db.Gyms.createIndex(
//  { "Address.Location": "2dsphere" },
//  { partialFilterExpression: { "Address.Location": { $exists: true } } }
//)
}
