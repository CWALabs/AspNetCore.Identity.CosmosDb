using Microsoft.AspNetCore.Identity;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.Identity.CosmosDb.Extensions
{
    /// \u003Csummary\u003E
    /// Extension methods for RoleManager
    /// \u003C/summary\u003E
    public static class RoleManagerExtensions
    {
        /// \u003Csummary\u003E
        /// Sets the name of the specified role and persists the change.
        /// \u003C/summary\u003E
        /// \u003Ctypeparam name="TRole"\u003EThe type representing a role.\u003C/typeparam\u003E
        /// \u003Cparam name="roleManager"\u003EThe RoleManager instance.\u003C/param\u003E
        /// \u003Cparam name="role"\u003EThe role whose name should be set.\u003C/param\u003E
        /// \u003Cparam name="name"\u003EThe name to set.\u003C/param\u003E
        /// \u003Creturns\u003EThe result of the asynchronous operation.\u003C/returns\u003E
        public static async Task<IdentityResult> SetRoleNameAsync<TRole>(
            this RoleManager<TRole> roleManager,
            TRole role,
            string name) where TRole : class
        {
            ArgumentNullException.ThrowIfNull(roleManager);
            ArgumentNullException.ThrowIfNull(role);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            // Set the role name directly using dynamic to access the Name property
            ((dynamic)role).Name = name;

            // Update the normalized role name
            await roleManager.UpdateNormalizedRoleNameAsync(role);

            // Persist the changes
            return await roleManager.UpdateAsync(role);
        }
    }
}
