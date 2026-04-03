using AspNetCore.Identity.CosmosDb.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.Identity.CosmosDb.Stores
{
    /// <summary>
    /// Cosmos DB Role Store
    /// </summary>
    /// <typeparam name="TRoleEntity"></typeparam>
    public class CosmosRoleStore<TRoleEntity, TKey> : IdentityStoreBase,
        IRoleStore<TRoleEntity>,
        IQueryableRoleStore<TRoleEntity>,
        IRoleClaimStore<TRoleEntity>
        where TRoleEntity : IdentityRole<TKey>, new()
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// Role query
        /// </summary>
        public IQueryable<TRoleEntity> Roles => (IQueryable<TRoleEntity>)_repo.Roles;

        /// <summary>
        /// UserRoles query
        /// </summary>
        public IQueryable<IdentityUserRole<TKey>> UserRoles => (IQueryable<IdentityUserRole<TKey>>)_repo.UserRoles;

        /// <summary>
        /// UserRoles query
        /// </summary>
        public IQueryable<IdentityRoleClaim<TKey>> RoleClaims => (IQueryable<IdentityRoleClaim<TKey>>)_repo.RoleClaims;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="repo"></param>
        public CosmosRoleStore(IRepository repo) : base(repo)
        {
        }


        // <inheritdoc />
        public async Task<IdentityResult> CreateAsync(TRoleEntity role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (role == null)
                throw new ArgumentNullException(nameof(role));

            try
            {
                _repo.Add(role);
                await _repo.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return ProcessExceptions(ex);
            }

            return IdentityResult.Success;
        }

        // <inheritdoc />
        public async Task<IdentityResult> DeleteAsync(TRoleEntity role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            try
            {
                var userRoles = await UserRoles.Where(w => w.RoleId.Equals(role.Id)).ToListAsync(cancellationToken);
                foreach (var userRole in userRoles)
                {
                    _repo.Delete(userRole);
                }

                var roleClaims = await RoleClaims.Where(w => w.RoleId.Equals(role.Id)).ToListAsync(cancellationToken);
                foreach (var roleClaim in roleClaims)
                {
                    _repo.Delete(roleClaim);
                }

                _repo.Delete(role);
                await _repo.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return ProcessExceptions(ex);
            }

            return IdentityResult.Success;
        }

        // <inheritdoc />
        public async Task<TRoleEntity?> FindByIdAsync(string roleId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(roleId))
                throw new ArgumentNullException(nameof(roleId));

            var typedId = (TKey)TypeDescriptor.GetConverter(typeof(TKey)).ConvertFromInvariantString(roleId)!;
            var role = await _repo.Table<TRoleEntity>()
                .SingleOrDefaultAsync(_ => _.Id.Equals(typedId), cancellationToken: cancellationToken);

            return role;
        }

        // <inheritdoc />
        public async Task<TRoleEntity?> FindByNameAsync(string normalizedName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(normalizedName))
                throw new ArgumentNullException(nameof(normalizedName));

            var role = await _repo.Table<TRoleEntity>()
                .FirstOrDefaultAsync(_ => _.NormalizedName == normalizedName, cancellationToken: cancellationToken);

            return role;
        }

        // <inheritdoc />
        public Task<string?> GetNormalizedRoleNameAsync(TRoleEntity role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            return Task.FromResult(role.NormalizedName);
        }

        // <inheritdoc />
        public Task<string> GetRoleIdAsync(TRoleEntity role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            return Task.FromResult(role.Id.ToString()!);
        }

        // <inheritdoc />
        public Task<string?> GetRoleNameAsync(TRoleEntity role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            return Task.FromResult(role.Name);
        }

        // <inheritdoc />
        public Task SetNormalizedRoleNameAsync(TRoleEntity role, string? normalizedName,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            SetRoleProperty(role, normalizedName, (u, m) => u.NormalizedName = normalizedName, cancellationToken);

            return Task.CompletedTask;
        }

        // <inheritdoc />
        public Task SetRoleNameAsync(TRoleEntity role, string? roleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (role == null)
                throw new ArgumentNullException(nameof(role));


            SetRoleProperty(role, roleName, (u, m) => u.Name = roleName, cancellationToken);

            return Task.CompletedTask;
        }

        // <inheritdoc />
        public async Task<IdentityResult> UpdateAsync(TRoleEntity role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(role);

            role.ConcurrencyStamp = Guid.NewGuid().ToString();

            try
            {
                _repo.Update(role);
                await _repo.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return ProcessExceptions(ex);
            }

            return IdentityResult.Success;
        }

        private void SetRoleProperty<T>(TRoleEntity user, T value, Action<TRoleEntity, T> setter,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (user == null) throw new ArgumentNullException(nameof(user));
            if (EqualityComparer<T>.Default.Equals(value, default)) throw new ArgumentNullException(nameof(value));

            setter(user, value);
        }

        // <inheritdoc />
        public void Dispose()
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        #region Methods that implement IRoleClaimStore<TRoleEntity>

        // <inheritdoc />
        public async Task<IList<Claim>> GetClaimsAsync(TRoleEntity role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            var claims = await GetRoleClaimsInternalAsync(role.Id, cancellationToken);

            return claims.Select(c => c.ToClaim()).ToList();
        }

        private async Task<List<IdentityRoleClaim<TKey>>> GetRoleClaimsInternalAsync(TKey roleId,
            CancellationToken cancellationToken)
        {
            return await _repo.Table<IdentityRoleClaim<TKey>>().Where(c => c.RoleId.Equals(roleId))
                .ToListAsync(cancellationToken);
        }

        // <inheritdoc />
        public async Task AddClaimAsync(TRoleEntity role, Claim claim, CancellationToken cancellationToken = default)
        {
            // Since the IdentityRoleClaim requires an integer ID, we need to get the last ID used and increment by one.
            // This means that if this fails, because of a concurrency issue, we need to retry.
            await Retry.DoAsync(async () => await InternalAddClaimAsync(role, claim, cancellationToken), TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
        }

        private async Task InternalAddClaimAsync(TRoleEntity role, Claim claim, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
                throw new ArgumentNullException(nameof(role));
            if (claim == null)
                throw new ArgumentNullException(nameof(claim));

            var identityRoleClaim = new IdentityRoleClaim<TKey>()
            {
                ClaimType = claim.Type,
                ClaimValue = claim.Value,
                RoleId = role.Id,
                Id = Utilities.GenerateRandomInt()
            };

            _repo.Add(identityRoleClaim);
            await _repo.SaveChangesAsync().WaitAsync(cancellationToken);
        }

        // <inheritdoc />
        public async Task RemoveClaimAsync(TRoleEntity role, Claim claim, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
                throw new ArgumentNullException(nameof(role));
            if (claim == null)
                throw new ArgumentNullException(nameof(claim));

            var doomed = await FindRoleClaimAsync(role.Id, claim.Type, claim.Value, cancellationToken);
            if (doomed != null)
            {
                _repo.Delete(doomed);
                await _repo.SaveChangesAsync().WaitAsync(cancellationToken);
            }
        }

        private async Task<IdentityRoleClaim<TKey>?> FindRoleClaimAsync(TKey roleId, string claimType, string claimValue,
            CancellationToken cancellationToken)
        {
            return await _repo.Table<IdentityRoleClaim<TKey>>()
                .FirstOrDefaultAsync(c => c.RoleId.Equals(roleId) &&
                                          c.ClaimValue == claimValue && c.ClaimType == claimType, cancellationToken);
        }

        #endregion
    }
}