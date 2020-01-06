namespace Rest4GP.Core.Parameters
{

    /// <summary>
    /// Parameters used for the query
    /// </summary>
    public class RestParameters
    {

        /// <summary>
        /// Number of elements to fetch
        /// </summary>
        public int Take { get; set; }

        /// <summary>
        /// Number of elements to skip
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// Sort by
        /// </summary>
        public RestSort Sort { get; set; }

        /// <summary>
        /// Filter to apply
        /// </summary>
        public RestFilter Filter { get; set; }

        /// <summary>
        /// Smart filter
        /// </summary>
        public RestSmartFilter SmartFilter { get; set; }
        
        /// <summary>
        /// True if the response has to contains the count field
        /// </summary>
        public bool WithCount { get; set; } = false;

    }
}