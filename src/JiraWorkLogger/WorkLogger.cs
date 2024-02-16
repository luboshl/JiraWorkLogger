using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JiraWorkLogger;

public class WorkLogger(
    ILogger<WorkLogger> logger,
    HttpClient httpClient,
    IConfiguration configuration)
{
    public async Task Run()
    {
        logger.LogInformation("Configured base URL: {BaseUrl}", configuration["app:baseUrl"]); 
        logger.LogInformation("Configured username: {Username}", configuration["app:username"]); 
        logger.LogInformation("Paste input and press Enter");

        var currentLine = Console.ReadLine();
        var input = new List<string>();

        while (currentLine?.Trim() != "")
        {
            input.Add(currentLine!);
            currentLine = Console.ReadLine();
        }

        var workLogs = Parser.ParseInput(input);

        foreach (var workLog in workLogs)
        {
            logger.LogInformation("{IssueKey} {Date} -> {Time}", workLog.IssueKey, workLog.Date, workLog.TimeInHours);
        }

        foreach (var group in workLogs.GroupBy(x => x.IssueKey).OrderBy(x => x.Key))
        {
            logger.LogInformation("Summary of {IssueKey}: {Sum}", group.Key, group.Sum(x => x.TimeInHours));
        }

        logger.LogInformation("Total summary: {Sum}", workLogs.Sum(x => x.TimeInHours));
        logger.LogInformation("Press Enter to continue or Ctrl+C to cancel");
        Console.ReadLine();

        foreach (var workLog in workLogs.OrderBy(x => x.IssueKey).ThenBy(x => x.Date))
        {
            var issueKey = workLog.IssueKey;
            var currentOffset = DateTimeOffset.Now.Offset;
            var dateTime = new DateTimeOffset(workLog.Date, new TimeOnly(0), currentOffset);
            var timeToLog = TimeSpan.FromHours((double)workLog.TimeInHours);

            await LogWork(issueKey, dateTime, timeToLog);
        }
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

            logger.LogInformation("Press Enter to continue or Ctrl+C to cancel");
            Console.ReadLine();
        }
    }
}
