using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TIR.Application.Contracts;
using TIR.Web.Presentation;

namespace TIR.Web.Pages.Attributes;

public sealed class EditModel(IAttributeService attributes, ILogger<EditModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string AttributeCode { get; private set; } = string.Empty;

    public string DataKindTitle { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var definition = await attributes.GetAsync(id, cancellationToken);

        if (definition is null || definition.IsDeleted)
            return NotFound();

        SetMetadata(definition);

        Input = new InputModel
        {
            Id = id,
            ExpectedRowVersion = Convert.ToBase64String(definition.RowVersion),
            Title = definition.Title,
            Description = definition.Description,
            IsRequired = definition.IsRequired,
            DisplayOrder = definition.DisplayOrder
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            var definition = await attributes.GetAsync(Input.Id, cancellationToken);

            if (definition is null)
                return NotFound();

            SetMetadata(definition);

            if (!ModelState.IsValid)
                return Page();

            var version = UiFieldFactory.DecodeRowVersion(Input.ExpectedRowVersion)
                ?? throw new FormatException("نسخه مشخصه موجود نیست.");

            await attributes.UpdateAsync(new AttributeUpdateRequest(Input.Id, version,
                Input.Title, Input.Description, Input.IsRequired, Input.DisplayOrder), cancellationToken);

            TempData["Success"] = "مشخصه به روز شد.";

            return RedirectToPage("/Attributes/Index");
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در ویرایش مشخصه {Id}", Input.Id);
            ModelState.AddModelError(string.Empty, UiErrors.Message(error));

            return Page();
        }
    }

    private void SetMetadata(AttributeDefinitionDto attribute)
    {
        AttributeCode = attribute.AttributeCode;
        DataKindTitle = attribute.DataKind switch
        {
            1 => "متن",
            2 => "صحیح",
            3 => "اعشار",
            4 => "تاریخ",
            5 => "ساعت",
            6 => "تاریخ و ساعت",
            7 => "بولین",
            8 => "انتخاب تکی",
            _ => "نامشخص"
        };
    }

    public sealed class InputModel
    {
        public long Id { get; set; }
        
        public string ExpectedRowVersion { get; set; } = string.Empty;
        
        public string Title { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public bool IsRequired { get; set; }
        
        public int DisplayOrder { get; set; }
    }
}
