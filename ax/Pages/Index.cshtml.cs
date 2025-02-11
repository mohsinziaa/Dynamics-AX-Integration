using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace ax.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _configuration;

        public List<itemInfo> ItemsList { get; private set; } = new List<itemInfo>();
        public List<string> SiteList { get; private set; } = new List<string>();

        public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task OnGetAsync()
        {
            try
            {
                string connString = _configuration.GetConnectionString("DefaultConnection");
                await using var connection = new SqlConnection(connString);
                await connection.OpenAsync();

                _logger.LogInformation("Connection Established Successfully!");

                // Retrieve items
                await FetchItemsAsync(connection);

            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Database Connection Failed! Error: {ex.Message}");
            }
        }

        private async Task FetchItemsAsync(SqlConnection connection)
        {
            const string sql = "SELECT TOP 10 ITEMID, ITEMNAME FROM INVENTTABLE WHERE DATAAREAID = 'mrp' AND DIMENSION2_ = '0600005'";
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            ItemsList.Clear(); // Clear existing data before adding new items

            while (await reader.ReadAsync())
            {
                var item = new itemInfo
                {
                    itemNumber = reader["ITEMID"].ToString() ?? string.Empty,
                    itemName = reader["ITEMNAME"].ToString() ?? string.Empty
                };
                ItemsList.Add(item);
            }
        }

        // AJAX Handler for Fetching Sites
        public JsonResult OnGetFetchSites() {
            // Set SiteList after fetching items
            SiteList = new List<string> { "MATCO01", "MATCO02", "RIVIANA" };
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
            var warehousesList = new List<string>();
            string connString = _configuration.GetConnectionString("DefaultConnection");

            await using var connection = new SqlConnection(connString);
            await connection.OpenAsync();

            const string sql = "SELECT DISTINCT INVENTLOCATIONID FROM InventDim WHERE INVENTSITEID = @SiteName AND INVENTLOCATIONID <> ' '";
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@SiteName", siteName);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string warehouse = reader["INVENTLOCATIONID"].ToString();
                if (!string.IsNullOrEmpty(warehouse))
                {
                    warehousesList.Add(warehouse);
                }
            }

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
            var locationsList = new List<string>();
            string connString = _configuration.GetConnectionString("DefaultConnection");

            await using var connection = new SqlConnection(connString);
            await connection.OpenAsync();

            const string sql = @"
                SELECT DISTINCT WMSLOCATIONID 
                FROM INVENTDIM 
                WHERE INVENTSITEID = @SiteName 
                  AND INVENTLOCATIONID = @WarehouseName 
                  AND LTRIM(RTRIM(WMSLOCATIONID)) <> ''";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@SiteName", siteName);
            command.Parameters.AddWithValue("@WarehouseName", warehouseName);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string location = reader["WMSLOCATIONID"].ToString();
                if (!string.IsNullOrEmpty(location))
                {
                    locationsList.Add(location);
                }
            }

            return locationsList;
        }
    }

    public class itemInfo
    {
        public string itemNumber { get; set; } = string.Empty;
        public string itemName { get; set; } = string.Empty;
    }
}
