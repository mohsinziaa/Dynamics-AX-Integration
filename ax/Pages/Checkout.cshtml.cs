using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ax.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ax.Pages
{
    [IgnoreAntiforgeryToken]
    public class CheckoutModel : PageModel
    {
        private readonly ILogger<CheckoutModel> _logger;
        private readonly DatabaseService _dbService;

        // A simple in-memory storage for orders
        private static List<OrderData> OrderDatabase = new List<OrderData>();

        public CheckoutModel(ILogger<CheckoutModel> logger, DatabaseService dbService)
        {
            _logger = logger;
            _dbService = dbService;
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
                const string insertQuery = @"
                    INSERT INTO [MATCOAX].[dbo].[SALESTABLE]
                        (SALESID, SALESNAME, CUSTACCOUNT, DELIVERYADDRESS, INVOICEACCOUNT,	
                        SALESTYPE, RECEIPTDATEREQUESTED, SHIPPINGDATEREQUESTED,	
                        CURRENCYCODE, DLVMODE, INVENTSITEID, INVENTLOCATIONID, 
                        PURCHORDERFORMNUM, REFJOURNALID, RECID, DATAAREAID)
                    VALUES
                        (@SalesId, @SalesName, @CustAccount, @DeliverAddress, @InvoiceAccount, 
                        @SalesType, @RecieptDateRequested, @ShippingDateRequested,
                        @CurrencyCode, @DlvMode, @InventSiteID, @InventLocationID,
                        @PurchOrderFormNum, @RefJournalID, @RecID, @DataAreaID)";

                var parameters = new Dictionary<string, object>
                {
                    { "@SalesId", "102" },
                    { "@SalesName", customer.Name },
                    { "@CustAccount", customer.CustomerAccount },
                    { "@DeliverAddress", customer.DeliveryAddress },
                    { "@InvoiceAccount", customer.CustomerAccount },
                    { "@SalesType", 1 },
                    { "@RecieptDateRequested", customer.PodDate },
                    { "@ShippingDateRequested", customer.PodDate },
                    { "@CurrencyCode", "PKR" },
                    { "@DlvMode", "Road" },
                    { "@InventSiteID", customer.Site },
                    { "@InventLocationID", customer.Warehouse },
                    { "@PurchOrderFormNum", customer.Reference },
                    { "@RefJournalID", customer.Reference },
                    { "@RecID", 102 }, // Generate unique RecID if needed
                    { "@DataAreaID", "mrp" }
                };

                // Execute query using DatabaseService
                await _dbService.ExecuteNonQueryAsync(insertQuery, parameters);

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
