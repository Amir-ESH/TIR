using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TIR.Application.Contracts;
using TIR.Web.Presentation;

namespace TIR.Web.Pages.Attributes;

public sealed class OptionsModel(IAttributeService attributes, ILogger<OptionsModel> logger) : PageModel
{
    [BindProperty]
    public NewOptionInput Input { get; set; } = new();

    public AttributeDefinitionDto Attribute { get; private set; } = new();
    public IReadOnlyList<AttributeOptionDto> Options { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken)) return NotFound();

        Input.DisplayOrder = Options.Count + 1;

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(long id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken)) return NotFound();
        if (!ModelState.IsValid) return Page();

        try
        {
            await attributes.CreateOptionAsync(new AttributeOptionCreateRequest(
                id, Input.OptionCode, Input.Title, Input.DisplayOrder), cancellationToken);

            TempData["Success"] = "گزینه جدید ثبت شد.";

            return RedirectToPage(new { id });
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در ثبت گزینه مشخصه {AttributeId}", id);
            ModelState.AddModelError(string.Empty, UiErrors.Message(error));
            return Page();
        }
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        long id, long optionId, string rowVersion, string title, int displayOrder,
        CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken)) return NotFound();

        try
        {
            var version = UiFieldFactory.DecodeRowVersion(rowVersion)
                ?? throw new FormatException("نسخه گزینه ارسال نشده است.");

            await attributes.UpdateOptionAsync(new AttributeOptionUpdateRequest(
                id, optionId, version, title, displayOrder), cancellationToken);

            TempData["Success"] = "تغییرات گزینه ذخیره شد.";
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در ویرایش گزینه {OptionId}", optionId);
            TempData["Error"] = UiErrors.Message(error);
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostSetEnabledAsync(
        long id, long optionId, string rowVersion, bool isEnabled,
        CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken)) return NotFound();

        try
        {
            var version = UiFieldFactory.DecodeRowVersion(rowVersion)
                ?? throw new FormatException("نسخه گزینه ارسال نشده است.");

            await attributes.SetOptionEnabledAsync(new AttributeOptionStateRequest(
                id, optionId, version, isEnabled), cancellationToken);

            TempData["Success"] = isEnabled ? "گزینه فعال شد." : "گزینه غیرفعال شد.";
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در تغییر وضعیت گزینه {OptionId}", optionId);
            TempData["Error"] = UiErrors.Message(error);
        }

        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return false;

        var definition = await attributes.GetAsync(id, cancellationToken);
        if (definition is null
            || definition.IsDeleted
            || definition.SourceKind != 1
            || definition.DataKind != 8)
        {
            return false;
        }

        Attribute = definition;
        Options = await attributes.ListOptionsAsync(id, includeDisabled: true, cancellationToken);
        return true;
    }

    public sealed class NewOptionInput
    {
        [Required(ErrorMessage = "کد گزینه الزامی است.")]
        [StringLength(100, ErrorMessage = "کد گزینه حداکثر ۱۰۰ کاراکتر است.")]
        public string OptionCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "عنوان گزینه الزامی است.")]
        [StringLength(200, ErrorMessage = "عنوان گزینه حداکثر ۲۰۰ کاراکتر است.")]
        public string Title { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }
    }
}
