using Avancira.Domain.Events;
using Backend.Domain.PromoCodes;

namespace Avancira.Domain.PromoCodes.Events;
public record PromoCodeMaxUsageReachedEvent(PromoCode PromoCode) : DomainEvent;

