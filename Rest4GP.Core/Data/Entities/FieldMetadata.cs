namespace Rest4GP.Core.Data.Entities
{

    /// <summary>
    /// Metadata for an entity field
    /// </summary>
    public class FieldMetadata
    {
        
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the field, if any
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Size of the field
        /// </summary>
        /// <remarks>
        /// For string values rappresents the number of chars.
        /// For numeric values, rappresents the total lenght of the data (integer + decimal digits)
        /// </remarks>
        public int Size { get; set; }


        /// <summary>
        /// Scale of the field (valid only for numeric column and rappresents the decimal digits)
        /// </summary>
        public int Scale { get; set; }


        /// <summary>
        /// Data type
        /// </summary>
        public FieldDataTypes Type { get; set; }

        /// <summary>
        /// True if the field is required
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// True if the filed is the primary key (or part of it for multiple fields primary key)
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// True if the field is read only (for example for computed data)
        /// </summary>
        public bool IsReadOnly { get; set; }
    }
}