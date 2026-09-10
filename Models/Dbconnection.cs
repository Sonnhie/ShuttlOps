using System;
using System.IO;
using DotNetEnv;

namespace ShuttlOps.Models
{
    public class Dbconnection
    {
        public static string GetConnectionString()
        {

            Env.Load();

            string? server = Environment.GetEnvironmentVariable("DB_SERVER");
            string? db = Environment.GetEnvironmentVariable("DB_NAME");
            string? user = Environment.GetEnvironmentVariable("DB_USER");
            string? pass = Environment.GetEnvironmentVariable("DB_PASSWORD");

            if (string.IsNullOrWhiteSpace(server) ||
                string.IsNullOrWhiteSpace(db) ||
                string.IsNullOrWhiteSpace(user))
            {
                throw new InvalidOperationException("Database environment variables are missing from .env.");
            }

            return $"Data Source={server};Initial Catalog={db};User ID={user};Password={pass};Encrypt=False";
        }
    }
}
