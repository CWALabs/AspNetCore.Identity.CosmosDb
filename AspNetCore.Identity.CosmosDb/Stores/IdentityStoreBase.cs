using AspNetCore.Identity.CosmosDb.Contracts;
using Microsoft.AspNetCore.Identity;
using System;

namespace AspNetCore.Identity.CosmosDb.Stores
{
    /// <summary>
    /// Identity store base
    /// </summary>
    public abstract class IdentityStoreBase
    {
        protected readonly IRepository _repo;
        protected bool _disposed;

        protected IdentityStoreBase(IRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Throws if this class has been disposed.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>
        /// Processes exceptions thrown by a store method.
        /// </summary>
        protected IdentityResult ProcessExceptions(Exception e) =>
            IdentityResult.Failed(new IdentityError { Code = "500", Description = e.Message });
    }
}
