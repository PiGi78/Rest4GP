using System.Collections.Generic;

namespace Rest4GP.Core.Parameters
{

    /// <summary>
    /// Sorting parameter
    /// </summary>
    public class RestSort
    {
        /// <summary>
        /// Fields used for sort
        /// </summary>
        public List<RestSortField> Fields { get; set;} = new List<RestSortField>();
    }



    /// <summary>
    /// Filed to sort for
    /// </summary>
    public class RestSortField
    {
        /// <summary>
        /// Field to order by
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Direction of the sort (asc/desc)
        /// </summary>
        public SortDirections Direction { get; set; }
    }


    /// <summary>
    /// Sort direction possibilities
    /// </summary>
    public enum SortDirections 
    {
        /// <summary>Order from lowest to highest value</summary>
        Ascending,

        /// <summary>Order from highest to lowest</summary>
        Descending
    }

}