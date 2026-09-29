namespace AnimalClassifier.Infrastructure.Data.Converters
{
    using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

    /// <summary>
    /// Every date is saved in UTC, but the database keeps no kind with it, so
    /// one read back would otherwise be sent out without its "Z" and taken by a
    /// browser as local time.
    /// </summary>
    public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter()
            : base(date => date, date => DateTime.SpecifyKind(date, DateTimeKind.Utc))
        {
        }
    }
}
