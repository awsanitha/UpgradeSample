using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace eShopLegacyMVC.Models
{
    public class CatalogItemHiLoGenerator
    {
        private const int HiLoIncrement = 10;
        private int _sequenceId = -1;
        private int _remainingLoIds = 0;
        private readonly object _sequenceLock = new object();

        public int GetNextSequenceValue(CatalogDBContext db)
        {
            lock (_sequenceLock)
            {
                if (_remainingLoIds == 0)
                {
                    var result = db.Database.SqlQuery<long>($"SELECT NEXT VALUE FOR catalog_hilo").Single();
                    _sequenceId = (int)result;
                    _remainingLoIds = HiLoIncrement - 1;
                    return _sequenceId;
                }
                else
                {
                    _remainingLoIds--;
                    return ++_sequenceId;
                }
            }
        }
    }
}
