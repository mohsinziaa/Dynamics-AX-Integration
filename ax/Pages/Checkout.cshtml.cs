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

        // A simple in-memory storage for orders
        private static List<OrderData> OrderDatabase = new List<OrderData>();

        public CheckoutModel(ILogger<CheckoutModel> logger, DatabaseService dbService)
        {
            _logger = logger;
            _dbService = dbService;
        }

        public void OnGet()
        {

        }
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

        private async Task<int> FetchSalesIdAsync()
        {
            try
            {
                // Log the start of the method execution
                //_logger.LogInformation("Executing query to fetch the next SalesID.");

                string fetchSalesIdQuery = @"
                SELECT NEXTREC 
                FROM NUMBERSEQUENCETABLE 
                WHERE DATAAREAID = 'mrp' AND NUMBERSEQUENCE = 'SO_2018'";

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

                //_logger.LogInformation("Fetched SalesID: {SalesID}", salesId);
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
                //_logger.LogInformation("Executing query to increment the SalesID in NUMBERSEQUENCETABLE.");

                string updateSalesIdQuery = @"
            UPDATE NUMBERSEQUENCETABLE
            SET NEXTREC = CAST(CAST(NEXTREC AS INT) + 1 AS VARCHAR)
            WHERE DATAAREAID = 'mrp' AND NUMBERSEQUENCE = 'SO_2018'";

                await _dbService.ExecuteNonQueryAsync(updateSalesIdQuery);
                //_logger.LogInformation("SalesID successfully incremented in NUMBERSEQUENCETABLE.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error incrementing SalesID: {ex.Message}", ex);
                throw;
            }
        }


        private async Task<string> FetchInventTransIdAsync()
        {
            try
            {
                // Log the start of the method execution
                //_logger.LogInformation("Executing query to fetch the next INVENTTRANSID.");

                string fetchInventTransIdQuery = @"
                SELECT NEXTREC, FORMAT
                FROM NUMBERSEQUENCETABLE 
                WHERE DATAAREAID = 'mrp' AND NUMBERSEQUENCE = 'Inve_78'";

                var parameters = new Dictionary<string, object>();

                // Execute the query and fetch the NEXTREC and FORMAT
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

                    //_logger.LogInformation("Fetched INVENTTRANSID: {InventTransID}", inventTransId);
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

        private async Task IncrementInventTransIdAsync()
        {
            try
            {
                // Log the start of the increment operation
                //_logger.LogInformation("Executing query to increment the INVENTTRANSID in NUMBERSEQUENCETABLE.");

                string updateInventTransIdQuery = @"
                UPDATE NUMBERSEQUENCETABLE
                SET NEXTREC = CAST(CAST(NEXTREC AS INT) + 1 AS VARCHAR)
                WHERE DATAAREAID = 'mrp' AND NUMBERSEQUENCE = 'Inve_78'";

                await _dbService.ExecuteNonQueryAsync(updateInventTransIdQuery);
                //_logger.LogInformation("INVENTTRANSID successfully incremented in NUMBERSEQUENCETABLE.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error incrementing INVENTTRANSID: {ex.Message}", ex);
                throw;
            }
        }

        private async Task<string> FetchInventDimIdAsync(ItemData item)
        {
            try
            {
                //_logger.LogInformation("Fetching INVENTDIMID for Site: {Site}, Warehouse: {Warehouse}, Location: {Location}",
                //    item.Site, item.Warehouse, item.Location);

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
                    //_logger.LogInformation("Fetched INVENTDIMID: {InventDimId}", inventDimId);
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

                // Execute the query to fetch the CUSTGROUP
                var result = await _dbService.ExecuteQueryAsync<string>(
                    fetchCustGroupQuery,
                    reader => reader.GetString(0), // Extract the first column as string
                    parameters
                );

                if (result.Count > 0)
                {
                    return result[0]; // Return the first (and expected) CUSTGROUP
                }
                else
                {
                    _logger.LogWarning($"No CUSTGROUP found for Account: {customerAccount}");
                    return string.Empty; // Return empty if no result
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching CUSTGROUP: {ex.Message}", ex);
                return string.Empty;
            }
        }

        public async Task<IActionResult> OnPostCreateSalesOrder([FromBody] OrderData orderData)
        {

            if (orderData == null)
            {
                _logger.LogWarning("Received invalid order data.");
                return new JsonResult(new { error = "Invalid order data" }) { StatusCode = 400 };
            }

            // Store the order data in the in-memory database
            OrderDatabase.Add(orderData);

            // Simulate processing (printing the stored data)
            _logger.LogInformation("Processing order...");

            // Insert customer data into SALESTABLE
            List<string> salesOrders = await InsertCustomerDataAsync(orderData.Customer, orderData.Items);

            // Return JSON response including salesOrders
            return new JsonResult(new
            {
                message = "Order data received successfully.",
                salesOrders = salesOrders
            });
        }

        private async Task<List<string>> InsertCustomerDataAsync(CustomerData customer, List<ItemData> items)
        {
            List<string> salesOrders = new List<string>(); // Store created Sales IDs
            try
            {
                foreach (var item in items)
                {
                    Console.WriteLine("\n\n\n\n---------------------------------------------");
                    Console.WriteLine("Sales Order Processing Started");
                    Console.WriteLine("---------------------------------------------");
                    Console.WriteLine($"Customer: {customer.Name}");
                    Console.WriteLine($"Customer Account: {customer.CustomerAccount}");

                    // Fetch the next SalesID
                    int salesId = await FetchSalesIdAsync();
                    await IncrementSalesIdAsync();
                    long salesTableRecId = await GetNextRecIdAsync("SALESTABLE");


                    Console.WriteLine($"Sales ID Generated: SO-{salesId}");
                    Console.WriteLine($"REC ID Generated for SALESTABLE: {salesTableRecId}");

                    // Insert customer data into SALESTABLE
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

                    await Task.Delay(1000); 
                    int salesTableResult = await _dbService.ExecuteNonQueryAsync(insertQuery, parametersForInsert);

                    if (salesTableResult > 0)
                    {
                        salesOrders.Add("SO-" + salesId.ToString());
                        Console.WriteLine($"\nCustomer data inserted successfully for SalesID: SO-{salesId}\n");
                        Console.WriteLine("---------------------------------------------");

                        // Insert Sales Line for the item
                        try
                        {
                            Console.WriteLine("Inserting Sales Lines:");
                            Console.WriteLine("---------------------------------------------");

                            Console.WriteLine($"Item: {item.ItemNumber} ({item.ItemName})");

                            long salesLineRecId = await GetNextRecIdAsync("SALESLINE");

                            Console.WriteLine($"REC ID Generated for SALESLINE: {salesLineRecId}");


                            // Fetch the CUSTGROUP based on the customer's account
                            string custGroup = await FetchCustGroupAsync(customer.CustomerAccount);
                            Console.WriteLine($"Customer Group: {custGroup}");


                            // Fetch the next INVENTTRANSID
                            string inventTransId = await FetchInventTransIdAsync();
                            await IncrementInventTransIdAsync();

                            // Fetch the INVENTDIMID for the item
                            string inventDimId = await FetchInventDimIdAsync(item);

                            Console.WriteLine($"      TransID: {inventTransId}");
                            Console.WriteLine($"      InventDimID: {inventDimId}");

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

                            await Task.Delay(1000);
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

                                    // Insert Invent Trans record
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
