using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace ax.Pages
{
    [IgnoreAntiforgeryToken]
    public class CheckoutModel : PageModel
    {
        private readonly ILogger<CheckoutModel> _logger;
        private readonly IConfiguration _configuration;

        // A simple in-memory storage for orders
        private static List<OrderData> OrderDatabase = new List<OrderData>();

        public CheckoutModel(ILogger<CheckoutModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public void OnGet()
        {
            // No changes needed for the OnGet method
        }

        public async Task<IActionResult> OnPostCreateSalesOrder([FromBody] OrderData orderData)
        {
            Console.WriteLine("This is from POST");

            if (orderData == null)
            {
                _logger.LogWarning("Received invalid order data.");
                return new JsonResult(new { error = "Invalid order data" }) { StatusCode = 400 };
            }

            // Store the order data in the in-memory database
            OrderDatabase.Add(orderData);

            // Log the customer data and items to the console
            _logger.LogInformation($"Received customer data: {JsonSerializer.Serialize(orderData.Customer)}");
            _logger.LogInformation($"Received items data: {JsonSerializer.Serialize(orderData.Items)}");

            // Simulate processing (printing the stored data)
            _logger.LogInformation("Processing order...");

            // Insert customer data into SALESTABLE
            await InsertCustomerDataAsync(orderData.Customer);

            // Print the stored data to the console (for demonstration purposes)
            Console.WriteLine("\nStored Orders");
            foreach (var order in OrderDatabase)
            {
                Console.WriteLine($"Customer: {order.Customer.Name}, Customer Account: {order.Customer.CustomerAccount}");
                foreach (var item in order.Items)
                {
                    Console.WriteLine($"Item: {item.ItemName}, Name: {item.ItemName}");
                }
            }

            return new JsonResult(new { message = "Order data received successfully." });
        }

        private async Task InsertCustomerDataAsync(CustomerData customer)
        {
            try
            {
                string connString = _configuration.GetConnectionString("DefaultConnection");

                await using var connection = new SqlConnection(connString);
                await connection.OpenAsync();

                string insertQuery = @"
                    INSERT INTO [MATCOAX].[dbo].[SALESTABLE]
                        ([SALESID], [SALESNAME], [RECID], [DATAAREAID], [CUSTACCOUNT])
                    VALUES
                        (@SalesId, @SalesName, @RecId, @DataAreaId, @CustomerAccount)";

                await using var command = new SqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@SalesId", "100"); 
                command.Parameters.AddWithValue("@SalesName", customer.Name); 
                command.Parameters.AddWithValue("@RecId", 100); 
                command.Parameters.AddWithValue("@DataAreaId", "mrp"); 
                command.Parameters.AddWithValue("@CustomerAccount", customer.CustomerAccount);
                //command.Parameters.AddWithValue("@Name", customer.Name);
                //command.Parameters.AddWithValue("@DeliveryAddress", customer.DeliveryAddress);
                //command.Parameters.AddWithValue("@PodDate", customer.PodDate);
                //command.Parameters.AddWithValue("@Reference", customer.Reference);
                //command.Parameters.AddWithValue("@ShippingTimezone", customer.ShippingTimezone);
                //command.Parameters.AddWithValue("@Site", customer.Site);
                //command.Parameters.AddWithValue("@Warehouse", customer.Warehouse);

                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("Customer data inserted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error inserting customer data: {ex.Message}");
            }
        }
    }

    public class OrderData
    {
        public CustomerData Customer { get; set; }
        public List<ItemData> Items { get; set; } = new List<ItemData>();
    }

    public class CustomerData
    {
        public string CustomerAccount { get; set; } = string.Empty;
        public string CustomerRequisition { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PodDate { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string SalesOrder { get; set; } = string.Empty;
        public string ShippingTimezone { get; set; } = string.Empty;
        public string Site { get; set; } = string.Empty;
        public string Warehouse { get; set; } = string.Empty;
    }

    public class ItemData
    {
        public string ItemName { get; set; } = string.Empty;
        public string ItemNumber { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string MasterUnit { get; set; } = string.Empty;
        public string MasterUnitQty { get; set; } = string.Empty;
        public string PackingUnit { get; set; } = string.Empty;
        public string PackingUnitQty { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public string Site { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string Warehouse { get; set; } = string.Empty;
    }
}
