using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Rest4GP.Core;
using Rest4GP.Core.Data;
using Rest4GP.Core.Data.Entities;
using Vision4GP.Core.FileSystem;

namespace Rest4GP.Microfocus
{


    /// <summary>
    /// Extension methods
    /// </summary>
    public static class Extensions
    {

        /// <summary>
        /// Adds a Vision data handler
        /// </summary>
        /// <param name="root">Root of the request to handle</param>
        /// <param name="services">Services where to add the handler</param>
        /// <returns>Services with the handler</returns>
        public static IServiceCollection AddVision4GP(this IServiceCollection services, string root)
        {
            if (string.IsNullOrEmpty(root)) throw new ArgumentNullException(nameof(root));

            // Add handler
            services.AddTransient<IRestRequestHandler>(x => {
                var mem = x.GetRequiredService<IMemoryCache>();
                var dataOpt = x.GetRequiredService<DataRequestOptions>();
                return new VisionRestHandler(root, mem, dataOpt);
            });

            // Returns the service collection
            return services;
        }




        /// <summary>
        /// Converts a Vision file definition to EntityMetadata
        /// </summary>
        /// <param name="fileDefinition">File definition to convert</param>
        /// <returns>Requested metadata</returns>
        public static EntityMetadata ToMetadata(this VisionFileDefinition fileDefinition)
        {
            if (fileDefinition == null) return null;

            var result = new EntityMetadata
            {
                Name = fileDefinition.FileName,
                IsReadOnly = false,
                Fields = GetFieldsMetadata(fileDefinition)
            };
            return result;
        }


        /// <summary>
        /// Get vision record fields as list of field metadata
        /// </summary>
        /// <param name="fileDefinition">Vision file definition</param>
        /// <returns>List of metadata</returns>
        private static List<FieldMetadata> GetFieldsMetadata(VisionFileDefinition fileDefinition)
        {
            var result = new List<FieldMetadata>();
            foreach (var field in fileDefinition.Fields.Where(x => !x.IsGroupField))
            {
                var metadata = new FieldMetadata {
                    IsReadOnly = false,
                    IsPrimaryKey = fileDefinition.Keys[0].Fields.Where(x => x.Name == field.Name).Count() > 0,
                    Name = field.GetDotnetName(),
                    Scale = field.Scale,
                    Size = field.Size,
                    Type = ToFieldDataType(field.FieldType)
                };
                result.Add(metadata);
            }
            return result;
        }

        /// <summary>
        /// Converts a Vision type to a field data type
        /// </summary>
        /// <param name="visionType">Vision type</param>
        /// <returns>Field data type</returns>
        private static FieldDataTypes ToFieldDataType(VisionFieldType visionType)
        {
            switch (visionType)
            {
                case VisionFieldType.Comp:
                    return FieldDataTypes.ByteArray;
                case VisionFieldType.Date:
                    return FieldDataTypes.DateTime;
                case VisionFieldType.Number:
                    return FieldDataTypes.Numeric;
                case VisionFieldType.String:
                    return FieldDataTypes.String;
                case VisionFieldType.JustifiedString:
                    return FieldDataTypes.String;
                default: 
                    return FieldDataTypes.String;
            }
        }



        /// <summary>
        /// Get the .NET name for a vision field
        /// </summary>
        /// <param name="fieldDefinition">Definition of the field</param>
        /// <returns>.NET name of the field</returns>
        public static string GetDotnetName(this VisionFieldDefinition fieldDefinition)
        {
            if (fieldDefinition == null ||
                string.IsNullOrEmpty(fieldDefinition.Name)) return null;
            
            var nameChars = fieldDefinition.Name.ToCharArray();
            var result = new char[] {};
            int position = 0;
            bool upperCase = true;
            foreach (var c in nameChars)
            {
                if (char.IsLetterOrDigit(c))
                {
                    result[position] = upperCase ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c);
                    upperCase = false;
                    position++;
                } 
                else
                {
                    upperCase = true;
                }
            }


            return new string(result);
        }



        /// <summary>
        /// Converts an enumerable of validation result in an IOException
        /// </summary>
        /// <param name="validationResults">Enumerable to convert</param>
        /// <param name="fileName">Name of the file that generated the exception</param>
        /// <returns>IOException</returns>
        public static IOException ToIOException (this IList<ValidationResult> validationResults, string fileName)
        {
            if (validationResults == null || validationResults.Any()) throw new IOException();

            var builder = new StringBuilder();
            builder.AppendLine($"Error working on vision file {fileName}")
                   .AppendLine()
                   .AppendLine("Error details:");

            foreach (var validation in validationResults)
            {
                builder.AppendLine()
                       .AppendLine($"Properties: {string.Join(',', validation.MemberNames)}")
                       .AppendLine($"Error: {validation.ErrorMessage}");
            }

            return new IOException(builder.ToString());
        }


    }

}