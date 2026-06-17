using System;
using Microsoft.Extensions.Hosting;

using var host = Host.CreateDefaultBuilder().Build();
var config = host.Services.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration)) as Microsoft.Extensions.Configuration.IConfiguration;
var env = host.Services.GetService(typeof(Microsoft.Extensions.Hosting.IHostEnvironment)) as Microsoft.Extensions.Hosting.IHostEnvironment;
Console.WriteLine("ASPNETCORE_ENVIRONMENT=" + Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
Console.WriteLine("ContentRoot=" + (env?.ContentRootPath ?? "(none)"));
Console.WriteLine("ConnectionString(Default)=" + (config?["ConnectionStrings:Default"] ?? "(null)"));
return 0;
