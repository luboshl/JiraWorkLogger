using System.Globalization;

namespace JiraWorkLogger;

public static class Parser
{
    public static ICollection<WorkLog> ParseInput(ICollection<string> input)
    {
        if (input.Count < 2)
        {
            throw new Exception("There must be at least 2 lines.");
        }

        var dates = input
            .First()
            .Split("\t", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseDate)
            .ToList();

        var timeLines = input
            .Skip(1)
            .Select(line => line
                .Split("\t", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList())
            .Select(line => new
            {
                IssueKey = line.First(),
                Times = line.Skip(1).Select(ParseDecimal).ToList()
            })
            .ToDictionary(x => x.IssueKey, x => x.Times);

        var workLogs = new List<WorkLog>();

        foreach (var timeLine in timeLines)
        {
            if (timeLine.Value.Count != dates.Count)
            {
                throw new Exception($"The number of times does not match the number of dates for '{timeLine.Key}'.");
            }

            workLogs.AddRange(
                timeLine.Value
                    .Select((time, i) => new WorkLog(dates[i], timeLine.Key, time)));
        }

        return workLogs;
    }

    private static DateOnly ParseDate(string dateString)
    {
        string[] formats = ["d.M.", "dd.MM.", "d.M.yyyy", "dd.MM.yyyy"];

        if (DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDateTime))
        {
            return DateOnly.FromDateTime(parsedDateTime);
        }

        throw new Exception($"Value '{dateString}' cannot be parsed as date.");
    }

    private static decimal ParseDecimal(string decimalString)
    {
        decimalString = decimalString.Replace(",", ".");
        return decimal.Parse(decimalString, CultureInfo.InvariantCulture);
    }
}
