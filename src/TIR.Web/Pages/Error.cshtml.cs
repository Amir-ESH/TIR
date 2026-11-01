using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TIR.Web.Pages;

public sealed class ErrorModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public new int StatusCode { get; set; } = 500;

    public void OnGet() { }
}
