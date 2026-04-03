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
            return await _db.Set<TEntity>().SingleOrDefaultAsync(predicate, cancellationToken);
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
            var dbEntry = _db.Entry(entity);
            dbEntry.State = EntityState.Modified;
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
            _db.Remove(entity);
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
            var entities = await _db.Set<TEntity>().Where(predicate).ToListAsync(cancellationToken);
            entities.ForEach(entity => _db.Remove(entity));
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}