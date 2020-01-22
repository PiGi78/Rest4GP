using System;
using Rest4GP.Core.Parameters;
using Rest4GP.Core.Parameters.Converters;

namespace Rest4GP.Core.Data
{

    /// <summary>
    /// Options for data request
    /// </summary>
    public class DataRequestOptions
    {
        
        /// <summary>
        /// Delay for metadata info, default 10 mins
        /// </summary>
        public TimeSpan MetadataCacheDelay { get; set; } = TimeSpan.FromMinutes(10);


        /// <summary>
        /// Converters for parameters
        /// </summary>
        public IParametersConverter ParametersConverter { get; set; } = new DefaultParametersConverter();


        /// <summary>
        /// Rule for enum serialization
        /// </summary>
        public EnumSerializationRules EnumSerializationRule { get; set; } = EnumSerializationRules.String;
        
    }


    /// <summary>
    /// Rules for enum serialization
    /// </summary>
    public enum EnumSerializationRules
    {
        /// <summary>
        /// Serialize enum with the string value     
        /// </summary>
        String,
        /// <summary>
        /// Serialize enum with the numeric value
        /// </summary>
        Numeric
    }
}