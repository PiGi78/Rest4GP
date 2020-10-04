using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
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
        /// Converts a Vision file definition to EntityMetadata
        /// </summary>
        /// <param name="fileDefinition">File definition to convert</param>
        /// <returns>Requested metadata</returns>
        public static EntityMetadata ToMetadata(this VisionFileDefinition fileDefinition)
        {
            if (fileDefinition == null) return null;

            var result = new EntityMetadata
            {
                Name = fileDefinition.SelectName,
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
            foreach (var field in fileDefinition.Fields.Where(x => x.IsGroupField = false))
            {
                var metadata = new FieldMetadata {
                    IsReadOnly = false,
                    IsPrimaryKey = fileDefinition.Keys[0].Fields.Where(x => x.Name == field.Name).Count() > 0,
                    Name = field.Name,
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