using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;

namespace AspNetCore.Identity.CosmosDb
{
    internal static class Utilities
    {
        internal static int GenerateRandomInt()
        {
            Thread.Sleep(20); // Ensure that the seed changes.
            var rand = new Random();
            return rand.Next(1, int.MaxValue);
        }
    }
}
