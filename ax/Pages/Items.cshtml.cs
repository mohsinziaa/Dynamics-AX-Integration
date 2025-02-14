using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using ax.Services;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ax.Pages
{
    public class ItemsModel : PageModel
    {
        private readonly ILogger<ItemsModel> _logger;
        private readonly DatabaseService _dbService;

        public List<itemInfo> ItemsList { get; private set; } = new List<itemInfo>();
        public List<string> SiteList { get; private set; } = new List<string>();

        public ItemsModel(ILogger<ItemsModel> logger, DatabaseService dbService)
        {
            _logger = logger;
            _dbService = dbService;
        }

        public async Task OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Fetching items from the database...");
                var items = await _dbService.ExecuteQueryAsync<itemInfo>(
                    "SELECT TOP 10 ITEMID, ITEMNAME FROM INVENTTABLE WHERE DATAAREAID = 'mrp' AND DIMENSION2_ = '0600005'",
                    reader => new itemInfo
                    {
                        itemNumber = reader["ITEMID"].ToString() ?? string.Empty,
                        itemName = reader["ITEMNAME"].ToString() ?? string.Empty
                    });

                ItemsList.AddRange(items);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Database query failed: {ex.Message}");
            }
        }

        // AJAX Handler for Fetching Sites
        public JsonResult OnGetFetchSites()
        {
            // Example: Set SiteList after fetching items from database (if needed)
            SiteList = new List<string> { "MATCO01", "MATCO02", "MATCO13", "RIVIANA", "GODOWNS" };
            return new JsonResult(SiteList);
        }

        public async Task<JsonResult> OnGetFetchWarehouses(string siteName)
        {
            try
            {
                var warehousesList = await FetchWarehousesAsync(siteName);
                return new JsonResult(warehousesList);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching warehouses: {ex.Message}");
                return new JsonResult(new { error = "An error occurred while fetching warehouses." });
            }
        }

        private async Task<List<string>> FetchWarehousesAsync(string siteName)
        {
            const string sql = "SELECT DISTINCT INVENTLOCATIONID FROM InventDim WHERE INVENTSITEID = @SiteName AND INVENTLOCATIONID <> ' '";
            var parameters = new Dictionary<string, object>
            {
                { "@SiteName", siteName }
            };

            var warehousesList = await _dbService.ExecuteQueryAsync<string>(
                sql,
                reader => reader["INVENTLOCATIONID"].ToString(),
                parameters);

            return warehousesList;
        }

        public async Task<JsonResult> OnGetFetchLocations(string siteName, string warehouseName)
        {
            try
            {
                var locationsList = await FetchLocationsAsync(siteName, warehouseName);
                return new JsonResult(locationsList);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching locations: {ex.Message}");
                return new JsonResult(new { error = "An error occurred while fetching locations." });
            }
        }

        private async Task<List<string>> FetchLocationsAsync(string siteName, string warehouseName)
        {
            const string sql = @"
                SELECT DISTINCT WMSLOCATIONID 
                FROM INVENTDIM 
                WHERE INVENTSITEID = @SiteName 
                  AND INVENTLOCATIONID = @WarehouseName 
                  AND LTRIM(RTRIM(WMSLOCATIONID)) <> ''";

            var parameters = new Dictionary<string, object>
            {
                { "@SiteName", siteName },
                { "@WarehouseName", warehouseName }
            };

            var locationsList = await _dbService.ExecuteQueryAsync<string>(
                sql,
                reader => reader["WMSLOCATIONID"].ToString(),
                parameters);

            return locationsList;
        }

        public async Task<JsonResult> OnGetFetchUnits()
        {
            try
            {
                var unitsList = await FetchUnitsAsync();
                return new JsonResult(unitsList);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching units: {ex.Message}");
                return new JsonResult(new { error = "An error occurred while fetching units." });
            }
        }

        private async Task<List<string>> FetchUnitsAsync()
        {
            const string sql = "SELECT DISTINCT SALESUNIT FROM SALESLINE WHERE SALESUNIT <> ' '";

            var unitsList = await _dbService.ExecuteQueryAsync<string>(
                sql,
                reader => reader["SALESUNIT"].ToString());

            return unitsList;
        }

        public async Task<JsonResult> OnGetFetchMasterUnitsAndQty(string itemNumber)
        {
            try
            {
                _logger.LogInformation($"Fetching master units and quantity for ItemNumber: {itemNumber}");

                var result = await FetchMasterUnitsAndQtyAsync(itemNumber);

                _logger.LogInformation($"Fetched Data - MasterUnits: {string.Join(", ", result.MasterUnits)}, MasterQty: {result.MasterQty}");

                return new JsonResult(new
                {
                    masterUnits = result.MasterUnits,
                    masterQty = result.MasterQty
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching master units and quantity: {ex.Message}");
                return new JsonResult(new { error = "An error occurred while fetching master units and quantity." });
            }
        }

        private async Task<(List<string> MasterUnits, string MasterQty)> FetchMasterUnitsAndQtyAsync(string itemNumber)
        {
            const string sql = @"
                SELECT DISTINCT MASTERUNIT, MASTERUNITQTY 
                FROM SALESLINE 
                WHERE ITEMID = @itemNumber AND MASTERUNIT <> ' '";

            var parameters = new Dictionary<string, object>
            {
                { "@itemNumber", itemNumber }
            };

            var result = await _dbService.ExecuteQueryAsync<(string MasterUnit, string MasterQty)>(
                sql,
                reader => (reader["MASTERUNIT"].ToString(), reader["MASTERUNITQTY"].ToString()),
                parameters);

            var masterUnits = new List<string>();
            string masterQty = string.Empty;

            foreach (var (masterUnit, qty) in result)
            {
                if (!string.IsNullOrEmpty(masterUnit))
                    masterUnits.Add(masterUnit);
                if (string.IsNullOrEmpty(masterQty) && !string.IsNullOrEmpty(qty))
                    masterQty = qty;
            }

            return (masterUnits, masterQty);
        }
    }

    public class itemInfo
    {
        public string itemNumber { get; set; } = string.Empty;
        public string itemName { get; set; } = string.Empty;
    }
}
