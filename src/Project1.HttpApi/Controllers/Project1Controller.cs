using Project1.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Project1.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class Project1Controller : AbpControllerBase
{
    protected Project1Controller()
    {
        LocalizationResource = typeof(Project1Resource);
    }
}
