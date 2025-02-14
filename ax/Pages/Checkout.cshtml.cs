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


        private async Task<int> FetchSalesIdAsync()
        {
            try
            {
                // Log the start of the method execution
                _logger.LogInformation("Executing query to fetch the next SalesID.");

                string fetchSalesIdQuery = @"
            SELECT NEXTREC 
            FROM NUMBERSEQUENCETABLE 
            WHERE FORMAT LIKE 'SO-%' AND DATAAREAID = 'mrp'";

                var parameters = new Dictionary<string, object>();

                // Execute the query and fetch the first column as an integer
                var result = await _dbService.ExecuteQueryAsync<int>(
                    fetchSalesIdQuery,
                    reader => reader.GetInt32(0), // Extract the first column as INT
                    parameters
                );

                // Ensure that the SalesID is valid, otherwise, default to a fallback value
                int salesId = result.Count > 0 ? result[0] : -1; // Returning -1 if NULL is fetched
                if (salesId == -1)
                {
                    _logger.LogError("Failed to fetch a valid SalesID from NUMBERSEQUENCETABLE.");
                    throw new InvalidOperationException("Failed to fetch a valid SalesID.");
                }

                _logger.LogInformation("Fetched SalesID: {SalesID}", salesId);
                return salesId;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching SalesID: {ex.Message}", ex);
                throw;
            }
        }

        private async Task IncrementSalesIdAsync()
        {
            try
            {
                // Log the start of the increment operation
                _logger.LogInformation("Executing query to increment the SalesID in NUMBERSEQUENCETABLE.");

                string updateSalesIdQuery = @"
            UPDATE NUMBERSEQUENCETABLE
            SET NEXTREC = CAST(CAST(NEXTREC AS INT) + 1 AS VARCHAR)
            WHERE FORMAT LIKE 'SO-%' AND DATAAREAID = 'mrp'";

                await _dbService.ExecuteNonQueryAsync(updateSalesIdQuery);
                _logger.LogInformation("SalesID successfully incremented in NUMBERSEQUENCETABLE.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error incrementing SalesID: {ex.Message}", ex);
                throw;
            }
        }

        private async Task InsertCustomerDataAsync(CustomerData customer)
        {
            try
            {
                // Log the start of the method execution
                _logger.LogInformation("Started inserting customer data for Customer: {CustomerName}, Account: {CustomerAccount}", customer.Name, customer.CustomerAccount);

                // Fetch the next SalesID
                int salesId = await FetchSalesIdAsync();

                // Increment the SalesID in the NUMBERSEQUENCETABLE
                await IncrementSalesIdAsync();

                // Insert customer data into SALESTABLE
                string insertQuery = @"
                                    INSERT INTO [MATCOAX].[dbo].[SALESTABLE]
                                        (SALESID, SALESNAME, CUSTACCOUNT, DELIVERYADDRESS, INVOICEACCOUNT,    
                                        SALESTYPE, RECEIPTDATEREQUESTED, SHIPPINGDATEREQUESTED,    
                                        CURRENCYCODE, DLVMODE, INVENTSITEID, INVENTLOCATIONID, 
                                        PURCHORDERFORMNUM, REFJOURNALID, RECID, LANGUAGEID, SALESRESPONSIBLE, DATAAREAID)
                                    VALUES
                                        (@SalesId, @SalesName, @CustAccount, @DeliverAddress, @InvoiceAccount, 
                                        @SalesType, @RecieptDateRequested, @ShippingDateRequested,
                                        @CurrencyCode, @DlvMode, @InventSiteID, @InventLocationID,
                                        @PurchOrderFormNum, @RefJournalID, @RecID, @LanguageID, @SalesResponsible, @DataAreaID)";

                _logger.LogInformation("Executing query to insert customer data into SALESTABLE.");

                var parametersForInsert = new Dictionary<string, object>
                    {
                    { "@SalesId", "SO-" + salesId.ToString() },
                    { "@SalesName", customer.Name },
                    { "@CustAccount", customer.CustomerAccount },
                    { "@DeliverAddress", customer.DeliveryAddress },
                    { "@InvoiceAccount", customer.CustomerAccount },
                    { "@SalesType", 3 },
                    { "@RecieptDateRequested", customer.PodDate },
                    { "@ShippingDateRequested", customer.PodDate },
                    { "@CurrencyCode", "PKR" },
                    { "@DlvMode", "Road" },
                    { "@InventSiteID", customer.Site },
                    { "@InventLocationID", customer.Warehouse },
                    { "@PurchOrderFormNum", customer.Reference },
                    { "@RefJournalID", customer.Reference },
                    { "@RecID", salesId }, // Using SalesID as RecID for now
                    { "@LanguageID", "EN-US" },
                    { "@SalesResponsible", "00550" },
                    { "@DataAreaID", "mrp" }
                };

                await _dbService.ExecuteNonQueryAsync(insertQuery, parametersForInsert);
                _logger.LogInformation("Customer data inserted successfully with SalesID: SO-{SalesID}", salesId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error inserting customer data: {ex.Message}", ex);
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
