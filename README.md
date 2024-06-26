# Brer

A rabbitMQ library for ASP.NET.

__Special thanks to Marco Pil__

[![Auto Publish to NuGet](https://github.com/karmalegend/Brer/actions/workflows/Release.yml/badge.svg)](https://github.com/karmalegend/Brer/actions/workflows/Release.yml)

- [Brer](#brer)
- [How do i use Brer?](#how-do-i-use-brer)
  - [Initial Setup](#initial-setup)
  - [Registering/Decorating an EventListener](#registeringdecorating-an-eventlistener)
      - [Handler](#handler)
      - [WildCardHandler](#wildcardhandler)
  - [Publishing events.](#publishing-events)
  - [Error handling.](#error-handling)
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
        .WithPassword(BrerOptionsBuilder.defaultLogin)
        .WithUsername(BrerOptionsBuilder.defaultLogin)
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
* `BrerHostName`
* `BrerPort`
* `BrerExchangeName`
* `BrerQueueName`
* `BrerUserName`
* `BrerPassword`


## Registering/Decorating an EventListener
Brer will automatically scan all referencing assemblies for EventListeners.

Once a class is annoted with the ```EventListener``` attribute it will further scan the class for ```Handler``` or ```WildCardHandler```.

If Listeners collide on topic names ```Handler``` will always take priority over ```WildCardHandler```.

#### Handler
```C#
[EventListener]
public class MyEventHandler{

    [Handler(topic:"MyTopic")]
    public async Task Handle(MyEvent @event){
        // do stuff
    }

}
```


#### WildCardHandler

When using a WildCardHandler you're **required** to use at least 1 wild card in the topic binding.

Wild cards can contain as many * as wanted but impose some restrictions on the usage of #.
Only 1 # can be used somewhere in the middle of a topic name and 1 the end. if you require more it's likely best to re-evaluate the topic naming or simply use *.

A valid format would be:
* ```MyGarage*.Rental.Cars.#.Internal.*.#```
* ```#```
* etc

An invalid format would be:
```MyGarage.#.Cars.#.Internal.*.#``` We use two #'s in the middle of the topic here which is not allowed.


```C#
[EventListener]
public class MyEventHandler{

    [WildCardHandler("MyTopic.#")]
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

## Error handling.
Brer currently does not support DLX or 'poison queues' brer does however add some message headers to give insights into
exceptions and requeue counts.

These headers are:
* `x-Brer-Exception`
* `x-Brer-Exception-Message`
* `x-Brer-Exception-StackTrace`
* `x-Brer-RequeueCount`

Using these headers, you could add your own DLX / poison queue logic f.e. by checking the requeue count and if it's above
3 sending it to a poison queue. Just make sure you preserve the other headers, to make debugging & tracing a lot easier.


# I want to contribute!
As of now we'd greatly appreciate adding support for different exchange types. And adding dlx support 
/ requeue-ing messages from a dlx.

notes: 
- No global usings
- Use SonarLint
- Rider/ReSharper default styling preferred


# I'm missing a feature!
[Simply open an issue](https://github.com/karmalegend/Brer/issues)

