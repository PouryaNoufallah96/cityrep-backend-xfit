using MongoDB.Driver;

namespace XFit.Utilities.MongoDatabase.Contracts
{
    public interface IMonjoConnection
    {
        IMongoClient Client { get; }
        IMongoDatabase Database { get; }
    }
}
