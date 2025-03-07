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

        // Stores customer data
        public customerInfo CustomerData { get; private set; } = new();

        // Stores the list of available sites
        public List<string> SiteList { get; private set; } = new List<string>();

        public IndexModel(DatabaseService dbService, ILogger<IndexModel> logger)
        {
            _dbService = dbService;
            _logger = logger;
        }

        /// Fetch customer data based on the provided customer name.
        public async Task<JsonResult> OnGetFetchCustomerData(string customerName)
        {
            try
            {
                var customerData = await FetchCustomerDataAsync(customerName);
                return customerData != null
                    ? new JsonResult(customerData)
                    : new JsonResult(new { error = "Customer not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching customer data: {ex.Message}");
                return new JsonResult(new { error = "An error occurred while fetching customer data." });
            }
        }

        /// Queries the database to fetch customer information.
        private async Task<customerInfo?> FetchCustomerDataAsync(string customerName)
        {
            // SQL query to fetch customer account number and address
            const string sql = "SELECT TOP 1 ACCOUNTNUM, ADDRESS FROM CUSTTABLE WHERE LOWER(NAME) = LOWER(@customerName)";

            var parameters = new Dictionary<string, object>
            {
                { "@customerName", customerName }
            };

            // Execute query and map results
            var result = await _dbService.ExecuteQueryAsync<customerInfo>(
                sql,
                reader => new customerInfo
                {
                    CustomerAccount = reader["ACCOUNTNUM"].ToString() ?? "",
                    DeliveryAddress = reader["ADDRESS"].ToString() ?? ""
                },
                parameters);

            return result.FirstOrDefault();
        }

        /// Fetches the list of available sites from the database.
        public JsonResult OnGetFetchSites()
        {
            // Example: Set SiteList after fetching items from database (if needed)
            SiteList = new List<string> { "MATCO02", "MATCO03", "MATCO13", "RIVIANA", "GODOWNS" };
            return new JsonResult(SiteList);
        }

    }

    /// Represents customer information, including account number and delivery address.
    public class customerInfo
    {
        public string CustomerAccount { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
    }
}
