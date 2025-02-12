using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace ax.Pages
{
    public class CustomerModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public customerInfo CustomerData { get; private set; } = new();

        public CustomerModel(IConfiguration configuration)
        {
            _configuration = configuration;
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
            string connString = _configuration.GetConnectionString("DefaultConnection");

            await using var connection = new SqlConnection(connString);
            await connection.OpenAsync();

            const string sql = "SELECT TOP 1 ACCOUNTNUM, ADDRESS FROM CUSTTABLE WHERE NAME = @customerName";
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@customerName", customerName);

            await using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync()
                ? new customerInfo
                {
                    CustomerAccount = reader["ACCOUNTNUM"].ToString(),
                    DeliveryAddress = reader["ADDRESS"].ToString()
                }
                : null;
        }
    }

    public class customerInfo
    {
        public string CustomerAccount { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
    }
}
