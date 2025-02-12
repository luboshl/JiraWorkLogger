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

        var header = input
            .First()
            .Split("\t", StringSplitOptions.TrimEntries)
            .ToArray();

        var expectedHeader = new[] { "Datum", "Hodin", "Popis činnosti", "Work Item" };

        for (var i = 0; i < expectedHeader.Length; i++)
        {
            if (header[i] != expectedHeader[i])
            {
                throw new Exception($"Expected header '{expectedHeader[i]}' but got '{header[i]}'.");
            }
        }

        var workLogs = input
            .Skip(1)
            .Select(line => line
                .Split("\t", StringSplitOptions.TrimEntries)
                .ToList())
            .Select(line => new WorkLog(
                ParseDate(line[0]),
                ParseDecimal(line[1]),
                line[2],
                line[3]))
            .ToList();
        
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
        if (string.IsNullOrWhiteSpace(decimalString))
        {
            return 0;
        }

        decimalString = decimalString.Replace(",", ".");
        return decimal.Parse(decimalString, CultureInfo.InvariantCulture);
    }
}
