using System.Net.Http.Headers;
using System.Text;
using JiraWorkLogger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, serviceProvider, loggerConfiguration) =>
    {
        loggerConfiguration.ReadFrom.Configuration(context.Configuration);
    })
    .ConfigureServices((context, services) =>
    {
        services
            .AddHostedService<Worker>()
            .AddHttpClient<WorkLogger>(client =>
            {
                var baseUrl = context.Configuration["app:baseUrl"] ?? throw new Exception("Missing 'baseUrl' configuration");
                var username = context.Configuration["app:username"] ?? throw new Exception("Missing 'username' configuration");
                var apiToken = context.Configuration["app:apiToken"] ?? throw new Exception("Missing 'apiToken' configuration");

                client.BaseAddress = new Uri(baseUrl);
                var byteArray = Encoding.ASCII.GetBytes($"{username}:{apiToken}");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            });
    })
    .Build();

await host.RunAsync();
