using Project1.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Project1.Web.Pages;

/* Inherit your PageModel classes from this class.
 */
public abstract class Project1PageModel : AbpPageModel
{
    protected Project1PageModel()
    {
        LocalizationResourceType = typeof(Project1Resource);
    }
}
