namespace Rest4GP.Core.Data.Entities
{

    /// <summary>
    /// Managed data types
    /// </summary>
    public enum FieldDataTypes
    {
        /// <summary>
        /// String data (char or varchar)
        /// </summary>
        String = 0,

        /// <summary>
        /// Date only
        /// </summary>
        Date = 10,

        /// <summary>
        /// Time only
        /// </summary>
        Time = 11,

        /// <summary>
        /// Date and time
        /// </summary>
        DateTime = 12,
        

        /// <summary>
        /// Numeric
        /// </summary>
        Numeric = 20,


        /// <summary>
        /// Byte array
        /// </summary>
        ByteArray = 50,
        
    }
    
}