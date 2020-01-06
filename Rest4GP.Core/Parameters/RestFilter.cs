using System;
using System.Collections.Generic;

namespace Rest4GP.Core.Parameters
{

    /// <summary>
    /// Filter of a request
    /// </summary>
    public class RestFilter : ICloneable
    {

        #region Complex filter

        /// <summary>
        /// Filters to compare
        /// </summary>
        public List<RestFilter> Filters { get; set; } = new List<RestFilter>();


        /// <summary>
        /// Logic to apply between filters
        /// </summary>
        public string Logic { get; set; }



        #endregion


        #region Simple filter
    

        /// <summary>
        /// Filed the filter referes to
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Operator to apply
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// Value to compare to Field
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// True for ignore case (for strings only)
        /// </summary>
        public bool IgnoreCase { get; set; }
        

        #endregion
    

        #region between


        /// <summary>
        /// Creates a filter that rappresents a between filter
        /// </summary>
        /// <param name="fieldName">name of the filter</param>
        /// <param name="from">From</param>
        /// <param name="to">To</param>
        /// <returns>New filter that is a between</returns>
        public static RestFilter CreateBetweenFilter(string fieldName, object from, object to) {
            if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException(nameof(fieldName));
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));


            var betweenFilter = new RestFilter { Logic = FilterLogics.And };
            betweenFilter.Filters.Add(new RestFilter {
                Field = fieldName,
                Operator = FilterOperators.IsGreatherThanOrEqual,
                Value = from
            });

            betweenFilter.Filters.Add(new RestFilter {
                Field = fieldName,
                Operator = FilterOperators.IsLessThanOrEqual,
                Value = to
            });
            
            return betweenFilter;
        }

        #endregion


        #region Cloneable

        /// <summary>
        /// Clone the current filter
        /// </summary>
        /// <returns>Object that rappresents the cloned filter</returns>
        public object Clone()
        {
            return CloneFilter();
        }


        /// <summary>
        /// Clone the current filter
        /// </summary>
        /// <returns>Cloned filter</returns>
        public RestFilter CloneFilter()
        {
            var result = new RestFilter();
            CloneProperties(this, result);
            return result;
        }


        /// <summary>
        /// Clone properties from a filter to another
        /// </summary>
        /// <param name="from">From filter</param>
        /// <param name="to">To filter</param>
        private void CloneProperties(RestFilter from, RestFilter to)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));

            to.Field = from.Field;
            to.IgnoreCase = from.IgnoreCase;
            to.Logic = from.Logic;
            to.Value = from.Value;
            if (from.Filters != null)
            {
                to.Filters = new List<RestFilter>();
                foreach (var filter in from.Filters)
                {
                    if (filter == null)
                    {
                        to.Filters.Add(null);
                    }
                    else
                    {
                        var innerFilter = filter.CloneFilter();
                        to.Filters.Add(innerFilter);
                    }
                }
            }
        }



        #endregion

    }


    /// <summary>
    /// Logics of filters
    /// </summary>
    public static class FilterLogics {

        /// <summary>
        /// And logic (both true gets true, otherwise false)
        /// </summary>
        public static readonly string And = "and";

        /// <summary>
        /// Or logic (both false gets false, otherwise true)
        /// </summary>
        public static readonly string Or = "or";
    }



    /// <summary>
    /// Operators of filters
    /// </summary>
    public static class FilterOperators {

        /// <summary>
        /// Equal: field has to be equal to the given value
        /// </summary>
        public const string IsEqual = "eq";

        /// <summary>
        /// Not equal: field has to be different from the given value
        /// </summary>
        public const string IsNotEqual = "neq";

        /// <summary>
        /// Null: field has to be null (no value)
        /// </summary>
        public const string IsNull = "isnull";

        /// <summary>
        /// Not null: field has to have a value
        /// </summary>
        public const string IsNotNull = "isnotnull";

        /// <summary>
        /// Less: field has to be less than the given value
        /// </summary>
        public const string IsLessThan = "lt";

        /// <summary>
        /// Less or equal: field has to be less than or equal to the given value
        /// </summary>
        public const string IsLessThanOrEqual = "lte";

        /// <summary>
        /// Greater: field has to be greater than the given value
        /// </summary>
        public const string IsGreaterThan = "gt";

        /// <summary>
        /// Greater or equal: field has to be greater or equal to the given value
        /// </summary>
        public const string IsGreatherThanOrEqual = "gte";

        /// <summary>
        /// Starts: field has to start with the given value (strings only)
        /// </summary>
        public const string StartsWith = "startswith";

        /// <summary>
        /// Ends: field has to end with the given value (strings only)
        /// </summary>
        public const string EndsWith = "endswith";

        /// <summary>
        /// Contains: field has to contain the given value (strings only)
        /// </summary>
        public const string Contains = "contains";

        /// <summary>
        /// Not contains: field has not to contain the given value (strings only)
        /// </summary>
        public const string DoesNotContain = "doesnotcontain";

        /// <summary>
        /// Empty: field has to be empty (strings only). Null is different from empty
        /// </summary>
        public const string IsEmpty = "isempty";

        /// <summary>
        /// Empty: field has not to be empty (strings only). Null is different from empty
        /// </summary>
        public const string IsNotEmpty = "isnotempty";
    }


}