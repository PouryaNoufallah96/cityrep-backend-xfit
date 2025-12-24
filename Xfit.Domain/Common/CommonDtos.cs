using MongoDB.Driver.GeoJsonObjectModel;

namespace Xfit.Domain.Common
{
    public enum Gender { Male, Female }

    public class AddressInfo
    {
        //public GeoLocation? Location { get; set; } = null; //[ Longitude , Latitude ]
        public GeoJsonPoint<GeoJson2DGeographicCoordinates>? Location { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
    }

  

    //    db.Gyms.createIndex({
    //  "Address.Location": "2dsphere"
    //})

//    db.Gyms.createIndex(
//  { "Address.Location": "2dsphere" },
//  { partialFilterExpression: { "Address.Location": { $exists: true } } }
//)
}
