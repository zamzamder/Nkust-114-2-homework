using Volo.Abp.Settings;

namespace Project1.Settings;

public class Project1SettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(Project1Settings.MySetting1));
    }
}
