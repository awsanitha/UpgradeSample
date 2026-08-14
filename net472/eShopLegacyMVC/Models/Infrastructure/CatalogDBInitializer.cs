using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using eShopLegacyMVC.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace eShopLegacyMVC.Models.Infrastructure
{
    public class CatalogDBInitializer
    {
        private const string DBCatalogSequenceName = "catalog_type_hilo";
        private const string DBBrandSequenceName = "catalog_brand_hilo";
        private const string CatalogItemHiLoSequenceScript = @"Models/Infrastructure/dbo.catalog_hilo.Sequence.sql";
        private const string CatalogBrandHiLoSequenceScript = @"Models/Infrastructure/dbo.catalog_brand_hilo.Sequence.sql";
        private const string CatalogTypeHiLoSequenceScript = @"Models/Infrastructure/dbo.catalog_type_hilo.Sequence.sql";

        private readonly CatalogItemHiLoGenerator _indexGenerator;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CatalogDBInitializer> _logger;
        private readonly bool _useCustomizationData;

        public CatalogDBInitializer(
            CatalogItemHiLoGenerator indexGenerator,
            IWebHostEnvironment env,
            IConfiguration configuration,
            ILogger<CatalogDBInitializer> logger)
        {
            _indexGenerator = indexGenerator;
            _env = env;
            _configuration = configuration;
            _logger = logger;
            _useCustomizationData = bool.Parse(_configuration["AppSettings:UseCustomizationData"] ?? "false");
        }

        public void Initialize(IServiceProvider services)
        {
            var context = services.GetRequiredService<CatalogDBContext>();

            try
            {
                context.Database.EnsureCreated();
                Seed(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred seeding the database.");
            }
        }

        private void Seed(CatalogDBContext context)
        {
            // Only seed if empty
            if (context.CatalogItems.Any())
                return;

            ExecuteScript(context, CatalogItemHiLoSequenceScript);
            ExecuteScript(context, CatalogBrandHiLoSequenceScript);
            ExecuteScript(context, CatalogTypeHiLoSequenceScript);

            AddCatalogTypes(context);
            AddCatalogBrands(context);
            AddCatalogItems(context);
            AddCatalogItemPictures();
        }

        private void AddCatalogTypes(CatalogDBContext context)
        {
            var preconfiguredTypes = _useCustomizationData
                ? GetCatalogTypesFromFile()
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

        private void AddCatalogBrands(CatalogDBContext context)
        {
            var preconfiguredBrands = _useCustomizationData
                ? GetCatalogBrandsFromFile()
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

        private void AddCatalogItems(CatalogDBContext context)
        {
            var preconfiguredItems = _useCustomizationData
                ? GetCatalogItemsFromFile(context)
                : PreconfiguredData.GetPreconfiguredCatalogItems();

            foreach (var item in preconfiguredItems)
            {
                var sequenceId = _indexGenerator.GetNextSequenceValue(context);
                item.Id = sequenceId;
                context.CatalogItems.Add(item);
            }

            context.SaveChanges();
        }

        private IEnumerable<CatalogType> GetCatalogTypesFromFile()
        {
            var contentRootPath = _env.ContentRootPath;
            string csvFile = Path.Combine(contentRootPath, "Setup", "CatalogTypes.csv");

            if (!File.Exists(csvFile))
                return PreconfiguredData.GetPreconfiguredCatalogTypes();

            string[] requiredHeaders = { "catalogtype" };
            var csvheaders = GetHeaders(csvFile, requiredHeaders);

            return File.ReadAllLines(csvFile)
                .Skip(1)
                .Select(CreateCatalogType)
                .Where(x => x != null)!;
        }

        private static CatalogType? CreateCatalogType(string type)
        {
            type = type.Trim('"').Trim();
            if (string.IsNullOrEmpty(type))
                throw new Exception("catalog Type Name is empty");
            return new CatalogType { Type = type };
        }

        private IEnumerable<CatalogBrand> GetCatalogBrandsFromFile()
        {
            var contentRootPath = _env.ContentRootPath;
            string csvFile = Path.Combine(contentRootPath, "Setup", "CatalogBrands.csv");

            if (!File.Exists(csvFile))
                return PreconfiguredData.GetPreconfiguredCatalogBrands();

            string[] requiredHeaders = { "catalogbrand" };
            GetHeaders(csvFile, requiredHeaders);

            return File.ReadAllLines(csvFile)
                .Skip(1)
                .Select(CreateCatalogBrand)
                .Where(x => x != null)!;
        }

        private static CatalogBrand? CreateCatalogBrand(string brand)
        {
            brand = brand.Trim('"').Trim();
            if (string.IsNullOrEmpty(brand))
                throw new Exception("catalog Brand Name is empty");
            return new CatalogBrand { Brand = brand };
        }

        private IEnumerable<CatalogItem> GetCatalogItemsFromFile(CatalogDBContext context)
        {
            var contentRootPath = _env.ContentRootPath;
            string csvFile = Path.Combine(contentRootPath, "Setup", "CatalogItems.csv");

            if (!File.Exists(csvFile))
                return PreconfiguredData.GetPreconfiguredCatalogItems();

            string[] requiredHeaders = { "catalogtypename", "catalogbrandname", "description", "name", "price", "picturefilename" };
            string[] optionalHeaders = { "availablestock", "restockthreshold", "maxstockthreshold", "onreorder" };
            var csvheaders = GetHeaders(csvFile, requiredHeaders, optionalHeaders);

            var catalogTypeIdLookup = context.CatalogTypes.ToDictionary(ct => ct.Type!, ct => ct.Id);
            var catalogBrandIdLookup = context.CatalogBrands.ToDictionary(ct => ct.Brand!, ct => ct.Id);

            return File.ReadAllLines(csvFile)
                .Skip(1)
                .Select(row => Regex.Split(row, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"))
                .Select(column => CreateCatalogItem(column, csvheaders, catalogTypeIdLookup, catalogBrandIdLookup))
                .Where(x => x != null)!;
        }

        private static CatalogItem? CreateCatalogItem(string[] column, string[] headers,
            Dictionary<string, int> catalogTypeIdLookup, Dictionary<string, int> catalogBrandIdLookup)
        {
            if (column.Length != headers.Length)
                throw new Exception($"column count '{column.Length}' not the same as headers count '{headers.Length}'");

            string catalogTypeName = column[Array.IndexOf(headers, "catalogtypename")].Trim('"').Trim();
            if (!catalogTypeIdLookup.ContainsKey(catalogTypeName))
                throw new Exception($"type={catalogTypeName} does not exist in catalogTypes");

            string catalogBrandName = column[Array.IndexOf(headers, "catalogbrandname")].Trim('"').Trim();
            if (!catalogBrandIdLookup.ContainsKey(catalogBrandName))
                throw new Exception($"brand={catalogBrandName} does not exist in catalogBrands");

            string priceString = column[Array.IndexOf(headers, "price")].Trim('"').Trim();
            if (!decimal.TryParse(priceString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal price))
                throw new Exception($"price={priceString} is not a valid decimal number");

            var catalogItem = new CatalogItem
            {
                CatalogTypeId = catalogTypeIdLookup[catalogTypeName],
                CatalogBrandId = catalogBrandIdLookup[catalogBrandName],
                Description = column[Array.IndexOf(headers, "description")].Trim('"').Trim(),
                Name = column[Array.IndexOf(headers, "name")].Trim('"').Trim(),
                Price = price,
                PictureFileName = column[Array.IndexOf(headers, "picturefilename")].Trim('"').Trim()
            };

            int availableStockIndex = Array.IndexOf(headers, "availablestock");
            if (availableStockIndex != -1)
            {
                string val = column[availableStockIndex].Trim('"').Trim();
                if (!string.IsNullOrEmpty(val) && int.TryParse(val, out int availableStock))
                    catalogItem.AvailableStock = availableStock;
            }

            int restockIndex = Array.IndexOf(headers, "restockthreshold");
            if (restockIndex != -1)
            {
                string val = column[restockIndex].Trim('"').Trim();
                if (!string.IsNullOrEmpty(val) && int.TryParse(val, out int restock))
                    catalogItem.RestockThreshold = restock;
            }

            int maxStockIndex = Array.IndexOf(headers, "maxstockthreshold");
            if (maxStockIndex != -1)
            {
                string val = column[maxStockIndex].Trim('"').Trim();
                if (!string.IsNullOrEmpty(val) && int.TryParse(val, out int maxStock))
                    catalogItem.MaxStockThreshold = maxStock;
            }

            int onReorderIndex = Array.IndexOf(headers, "onreorder");
            if (onReorderIndex != -1)
            {
                string val = column[onReorderIndex].Trim('"').Trim();
                if (!string.IsNullOrEmpty(val) && bool.TryParse(val, out bool onReorder))
                    catalogItem.OnReorder = onReorder;
            }

            return catalogItem;
        }

        private static string[] GetHeaders(string csvFile, string[] requiredHeaders, string[]? optionalHeaders = null)
        {
            string[] csvheaders = File.ReadLines(csvFile).First().ToLowerInvariant().Split(',');

            if (csvheaders.Length < requiredHeaders.Length)
                throw new Exception($"requiredHeader count '{requiredHeaders.Length}' is bigger than csv header count '{csvheaders.Length}'");

            if (optionalHeaders != null && csvheaders.Length > requiredHeaders.Length + optionalHeaders.Length)
                throw new Exception($"csv header count '{csvheaders.Length}' is larger than required '{requiredHeaders.Length}' and optional '{optionalHeaders.Length}' headers count");

            foreach (var requiredHeader in requiredHeaders)
            {
                if (!csvheaders.Contains(requiredHeader.ToLowerInvariant()))
                    throw new Exception($"does not contain required header '{requiredHeader}'");
            }

            return csvheaders;
        }

        private static int GetSequenceIdFromSelectedDBSequence(CatalogDBContext context, string dbSequenceName)
        {
            var sequenceId = context.Database
                .SqlQuery<long>($"SELECT NEXT VALUE FOR {dbSequenceName}")
                .Single();
            return (int)sequenceId;
        }

        private void ExecuteScript(CatalogDBContext context, string scriptFile)
        {
            var scriptFilePath = Path.Combine(_env.ContentRootPath, scriptFile);
            if (File.Exists(scriptFilePath))
                context.Database.ExecuteSqlRaw(File.ReadAllText(scriptFilePath));
        }

        private void AddCatalogItemPictures()
        {
            if (!_useCustomizationData) return;

            var contentRootPath = _env.ContentRootPath;
            var picturePath = new DirectoryInfo(Path.Combine(contentRootPath, "Pics"));
            foreach (var file in picturePath.GetFiles())
                file.Delete();

            string zipFile = Path.Combine(contentRootPath, "Setup", "CatalogItems.zip");
            ZipFile.ExtractToDirectory(zipFile, picturePath.ToString());
        }
    }
}
