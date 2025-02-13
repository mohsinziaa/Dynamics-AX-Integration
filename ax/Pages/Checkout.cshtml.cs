using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace ax.Pages
{
    [IgnoreAntiforgeryToken]
    public class CheckoutModel : PageModel
    {
        private readonly ILogger<CheckoutModel> _logger;

        public CheckoutModel(ILogger<CheckoutModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            Console.WriteLine("This is from GET");
        }

        
        public IActionResult OnPostTest()
        {
            Console.WriteLine("This is from POST");
            return new JsonResult(new { message = "Hello from POST!" }); // Ensure a response is returned
        }
    }
}
