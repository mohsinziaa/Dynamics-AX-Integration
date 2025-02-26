using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ax.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;

namespace ax.Pages
{
    public class ItemsModel : PageModel
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ItemsModel> _logger;
        private readonly DatabaseService _dbService;

        // List of site and items.
        public List<itemInfo> ItemsList { get; private set; } = new List<itemInfo>();
        public List<string> SiteList { get; private set; } = new List<string>();

        public ItemsModel(ILogger<ItemsModel> logger, IWebHostEnvironment webHostEnvironment, DatabaseService dbService)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _dbService = dbService;
        }

        /// Load item list from a text file when the page is accessed
        public void OnGet()
        {
            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "items-list.txt");

            if (System.IO.File.Exists(filePath))
            {
                string[] lines = System.IO.File.ReadAllLines(filePath); 

                foreach (string line in lines)
                {
                    string[] columns = line.Split('\t'); 

                    var item = new itemInfo
                    {
                        itemNumber = columns[0].Trim(), 
                        itemName = columns[1].Trim()    
                    };
                    ItemsList.Add(item);
                }
            }
            else
            {
                _logger.LogError("File not found: " + filePath);
            }
        }

        /// Fetch the S&D sites.
        public JsonResult OnGetFetchSites()
        {
            SiteList = new List<string> { "MATCO02", "MATCO03", "MATCO13", "RIVIANA", "GODOWNS" };
            return new JsonResult(SiteList);
        }

        /// Fetch warehouses based on selected site
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

        // Query the database to get warehouses for a given site
        private async Task<List<string>> FetchWarehousesAsync(string siteName)
        {
            const string sql = "SELECT DISTINCT INVENTLOCATIONID FROM InventDim WHERE INVENTSITEID = @SiteName AND INVENTLOCATIONID <> ' '";
            var parameters = new Dictionary<string, object>
            {
                { "@SiteName", siteName }
            };

            var warehousesList = await _dbService.ExecuteQueryAsync<string>(
                sql,
                reader => reader["INVENTLOCATIONID"].ToString() ?? "",
                parameters);

            return warehousesList;
        }

        // Fetch locations based on site and warehouse
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

        // Query the database to get locations for a given site and warehouse
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
                reader => reader["WMSLOCATIONID"].ToString() ?? "",
                parameters);

            return locationsList;
        }

        // Fetch unit of measure for a given item
        public async Task<JsonResult> OnGetFetchUnit(string itemNumber)
        {
            try
            {
                var itemUnit = await FetchUnitAsync(itemNumber);
                return new JsonResult(itemUnit);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching units: {ex.Message}");
                return new JsonResult(new { error = "An error occurred while fetching units." });
            }
        }

        // Query the database to get the unit of measure for an item
        private async Task<string> FetchUnitAsync(string itemNumber)
        {
            const string sql = "SELECT UNITID FROM InventTableModule WHERE ITEMID = @itemNumber AND MODULETYPE = 2";

            var parameters = new Dictionary<string, object>
            {
                { "@itemNumber", itemNumber }
            };

            var result = await _dbService.ExecuteQueryAsync<string>(
                sql,
                reader => reader["UNITID"].ToString() ?? "",
                parameters);


            // Default values if no record is found
            string itemUnit = string.Empty;

            if (result.Count > 0)
            {
                itemUnit = result[0];
            }

            return result.FirstOrDefault() ?? "";
        }

        // Fetch master unit and quantity for an item
        public async Task<JsonResult> OnGetFetchMasterUnitsAndQty(string itemNumber)
        {
            try
            {
                var result = await FetchMasterUnitsAndQtyAsync(itemNumber);

                Console.WriteLine($"Item: {itemNumber} => Fetched Data - MasterUnit: {result.MasterUnit}, MasterQty: {result.MasterQty}");

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

        // Query the database to fetch master unit and quantity for an item
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

            var result = await _dbService.ExecuteQueryAsync<(string MasterUnit, string MasterQty)>(
                sql,
                reader => (reader["MASTERBAGUNIT"].ToString() ?? "", reader["MASTERBAGQTYFACTOR"].ToString() ?? ""),
                parameters);

            // Default values if no record is found
            string masterUnit = string.Empty;
            string masterQty = string.Empty;

            if (result.Count > 0)
            {
                masterUnit = result[0].MasterUnit;
                masterQty = result[0].MasterQty;
            }

                return (masterUnit, masterQty);
            }

    }

    // Model to store item details
    public class itemInfo
    {
        public string itemNumber { get; set; } = string.Empty;
        public string itemName { get; set; } = string.Empty;
    }
}
