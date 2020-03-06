# ContextRunner.NLog

[![NuGet](https://img.shields.io/nuget/dt/contextrunner.nlog.svg)](https://www.nuget.org/packages/contextrunner.nlog) 
[![NuGet](https://img.shields.io/nuget/vpre/contextrunner.nlog.svg)](https://www.nuget.org/packages/contextrunner.nlog)

ContextRunner.NLog is an implementation of ContextRunner that utilizes NLog to ouput the resulting log entries.

### Installing ContextRunner

You should install [ContextRunner.NLog with NuGet](https://www.nuget.org/packages/ContextRunner.NLog):

    Install-Package ContextRunner.NLog
    
Or via the .NET Core command line interface:

    dotnet add package ContextRunner.NLog

Either commands, from Package Manager Console or .NET Core CLI, will download and install ContextRunner.NLog and all required dependencies, including the base ContextRunner package.

### NlogContextRunner Settings
Some of the NLogContextRunner's behavior can be customized with the following settings inside of the `NLogContextRunnerConfig` object:
* AddSpacingToEntries - Adds a `\t` to the output message for each nested log entry based the on ContextDepth
* SanitizedProperties - An array of property names to strip out of all object logged with the context. Uses the KeyBasedSanitizer.

### Rules and Logger Names
Live log output contains the context state at the time of the log entry and uses the name of the context as the logger name. Aggregated log entries are output when the root context completes and contains the end context state. The logger's name for the aggregated context output is the name of the context prefixed with `context_`. 

The following `nlog.config` seperates the aggregate output into its own file and includes the live output in an "all" file.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true"
      internalLogLevel="Error"
      internalLogFile="logs/internal-nlog.txt">

  <extensions>
    <add assembly="NLog.Web.AspNetCore"/>
  </extensions>

  <targets>
    <target xsi:type="File" name="allfile" fileName="logs/nlog-all-${shortdate}.log">
        <layout xsi:type="JsonLayout">
            <attribute name="time" layout="${longdate}" />
            <attribute name="level" layout="${level:upperCase=true}"/>
            <attribute name="logger" layout="${logger}"/>
            <attribute name="message" layout="${message}" />
             <attribute name="properties" encode="false" >
                 <layout type='JsonLayout' includeAllProperties="true" maxRecursionLimit="1">
                    <attribute name="exception" layout="${exception:format=tostring}" />
                 </layout>
             </attribute>
        </layout>
      </target>
      
    <target xsi:type="File" name="contextfile" fileName="logs/contexts-${shortdate}.log">
        <layout xsi:type="JsonLayout">
            <attribute name="time" layout="${longdate}" />
            <attribute name="level" layout="${level:upperCase=true}"/>
            <attribute name="logger" layout="${logger}"/>
            <attribute name="message" layout="${message}" />
             <attribute name="properties" encode="false" >
                 <layout type='JsonLayout' includeAllProperties="true" maxRecursionLimit="1">
                    <attribute name="exception" layout="${exception:format=tostring}" />
                 </layout>
             </attribute>
        </layout>
      </target>
  </targets>

  <rules>
    <logger name="context_*" writeTo="contextfile" final="true" />
    <logger name="*" writeTo="allfile" />
  </rules>
</nlog>
```