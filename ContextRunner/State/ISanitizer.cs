namespace ContextRunner.State
{
    public interface ISanitizer
    {
        object? Sanitize(KeyValuePair<string, object?> contextParam);
    }
}
