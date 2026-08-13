using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using eShopLegacyMVC.Models;

namespace eShopLegacyMVC.Models.Infrastructure
{
    public static class CatalogDBInitializer
    {
        private const string DBCatalogSequenceName = "catalog_type_hilo";
        private const string DBBrandSequenceName = "catalog_brand_hilo";
        private const string CatalogItemHiLoSequenceScript = @"Models\Infrastructure\dbo.catalog_hilo.Sequence.sql";
        private const string CatalogBrandHiLoSequenceScript = @"Models\Infrastructure\dbo.catalog_brand_hilo.Sequence.sql";
        private const string CatalogTypeHiLoSequenceScript = @"Models\Infrastructure\dbo.catalog_type_hilo.Sequence.sql";

        public static void Initialize(CatalogDBContext context, string contentRootPath, bool useCustomizationData)
        {
            context.Database.EnsureCreated();

            // Only seed if no data present
            if (context.CatalogTypes.Any())
                return;

            var indexGenerator = new CatalogItemHiLoGenerator();

            ExecuteScript(context, contentRootPath, CatalogItemHiLoSequenceScript);
            ExecuteScript(context, contentRootPath, CatalogBrandHiLoSequenceScript);
            ExecuteScript(context, contentRootPath, CatalogTypeHiLoSequenceScript);

            AddCatalogTypes(context, contentRootPath, useCustomizationData);
            AddCatalogBrands(context, contentRootPath, useCustomizationData);
            AddCatalogItems(context, contentRootPath, useCustomizationData, indexGenerator);
            AddCatalogItemPictures(contentRootPath, useCustomizationData);
        }

        private static void AddCatalogTypes(CatalogDBContext context, string contentRootPath, bool useCustomizationData)
        {
            var preconfiguredTypes = useCustomizationData
                ? GetCatalogTypesFromFile(contentRootPath)
                : PreconfiguredData.GetPreconfiguredCatalogTypes();

            int sequenceId = GetSequenceIdFromSelectedDBSequence(context, DBCatalogSequenceName);
            foreach (var type in preconfiguredTypes)
            {
                type.Id = sequenceId;
                context.CatalogTypes.Add(type);
                sequenceId++;
            }

            context.SaveChanges();
        }

        private static void AddCatalogBrands(CatalogDBContext context, string contentRootPath, bool useCustomizationData)
        {
            var preconfiguredBrands = useCustomizationData
                ? GetCatalogBrandsFromFile(contentRootPath)
                : PreconfiguredData.GetPreconfiguredCatalogBrands();

            int sequenceId = GetSequenceIdFromSelectedDBSequence(context, DBBrandSequenceName);
            foreach (var brand in preconfiguredBrands)
            {
                brand.Id = sequenceId;
                context.CatalogBrands.Add(brand);
                sequenceId++;
            }

            context.SaveChanges();
        }

        private static void AddCatalogItems(CatalogDBContext context, string contentRootPath, bool useCustomizationData, CatalogItemHiLoGenerator indexGenerator)
        {
            var preconfiguredItems = useCustomizationData
                ? GetCatalogItemsFromFile(context, contentRootPath)
                : PreconfiguredData.GetPreconfiguredCatalogItems();

            foreach (var item in preconfiguredItems)
            {
                var sequenceId = indexGenerator.GetNextSequenceValue(context);
                item.Id = sequenceId;
                context.CatalogItems.Add(item);
            }

            context.SaveChanges();
        }

        private static IEnumerable<CatalogType> GetCatalogTypesFromFile(string contentRootPath)
        {
            string csvFileCatalogTypes = Path.Combine(contentRootPath, "Setup", "CatalogTypes.csv");

            if (!File.Exists(csvFileCatalogTypes))
            {
                return PreconfiguredData.GetPreconfiguredCatalogTypes();
            }

            string[] requiredHeaders = { "catalogtype" };
            string[] csvheaders = GetHeaders(csvFileCatalogTypes, requiredHeaders);

            return File.ReadAllLines(csvFileCatalogTypes)
                .Skip(1)
                .Select(x => CreateCatalogType(x))
                .Where(x => x != null)!;
        }

        private static CatalogType? CreateCatalogType(string type)
        {
            type = type.Trim('"').Trim();

            if (string.IsNullOrEmpty(type))
            {
                throw new Exception("catalog Type Name is empty");
            }

            return new CatalogType { Type = type };
        }

        private static IEnumerable<CatalogBrand> GetCatalogBrandsFromFile(string contentRootPath)
        {
            string csvFileCatalogBrands = Path.Combine(contentRootPath, "Setup", "CatalogBrands.csv");

            if (!File.Exists(csvFileCatalogBrands))
            {
                return PreconfiguredData.GetPreconfiguredCatalogBrands();
            }

            string[] requiredHeaders = { "catalogbrand" };
            GetHeaders(csvFileCatalogBrands, requiredHeaders);

            return File.ReadAllLines(csvFileCatalogBrands)
                .Skip(1)
                .Select(x => CreateCatalogBrand(x))
                .Where(x => x != null)!;
        }

        private static CatalogBrand? CreateCatalogBrand(string brand)
        {
            brand = brand.Trim('"').Trim();

            if (string.IsNullOrEmpty(brand))
            {
                throw new Exception("catalog Brand Name is empty");
            }

            return new CatalogBrand { Brand = brand };
        }

        private static IEnumerable<CatalogItem> GetCatalogItemsFromFile(CatalogDBContext context, string contentRootPath)
        {
            string csvFileCatalogItems = Path.Combine(contentRootPath, "Setup", "CatalogItems.csv");

            if (!File.Exists(csvFileCatalogItems))
            {
                return PreconfiguredData.GetPreconfiguredCatalogItems();
            }

            string[] requiredHeaders = { "catalogtypename", "catalogbrandname", "description", "name", "price", "pictureFileName" };
            string[] optionalheaders = { "availablestock", "restockthreshold", "maxstockthreshold", "onreorder" };
            string[] csvheaders = GetHeaders(csvFileCatalogItems, requiredHeaders, optionalheaders);

            var catalogTypeIdLookup = context.CatalogTypes.ToDictionary(ct => ct.Type!, ct => ct.Id);
            var catalogBrandIdLookup = context.CatalogBrands.ToDictionary(ct => ct.Brand!, ct => ct.Id);

            return File.ReadAllLines(csvFileCatalogItems)
                .Skip(1)
                .Select(row => Regex.Split(row, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"))
                .Select(column => CreateCatalogItem(column, csvheaders, catalogTypeIdLookup, catalogBrandIdLookup))
                .Where(x => x != null)!;
        }

        private static CatalogItem? CreateCatalogItem(string[] column, string[] headers, Dictionary<string, int> catalogTypeIdLookup, Dictionary<string, int> catalogBrandIdLookup)
        {
            if (column.Length != headers.Length)
            {
                throw new Exception($"column count '{column.Length}' not the same as headers count'{headers.Length}'");
            }

            string catalogTypeName = column[Array.IndexOf(headers, "catalogtypename")].Trim('"').Trim();
            if (!catalogTypeIdLookup.ContainsKey(catalogTypeName))
            {
                throw new Exception($"type={catalogTypeName} does not exist in catalogTypes");
            }

            string catalogBrandName = column[Array.IndexOf(headers, "catalogbrandname")].Trim('"').Trim();
            if (!catalogBrandIdLookup.ContainsKey(catalogBrandName))
            {
                throw new Exception($"brand={catalogBrandName} does not exist in catalogBrands");
            }

            string priceString = column[Array.IndexOf(headers, "price")].Trim('"').Trim();
            if (!decimal.TryParse(priceString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal price))
            {
                throw new Exception($"price={priceString} is not a valid decimal number");
            }

            var catalogItem = new CatalogItem
            {
                CatalogTypeId = catalogTypeIdLookup[catalogTypeName],
                CatalogBrandId = catalogBrandIdLookup[catalogBrandName],
                Description = column[Array.IndexOf(headers, "description")].Trim('"').Trim(),
                Name = column[Array.IndexOf(headers, "name")].Trim('"').Trim(),
                Price = price,
                PictureFileName = column[Array.IndexOf(headers, "picturefilename")].Trim('"').Trim(),
            };

            int availableStockIndex = Array.IndexOf(headers, "availablestock");
            if (availableStockIndex != -1)
            {
                string availableStockString = column[availableStockIndex].Trim('"').Trim();
                if (!string.IsNullOrEmpty(availableStockString))
                {
                    if (int.TryParse(availableStockString, out int availableStock))
                        catalogItem.AvailableStock = availableStock;
                    else
                        throw new Exception($"availableStock={availableStockString} is not a valid integer");
                }
            }

            int restockThresholdIndex = Array.IndexOf(headers, "restockthreshold");
            if (restockThresholdIndex != -1)
            {
                string restockThresholdString = column[restockThresholdIndex].Trim('"').Trim();
                if (!string.IsNullOrEmpty(restockThresholdString))
                {
                    if (int.TryParse(restockThresholdString, out int restockThreshold))
                        catalogItem.RestockThreshold = restockThreshold;
                    else
                        throw new Exception($"restockThreshold={restockThresholdString} is not a valid integer");
                }
            }

            int maxStockThresholdIndex = Array.IndexOf(headers, "maxstockthreshold");
            if (maxStockThresholdIndex != -1)
            {
                string maxStockThresholdString = column[maxStockThresholdIndex].Trim('"').Trim();
                if (!string.IsNullOrEmpty(maxStockThresholdString))
                {
                    if (int.TryParse(maxStockThresholdString, out int maxStockThreshold))
                        catalogItem.MaxStockThreshold = maxStockThreshold;
                    else
                        throw new Exception($"maxStockThreshold={maxStockThresholdString} is not a valid integer");
                }
            }

            int onReorderIndex = Array.IndexOf(headers, "onreorder");
            if (onReorderIndex != -1)
            {
                string onReorderString = column[onReorderIndex].Trim('"').Trim();
                if (!string.IsNullOrEmpty(onReorderString))
                {
                    if (bool.TryParse(onReorderString, out bool onReorder))
                        catalogItem.OnReorder = onReorder;
                    else
                        throw new Exception($"onReorder={onReorderString} is not a valid boolean");
                }
            }

            return catalogItem;
        }

        private static string[] GetHeaders(string csvfile, string[] requiredHeaders, string[]? optionalHeaders = null)
        {
            string[] csvheaders = File.ReadLines(csvfile).First().ToLowerInvariant().Split(',');

            if (csvheaders.Length < requiredHeaders.Length)
            {
                throw new Exception($"requiredHeader count '{requiredHeaders.Length}' is bigger than csv header count '{csvheaders.Length}'");
            }

            if (optionalHeaders != null)
            {
                if (csvheaders.Length > (requiredHeaders.Length + optionalHeaders.Length))
                {
                    throw new Exception($"csv header count '{csvheaders.Length}' is larger than required '{requiredHeaders.Length}' and optional '{optionalHeaders.Length}' headers count");
                }
            }

            foreach (var requiredHeader in requiredHeaders)
            {
                if (!csvheaders.Contains(requiredHeader.ToLowerInvariant()))
                {
                    throw new Exception($"does not contain required header '{requiredHeader}'");
                }
            }

            return csvheaders;
        }

        private static int GetSequenceIdFromSelectedDBSequence(CatalogDBContext context, string dBSequenceName)
        {
            var connection = context.Database.GetDbConnection();
            var wasOpen = connection.State == System.Data.ConnectionState.Open;
            if (!wasOpen)
                connection.Open();

            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"SELECT NEXT VALUE FOR {dBSequenceName}";
                var result = cmd.ExecuteScalar();
                return Convert.ToInt32(result);
            }
            finally
            {
                if (!wasOpen)
                    connection.Close();
            }
        }

        private static void ExecuteScript(CatalogDBContext context, string contentRootPath, string scriptFile)
        {
            var scriptFilePath = Path.Combine(contentRootPath, scriptFile);
            if (File.Exists(scriptFilePath))
            {
                context.Database.ExecuteSqlRaw(File.ReadAllText(scriptFilePath));
            }
        }

        private static void AddCatalogItemPictures(string contentRootPath, bool useCustomizationData)
        {
            if (!useCustomizationData)
                return;

            var picturePath = new DirectoryInfo(Path.Combine(contentRootPath, "Pics"));
            if (!picturePath.Exists)
                picturePath.Create();

            foreach (var file in picturePath.GetFiles())
            {
                file.Delete();
            }

            string zipFileCatalogItemPictures = Path.Combine(contentRootPath, "Setup", "CatalogItems.zip");
            if (File.Exists(zipFileCatalogItemPictures))
            {
                ZipFile.ExtractToDirectory(zipFileCatalogItemPictures, picturePath.ToString());
            }
        }
    }
}
