# Brer

A rabbitMQ library for ASP.NET

- [Brer](#brer)
- [How do i use Brer?](#how-do-i-use-brer)
  - [Initial Setup](#initial-setup)
  - [Registering/Decorating an EventListener](#registeringdecorating-an-eventlistener)
  - [Publishing events.](#publishing-events)
- [I want to contribute!](#i-want-to-contribute)
- [I'm missing a feature!](#im-missing-a-feature)


# How do i use Brer?
Brer makes use of a [hostedservice](https://learn.microsoft.com/en-us/dotnet/core/extensions/timer-service?pivots=dotnet-6-00) in ASP.NET.


A basic brer startup example can be found [here](https://github.com/karmalegend/Brer/tree/main/Brer/Example%20Project).

## Initial Setup

In short one must register the Brer services as follows:
```C#
services.UseBrer(
    new BrerOptionsBuilder().WithAddress(BrerOptionsBuilder.localHost, BrerOptionsBuilder.defaultPort)
        .WithPassWord(BrerOptionsBuilder.defaultLogin)
        .WithUserName(BrerOptionsBuilder.defaultLogin)
        .WithExchange("MyExchange")
        .WithQueueName("MyQueue")
        .Build()
);
```

Brer can also be configured using environment variables as follows:
```C#
services.UseBrer(new BrerOptionsBuilder().ReadFromEnviromentVariables().Build());
```
Where it will look for the following variables:
* BrerHostName
* BrerPort
* BrerExchangeName
* BrerQueueName
* BrerUserName
* BrerPassword


## Registering/Decorating an EventListener
Brer will automatically scan all referencing assemblies for EventListeners.

Once a class is annoted with the ```EventListener``` attribute it will further scan the class for ```Handlers```.

**NOTE:** wildcards are currently not (officially) supported.

```C#
[EventListener]
public class MyEventHandler{

    [Handler(topic:"MyTopic")]
    public async Task Handle(MyEvent @event){
        // do stuff
    }

}
```


## Publishing events.

To publish an event simply inject the ```IBrerPublisher``` into your class.
```C#
public class MyEventPublisher{

    private readonly IBrerPublisher _brerPublisher;

    MyEventPublisher(IBrerPublisher brerPublisher){
        _brerPublisher = brerPublisher;
    }

    PublishEvent(){
        _brerPublisher.Publish("MyTopic", MyEvent)
    }
}
```


# I want to contribute!
As of now we'd greatly appreciate more Example projects & unit-testing of the library.

notes: 
- No global usings
- Use SonarLint
- Rider/ReSharper default styling preferred


# I'm missing a feature!
[Simply open an issue](https://github.com/karmalegend/Brer/issues)

