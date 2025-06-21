using Avancira.Domain.Common.Events;
using Avancira.Domain.PromoCodes;

namespace Avancira.Domain.PromoCodes.Events;
public record PromoCodeMaxUsageReachedEvent(PromoCode PromoCode) : DomainEvent;
