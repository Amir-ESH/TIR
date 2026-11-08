using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TIR.Application.Contracts;
using TIR.Web.Presentation;

namespace TIR.Web.Pages.Attributes;

public sealed class CreateModel(IAttributeService attributes, ILogger<CreateModel> logger) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty] public bool Popup { get; set; }
    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();
        try
        {
            await attributes.CreateAsync(new AttributeCreateRequest(Input.AttributeCode, Input.Title,
                Input.Description, Input.SourceKind, Input.DataKind, Input.IsRequired, Input.DisplayOrder), cancellationToken);
            TempData["Success"] = "مشخصه ثبت شد.";
            if (Popup) return RedirectToPage("/PopupComplete", new { scope = "attributes" });
            return RedirectToPage("/Attributes/Index");
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در ثبت مشخصه");
            ModelState.AddModelError(string.Empty, UiErrors.Message(error));
            return Page();
        }
    }

    public sealed class InputModel
    {
        public string AttributeCode { get; set; } = string.Empty;
        
        public string Title { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public byte SourceKind { get; set; } = 1;
        
        public byte DataKind { get; set; } = 1;
        public bool IsRequired { get; set; }
        public int DisplayOrder { get; set; }
    }
}
