namespace ContextRunner.Logging
{
    public enum ContextLogEntryType
    {
        AlwaysShow,
        ShowOnlyOnError,
        ContextStart,
        ChildContextStart,
        ContextEnd,
        ChildContextEnd,
        Summary,
        OutOfContext
    }
}