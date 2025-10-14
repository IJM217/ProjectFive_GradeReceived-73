//MSTISR001


// Summary: Builds configured SQL connections for the application, resolving database file paths.

#region Using Directives
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
#endregion

namespace PhumlaKamnandiHotelSystem.Data
{
    public static class DatabaseConnectionFactory
    {
        #region Constants

        private const string ConnectionName = "PhumlaKamnandiHotelSystem.Properties.Settings.PKDBConnectionString";

        #endregion

        #region Lazy State

        private static readonly Lazy<string> ConnectionString = new Lazy<string>(BuildConnectionString);

        #endregion

        #region Factory Methods

        public static SqlConnection Create()
        {
            return new SqlConnection(ConnectionString.Value);
        }

        #endregion

        #region Helper Methods

        private static string BuildConnectionString()
        {
            var settings = ConfigurationManager.ConnectionStrings[ConnectionName];
            if (settings == null)
            {
                throw new InvalidOperationException($"Connection string '{ConnectionName}' was not found in configuration.");
            }

            var connectionString = settings.ConnectionString;
            const string placeholder = "|DataDirectory|";

            if (connectionString.IndexOf(placeholder, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var databasePath = LocateDatabaseFile("PKDB.mdf");
                var dataDirectory = Path.GetDirectoryName(databasePath)!;
                AppDomain.CurrentDomain.SetData("DataDirectory", dataDirectory);
                connectionString = connectionString.Replace(placeholder, dataDirectory);
            }

            return connectionString;
        }

        private static string LocateDatabaseFile(string fileName)
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            while (directory != null)
            {
                var directPath = Path.Combine(directory.FullName, fileName);
                if (File.Exists(directPath))
                {
                    return directPath;
                }

                var dataPath = Path.Combine(directory.FullName, "Data", fileName);
                if (File.Exists(dataPath))
                {
                    return dataPath;
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException($"Unable to locate database file '{fileName}'.");
        }

        #endregion
    }
}
