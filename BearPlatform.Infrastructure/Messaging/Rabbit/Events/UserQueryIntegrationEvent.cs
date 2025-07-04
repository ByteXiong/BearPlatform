using BearPlatform.EventBus.Events;

namespace BearPlatform.Infrastructure.Messaging.Rabbit.Events;

// Integration Events notes: 
// An Event is “something that has happened in the past”, therefore its name has to be   
// An Integration Event is an event that can cause side effects to other microsrvices, Bounded-Contexts or external systems.
public record UserQueryIntegrationEvent(long UserId, int Line) : IntegrationEvent;
