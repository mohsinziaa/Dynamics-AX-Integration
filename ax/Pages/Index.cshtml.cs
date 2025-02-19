using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using ax.Services;
using System.Threading.Tasks;

namespace ax.Pages
{
    public class IndexModel : PageModel
    {
        private readonly DatabaseService _dbService;
        private readonly ILogger<IndexModel> _logger;
        public customerInfo CustomerData { get; private set; } = new();
        public List<string> SiteList { get; private set; } = new List<string>();

        public IndexModel(DatabaseService dbService, ILogger<IndexModel> logger)
        {
            _dbService = dbService;
            _logger = logger;
        }
        public async Task<JsonResult> OnGetFetchCustomerData(string customerName)
        {
            var customerData = await FetchCustomerDataAsync(customerName);
            return customerData != null
                ? new JsonResult(customerData)
                : new JsonResult(new { error = "Customer not found." });
        }

        private async Task<customerInfo?> FetchCustomerDataAsync(string customerName)
        {
            const string sql = "SELECT TOP 1 ACCOUNTNUM, ADDRESS FROM CUSTTABLE WHERE NAME = @customerName";

            var parameters = new Dictionary<string, object>
            {
                { "@customerName", customerName }
            };

            var result = await _dbService.ExecuteQueryAsync<customerInfo>(
                sql,
                reader => new customerInfo
                {
                    CustomerAccount = reader["ACCOUNTNUM"].ToString(),
                    DeliveryAddress = reader["ADDRESS"].ToString()
                },
                parameters);

            return result.FirstOrDefault();
        }

        public JsonResult OnGetFetchSites()
        {
            // Example: Set SiteList after fetching items from database (if needed)
            SiteList = new List<string> { "MATCO02", "MATCO03", "MATCO13", "RIVIANA", "GODOWNS" };
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

    }

    public class customerInfo
    {
        public string CustomerAccount { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
    }
}
