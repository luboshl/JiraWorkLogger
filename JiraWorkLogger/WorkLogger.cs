using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JiraWorkLogger;

public class WorkLogger(
    ILogger<WorkLogger> logger,
    IConfiguration configuration,
    HttpClient httpClient)
{
    public async Task Run()
    {
        var issueKey = "HL-193";
        var timeToLog = TimeSpan.FromHours(3.5);
        var currentOffset = DateTimeOffset.Now.Offset;
        await LogWork(issueKey, new DateTimeOffset(2024, 1, 1, 0, 0, 0, currentOffset), timeToLog);
    }

    private async Task LogWork(string issueKey, DateTimeOffset datetime, TimeSpan timeToLog)
    {
        logger.LogInformation("Log {IssueKey}: {TimeToLog} at {DateTime}", issueKey, timeToLog, datetime);

        var json = $$"""
                     {
                       "started": "{{datetime:yyyy-MM-dd'T'HH:mm:ss.000zz00}}",
                       "timeSpentSeconds": {{(int)timeToLog.TotalSeconds}}
                     }
                     """;

        var data = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync($"rest/api/3/issue/{issueKey}/worklog", data);
        var result = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogInformation("Succeeded - {Result}", result);
            }
            else
            {
                logger.LogInformation("Succeeded");
            }
        }
        else
        {
            logger.LogError("Failed with {StatusCode} - {Result}", response.StatusCode, result);
        }
    }
}
