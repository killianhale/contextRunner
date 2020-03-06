# ContextRunner

[![NuGet](https://img.shields.io/nuget/dt/contextrunner.svg)](https://www.nuget.org/packages/contextrunner) 
[![NuGet](https://img.shields.io/nuget/vpre/contextrunner.svg)](https://www.nuget.org/packages/contextrunner)

Ipsum exercitation reprehenderit minim in aliquip deserunt amet nisi esse. Ad exercitation fugiat incididunt ex qui laborum eiusmod Lorem exercitation. Ullamco magna qui qui reprehenderit velit laboris enim ut ut Lorem non esse fugiat.

## Implementations
On its own, ContextRunner doesn't output any log entries. The following are existing implementations that add logging output:

* [ContextRunner.NLog](/ContextRunner.NLog) - An implementation utilizing NLog to output log entries
* [ContextRunner.FlatIO](/ContextRunner.FlatIO) - An example logging implementation that just uses `System.IO` to output log entries instead of using a common logging library.
* [ContextRunner](/ContextRunner) - You can of course implement your own ContextRunner.. just follow the steps to do so [here](/ContextRunner)!

## Extensions
The following add functionality to ContextRunner or provide tools for using it in different ways:

* [ContextRunner.Http](/ContextRunner.Http) - An ASP.NET Core middleware that wraps incoming requests inside of a context.

## Using ContextRunner

### Initialization
Initializing ContextRunner is best done with dependency injection and can be done with whichever tool you choose. You'll just need to bind `IContextRunner` to either an existing implementation (see above), your own implementation deriving from ContextRunner, or the ContextRunner class itself with the necessary arguments. Some examples are below:

ContextRunner.NLog with `Autofac`
```c#
builder.RegisterType<NlogContextRunner>().As<IContextRunner>();
```

ContextRunner with `Microsoft.Extensions.DependencyInjection`
```c#
services.AddSingleton<IContextRunner, ContextRunner.ContextRunner>(
    builder => {
        var logFactory = builder.GetRequiredService<ILoggerFactory>();

        return new ContextRunner.ContextRunner(
            context => context.Logger.WhenEntryLogged.Subscribe(
                entry => logFactory.CreateLogger("livecontextentries").Log(entry.LogLevel, entry.Message),
                _ => { }, //onError isn't used
                () => logFactory.CreateLogger("aggregatecontextentries").Log(LogLevel.Information, $"Context '{context.ContextName}' has finished!")
            ));
    });
```

### Creating and Using Contexts
To create contexts, inject `IContextRunner` into your class and call the `RunAction` method. The first argument on this method is a delegate that recieves the context as an input parameter and the second argument is the name of the context. The delegate should be where your operations take place, using the context to add log entries and add state for logging and debugging. The various overrides allow you to return a value from the operation running inside of the context and to leverage async and await. Any unhandled exceptions that are thrown inside of the delegate will be wrapped inside of a ContextException containing a reference to the context.

The `Logger` property on the context contains logging methods for the different log levels. Using these will add a log entry to the context and will be picked up by your context runner implementation's subsription to the `WhenEntryLogged` observable. Each log entry will contain the log level, message, and the elapsed time from the start of the context to when the entry was created.

The `State` property allows you to add data to the context for logging and debugging. Its `GetParam()`, `SetParam()`, and `RemoveParam()` methods allow you to get or manipluate a particular context parameter and the `Params` property let's you get all state parameters. The `State` object also has a reference to an `ISanitizer` array. `ISanitizer` defines a `Sanitize` method that takes in the key value pair of a state parameter and returns an object. Each sanitizer is invoked when a parameter is created or updated, enabling you to log part of an object and redact sensitive information.

The following is an example showing a context being created, log entries added, and state parameters being set:

```c#
public async Task<User> GetUser(Guid id)
{
    return await _runner.RunAction(async context =>
    {
        context.State.SetParam("id", id);

        context.Logger.Debug($"Getting User ID {id}...");

        var user = await _userRepo.GetById(id);

        context.State.SetParam("user", user);

        return user;

    }, nameof(UserService));
}
```
