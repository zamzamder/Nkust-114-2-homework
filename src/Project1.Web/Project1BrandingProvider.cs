using Microsoft.Extensions.Localization;
using Project1.Localization;
using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace Project1.Web;

[Dependency(ReplaceServices = true)]
public class Project1BrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<Project1Resource> _localizer;

    public Project1BrandingProvider(IStringLocalizer<Project1Resource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
