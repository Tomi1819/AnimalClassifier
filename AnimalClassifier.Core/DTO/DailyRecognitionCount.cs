namespace AnimalClassifier.Core.DTO
{
    /// <summary>
    /// The number of recognitions made on one day, in the time zone the day
    /// was requested in.
    /// </summary>
    public class DailyRecognitionCount
    {
        public DateOnly Date { get; set; }
        public int Count { get; set; }
    }
}
