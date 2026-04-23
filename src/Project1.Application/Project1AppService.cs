using System;
using System.Collections.Generic;
using System.Text;
using Project1.Localization;
using Volo.Abp.Application.Services;

namespace Project1;

/* Inherit your application services from this class.
 */
public abstract class Project1AppService : ApplicationService
{
    protected Project1AppService()
    {
        LocalizationResource = typeof(Project1Resource);
    }
}
