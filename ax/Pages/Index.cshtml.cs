using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;

namespace ax.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _configuration;
        // Initialize the list of clients properly
        public List<itemInfo> itemsList { get; private set; } = new List<itemInfo>();
        public List<string> siteList { get; private set; } = new List<string>();
        public List<string> warehousesList { get; private set; } = new List<string>();


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

                await using (SqlConnection connection = new SqlConnection(connString))
                {
                    await connection.OpenAsync();
                    Console.WriteLine(" Connection Established Successfully!");  // Print success message

                    //CUSTTABLE
                    //INVENTTABLE
                    string sql = "SELECT ITEMID, ITEMNAME FROM INVENTTABLE WHERE DATAAREAID = 'mrp' AND DIMENSION2_ = '0600005'";

                    await using (SqlCommand command = new SqlCommand(sql, connection))
                    await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        itemsList.Clear(); // Clear existing data before adding new items

                        while (await reader.ReadAsync())
                        {
                            var item = new itemInfo
                            {
                                itemNumber = reader["ITEMID"].ToString() ?? string.Empty,
                                itemName = reader["ITEMNAME"].ToString() ?? string.Empty
                            };

                            itemsList.Add(item);
                        }
                    }

                    // Set siteList after fetching items
                    siteList = new List<string> { "MATCO01", "MATCO02", "RIVIANA" };

                    Console.WriteLine($"📊 Retrieved {itemsList.Count} Items.");
                    Console.WriteLine($"🏭 Site List: {string.Join(", ", siteList)}");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Database Connection Failed! Error: {ex.Message}");
                _logger.LogError($"Database Connection Failed! Error: {ex.Message}");
            }
        }


        // AJAX Handler for Fetching Sites
        public JsonResult OnGetFetchSites()
        {
            // Set siteList after fetching items
            siteList = new List<string> { "MATCO01", "MATCO02", "RIVIANA" };
            Console.WriteLine($"🏭 Site List: {string.Join(", ", siteList)}");
            return new JsonResult(siteList);
        }

        public async Task<JsonResult> OnGetFetchWarehouses(string siteName)
        {
            List<string> warehousesList = new List<string>();

            try
            {
                // Connection string from configuration
                string connString = _configuration.GetConnectionString("DefaultConnection");

                await using (SqlConnection connection = new SqlConnection(connString))
                {
                    await connection.OpenAsync();

                    string sql = "SELECT DISTINCT INVENTLOCATIONID FROM InventDim WHERE INVENTSITEID = @SiteName AND INVENTLOCATIONID <> ' '";

                    await using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        // Adding the siteName parameter to the query
                        command.Parameters.AddWithValue("@SiteName", siteName);

                        await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string warehouse = reader["INVENTLOCATIONID"].ToString();
                                if (!string.IsNullOrEmpty(warehouse))
                                {
                                    warehousesList.Add(warehouse);
                                }
                            }
                        }
                    }
                }

                return new JsonResult(warehousesList);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching warehouses: {ex.Message}");
                return new JsonResult(new { error = "An error occurred while fetching warehouses." });
            }
        }



    }

    public class itemInfo
    {
        public string itemNumber = string.Empty;
        public string itemName = string.Empty;
    }

}
