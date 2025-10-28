using Avancira.Domain.Common.Exceptions;

namespace Avancira.Application.StudentProfiles
{
    public sealed record SubscriptionPeriod
    {
        public DateTime StartUtc { get; init; }
        public DateTime EndUtc { get; init; }

        public SubscriptionPeriod(DateTime startUtc, DateTime endUtc)
        {
            if (endUtc <= startUtc)
                throw new AvanciraDomainException("Subscription end date must be after start date.");

            StartUtc = startUtc;
            EndUtc = endUtc;
        }

        /// <summary>
        /// Returns true if the subscription is currently active for the given UTC time.
        /// </summary>
        public bool IsActive(DateTime nowUtc) => nowUtc >= StartUtc && nowUtc <= EndUtc;

        /// <summary>
        /// Returns true if the subscription period has fully expired.
        /// </summary>
        public bool IsExpired(DateTime nowUtc) => nowUtc > EndUtc;

        /// <summary>
        /// Extends the subscription by a given duration (returns a new instance).
        /// </summary>
        public SubscriptionPeriod Extend(TimeSpan extension) =>
            new(StartUtc, EndUtc.Add(extension));

        public override string ToString() =>
            $"{StartUtc:u} → {EndUtc:u}";
    }
}
