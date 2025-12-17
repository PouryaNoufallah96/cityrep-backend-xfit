using XFit.Utilities.Attributes;
using XFit.Utilities.MongoDatabase.Documents;

namespace Xfit.Domain.Collections
{

    [MonjoCollectionName("GymTrends")]
    public class GymTrend : BaseDocument 
    {
        public string GymTrendId { get; set; } = Guid.NewGuid().ToString("N"); 
        public string Title { get; set; }
        public string IconUrl { get; set; } = null;

    }



}
