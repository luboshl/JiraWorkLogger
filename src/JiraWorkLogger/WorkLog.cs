namespace JiraWorkLogger;

public record WorkLog(DateOnly Date, string IssueKey, decimal TimeInHours);