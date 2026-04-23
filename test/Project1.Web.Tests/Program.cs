using Microsoft.AspNetCore.Builder;
using Project1;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();

builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("Project1.Web.csproj");
await builder.RunAbpModuleAsync<Project1WebTestModule>(applicationName: "Project1.Web" );

public partial class Program
{
}
