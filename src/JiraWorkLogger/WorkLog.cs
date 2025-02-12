namespace JiraWorkLogger;

public record WorkLog(DateOnly Date, decimal TimeInHours, string Description, string IssueKey);