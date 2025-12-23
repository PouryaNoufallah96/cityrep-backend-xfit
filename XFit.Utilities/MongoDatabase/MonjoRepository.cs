using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Linq.Expressions;
using XFit.Utilities.Attributes;
using XFit.Utilities.Constants;
using XFit.Utilities.MongoDatabase.Builders;
using XFit.Utilities.MongoDatabase.Contracts;
using XFit.Utilities.MongoDatabase.Documents;
using XFit.Utilities.MongoDatabase.Extensions;
using XFit.Utilities.MongoDatabase.Filter;

namespace XFit.Utilities.MongoDatabase
{
    public class MonjoRepository<TDocument> : IMonjoRepository<TDocument> where TDocument : BaseDocument
    {
        private readonly IMonjoConnection _connection;

        protected readonly IMongoCollection<TDocument> _collection;

        //private readonly Expression<Func<TDocument, bool>> _defaultCondition;

        public MonjoRepository(IMonjoConnection connection)
        {
            _connection = connection;

            _collection = _connection.Database.GetCollection<TDocument>(CollectionName);
            //_defaultCondition = TDocument => !TDocument.IsDeleted;
            Configure();
        }

        protected virtual void Configure()
        {
        }

        public string CollectionName
        {
            get
            {
                var documentType = typeof(TDocument);
                return ((MonjoCollectionNameAttribute)documentType.GetCustomAttributes(
                        typeof(MonjoCollectionNameAttribute),
                        true)
                    .FirstOrDefault())?.CollectionName;
            }
        }

        public string IdentifierName
        {
            get => MongoCollectionExtensions.GetIdentifierName<TDocument>();
        }

        public bool CollectionExists()
        {
            return _connection.Database.ContainsCollection(CollectionName);
        }

        public virtual IQueryable<TDocument> AsQueryable()
        {
            return _collection.AsQueryable().Where(t => !t.IsDeleted);
        }

        public virtual async Task<IList<BsonDocument>> AggregateAsync(
            PipelineDefinition<TDocument, BsonDocument> pipeline)
        {
            var asyncCursor = await _collection.AggregateAsync(pipeline);

            return await asyncCursor.ToListAsync();
        }

        public virtual IEnumerable<TDocument> FilterBy(
            Expression<Func<TDocument, bool>> filterExpression)
        {
            return _collection.Find(CombineExpressionToDefalutFilter(filterExpression)).ToEnumerable();
        }

        public virtual async Task<IList<TDocument>> FilterByAsync(
            Expression<Func<TDocument, bool>> filterExpression)
        {
            return await _collection.Find(CombineExpressionToDefalutFilter(filterExpression)).ToListAsync();
        }

        public virtual async Task<MonjoFilteredResult<TDocument>> FilterByAsync(MonjoQuery query)
        {
            return await _collection.AsQueryable().ExecuteAsync(query);
        }

        public virtual IEnumerable<TProjected> FilterBy<TProjected>(
            Expression<Func<TDocument, bool>> filterExpression,
            Expression<Func<TDocument, TProjected>> projectionExpression)
        {
            return _collection.Find(CombineExpressionToDefalutFilter(filterExpression)).Project(projectionExpression).ToEnumerable();
        }

        public virtual async Task<IList<TProjected>> FilterByAsync<TProjected>(
            Expression<Func<TDocument, bool>> filterExpression,
            Expression<Func<TDocument, TProjected>> projectionExpression)
        {
            return await _collection.Find(CombineExpressionToDefalutFilter(filterExpression)).Project(projectionExpression).ToListAsync();
        }

        public long Count(Expression<Func<TDocument, bool>> filterExpression)
        {
            return _collection.CountDocuments(CombineExpressionToDefalutFilter(filterExpression));
        }

        public async Task<long> CountAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            return await _collection.CountDocumentsAsync(CombineExpressionToDefalutFilter(filterExpression));
        }

        public async Task<long> CountAsync(FilterDefinition<TDocument> filter)
        {
            return await _collection.CountDocumentsAsync(CombineFilterToDefalutFilterDefinition(filter));
        }


        public virtual IFindFluent<TDocument, TDocument> Find(FilterDefinition<TDocument> filter)
        {
            return _collection.Find(CombineFilterToDefalutFilterDefinition(filter));
        }

        public virtual async Task<IAsyncCursor<TDocument>> FindAsync(
            FilterDefinition<TDocument> filter,
            int batchSize = 500,
            CancellationToken cancellationToken = default)
        {
            var finalFilter = CombineFilterToDefalutFilterDefinition(filter);

            return await _collection.FindAsync(finalFilter, new FindOptions<TDocument>
            {
                BatchSize = batchSize
            }, cancellationToken);
        }

        public bool Exists(Expression<Func<TDocument, bool>> filterExpression)
        {
            return AsQueryable().Where(filterExpression).Any();
        }

        public async Task<bool> ExistsAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            return await AsQueryable().Where(filterExpression).AnyAsync();
        }

        public virtual IFindFluent<TDocument, TDocument> Find(
            Expression<Func<TDocument, bool>> filterExpression, int batchSize = 500)
        {
            return _collection.Find(CombineExpressionToDefalutFilter(filterExpression), new FindOptions()
            {
                BatchSize = batchSize
            });
        }

        public virtual Task<IAsyncCursor<TDocument>> FindAsync(
            Expression<Func<TDocument, bool>> filterExpression, int batchSize = 500)
        {
            return _collection.FindAsync(CombineExpressionToDefalutFilter(filterExpression), new FindOptions<TDocument>()
            {
                BatchSize = batchSize
            });
        }

        public virtual TDocument FindOne(Expression<Func<TDocument, bool>> filterExpression)
        {
            return _collection.Find(CombineExpressionToDefalutFilter(filterExpression)).FirstOrDefault();
        }

        public virtual Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            return Task.Run(() => Find(filterExpression).FirstOrDefaultAsync());
        }

        public virtual TDocument FindById(object id)
        {
            var filter = Builders<TDocument>.Filter.Eq(IdentifierName, id);
            return _collection.Find(CombineFilterToDefalutFilterDefinition(filter)).SingleOrDefault();
        }

        public virtual async Task<TDocument> FindByIdAsync(object id)
        {
            var filter = Builders<TDocument>.Filter.Eq(IdentifierName, id);
            return await (await _collection.FindAsync(CombineFilterToDefalutFilterDefinition(filter))).SingleOrDefaultAsync();
        }

        public virtual void InsertOne(TDocument document)
        {
            //document.SetCreatedByInfo();
            _collection.InsertOne(document);
        }

        public virtual async Task InsertOneAsync(TDocument document)
        {
            //document.SetCreatedByInfo();
            await _collection.InsertOneAsync(document);
        }

        public virtual void InsertMany(IEnumerable<TDocument> documents)
        {
            _collection.InsertMany(documents);
        }

        public virtual async Task InsertManyAsync(IEnumerable<TDocument> documents)
        {
            //foreach (var doc in documents)
            //{
            //    doc.SetCreatedByInfo();
            //}
            await _collection.InsertManyAsync(documents);
        }

        public virtual void ReplaceOne(TDocument document)
        {
            var filter = Builders<TDocument>.Filter.Eq(IdentifierName, document.GetIdentifierValue());

            UpdateDocument(document);
            _collection.FindOneAndReplace(CombineFilterToDefalutFilterDefinition(filter), document);
        }

        public virtual async Task ReplaceOneAsync(TDocument document)
        {
            var filter = Builders<TDocument>.Filter.Eq(IdentifierName, document.GetIdentifierValue());
            UpdateDocument(document);
            await _collection.FindOneAndReplaceAsync(CombineFilterToDefalutFilterDefinition(filter), document);
        }

        public virtual async Task ReplaceManyAsync(IEnumerable<ReplaceManyInput<TDocument>> replaceManyInputs)
        {
            var operations = new List<WriteModel<TDocument>>();

            foreach (var replaceManyInput in replaceManyInputs)
            {
                FilterDefinition<TDocument> filter;

                if (replaceManyInput.FilterExpression != null)
                    filter = Builders<TDocument>.Filter.Where(replaceManyInput.FilterExpression);
                else
                    filter = Builders<TDocument>.Filter.Eq(IdentifierName, replaceManyInput.Document.Id);

                var replaceOne = new ReplaceOneModel<TDocument>(filter, replaceManyInput.Document);
                operations.Add(replaceOne);
            }

            if (operations.Count != 0)
                await _collection.BulkWriteAsync(operations, new BulkWriteOptions { IsOrdered = false });
        }

        public Task BulkWriteAsync(IEnumerable<WriteModel<TDocument>> requests)
        {
            return _collection.BulkWriteAsync(requests);
        }


        public virtual void DeleteMany(Expression<Func<TDocument, bool>> filterExpression)
        {
            var update = CreateDeleteUpdate();
            _collection.UpdateMany(CombineExpressionToDefalutFilter(filterExpression), update);
        }

        public virtual async Task DeleteManyAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            var update = CreateDeleteUpdate();
            await _collection.UpdateManyAsync(CombineExpressionToDefalutFilter(filterExpression), update);
        }

        public virtual void DeleteOne(Expression<Func<TDocument, bool>> filterExpression)
        {
            var update = CreateDeleteUpdate();
            _collection.FindOneAndUpdate(CombineExpressionToDefalutFilter(filterExpression), update);
        }

        public virtual async Task DeleteOneAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            var update = CreateDeleteUpdate();
            await _collection.FindOneAndUpdateAsync(CombineExpressionToDefalutFilter(filterExpression), update);
        }

        public virtual void DeleteById(object id)
        {
            var filter = Builders<TDocument>.Filter.Eq(IdentifierName, id);
            var update = CreateDeleteUpdate();
            _collection.FindOneAndUpdate(CombineFilterToDefalutFilterDefinition(filter), update);
        }

        public virtual async Task DeleteByIdAsync(object id)
        {
            var filter = Builders<TDocument>.Filter.Eq(IdentifierName, id);
            var update = CreateDeleteUpdate();
            await _collection.FindOneAndUpdateAsync(CombineFilterToDefalutFilterDefinition(filter), update);
        }

        public void CreateIndexOne(CreateIndexModel<TDocument> createIndexModel)
        {
            _collection.Indexes.CreateOne(createIndexModel);
        }

        public async Task CreateIndexOneAsync(CreateIndexModel<TDocument> createIndexModel)
        {
            await _collection.Indexes.CreateOneAsync(createIndexModel);
        }

        public void CreateIndexMany(IEnumerable<CreateIndexModel<TDocument>> createIndexModels)
        {
            _collection.Indexes.CreateMany(createIndexModels);
        }

        public async Task CreateIndexManyAsync(IEnumerable<CreateIndexModel<TDocument>> createIndexModels)
        {
            await _collection.Indexes.CreateManyAsync(createIndexModels);
        }

        public void DropIndexOne(string name)
        {
            _collection.Indexes.DropOne(name);
        }

        public async Task DropIndexOneAsync(string name)
        {
            await _collection.Indexes.DropOneAsync(name);
        }

        public void DropIndexAll()
        {
            _collection.Indexes.DropAll();
        }

        public async Task DropIndexAllAsync()
        {
            await _collection.Indexes.DropAllAsync();
        }

        public IMonjoIndexBuilder<TDocument> AscendingIndex(Expression<Func<TDocument, object>> filterExpression)
        {
            var indexBuilder = new IndexKeysDefinitionBuilder<TDocument>();
            return new MonjoIndexBuilder<TDocument>(this, indexBuilder.Ascending(filterExpression));
        }

        public IMonjoIndexBuilder<TDocument> DescendingIndex(Expression<Func<TDocument, object>> filterExpression)
        {
            var indexBuilder = new IndexKeysDefinitionBuilder<TDocument>();
            return new MonjoIndexBuilder<TDocument>(this, indexBuilder.Descending(filterExpression));
        }



        public void CreateGeo2DSphereIndex(
            string fieldPath,
            string indexName = null,
            bool sparse = true
        )
        {
            var keys = Builders<TDocument>.IndexKeys.Geo2DSphere(fieldPath);

            var options = new CreateIndexOptions
            {
                Name = indexName ?? $"idx_{fieldPath.Replace(".", "_")}_2dsphere",
                Sparse = sparse
            };

            _collection.Indexes.CreateOne(new CreateIndexModel<TDocument>(keys, options));
        }






        public async Task<TDocument> FindOneAndUpdateAsync(FilterDefinition<TDocument> filter,
            UpdateDefinition<TDocument> update)
        {
            return await _collection.FindOneAndUpdateAsync(CombineFilterToDefalutFilterDefinition(filter), CombineUpdateToDefalutUpdateDefinition(update));
        }

        public TDocument FindOneAndUpdate(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update)
        {
            return _collection.FindOneAndUpdate(CombineFilterToDefalutFilterDefinition(filter), CombineUpdateToDefalutUpdateDefinition(update));
        }

        public async Task<UpdateResult> UpdateManyAsync(FilterDefinition<TDocument> filter,
            UpdateDefinition<TDocument> update)
        {
            return await _collection.UpdateManyAsync(CombineFilterToDefalutFilterDefinition(filter), CombineUpdateToDefalutUpdateDefinition(update));
        }

        public async Task<UpdateResult> UpdateManyAsync(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null)
        {
            return await _collection.UpdateManyAsync(CombineFilterToDefalutFilterDefinition(filter),
                CombineUpdateToDefalutUpdateDefinition(update),
                options);
        }

        public UpdateResult UpdateMany(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update)
        {
            return _collection.UpdateMany(CombineFilterToDefalutFilterDefinition(filter), CombineUpdateToDefalutUpdateDefinition(update));
        }

        public UpdateResult UpsertOne(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update)
        {
            return _collection.UpdateOne(CombineFilterToDefalutFilterDefinition(filter), CombineUpdateToDefalutUpdateDefinition(update), new UpdateOptions() { IsUpsert = true });
        }

        public async Task<UpdateResult> UpsertOneAsync(FilterDefinition<TDocument> filter,
            UpdateDefinition<TDocument> update)
        {
            return await _collection.UpdateOneAsync(CombineFilterToDefalutFilterDefinition(filter), CombineUpdateToDefalutUpdateDefinition(update), new UpdateOptions() { IsUpsert = true });
        }

        public UpdateResult UpsertMany(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update)
        {
            return _collection.UpdateMany(CombineFilterToDefalutFilterDefinition(filter), CombineUpdateToDefalutUpdateDefinition(update), new UpdateOptions() { IsUpsert = true });
        }

        public async Task<UpdateResult> UpsertManyAsync(FilterDefinition<TDocument> filter,
            UpdateDefinition<TDocument> update)
        {
            return await _collection.UpdateManyAsync(CombineFilterToDefalutFilterDefinition(filter), CombineUpdateToDefalutUpdateDefinition(update), new UpdateOptions() { IsUpsert = true });
        }

        public async Task<TDocument> FindOneAndUpdateAsync(Expression<Func<TDocument, bool>> filter,
            UpdateDefinition<TDocument> update)
        {
            return await _collection.FindOneAndUpdateAsync(CombineExpressionToDefalutFilter(filter), CombineUpdateToDefalutUpdateDefinition(update));
        }

        public TDocument FindOneAndUpdate(Expression<Func<TDocument, bool>> filter, UpdateDefinition<TDocument> update)
        {
            return _collection.FindOneAndUpdate(CombineExpressionToDefalutFilter(filter), CombineUpdateToDefalutUpdateDefinition(update));
        }

        public async Task<UpdateResult> UpdateManyAsync(Expression<Func<TDocument, bool>> filter,
            UpdateDefinition<TDocument> update)
        {
            return await _collection.UpdateManyAsync(CombineExpressionToDefalutFilter(filter), CombineUpdateToDefalutUpdateDefinition(update));
        }

        public UpdateResult UpdateMany(Expression<Func<TDocument, bool>> filter, UpdateDefinition<TDocument> update)
        {
            return _collection.UpdateMany(CombineExpressionToDefalutFilter(filter), CombineUpdateToDefalutUpdateDefinition(update));
        }

        public UpdateResult UpsertOne(Expression<Func<TDocument, bool>> filter, UpdateDefinition<TDocument> update)
        {
            return _collection.UpdateOne(CombineExpressionToDefalutFilter(filter), CombineUpdateToDefalutUpdateDefinition(update), new UpdateOptions() { IsUpsert = true });
        }

        public async Task<UpdateResult> UpsertOneAsync(Expression<Func<TDocument, bool>> filter,
            UpdateDefinition<TDocument> update)
        {
            return await _collection.UpdateOneAsync(CombineExpressionToDefalutFilter(filter), CombineUpdateToDefalutUpdateDefinition(update), new UpdateOptions() { IsUpsert = true });
        }

        public UpdateResult UpsertMany(Expression<Func<TDocument, bool>> filter, UpdateDefinition<TDocument> update)
        {
            return _collection.UpdateMany(CombineExpressionToDefalutFilter(filter), CombineUpdateToDefalutUpdateDefinition(update), new UpdateOptions() { IsUpsert = true });
        }

        public async Task<UpdateResult> UpsertManyAsync(Expression<Func<TDocument, bool>> filter,
            UpdateDefinition<TDocument> update)
        {
            return await _collection.UpdateManyAsync(CombineExpressionToDefalutFilter(filter), CombineUpdateToDefalutUpdateDefinition(update), new UpdateOptions() { IsUpsert = true });
        }

        public virtual void RealDeleteMany(Expression<Func<TDocument, bool>> filterExpression)
        {
            _collection.DeleteManyAsync(filterExpression);
        }

        public virtual async Task RealDeleteManyAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            await _collection.DeleteManyAsync(filterExpression);
        }


        private static FilterDefinition<TDocument> CombineExpressionToDefalutFilter(Expression<Func<TDocument, bool>> filterExpression)
        {
            Expression<Func<TDocument, bool>> defaultCondition = TDocument => !TDocument.IsDeleted;
            var combinedFilter = Builders<TDocument>.Filter.And(defaultCondition, Builders<TDocument>.Filter.Where(filterExpression));
            return combinedFilter;
        }

        private static FilterDefinition<TDocument> CombineFilterToDefalutFilterDefinition(FilterDefinition<TDocument> filter)
        {
            var defaultFilter = Builders<TDocument>.Filter.Eq(q => q.IsDeleted, false);
            FilterDefinition<TDocument> combinedFilter = defaultFilter & filter;
            return combinedFilter;
        }

        //private static UpdateDefinition<TDocument> CombineUpdateToDefalutUpdateDefinition(UpdateDefinition<TDocument> update)
        //{
        //    return update.Set(q => q.ModifiedMoment, DateTime.UtcNow);
        //}

        private static UpdateDefinition<TDocument> CombineUpdateToDefalutUpdateDefinition(UpdateDefinition<TDocument> update)
        {
            var user = CurrentRequestContext.User ?? new RequestUserInfo();

            return update
                .Set(q => q.ModifiedMoment, DateTime.UtcNow)
                .Set(q => q.ModifiedBy, user.PublicKey)
                .Set(q => q.ModifiedByInfo, user.DisplayInfo);
        }

        private UpdateDefinition<TDocument> CreateDeleteUpdate()
        {
            var user = CurrentRequestContext.User ?? new RequestUserInfo();

            return Builders<TDocument>.Update
                .Set(q => q.IsDeleted, true)
                .Set(q => q.DeletedMoment, DateTime.UtcNow)
                .Set(q => q.DeletedBy, user.PublicKey)
                .Set(q => q.DeletedByInfo, user.DisplayInfo);
        }

        private void UpdateDocument(TDocument document)
        {
            var user = CurrentRequestContext.User ?? new RequestUserInfo();

            document.ModifiedMoment = DateTime.UtcNow;
            document.ModifiedByInfo = user.DisplayInfo;
            document.ModifiedBy = user.PublicKey;

        }

    }

    public class ReplaceManyInput<TDocument>
    {
        public TDocument Document { get; set; }
        public Expression<Func<TDocument, bool>> FilterExpression { get; set; }
    }
}