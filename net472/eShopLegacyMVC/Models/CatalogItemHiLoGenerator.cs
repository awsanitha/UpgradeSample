using Microsoft.EntityFrameworkCore;
using System;
using System.Data;

namespace eShopLegacyMVC.Models
{
    public class CatalogItemHiLoGenerator
    {
        private const int HiLoIncrement = 10;
        private int sequenceId = -1;
        private int remainingLoIds = 0;
        private readonly object sequenceLock = new object();

        public int GetNextSequenceValue(CatalogDBContext db)
        {
            lock (sequenceLock)
            {
                if (remainingLoIds == 0)
                {
                    sequenceId = (int)GetNextSequenceFromDb(db);
                    remainingLoIds = HiLoIncrement - 1;
                    return sequenceId;
                }
                else
                {
                    remainingLoIds--;
                    return ++sequenceId;
                }
            }
        }

        private static long GetNextSequenceFromDb(CatalogDBContext db)
        {
            var connection = db.Database.GetDbConnection();
            var wasOpen = connection.State == ConnectionState.Open;
            if (!wasOpen)
                connection.Open();

            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT NEXT VALUE FOR catalog_hilo;";
                var result = cmd.ExecuteScalar();
                return Convert.ToInt64(result);
            }
            finally
            {
                if (!wasOpen)
                    connection.Close();
            }
        }
    }
}
