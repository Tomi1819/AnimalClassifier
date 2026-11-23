namespace AnimalClassifier.Core.Recognitions
{
    using AnimalClassifier.Core.Common.Exceptions;

    /// <summary>
    /// The time zone a user's days start and end in, which only their browser
    /// knows, for whatever counts their recognitions by the day. The database
    /// keeps every date in UTC.
    /// </summary>
    public static class UserTimeZone
    {
        /// <summary>
        /// Written to be shown as it stands.
        /// </summary>
        public const string UnknownTimeZone = "The time zone is not recognized.";

        /// <param name="timeZoneId">An IANA name, such as <c>Europe/Sofia</c>, or null for UTC.</param>
        /// <exception cref="RequestRefusedException">When no time zone has the name.</exception>
        public static TimeZoneInfo Find(string? timeZoneId)
        {
            if (timeZoneId is null)
            {
                return TimeZoneInfo.Utc;
            }

            return TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone)
                ? timeZone
                : throw new RequestRefusedException(UnknownTimeZone);
        }
    }
}
