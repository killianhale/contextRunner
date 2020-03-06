# ContextRunner.Http

[![NuGet](https://img.shields.io/nuget/dt/contextrunner.http.svg)](https://www.nuget.org/packages/contextrunner.http) 
[![NuGet](https://img.shields.io/nuget/vpre/contextrunner.http.svg)](https://www.nuget.org/packages/contextrunner.http)

ContextRunner.Http is an `ASP.NET Core` middleware that utilizes ContextRunner to wrap incoming web requests in its own top level context, giving you one aggregated log entry for the request with all the associated context state.

### Installing ContextRunner

You should install [ContextRunner.Http with NuGet](https://www.nuget.org/packages/ContextRunner.Http):

    Install-Package ContextRunner.Http
    
Or via the .NET Core command line interface:

    dotnet add package ContextRunner.Http

Either commands, from Package Manager Console or .NET Core CLI, will download and install ContextRunner.Http and all required dependencies. 

Note: You'll need to either install or provide a logging implementation to actually get logging output.

### Initialization
To initialize ContextRunner.Http, simply call the provided `IApplicationBuilder` extension method within your startup's `Configure()` method:

```c#
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
  //Note: If using exception middleware, it must come first!
  //app.UseMiddleware<ExceptionMiddleware>();

  app.AddContextMiddleware();
}
```

### Context Name and Context State
ContextRunner.Http will name the context based on the matching endpoint, using `{ControllerName}_{MethodName}`. It will also automatically add a state param to your context for the incoming request. The following is an example taken from the ContextLogger.NLog implementation:

```json
{
  "time": "2020-03-03 00:02:23.3827",
  "level": "INFO",
  "logger": "context_Appointment_Post",
  "message": "The context 'Appointment_Post' ended without error.",
  "properties": {
      "contextDepth": 0,
      "contextName": "Appointment_Post",
      "timeElapsed": "00:00:01.7392562",
      "request": {
          "Content-Type": "application\/json",
          "Accept": "application\/json",
          "Host": "localhost:5001",
          "Referer": "https:\/\/localhost:5001\/index.html",
          "User-Agent": "Mozilla\/5.0 (Macintosh; Intel Mac OS X 10_15_1) AppleWebKit\/537.36 (KHTML, like Gecko) Chrome\/79.0.3945.117 Safari\/537.36",
          "Origin": "https:\/\/localhost:5001",
          "Path": "\/v1\/Appointment",
          "Method": "POST"
      }
  },
  "entries": []
}
```
