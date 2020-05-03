using System;
using Cinema.Core.Data;

namespace Cinema.Web.Infrastructure
{
    /// <summary>
    /// Containers cannot easily rewrite Web.config, so a <c>CINEMA_DB</c> environment variable
    /// (a full SQL Server connection string) wins over the named connection string.
    /// </summary>
    public static class ConnectionStrings
    {
        public const string EnvironmentVariable = "CINEMA_DB";

        public static string Cinema()
        {
            var fromEnv = Environment.GetEnvironmentVariable(EnvironmentVariable);
            return string.IsNullOrWhiteSpace(fromEnv) ? "name=" + CinemaDbContext.ConnectionStringName : fromEnv;
        }
    }
}
