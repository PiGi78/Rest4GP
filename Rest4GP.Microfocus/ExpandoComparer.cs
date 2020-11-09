using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using Rest4GP.Core.Parameters;

namespace Rest4GP.Microfocus
{

    /// <summary>
    /// Comparer for expando object
    /// </summary>
    internal class ExpandoComparer : IComparer<ExpandoObject>
    {

        /// <summary>
        /// Creates a new instance of Expando Comparer
        /// </summary>
        /// <param name="sort">Sort method</param>
        public ExpandoComparer(RestSort sort)
        {
            Sort = sort;
        }

        /// <summary>
        /// Sort info
        /// </summary>
        private RestSort Sort { get; }


        /// <summary>
        /// Compare two objects
        /// </summary>
        /// <param name="x">First object</param>
        /// <param name="y">Second object</param>
        /// <returns>Result of the compare</returns>
        public int Compare([AllowNull] ExpandoObject x, [AllowNull] ExpandoObject y)
        {
            if (Sort == null) return 0;

            if (x == null && y == null) return 0;
            if (x == null) return 1;
            if (y == null) return -1;

            var dicX = (IDictionary<string, object>)x;
            var dicY = (IDictionary<string, object>)y;


            var comparer = Comparer<object>.Default;
            foreach (var field in Sort.Fields)
            {
                object valueX = dicX.ContainsKey(field.Field) ? dicX[field.Field] : null;
                object valueY = dicY.ContainsKey(field.Field) ? dicY[field.Field] : null;
                if (valueX == null && valueY == null) continue;

                var multiplier = field.Direction == SortDirections.Ascending ? 1 : -1;
                if (valueX == null) return 1 * multiplier;
                if (valueY == null) return -1 * multiplier;

                var result = comparer.Compare(valueX, valueY);
                if (result == 0) continue;

                return result * multiplier;
            }

            return 0;
        }
    }
}