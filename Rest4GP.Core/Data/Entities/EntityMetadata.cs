using System.Collections.Generic;

namespace Rest4GP.Core.Data.Entities
{

    /// <summary>
    /// Entity metadata
    /// </summary>
    public class EntityMetadata
    {

        /// <summary>
        /// Entity name
        /// </summary>
        public string Name { get; set; }
        

        /// <summary>
        /// Entity description
        /// </summary>
        public string Description { get; set; }


        /// <summary>
        /// True if the entity is for read only
        /// </summary>
        /// <remarks>
        /// Set to true for views on database
        /// </remarks>
        public bool IsReadOnly {get;set;}

        
        /// <summary>
        /// Fields that compose the entity
        /// </summary>
        public List<FieldMetadata> Fields { get; set; } = new List<FieldMetadata>();

    }

}