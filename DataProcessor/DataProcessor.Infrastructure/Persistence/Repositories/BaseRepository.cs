using DataProcessor.Domain.Entities.Base;
using MongoDB.Driver;

namespace DataProcessor.Infrastructure.Persistence.Repositories;

public class BaseRepository<T>(IMongoDatabase database) : IBaseRepository<T> where T : BaseEntity
{
    protected readonly IMongoCollection<T> Collection = database.GetCollection<T>(typeof(T).Name);

    public virtual async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Eq("Id", id);
        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Collection.Find(Builders<T>.Filter.Empty).ToListAsync(cancellationToken);
    }

    public virtual async Task InsertAsync(T entity, CancellationToken cancellationToken = default)
    {
        await Collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var idProp = typeof(T).GetProperty("Id")
            ?? throw new InvalidOperationException("Entity must have an Id property.");

        var id = idProp.GetValue(entity)?.ToString();
        if (string.IsNullOrEmpty(id))
            throw new InvalidOperationException("Entity Id cannot be null or empty.");

        var filter = Builders<T>.Filter.Eq("Id", id);
        await Collection.ReplaceOneAsync(filter, entity, new ReplaceOptions { IsUpsert = false }, cancellationToken);
    }

    public virtual async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Eq("Id", id);
        await Collection.DeleteOneAsync(filter, cancellationToken);
    }
}
