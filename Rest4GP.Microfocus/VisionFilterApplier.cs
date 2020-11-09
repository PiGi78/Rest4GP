using Microsoft.CSharp.RuntimeBinder;
using Rest4GP.Core.Data.Entities;
using Rest4GP.Core.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Rest4GP.Microfocus
{

    /// <summary>
    /// Applier for Rest4GP filters to a Vision record (dictionary)
    /// </summary>
    public class VisionFilterApplier
    {


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="restParam">Rest parameters</param>
        /// <param name="metadata">Field metadata</param>
        public VisionFilterApplier(RestParameters restParam, List<FieldMetadata> metadata)
        {
            if (restParam == null) throw new ArgumentNullException(nameof(restParam));
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));

            Filter = restParam.Filter;
            Metadata = metadata;

            // If we have a smart filter, add it to common filter
            if (restParam.SmartFilter != null)
            {
                var restSmartFilter = restParam.SmartFilter.ComposeFilter(metadata);
                if (restSmartFilter != null)
                {
                    if (Filter == null)
                    {
                        Filter = restSmartFilter;
                    }
                    else
                    {
                        Filter = new RestFilter
                        {
                            Logic = FilterLogics.And,
                            Filters = new List<RestFilter>
                            {
                                restParam.Filter,
                                restSmartFilter
                            }
                        };
                    }
                }
            }


        }


        /// <summary>
        /// Filter to apply
        /// </summary>
        private RestFilter Filter { get; }


        /// <summary>
        /// Fileds metadata
        /// </summary>
        private List<FieldMetadata> Metadata { get; }


        /// <summary>
        /// Get a function that apply the filter to a given object
        /// </summary>
        public Func<dynamic, bool> GetFilterFunction()
        {
            if (Filter == null) return null;


            var objectType = typeof(object);
            var objectParam = Expression.Parameter(objectType, "x");

            var expression = ComposeExpressionFilter(Filter, objectType, objectParam);
            return Expression.Lambda<Func<dynamic, bool>>(expression, objectParam).Compile();
        }


        private Expression ComposeExpressionFilter(RestFilter filter, Type objectType, ParameterExpression objectParam)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (objectType == null) throw new ArgumentNullException(nameof(objectType));
            if (objectParam == null) throw new ArgumentNullException(nameof(objectParam));


            // Complex filter
            if (filter.Filters != null &&
                filter.Filters.Any())
            {
                var leftFilter = filter.Filters[0];
                var rightFilter = filter.Filters[1];
                var leftExpression = ComposeExpressionFilter(leftFilter, objectType, objectParam);
                var rightExpression = ComposeExpressionFilter(rightFilter, objectType, objectParam);
                if (FilterLogics.And.Equals(filter.Logic, StringComparison.InvariantCultureIgnoreCase))
                {
                    return Expression.And(leftExpression, rightExpression);
                }
                return Expression.Or(leftExpression, rightExpression);
            }


            // Simple filter
            var fieldName = filter.Field;
            var fieldValueAsExpressionParam = GetDynamciProperty(fieldName, objectParam);
            var filterValueAsExpressionValue = ConvertData(Expression.Constant(filter.Value), fieldName);
            var nullAsExpressionParam = Expression.Constant(null);
            var emptyStringAsExpressionParam = Expression.Constant(string.Empty);
            ConstantExpression stringComparison;
            UnaryExpression fieldValueAsStringExpression;
            switch (filter.Operator)
            {
                case FilterOperators.IsEqual:
                    return Expression.Equal(fieldValueAsExpressionParam, filterValueAsExpressionValue);

                case FilterOperators.IsNotEqual:
                    return Expression.NotEqual(fieldValueAsExpressionParam, filterValueAsExpressionValue);

                case FilterOperators.IsGreaterThan:
                    return Expression.GreaterThan(fieldValueAsExpressionParam, filterValueAsExpressionValue);

                case FilterOperators.IsGreatherThanOrEqual:
                    return Expression.GreaterThanOrEqual(fieldValueAsExpressionParam, filterValueAsExpressionValue);

                case FilterOperators.IsLessThan:
                    return Expression.LessThan(fieldValueAsExpressionParam, filterValueAsExpressionValue);

                case FilterOperators.IsLessThanOrEqual:
                    return Expression.LessThanOrEqual(fieldValueAsExpressionParam, filterValueAsExpressionValue);

                case FilterOperators.IsNull:
                    return Expression.Equal(fieldValueAsExpressionParam, filterValueAsExpressionValue);

                case FilterOperators.IsNotNull:
                    return Expression.NotEqual(fieldValueAsExpressionParam, nullAsExpressionParam);

                case FilterOperators.IsEmpty:
                    return Expression.Equal(fieldValueAsExpressionParam, emptyStringAsExpressionParam);

                case FilterOperators.IsNotEmpty:
                    return Expression.NotEqual(fieldValueAsExpressionParam, emptyStringAsExpressionParam);

                case FilterOperators.Contains:
                case FilterOperators.DoesNotContain:
                    var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string), typeof(StringComparison) });
                    stringComparison = GetStringComparisonExpression(filter);
                    fieldValueAsStringExpression = GetStringValue(fieldValueAsExpressionParam);
                    var contains = Expression.Call(fieldValueAsStringExpression, containsMethod, filterValueAsExpressionValue, stringComparison);
                    if (filter.Operator.Equals(FilterOperators.DoesNotContain, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return Expression.Not(contains);
                    }
                    return contains;

                case FilterOperators.EndsWith:
                    var endsWithMethod = typeof(string).GetMethod("EndsWith", new[] { typeof(string), typeof(StringComparison) });
                    stringComparison = GetStringComparisonExpression(filter);
                    fieldValueAsStringExpression = GetStringValue(fieldValueAsExpressionParam);
                    var endsWith = Expression.Call(fieldValueAsStringExpression, endsWithMethod, filterValueAsExpressionValue, stringComparison);
                    return endsWith;

                case FilterOperators.StartsWith:
                    var startsWithMethod = typeof(string).GetMethod("StartsWith", new[] { typeof(string), typeof(StringComparison) });
                    stringComparison = GetStringComparisonExpression(filter);
                    fieldValueAsStringExpression = GetStringValue(fieldValueAsExpressionParam);
                    var startsWith = Expression.Call(fieldValueAsStringExpression, startsWithMethod, filterValueAsExpressionValue, stringComparison);
                    return startsWith;

                default:
                    throw new ApplicationException($"Operation '{filter.Operator}' not managed in Vision filter");
            }
        }


        /// <summary>
        /// Get an expression that converts a dynamic expression value into string value
        /// </summary>
        /// <param name="fieldValueAsExpression">Value of the string</param>
        /// <returns>Requested expression</returns>
        private UnaryExpression GetStringValue(Expression fieldValueAsExpression)
        {
            return Expression.Convert(GetEmptyStringIfNull(fieldValueAsExpression), typeof(string));
        }


        /// <summary>
        /// Get an Expression that return a string value or an empty string if the value is null
        /// </summary>
        /// <param name="value">Value to check</param>
        /// <returns>Requested expression</returns>
        private Expression GetEmptyStringIfNull(Expression value)
        {
            return Expression.Condition(Expression.Equal(value, Expression.Constant(null)), Expression.Constant(string.Empty, value.Type), value);
        }


        /// <summary>
        /// Get the Expression for extract a property value from a dynamic object
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        /// <param name="objectParam">Parameter where to extract the value from</param>
        /// <returns>Requested Expression</returns>
        private Expression GetDynamciProperty(string propertyName, ParameterExpression objectParam)
        {
            if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));
            if (objectParam == null) throw new ArgumentNullException(nameof(objectParam));


            var binder = Binder.GetMember(CSharpBinderFlags.None,
                propertyName,
                typeof(object),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });

            var propertyValue = Expression.Dynamic(binder, typeof(object), objectParam);
            return ConvertData(propertyValue, propertyName);
        }


        /// <summary>
        /// Converts an expression data for a property
        /// </summary>
        /// <param name="propertyValue">Value to convert</param>
        /// <param name="propertyName">Property name</param>
        /// <returns>Converted value</returns>
        private Expression ConvertData(Expression propertyValue, string propertyName)
        {
            var field = Metadata.SingleOrDefault(x => x.Name.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase));

            if (field == null) return propertyValue;

            switch (field.Type)
            {
                case FieldDataTypes.String:
                    return Expression.Convert(propertyValue, typeof(string));
                case FieldDataTypes.Date:
                    return Expression.Convert(propertyValue, typeof(DateTime?));
                case FieldDataTypes.Time:
                    return Expression.Convert(propertyValue, typeof(DateTime?));
                case FieldDataTypes.DateTime:
                    return Expression.Convert(propertyValue, typeof(DateTime?));
                case FieldDataTypes.Numeric:
                    if (field.Scale > 0)  return Expression.Convert(propertyValue, typeof(decimal));
                    return Expression.Convert(propertyValue, typeof(Int64));
                case FieldDataTypes.ByteArray:
                    return Expression.Convert(propertyValue, typeof(byte[]));
                default:
                    return propertyValue;
            }
        }



        /// <summary>
        /// Expression per il valore di string comparison
        /// </summary>
        /// <param name="filter">Filtro per cui si vuole il valore</param>
        /// <returns></returns>
        private ConstantExpression GetStringComparisonExpression(RestFilter filter)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            if (filter.IgnoreCase) return Expression.Constant(StringComparison.InvariantCultureIgnoreCase);

            return Expression.Constant(StringComparison.InvariantCulture);
        }



    }
}
