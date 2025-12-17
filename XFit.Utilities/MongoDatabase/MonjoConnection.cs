using MongoDB.Driver;
using static XFit.Utilities.Constants.RegisterMode;
using XFit.Utilities.MongoDatabase.Contracts;


namespace XFit.Utilities.MongoDatabase
{
    public class MonjoConnection : IMonjoConnection, ISingletonDependency
    {
        public IMongoClient Client { get; }
        public IMongoDatabase Database { get; }
        public MonjoConnection(IMonjoSettings settings)
        {
            Client = new MongoClient(settings.ConnectionString);
            Database = Client.GetDatabase(settings.DatabaseName);
        }
    }
}
