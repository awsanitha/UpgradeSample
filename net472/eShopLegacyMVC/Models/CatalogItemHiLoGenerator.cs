using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace eShopLegacyMVC.Models
{
    public class CatalogItemHiLoGenerator
    {
        private const int HiLoIncrement = 10;
        private int sequenceId = -1;
        private int remainningLoIds = 0;
        private readonly object sequenceLock = new object();

        public int GetNextSequenceValue(CatalogDBContext db)
        {
            lock (sequenceLock)
            {
                if (remainningLoIds == 0)
                {
                    // EF Core: use FromSqlRaw on a keyless entity or ExecuteSqlRaw
                    var result = db.Database.SqlQueryRaw<long>("SELECT NEXT VALUE FOR catalog_hilo").Single();
                    sequenceId = (int)result;
                    remainningLoIds = HiLoIncrement - 1;
                    return sequenceId;
                }
                else
                {
                    remainningLoIds--;
                    return ++sequenceId;
                }
            }
        }
    }
}
