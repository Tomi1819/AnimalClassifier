namespace AnimalClassifier.Core.Common.Models
{
    /// <summary>
    /// One page of a longer list, and what a client needs to page through the
    /// rest of it.
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = [];

        /// <summary>
        /// Which page this is, counted from 1.
        /// </summary>
        public int Page { get; set; }

        public int PageSize { get; set; }

        /// <summary>
        /// How many items the whole list holds.
        /// </summary>
        public int TotalCount { get; set; }
    }
}
