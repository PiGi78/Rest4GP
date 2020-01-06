namespace Rest4GP.Core
{

    /// <summary>
    /// Rest methods
    /// </summary>
    public enum RestMethods : short
    {
        /// <summary>
        /// Not defined/managed
        /// </summary>
        Undefined,
        /// <summary>
        /// Get verb, used for read data
        /// </summary>
        Get,
        /// <summary>
        /// Post verb, used for insert a new entity
        /// </summary>
        Post,   
        /// <summary>
        /// Put verb, used for full update of an entity
        /// </summary>
        Put,
        /// <summary>
        /// Patch verb, used for partial update of an entity
        /// </summary>
        Patch,
        /// <summary>
        /// Delete verb, used for delete an entity
        /// </summary>
        Delete
    }
}