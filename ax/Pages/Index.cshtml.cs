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

        public customerInfo CustomerData { get; private set; } = new();

        public IndexModel(DatabaseService dbService)
        {
            _dbService = dbService;
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
    }

    public class customerInfo
    {
        public string CustomerAccount { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
    }
}
