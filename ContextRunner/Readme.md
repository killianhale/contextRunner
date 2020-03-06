The following details out how to implement your own ContextRunner to output log entries however you wish. If you're looking for information about ContextRunner in general, please click [here](https://github.com/matholum/contextRunner).

### Installing ContextRunner

You should install [ContextRunner with NuGet](https://www.nuget.org/packages/ContextRunner):

    Install-Package ContextRunner
    
Or via the .NET Core command line interface:

    dotnet add package ContextRunner

Either commands, from Package Manager Console or .NET Core CLI, will download and install ContextRunner and all required dependencies.

### Implementing ContextRunner
Implementing your own log output is as easy as installing ContextRunner and either providing an `onSetup` deleagte as an argument to the `ContextRunner` class or to create a class derived from `ContextRunner` that directly sets the `OnSetup` delegate property.

The `OnStart` delegate is invoked from root level contexts just before invoking action delegate containing the operation to take place in the context. The `OnStart` delegate recieves the context as an input parameter. To implement logging output, you'll have to use this delegate and the context parameter to subscribe to the `WhenEntryLogged` observable on the `Logger` property of the context. The `onNext` of the observable can be used to log live log entry output with the state of the context at the time of the entry. The `onComplete` of the observable can be used for aggregated log ouput with all of the log entries created during the life of the context and the end state within it. The `onError` isn't used.

The following is an example taken from `ContextRunner.NLog`:

```c#
public class NlogContextRunner : ContextRunner
{
    private readonly NlogContextRunnerConfig _config;

    public NlogContextRunner(IOptionsMonitor<NlogContextRunnerConfig> nlogContextOptions)
    {
        _config = nlogContextOptions.CurrentValue;

        OnStart = Setup;
        //...
    }

    private void Setup(ActionContext context)
    {
        context.Logger.WhenEntryLogged.Subscribe(
            entry => LogEntry(context, entry),
            exception => LogContext(context),
            () => LogContext(context));
    }

    //...
}
```

### Sanitizers
There's an optional second argument to the constructor for `ContextRunner` which sets the `Sanitizers` property of type `ISanitizer` array. `ISanitizer` defines a `Sanitize` method that takes in the key value pair of a state parameter and returns an object. Each sanitizer is invoked when a parameter is created or updated on the context state, enabling you to log part of an object and redact sensitive information. You can take a look at the `KeyBasedSanitizer` for reference.