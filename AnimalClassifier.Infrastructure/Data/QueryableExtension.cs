namespace AnimalClassifier.Infrastructure.Data
{
    using AnimalClassifier.Core.Data.Entities;
    using Microsoft.EntityFrameworkCore;

    public static class QueryableExtension
    {
        /// <summary>
        /// One page of an ordered query, counted from 1. A page past the last
        /// is empty, however far past it is: the items before a page are
        /// counted in a long, and any more than an int holds are as many as
        /// there could be.
        /// </summary>
        public static IQueryable<T> TakePage<T>(this IQueryable<T> query, int page, int pageSize) =>
            query.Skip((int)Math.Min((page - 1L) * pageSize, int.MaxValue))
                 .Take(pageSize);

        /// <summary>
        /// The feedback an administrator reviews. Feedback whose user did not
        /// allow training is never reviewed, so the review never sees it.
        /// </summary>
        public static IQueryable<RecognitionFeedback> ForReview(this IQueryable<RecognitionFeedback> feedback) =>
            feedback.Where(f => f.AllowsTraining);

        /// <summary>
        /// The user's recognitions that are still in their history, of a file
        /// whose name ends with one of the extensions. The match is SQL's
        /// LIKE, so the column's collation compares; the extensions are
        /// MediaFile's, none of which holds a character LIKE treats as a
        /// wildcard.
        /// </summary>
        public static IQueryable<AnimalRecognitionLog> InHistoryOf(this IQueryable<AnimalRecognitionLog> logs, string userId, IEnumerable<string> extensions) =>
            logs.Where(l => l.UserId == userId
                && !l.IsDeleted
                && extensions.Any(extension => EF.Functions.Like(l.FileName, "%" + extension)));
    }
}
