using MongoDB.Driver.GeoJsonObjectModel;
using System.Text.Json.Serialization;

namespace Xfit.Domain.Common
{
    public enum Gender { Male, Female }

    public class AddressInfo
    {
        //public GeoLocation? Location { get; set; } = null; //[ Longitude , Latitude ]
        [JsonIgnore]public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; set; }
        public GeoLocation GeoLocation { get; set; }
        public string Province { get; set; } = "تهران";
        public string City { get; set; } = "تهران";
        public string Address { get; set; }
        public string PostalCode { get; set; } = null;
    }

    public class GeoLocation
    {

        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }

    //    db.Gyms.createIndex({
    //  "Address.Location": "2dsphere"
    //})

    //    db.Gyms.createIndex(
    //  { "Address.Location": "2dsphere" },
    //  { partialFilterExpression: { "Address.Location": { $exists: true } } }
    //)
}
