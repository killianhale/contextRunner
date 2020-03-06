# ContextRunner.FlatIO

[![NuGet](https://img.shields.io/nuget/dt/contextrunner.flatio.svg)](https://www.nuget.org/packages/contextrunner.flatio) 
[![NuGet](https://img.shields.io/nuget/vpre/contextrunner.flatio.svg)](https://www.nuget.org/packages/contextrunner.flatio)

ContextRunner.FlatIO is an example of a custom implementation of ContextRunner that doesn't use a logging framework but instead writes to log files itself.

### Installing ContextRunner

You should install [ContextRunner.FlatIO with NuGet](https://www.nuget.org/packages/ContextRunner.FlatIO):

    Install-Package ContextRunner.FlatIO
    
Or via the .NET Core command line interface:

    dotnet add package ContextRunner.FlatIO

Either commands, from Package Manager Console or .NET Core CLI, will download and install ContextRunner.FlatIO and all required dependencies, including the base ContextRunner package.

### FlatIOContextRunner Settings
Some of the FlatIOContextRunner's behavior can be customized with the following settings inside of the `FlatIOContextRunnerConfig` object:
* LogDir - The directory to output log files to. If not specified, `./logs` is used.
* BaseLogName - The base filename of the resulting log files. If not specified, `flatcontext` is used.

### Output
This implementation of ContextRunner doesn't consume the live log entry output. Aggregated log entries are output when the root context completes and contains the end context state. The resulting log file is saved as `{logDir}/{baseLogName}-{dateSuffix}.log`.

The following is an example of a lof file produced by ContextRunner.FlatIO:

```
====================================================================================================
 Context: Appointment_Post
 Timestamp: 2020-03-03 03:54:22.0373
 TimeElapsed: 00:00:09.2728397
 Level: ERROR
 Summary: The context 'Appointment_Post' ended with an error. There was a problem with the connection to the service 'EventStore'
====================================================================================================
	Exception: EventStoreLearning.Exceptions.ServiceConnectionException: There was a problem with the connection to the service 'EventStore'
 ---> ...
	command: EventStoreLearning.Appointment.Commands.CreateAppointmentCommand
	commandType: CreateAppointmentCommand
	request: System.Collections.Generic.Dictionary`2[System.String,System.String]
====================================================================================================
00:00:00.0015893 DEBUG 	Handling command CreateAppointmentCommand for Aggregate Appointment
00:00:00.0018325 DEBUG 		Fetching aggregate dependency of type Appointment and ID 03682fcc-6af6-4dfe-a50a-96e7bbab57e9.
00:00:00.0624309 INFO  			Getting Aggregate of type Appointment and ID 03682fcc-6af6-4dfe-a50a-96e7bbab57e9.
00:00:00.0024198 DEBUG 				Getting all events for Aggregate of type Appointment (Stream '$ce-Appointment') starting at position 0.
00:00:00.0042531 TRACE 				Getting event slice 0-200 from stream '$ce-Appointment' from the event store.
00:00:02.8105842 ERROR 				There was a problem with the connection to the service 'EventStore'
00:00:03.0608987 TRACE 				An exception of type ServiceConnectionException was thrown within the context 'EventStoreClient.GetAllEventsForAggregateType'!
00:00:03.2022056 TRACE 				Context EventStoreClient.GetAllEventsForAggregateType has ended.
00:00:04.4800381 TRACE 			Context EventStoreClient.GetAggregateById has ended.
00:00:05.7231170 TRACE 		Context AggregateOrchestrator has ended.
00:00:07.6081012 TRACE 	Context AppointmentCommandHandler has ended.
00:00:09.2728397 TRACE Context Appointment_Post has ended.
====================================================================================================
```