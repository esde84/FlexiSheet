namespace FlexiSheet.Domain;

public sealed class RuleViolationException(string message) : Exception(message);
