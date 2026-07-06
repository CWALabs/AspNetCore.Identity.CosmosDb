using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AspNetCore.Identity.CosmosDb.Contracts;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.Identity.CosmosDb.Repositories
{
    // Keep synchronous members for compatibility with older callers, but prefer the async
    // methods for all Cosmos-backed execution paths.
    public class CosmosIdentityRepository<TDbContext, TUserEntity, TRoleEntity, TKey> : IRepository
        where TDbContext : CosmosIdentityDbContext<TUserEntity, TRoleEntity, TKey>
        where TUserEntity : IdentityUser<TKey>
        where TRoleEntity : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
    {
        protected TDbContext _db;

        public IQueryable Users
        {
            get { return _db.Users.AsQueryable(); }
        }

        public IQueryable Roles
        {
            get { return _db.Roles.AsQueryable(); }
        }

        public IQueryable UserClaims
        {
            get { return _db.UserClaims.AsQueryable(); }
        }

        public IQueryable UserRoles
        {
            get { return _db.UserRoles.AsQueryable(); }
        }

        public IQueryable UserLogins
        {
            get { return _db.UserLogins.AsQueryable(); }
        }

        public IQueryable RoleClaims
        {
            get { return _db.RoleClaims.AsQueryable(); }
        }

        public IQueryable UserTokens
        {
            get { return _db.UserTokens.AsQueryable(); }
        }

        public CosmosIdentityRepository(TDbContext db)
        {
            _db = db;
        }

        public DbSet<TEntity> Table<TEntity>()
            where TEntity : class, new()
        {
            return _db.Set<TEntity>();
        }

        [Obsolete("Synchronous Cosmos operations are not recommended. Use GetByIdAsync instead.")]
        public TEntity? GetById<TEntity>(string id)
            where TEntity : class, new()
        {
            return GetByIdAsync<TEntity>(id).GetAwaiter().GetResult();
        }

        public async Task<TEntity?> GetByIdAsync<TEntity>(string id, CancellationToken cancellationToken = default)
            where TEntity : class, new()
        {
            return await _db.Set<TEntity>()
                .AsNoTracking()
                .WithPartitionKey(id)
                .SingleOrDefaultAsync(cancellationToken);
        }

        [Obsolete("Synchronous Cosmos operations are not recommended. Use TryFindOneAsync instead.")]
        public TEntity? TryFindOne<TEntity>(Expression<Func<TEntity, bool>> predicate)
            where TEntity : class, new()
        {
            return TryFindOneAsync<TEntity>(predicate).GetAwaiter().GetResult();
        }

        public async Task<TEntity?> TryFindOneAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
            where TEntity : class, new()
        {
            return await _db.Set<TEntity>().AsNoTracking().SingleOrDefaultAsync(predicate, cancellationToken);
        }

        public IQueryable<TEntity> Find<TEntity>(Expression<Func<TEntity, bool>> predicate)
            where TEntity : class, new()
        {
            return _db.Set<TEntity>().Where(predicate);
        }

        public void Add<TEntity>(TEntity entity)
            where TEntity : class, new()
        {
            _db.Add(entity);
        }

        public void Update<TEntity>(TEntity entity)
            where TEntity : class, new()
        {
            // Get the primary key values for the entity
            var entry = _db.Entry(entity);
            var keyValues = entry.Metadata.FindPrimaryKey()?.Properties
                .Select(p => entry.Property(p.Name).CurrentValue)
                .ToArray();

            if (keyValues != null && keyValues.Length > 0)
            {
                // Check if another instance with the same key is already tracked
                var localEntity = _db.Set<TEntity>().Local
                    .FirstOrDefault(e =>
                    {
                        var localEntry = _db.Entry(e);
                        var localKeyValues = entry.Metadata.FindPrimaryKey()?.Properties
                            .Select(p => localEntry.Property(p.Name).CurrentValue)
                            .ToArray();

                        if (localKeyValues == null || localKeyValues.Length != keyValues.Length)
                            return false;

                        // Compare key values, handling byte arrays specially
                        for (int i = 0; i < keyValues.Length; i++)
                        {
                            if (keyValues[i] is byte[] keyBytes && localKeyValues[i] is byte[] localBytes)
                            {
                                if (!keyBytes.SequenceEqual(localBytes))
                                    return false;
                            }
                            else if (!Equals(keyValues[i], localKeyValues[i]))
                            {
                                return false;
                            }
                        }

                        return true;
                    });

                // If found and it's a different instance, detach it
                if (localEntity != null && !ReferenceEquals(localEntity, entity))
                {
                    _db.Entry(localEntity).State = EntityState.Detached;
                }
            }

            entry.State = EntityState.Modified;
        }

        [Obsolete("Synchronous Cosmos operations are not recommended. Use DeleteByIdAsync instead.")]
        public void DeleteById<TEntity>(string id)
            where TEntity : class, new()
        {
            DeleteByIdAsync<TEntity>(id).GetAwaiter().GetResult();
        }

        public async Task DeleteByIdAsync<TEntity>(string id, CancellationToken cancellationToken = default)
            where TEntity : class, new()
        {
            var entity = await GetByIdAsync<TEntity>(id, cancellationToken);
            if (entity != null)
            {
                Delete(entity);
            }
        }

        public void Delete<TEntity>(TEntity entity)
            where TEntity : class, new()
        {
            var entry = _db.Entry(entity);

            // If the entity is not tracked, we need to attach it first
            // But if another instance with the same key is tracked, we need to use that one instead
            if (entry.State == EntityState.Detached)
            {
                var keyValues = entry.Metadata.FindPrimaryKey()!.Properties
                    .Select(p => entry.Property(p.Name).CurrentValue)
                    .ToArray();

                // Find if there's already a tracked entity with the same key
                var trackedEntity = _db.ChangeTracker.Entries<TEntity>()
                    .FirstOrDefault(e =>
                    {
                        var trackedKeyValues = e.Metadata.FindPrimaryKey()!.Properties
                            .Select(p => e.Property(p.Name).CurrentValue)
                            .ToArray();

                        // Compare key values, handling byte arrays specially
                        if (keyValues.Length != trackedKeyValues.Length)
                            return false;

                        for (int i = 0; i < keyValues.Length; i++)
                        {
                            var keyValue = keyValues[i];
                            var trackedKeyValue = trackedKeyValues[i];

                            // Handle byte array comparison
                            if (keyValue is byte[] keyBytes && trackedKeyValue is byte[] trackedKeyBytes)
                            {
                                if (!keyBytes.SequenceEqual(trackedKeyBytes))
                                    return false;
                            }
                            else if (!Equals(keyValue, trackedKeyValue))
                            {
                                return false;
                            }
                        }

                        return true;
                    });

                if (trackedEntity != null)
                {
                    // Use the already-tracked entity
                    _db.Remove(trackedEntity.Entity);
                }
                else
                {
                    // Attach and delete
                    _db.Attach(entity);
                    _db.Remove(entity);
                }
            }
            else
            {
                // Entity is already tracked, just mark for deletion
                _db.Remove(entity);
            }
        }

        [Obsolete("Synchronous Cosmos operations are not recommended. Use DeleteAsync(predicate) instead.")]
        public void Delete<TEntity>(Expression<Func<TEntity, bool>> predicate)
            where TEntity : class, new()
        {
            DeleteAsync<TEntity>(predicate).GetAwaiter().GetResult();
        }

        public async Task DeleteAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
            where TEntity : class, new()
        {
            var entities = await _db.Set<TEntity>().AsNoTracking().Where(predicate).ToListAsync(cancellationToken);
            entities.ForEach(entity => Delete(entity));
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}