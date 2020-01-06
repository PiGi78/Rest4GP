using System;
using System.Collections.Generic;
using System.Linq;
using Rest4GP.Core.Data.Entities;

namespace Rest4GP.Core.Parameters
{

    /// <summary>
    /// Smart filter
    /// </summary>
    public class RestSmartFilter
    {

        /// <summary>
        /// Creates a new Smart Filter
        /// </summary>
        /// <param name="value">Value of the filter</param>
        public RestSmartFilter(string value = null)
        {
            Value = value;
        }

        /// <summary>
        /// Value of the smart filter
        /// </summary>
        public string Value { get; set; }


        /// <summary>
        /// Compose a filter that rappresents the smart filter logic
        /// </summary>
        /// <param name="fields">Fields where to apply the filter</param>
        /// <returns>Filter logic or null if not applicable</returns>
        public RestFilter ComposeFilter(List<FieldMetadata> fields)
        {
            // No fields -> No filter
            if (fields == null || !fields.Any()) return null;

            // No Value -> No filter
            if (string.IsNullOrEmpty(Value)) return null;

            // Parse filter
            ParseSmartFilter(Value, out decimal? minNum, out decimal? maxNum, out DateTime? minDate, out DateTime? maxDate);

            return ComposeSmartFilter(fields, minNum, maxNum, minDate, maxDate);
        }



        /// <summary>
        /// Gets the list that rapresents the smart filter
        /// </summary>
        /// <param name="fields">Fields of the table</param>
        /// <param name="minNum">Min number value (if any)</param>
        /// <param name="maxNum">Max number value (if any)</param>
        /// <param name="minDate">Min date value (if any)</param>
        /// <param name="maxDate">Max date value (if any)</param>
        /// <returns>List of filters</returns>
        private RestFilter ComposeSmartFilter(List<FieldMetadata> fields, decimal? minNum, decimal? maxNum, DateTime? minDate, DateTime? maxDate)
        {
            var filters = new List<RestFilter>();
            foreach (var field in fields)
            {
                switch (field.Type)
                {
                    case FieldDataTypes.String:
                        filters.Add(new RestFilter  {
                            Field = field.Name,
                            IgnoreCase = true,
                            Operator = FilterOperators.Contains,
                            Value = Value
                        });
                        break;
                    case FieldDataTypes.Numeric:
                        if (minNum.HasValue)
                        {
                            if (maxNum.HasValue)
                            {
                                filters.Add(RestFilter.CreateBetweenFilter(field.Name, minNum, maxNum));
                            }
                            else
                            {
                                filters.Add(new RestFilter  {
                                    Field = field.Name,
                                    Operator = FilterOperators.IsEqual,
                                    Value = minNum
                                });
                            }
                        }
                        break;
                    case FieldDataTypes.Date:
                    case FieldDataTypes.DateTime:
                        if (minDate.HasValue)
                        {
                            if (maxDate.HasValue)
                            {
                                filters.Add(RestFilter.CreateBetweenFilter(field.Name, minDate, maxDate));
                            }
                            else
                            {
                                filters.Add(new RestFilter  {
                                    Field = field.Name,
                                    Operator = FilterOperators.IsEqual,
                                    Value = minDate
                                });
                            }
                        }
                        break;
                }
            }
            
            // No filters => return null
            if (!filters.Any()) return null;

            // Single filter
            var maxIndex = filters.Count;
            if (maxIndex == 1) {
                return filters[0];
            }

            // Complex filter
            var result = new RestFilter {
                Logic = FilterLogics.Or,
                Filters = filters
            };

            return result;
        }


        /// <summary>
        /// Parses a smart filter and back special values
        /// </summary>
        /// <remarks>
        /// It works in this way:
        /// - if smart filter cant' be converted in numbers, minNum and maxNum are both null
        /// - if smart filter is a single number, minNum is that value, maxNum is null
        /// - if smart filter has a single minus, with left and right side both convertible in
        ///      numbers, then minNum is the minimum value, maxNum is the maximum value
        /// 
        /// Date works in the same way
        /// </remarks>
        /// <param name="smartFilter">Smart filter to parse</param>
        /// <param name="minNum">Minimum numeric value (if any)</param>
        /// <param name="maxNum">Maximum numeric value (if any)</param>
        /// <param name="minDate">Minimum date value (if any)</param>
        /// <param name="maxDate">Maximum date value (if any)</param>
        private void ParseSmartFilter(string smartFilter, 
                                      out decimal? minNum, out decimal? maxNum,
                                      out DateTime? minDate, out DateTime? maxDate)
        {
            minNum = null;
            maxNum = null;
            minDate = null;
            maxDate = null;
            if (string.IsNullOrEmpty(smartFilter)) return;

            // Check for between clause
            var splittedString = smartFilter.Split(" - ".ToCharArray());
            if (splittedString.Length == 2)
            {
                var left = splittedString[0].Trim();
                var right = splittedString[1].Trim();

                // Between date (check also for the highest value)
                if (DateTime.TryParse(left, out DateTime leftDate) &&
                    DateTime.TryParse(right, out DateTime rightDate))
                {
                    if (minDate > maxDate)
                    {
                        maxDate = leftDate;
                        minDate = rightDate;
                    }
                    else
                    {
                        minDate = leftDate;
                        maxDate = rightDate;
                    }
                    return;
                }

                // Between numbers
                if (decimal.TryParse(left, out decimal leftNum) &&
                    decimal.TryParse(right, out decimal rightNum))
                {
                    minNum = Math.Min(leftNum, rightNum);
                    maxNum = Math.Max(leftNum, rightNum);
                    return;
                }
                
            }

            // Check if value can be converted in a date
            if (DateTime.TryParse(smartFilter, out DateTime minDateValue))
            {
                minDate = minDateValue;
                return;
            }

            // Check if value can be converted in a number
            if (decimal.TryParse(smartFilter, out decimal minNumValue))
            {
                minNum = minNumValue;
                return;
            }

        }

        
    }
}
