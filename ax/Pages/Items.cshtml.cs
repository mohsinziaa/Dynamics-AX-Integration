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
            const string sql = "SELECT DISTINCT UNITID FROM UNIT WHERE UNITID <> ' '";

            var unitsList = await _dbService.ExecuteQueryAsync<string>(
                sql,
                reader => reader["UNITID"].ToString());

            return unitsList;
        }

        public async Task<JsonResult> OnGetFetchMasterUnitsAndQty(string itemNumber)
        {
            try
            {
                _logger.LogInformation($"Fetching master units and quantity for ItemNumber: {itemNumber}");

                // Fetch data from MASTERBAGSDETAIL and return the result
                var result = await FetchMasterUnitsAndQtyAsync(itemNumber);

                _logger.LogInformation($"Fetched Data - MasterUnit: {result.MasterUnit}, MasterQty: {result.MasterQty}");

                return new JsonResult(new
                {
                    masterUnit = result.MasterUnit,
                    masterQty = result.MasterQty
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching master unit and quantity: {ex.Message}");
                return new JsonResult(new { error = "An error occurred while fetching master unit and quantity." });
            }
        }

        private async Task<(string MasterUnit, string MasterQty)> FetchMasterUnitsAndQtyAsync(string itemNumber)
        {
            const string sql = @"
        SELECT MASTERBAGUNIT, MASTERBAGQTYFACTOR 
        FROM MASTERBAGSDETAIL 
        WHERE ITEMID = @itemNumber";

            var parameters = new Dictionary<string, object>
    {
        { "@itemNumber", itemNumber }
    };

            // Fetch the result from MASTERBAGSDETAIL table
            var result = await _dbService.ExecuteQueryAsync<(string MasterUnit, string MasterQty)>(
                sql,
                reader => (reader["MASTERBAGUNIT"].ToString(), reader["MASTERBAGQTYFACTOR"].ToString()),
                parameters);

            // Default values if no record is found
            string masterUnit = string.Empty;
            string masterQty = string.Empty;

            // If a record is found, use the fetched data
            if (result.Count > 0)
            {
                masterUnit = result[0].MasterUnit;
                masterQty = result[0].MasterQty;
            }

            // Return the fetched master unit and qty (or default empty values)
            return (masterUnit, masterQty);
        }

    }

    public class itemInfo
    {
        public string itemNumber { get; set; } = string.Empty;
        public string itemName { get; set; } = string.Empty;
    }
}
