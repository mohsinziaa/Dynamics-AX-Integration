using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ax.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ax.Pages
{
    [IgnoreAntiforgeryToken]
    public class CheckoutModel : PageModel
    {
        private readonly ILogger<CheckoutModel> _logger;
        private readonly DatabaseService _dbService;

        // Simulated in-memory order storage
        private static List<OrderData> OrderDatabase = new List<OrderData>();

        public CheckoutModel(ILogger<CheckoutModel> logger, DatabaseService dbService)
        {
            _logger = logger;
            _dbService = dbService;
        }

        public void OnGet()
        {
            _logger.LogInformation("Checkout page loaded.");
        }

        /// Retrieves the next available RECID for a given table.
        private async Task<long> GetNextRecIdAsync(string tableName)
        {
            try
            {
                string query = $@"
                WITH MissingNumbers AS (
                    SELECT t1.RECID + 1 AS MissingID
                    FROM {tableName} t1
                    LEFT JOIN {tableName} t2 ON t1.RECID + 1 = t2.RECID
                    WHERE t2.RECID IS NULL
                )
                SELECT TOP 1 MissingID 
                FROM MissingNumbers
                WHERE MissingID NOT IN (SELECT RECID FROM {tableName})
                ORDER BY MissingID ASC";

                var parameters = new Dictionary<string, object>();

                var result = await _dbService.ExecuteQueryAsync<long>(
                    query,
                    reader => reader.GetInt64(0),
                    parameters
                );

                return result.Count > 0 ? result[0] : 1; // Start from 1 if no record exists
            }

            catch (Exception ex)
            {
                _logger.LogError($"Error fetching next RECID for {tableName}: {ex.Message}", ex);
                throw;
            }
        }

        /// Fetches the next Sales Order ID.
        private async Task<int> FetchSalesIdAsync()
        {
            try
            {

                string fetchSalesIdQuery = @"
                SELECT NEXTREC 
                FROM NUMBERSEQUENCETABLE 
                WHERE DATAAREAID = 'mrp' AND NUMBERSEQUENCE = 'SO_2018'";

                var parameters = new Dictionary<string, object>();

                var result = await _dbService.ExecuteQueryAsync<int>(
                    fetchSalesIdQuery,
                    reader => reader.GetInt32(0), 
                    parameters
                );

                int salesId = result.Count > 0 ? result[0] : -1;
                if (salesId == -1)
                {
                    _logger.LogError("Failed to fetch a valid SalesID from NUMBERSEQUENCETABLE.");
                    throw new InvalidOperationException("Failed to fetch a valid SalesID.");
                }

                return salesId;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching SalesID: {ex.Message}", ex);
                throw;
            }
        }

        /// Increments the Sales Order ID.
        private async Task IncrementSalesIdAsync()
        {
            try
            {
                string updateSalesIdQuery = @"
                UPDATE NUMBERSEQUENCETABLE
                SET NEXTREC = CAST(CAST(NEXTREC AS INT) + 1 AS VARCHAR)
                WHERE DATAAREAID = 'mrp' AND NUMBERSEQUENCE = 'SO_2018'";

                await _dbService.ExecuteNonQueryAsync(updateSalesIdQuery);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error incrementing SalesID: {ex.Message}", ex);
                throw;
            }
        }

        /// Fetches the next available INVENTTRANSID.
        private async Task<string> FetchInventTransIdAsync()
        {
            try
            {

                string fetchInventTransIdQuery = @"
                SELECT NEXTREC, FORMAT
                FROM NUMBERSEQUENCETABLE 
                WHERE DATAAREAID = 'mrp' AND NUMBERSEQUENCE = 'Inve_78'";

                var parameters = new Dictionary<string, object>();

                var result = await _dbService.ExecuteQueryAsync<dynamic>(
                    fetchInventTransIdQuery,
                    reader => new { NEXTREC = reader.GetInt32(0), FORMAT = reader.GetString(1) },
                    parameters
                );

                if (result.Count > 0)
                {
                    var nextRec = result[0].NEXTREC;
                    var format = result[0].FORMAT;

                    // Format the NEXTREC based on the FORMAT
                    string inventTransId = nextRec.ToString("D8") + "_078";

                    return inventTransId;
                }
                else
                {
                    _logger.LogError("Failed to fetch a valid INVENTTRANSID from NUMBERSEQUENCETABLE.");
                    throw new InvalidOperationException("Failed to fetch a valid INVENTTRANSID.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching INVENTTRANSID: {ex.Message}", ex);
                throw;
            }
        }

        /// Increments the INVENTTRANSID in the database.
        private async Task IncrementInventTransIdAsync()
        {
            try
            {

                string updateInventTransIdQuery = @"
                UPDATE NUMBERSEQUENCETABLE
                SET NEXTREC = CAST(CAST(NEXTREC AS INT) + 1 AS VARCHAR)
                WHERE DATAAREAID = 'mrp' AND NUMBERSEQUENCE = 'Inve_78'";

                await _dbService.ExecuteNonQueryAsync(updateInventTransIdQuery);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error incrementing INVENTTRANSID: {ex.Message}", ex);
                throw;
            }
        }

        /// Fetches the INVENTDIMID for a given item.
        private async Task<string> FetchInventDimIdAsync(ItemData item)
        {
            try
            {
                string fetchInventDimIdQuery = @"
                SELECT TOP 1 INVENTDIMID FROM INVENTDIM 
                WHERE DATAAREAID = 'MRP'
                AND INVENTSITEID = @Site
                AND INVENTLOCATIONID = @Warehouse
                AND WMSLOCATIONID = @Location";

                var parameters = new Dictionary<string, object>
                {
                    { "@Site", item.Site },
                    { "@Warehouse", item.Warehouse },
                    { "@Location", item.Location }
                };

                var result = await _dbService.ExecuteQueryAsync<string>(
                    fetchInventDimIdQuery,
                    reader => reader.GetString(0),
                    parameters
                );

                if (result.Count > 0)
                {
                    string inventDimId = result[0];
                    return inventDimId;
                }
                else
                {
                    _logger.LogWarning("No INVENTDIMID found for Site: {Site}, Warehouse: {Warehouse}, Location: {Location}",
                        item.Site, item.Warehouse, item.Location);
                    return string.Empty; 
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching INVENTDIMID: {ex.Message}", ex);
                return string.Empty; 
            }
        }

        /// Fetches the CUSTGROUP for a given customer.
        private async Task<string> FetchCustGroupAsync(string customerAccount)
        {
            try
            {
                string fetchCustGroupQuery = @"
                SELECT TOP 1 CUSTGROUP
                FROM CUSTTABLE
                WHERE ACCOUNTNUM = @AccountNum";

                var parameters = new Dictionary<string, object>
                {
                    { "@AccountNum", customerAccount }
                };

                var result = await _dbService.ExecuteQueryAsync<string>(
                    fetchCustGroupQuery,
                    reader => reader.GetString(0), 
                    parameters
                );

                if (result.Count > 0)
                {
                    return result[0]; 
                }
                else
                {
                    _logger.LogWarning($"No CUSTGROUP found for Account: {customerAccount}");
                    return string.Empty; 
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching CUSTGROUP: {ex.Message}", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Handles the HTTP POST request to create a Sales Order.
        /// Validates the received order data and processes it by inserting customer and item details into the database.
        /// </summary>
        /// <param name="orderData">Order data containing customer and item details.</param>
        /// <returns>A JSON response indicating success or failure with the generated Sales Order IDs.</returns>
        public async Task<IActionResult> OnPostCreateSalesOrder([FromBody] OrderData orderData)
        {

            // Validate the incoming order data
            if (orderData == null)
            {
                _logger.LogWarning("Received invalid order data.");
                return new JsonResult(new { error = "Invalid order data" }) { StatusCode = 400 };
            }

            OrderDatabase.Add(orderData);

            _logger.LogInformation("Processing order...");

            // Process the order and insert customer data, returning the generated Sales Order IDs
            List<string> salesOrders = await InsertCustomerDataAsync(orderData.Customer, orderData.Items);

            // Return the created Sales Order IDs as a response
            return new JsonResult(new
            {
                message = "Order data received successfully.",
                salesOrders = salesOrders
            });
        }

        /// <summary>
        /// Inserts customer and item details into the database to create a Sales Order.
        /// Generates a unique Sales Order ID and associates it with item details in the SALESTABLE and SALESLINE tables.
        /// </summary>
        /// <param name="customer">Customer details including name, account, and address.</param>
        /// <param name="items">List of items to be added to the Sales Order.</param>
        /// <returns>A list of generated Sales Order IDs.</returns>
        private async Task<List<string>> InsertCustomerDataAsync(CustomerData customer, List<ItemData> items)
        {
            List<string> salesOrders = new List<string>(); 
            try
            {
                foreach (var item in items)
                {
                    Console.WriteLine("\n\n\n\n---------------------------------------------");
                    Console.WriteLine("Sales Order Processing Started");
                    Console.WriteLine("---------------------------------------------");
                    Console.WriteLine($"Customer: {customer.Name}");
                    Console.WriteLine($"Customer Account: {customer.CustomerAccount}");

                    // Fetch and increment Sales Order ID
                    int salesId = await FetchSalesIdAsync();
                    await IncrementSalesIdAsync();

                    // Get next available RECID for SALESTABLE
                    long salesTableRecId = await GetNextRecIdAsync("SALESTABLE");


                    Console.WriteLine($"Sales ID Generated: SO-{salesId}");
                    Console.WriteLine($"REC ID Generated for SALESTABLE: {salesTableRecId}");

                    // Insert into SALESTABLE (Sales Order Header)
                    string insertQuery = @"
                    INSERT INTO [MATCOAX].[dbo].[SALESTABLE]
                        (SALESID, SALESNAME, CUSTACCOUNT, DELIVERYADDRESS, INVOICEACCOUNT,    
                        SALESTYPE, RECEIPTDATEREQUESTED, SHIPPINGDATEREQUESTED,    
                        CURRENCYCODE, DLVMODE, INVENTSITEID, INVENTLOCATIONID, 
                        PURCHORDERFORMNUM, CUSTOMERREF, RECID, 
                        LANGUAGEID, SALESRESPONSIBLE, DATAAREAID, 
                        DIMENSION, DIMENSION2_, DIMENSION3_, CREATEDBY, 
                        CREATEDDATETIME)

                    VALUES
                        (@SalesId, @SalesName, @CustAccount, @DeliverAddress, @InvoiceAccount, 
                        @SalesType, @ReceiptDateRequested, @ShippingDateRequested,
                        @CurrencyCode, @DlvMode, @InventSiteID, @InventLocationID,
                        @PurchOrderFormNum, @CustomerRef, @RecID, 
                        @LanguageID, @SalesResponsible, @DataAreaID, 
                        @Dimension1, @Dimension2, @Dimension3, @CreatedBy, 
                        GETDATE())";

                    var parametersForInsert = new Dictionary<string, object>
                    {
                        { "@SalesId", "SO-" + salesId.ToString() },
                        { "@SalesName", customer.Name },
                        { "@CustAccount", customer.CustomerAccount },
                        { "@DeliverAddress", customer.DeliveryAddress },
                        { "@InvoiceAccount", customer.CustomerAccount },
                        { "@SalesType", 3 },
                        { "@ReceiptDateRequested", customer.PodDate },
                        { "@ShippingDateRequested", customer.PodDate },
                        { "@CurrencyCode", "PKR" },
                        { "@DlvMode", "ROAD" },
                        { "@InventSiteID", customer.Site },
                        { "@InventLocationID", customer.Warehouse },
                        { "@PurchOrderFormNum", customer.CustomerRequisition },
                        { "@CustomerRef", customer.Reference },
                        { "@RecID", salesTableRecId },
                        { "@LanguageID", "EN-US" },
                        { "@SalesResponsible", "01631" },
                        { "@DataAreaID", "mrp" },
                        { "@Dimension1", "06" },
                        { "@Dimension2", "0600001" },
                        { "@Dimension3", "02" },
                        { "@CreatedBy", "mziaa" },
                    };

                    await Task.Delay(2000); 
                    int salesTableResult = await _dbService.ExecuteNonQueryAsync(insertQuery, parametersForInsert);

                    if (salesTableResult > 0)
                    {
                        salesOrders.Add("SO-" + salesId.ToString());
                        Console.WriteLine($"\nCustomer data inserted successfully for SalesID: SO-{salesId}\n");
                        Console.WriteLine("---------------------------------------------");

                        try
                        {
                            Console.WriteLine("Inserting Sales Lines:");
                            Console.WriteLine("---------------------------------------------");

                            Console.WriteLine($"Item: {item.ItemNumber} ({item.ItemName})");

                            // Get next RECID for SALESLINE
                            long salesLineRecId = await GetNextRecIdAsync("SALESLINE");

                            Console.WriteLine($"REC ID Generated for SALESLINE: {salesLineRecId}");


                            // Fetch customer group based on account number
                            string custGroup = await FetchCustGroupAsync(customer.CustomerAccount);
                            Console.WriteLine($"Customer Group: {custGroup}");


                            // Generate unique inventory transaction ID and increment it
                            string inventTransId = await FetchInventTransIdAsync();
                            await IncrementInventTransIdAsync();

                            // Fetch inventory dimension ID based on warehouse details
                            string inventDimId = await FetchInventDimIdAsync(item);

                            Console.WriteLine($"      TransID: {inventTransId}");
                            Console.WriteLine($"      InventDimID: {inventDimId}");

                            // Insert into SALESLINE (Sales Order Line Items)
                            string insertSalesLineQuery = @"
                            INSERT INTO [MATCOAX].[dbo].[SALESLINE]
                            (SALESID, ITEMID, NAME, SALESUNIT, SALESQTY, 
                            PACKINGUNIT, PACKINGUNITQTY, MASTERUNIT, MASTERUNITQTY, CURRENCYCODE, 
                            RECID, DATAAREAID, SALESTYPE, INVENTTRANSID, INVENTDIMID, 
                            DIMENSION, DIMENSION2_, DIMENSION3_, LINENUM, QTYORDERED, 
                            CUSTACCOUNT, DELIVERYADDRESS, PRICEUNIT, CUSTGROUP,
                            RECEIPTDATEREQUESTED, CREATEDDATETIME)
                        
                        VALUES
                            (@SalesId, @ItemID, @ItemName, @SalesUnit, @SalesQty, 
                            @PackingUnit, @PackingUnitQty, @MasterUnit, @MasterUnitQty, @CurrencyCode, 
                            @RecID, @DataAreaID, @SalesType, @InventTransID, @InventDimID, 
                            @Dimension1, @Dimension2, @Dimension3, @LineNum, @QtyOredered, 
                            @CustAccount, @DeliveryAddress, @PriceUnit, @CustGroup, 
                            GETDATE(), GETDATE())";

                            var masterUnitQty = string.IsNullOrEmpty(item.MasterUnitQty) ? 0 : Convert.ToDecimal(item.MasterUnitQty);

                            var salesLineParams = new Dictionary<string, object>
                        {
                            { "@SalesId", "SO-" + salesId.ToString() },
                            { "@ItemID", item.ItemNumber },
                            { "@ItemName", item.ItemName },
                            { "@SalesUnit", item.Unit },
                            { "@SalesQty", item.Quantity },
                            { "@PackingUnit", item.PackingUnit },
                            { "@PackingUnitQty", item.PackingUnitQty },
                            { "@MasterUnit", item.MasterUnit },
                            { "@MasterUnitQty", masterUnitQty },
                            { "@CurrencyCode", "PKR" },
                            { "@RecID", salesLineRecId },
                            { "@DataAreaID", "mrp" },
                            { "@SalesType", 3 },
                            { "@InventTransID", inventTransId},
                            { "@InventDimID", inventDimId},
                            { "@Dimension1", "06" },
                            { "@Dimension2", "0600005" },
                            { "@Dimension3", "01" },
                            { "@LineNum", 1 },
                            { "@QtyOredered", item.Quantity },
                            { "@CustAccount", customer.CustomerAccount },
                            { "@DeliveryAddress", customer.DeliveryAddress },
                            { "@PriceUnit", 1 },
                            { "@CustGroup", custGroup },
                        };

                            await Task.Delay(2000);
                            int salesLineResult = await _dbService.ExecuteNonQueryAsync(insertSalesLineQuery, salesLineParams);

                            if (salesLineResult > 0)
                            {

                                Console.WriteLine($"\nSales Line inserted successfully for SalesID: SO-{salesId}, Item: {item.ItemNumber}\n");

                                Console.WriteLine("---------------------------------------------");

                                try
                                {
                                    Console.WriteLine("Inserting Invent Trans");
                                    Console.WriteLine("---------------------------------------------\n");
                                    Console.WriteLine($"TransID: {inventTransId}");
                                    Console.WriteLine($"InventDimID: {inventDimId}");
                                    Console.WriteLine($"RecID: {salesLineRecId}");

                                    // Insert into INVENTTRANS (inventory transaction details)
                                    string insertInventTransQuery = @"
                                    INSERT INTO [MATCOAX].[dbo].[INVENTTRANS]
                                    (ITEMID, TRANSREFID, CUSTVENDAC, INVENTTRANSID, 
                                    INVENTDIMID, CURRENCYCODE, TRANSTYPE, 
                                    QTY, DATAAREAID, RECID, DATEPHYSICAL) 

                                    VALUES
                                        (@ItemID, @TransrefID, @CustVendAcc, @InventTransID,
                                        @InventDimID, @CurrencyCode, @TransType,
                                        @Qty, @DataAreaID, @RecID, GETDATE())";

                                    var inventTransParams = new Dictionary<string, object>
                                    {
                                        { "@ItemID", item.ItemNumber },
                                        { "@TransrefID", "SO-" + salesId.ToString() },
                                        { "@CustVendAcc", customer.CustomerAccount },
                                        { "@InventTransID", inventTransId },
                                        { "@InventDimID", inventDimId },
                                        { "@CurrencyCode", "PKR" },
                                        { "@TransType", 0 },
                                        { "@Qty", item.Quantity },
                                        { "@DataAreaID", "mrp" },
                                        { "@RecID", salesLineRecId },
                                    };

                                    await Task.Delay(1000);
                                    await _dbService.ExecuteNonQueryAsync(insertInventTransQuery, inventTransParams);
                                    Console.WriteLine($"\nInvent Trans inserted successfully for SO-{salesId}\n");
                                }

                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error creating Invent Trans for Item {item.ItemNumber}: {ex.Message}");
                                }


                            }
                            else
                            {
                                Console.WriteLine($"Error creating Sales Line for Item {item.ItemNumber}");
                            }
                        }


                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error creating Sales Line for Item {item.ItemNumber}: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error inserting Sales Order");
                    }

                    Console.WriteLine("---------------------------------------------");
                    Console.WriteLine("Sales Order Processing Completed!");
                    Console.WriteLine("---------------------------------------------\n");

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting Sales Order: {ex.Message}");
            }

            return salesOrders;
        }

    }

    /// Represents an order containing customer information and a list of items.
    public class OrderData
    {
        public CustomerData Customer { get; set; } = new CustomerData();
        public List<ItemData> Items { get; set; } = new List<ItemData>();
    }


    /// Contains details about the customer placing the order.
    public class CustomerData
    {
        public string CustomerAccount { get; set; } = string.Empty;
        public string CustomerRequisition { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PodDate { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string SalesOrder { get; set; } = string.Empty;
        public string Site { get; set; } = string.Empty;
        public string Warehouse { get; set; } = string.Empty;
    }

    /// Represents an item in the order with its details.
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
