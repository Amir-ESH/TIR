using Microsoft.AspNetCore.Mvc.RazorPages;
using TIR.Application.Contracts;
using TIR.Web.Presentation;

namespace TIR.Web.Pages;

public sealed class IndexModel(IEmployeeService employees, IAttributeService attributes,
    IFormulaService formulas, ILogger<IndexModel> logger) : PageModel
{
    public long EmployeeCount { get; private set; }
    
    public long AttributeCount { get; private set; }
    
    public int ActiveFormulaCount { get; private set; }
    
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var employeeTask = employees.ListAsync(new EmployeeListRequest(PageSize: 1), cancellationToken);
            var attributeTask = attributes.ListAsync(new AttributeListRequest(PageSize: 1), cancellationToken);
            var formulaTask = formulas.GetCalculationOrderAsync(cancellationToken);

            await Task.WhenAll(employeeTask, attributeTask, formulaTask);

            EmployeeCount = employeeTask.Result.TotalCount;
            AttributeCount = attributeTask.Result.TotalCount;
            ActiveFormulaCount = formulaTask.Result.Count;
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در بارگذاری اطلاعات داشبورد");
            ErrorMessage = UiErrors.Message(error);
        }
    }
}
