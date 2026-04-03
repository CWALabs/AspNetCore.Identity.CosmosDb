using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.Identity.CosmosDb.Contracts
{
    /// <summary>
    /// Cosmos Repository interface
    /// </summary>
    public interface IRepository
    {
        IQueryable Users { get; }
        IQueryable Roles { get; }

        IQueryable UserClaims { get; }
        IQueryable UserRoles { get; }

        IQueryable UserLogins { get; }
        IQueryable RoleClaims { get; }
        IQueryable UserTokens { get; }

        DbSet<TEntity> Table<TEntity>() where TEntity : class, new();

        [Obsolete("Synchronous Cosmos operations are not recommended. Use GetByIdAsync instead.")]
        TEntity? GetById<TEntity>(string id) where TEntity : class, new();

        Task<TEntity?> GetByIdAsync<TEntity>(string id, CancellationToken cancellationToken = default) where TEntity : class, new();

        [Obsolete("Synchronous Cosmos operations are not recommended. Use TryFindOneAsync instead.")]
        TEntity? TryFindOne<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class, new();

        Task<TEntity?> TryFindOneAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class, new();

        IQueryable<TEntity> Find<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class, new();

        void Add<TEntity>(TEntity entity) where TEntity : class, new();

        void Update<TEntity>(TEntity entity) where TEntity : class, new();

        [Obsolete("Synchronous Cosmos operations are not recommended. Use DeleteByIdAsync instead.")]
        void DeleteById<TEntity>(string id) where TEntity : class, new();

        Task DeleteByIdAsync<TEntity>(string id, CancellationToken cancellationToken = default) where TEntity : class, new();

        void Delete<TEntity>(TEntity entity) where TEntity : class, new();

        [Obsolete("Synchronous Cosmos operations are not recommended. Use DeleteAsync(predicate) instead.")]
        void Delete<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class, new();

        Task DeleteAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class, new();

        Task SaveChangesAsync();
    }
}