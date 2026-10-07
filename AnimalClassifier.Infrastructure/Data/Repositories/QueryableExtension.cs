namespace AnimalClassifier.Infrastructure.Data.Repositories
{
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
    }
}
