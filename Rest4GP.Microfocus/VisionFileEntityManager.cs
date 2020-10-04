using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Rest4GP.Core.Data;
using Rest4GP.Core.Data.Entities;
using Rest4GP.Core.Parameters;
using Vision4GP.Core.FileSystem;

namespace Rest4GP.Microfocus
{

    /// <summary>
    /// Vision file entity manager
    /// </summary>
    public class VisionFileEntityManager : IEntityManager
    {


        /// <summary>
        /// Vision file manager
        /// </summary>
        /// <param name="fileDefintion"></param>
        /// <param name="fileSystem"></param>
        public VisionFileEntityManager(VisionFileDefinition fileDefintion, IVisionFileSystem fileSystem)
        {
            FileDefinition = fileDefintion ?? throw new ArgumentNullException(nameof(fileDefintion));
            FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            EntityMetadata = FileDefinition.ToMetadata();
        }

        /// <summary>
        /// Definition of the Vision file to manage
        /// </summary>
        private VisionFileDefinition FileDefinition { get; }


        /// <summary>
        /// Vision file system to use
        /// </summary>
        private IVisionFileSystem FileSystem { get; }


        /// <summary>
        /// Metadata of the entity
        /// </summary>
        public EntityMetadata EntityMetadata { get; }


        /// <summary>
        /// Delete an entity from the file
        /// </summary>
        /// <param name="fields">List of the properties of the entity</param>
        /// <returns>List of validation errors or null if anything is ok</returns>
        public Task<IList<ValidationResult>> DeleteEntityAsync(IDictionary<string, object> fields)
        {
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            using (var file = FileSystem.GetVisionFile(FileDefinition.FileName))
            {
                var record = file.GetNewRecord();
                var keyResult = loadPrimaryKey(record, fields);

                if (keyResult.Count > 0) return Task.FromResult(keyResult);

                file.Open(FileOpenMode.InputOutput);
                record = file.ReadLock(record);
                if (record != null)
                {
                    file.Delete(record);
                }
                file.Close();
            }

            return Task.FromResult<IList<ValidationResult>>(null);
        }

        /// <summary>
        /// Loads the primary key of the file
        /// </summary>
        /// <param name="record">Record to fill</param>
        /// <param name="fields">Fiels from where take the values</param>
        private IList<ValidationResult> loadPrimaryKey(IVisionRecord record, IDictionary<string, object> fields)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            var result = new List<ValidationResult>();
            var key = FileDefinition.Keys.First(x => x.IsUnique);
            foreach (var field in key.Fields)
            {
                var name = field.Name;
                if (fields.ContainsKey(name))
                {
                    var fieldValue = fields[name];
                    record.SetValue(name, fieldValue);
                }
                else
                {
                    result.Add(new ValidationResult("Key field missing", new string[] { field.Name }));
                }
            }
            return result;
        }




        /// <summary>
        /// Fetch all entities that match the given parameters
        /// </summary>
        /// <param name="parameters">Parameters to filter data</param>
        /// <returns>Entities that match the given parameters</returns>
        public Task<FetchEntitiesResponse> FetchEntitiesAsync(RestParameters parameters)
        {
            var result = new FetchEntitiesResponse();

            var recordCount = 0;
            var take = (parameters?.Take).GetValueOrDefault();
            var skip = (parameters?.Skip).GetValueOrDefault();

            using (var file = FileSystem.GetVisionFile(FileDefinition.FileName))
            {
                file.Open(FileOpenMode.Input);

                if (file.Start())
                {
                    IVisionRecord record;
                    while (true)
                    {
                        record = file.ReadNext();
                        if (record == null) break;


                        // TODO: Add filter management

                        recordCount++;

                        // Skip
                        if (skip > 0 && recordCount <= skip) continue;
                        
                        // Add element
                        var element = new ExpandoObject();
                        var dictElement = (IDictionary<string, object>)element;
                        foreach (var property in FileDefinition.Fields.Where(x => x.IsGroupField = false))
                        {
                            dictElement[property.Name] = record.GetValue(property.Name);
                        }
                        result.Entities.Add(element);

                        // Take
                        if (take > 0 && take == (recordCount - skip)) break;
                    }
                }

                file.Close();
            }

            return Task.FromResult(result);
        }


        /// <summary>
        /// Insert a new entity in the vision file
        /// </summary>
        /// <param name="fields">List of the properties of the entity</param>
        /// <returns>Properties of the key of the inserted item</returns>
        public Task<IDictionary<string, object>> InsertEntityAsync(IDictionary<string, object> fields)
        {
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            using (var file = FileSystem.GetVisionFile(FileDefinition.FileName))
            {
                var record = file.GetNewRecord();

                // Load primary key
                var keyResult = loadPrimaryKey(record, fields);
                if (keyResult.Count > 0) keyResult.ToIOException(FileDefinition.FileName);


                file.Open(FileOpenMode.InputOutput);


                // Check for no records with the same key
                record = file.Read(record);
                if (record != null)
                {
                    throw new IOException($"Duplicate key on file {FileDefinition.FileName}");
                }

                // Load properties
                foreach (var propertyName in fields.Keys)
                {
                    var propertyValue = fields[propertyName];
                    record.SetValue(propertyName, propertyValue);
                }

                // Insert
                file.Write(record);

                file.Close();
            }

            return Task.FromResult(fields);
        }


        /// <summary>
        /// Update an entity in the vision file
        /// </summary>
        /// <param name="fields">List of the properties of the entity</param>
        /// <returns>List of validation errors or null if anything is ok</returns>
        public Task<IList<ValidationResult>> UpdateEntityAsync(IDictionary<string, object> fields)
        {
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            using (var file = FileSystem.GetVisionFile(FileDefinition.FileName))
            {
                var record = file.GetNewRecord();

                // Load primary key
                var keyResult = loadPrimaryKey(record, fields);
                if (keyResult.Count > 0) keyResult.ToIOException(FileDefinition.FileName);


                file.Open(FileOpenMode.InputOutput);


                // Lock the record
                record = file.Read(record);
                if (record == null)
                {
                    throw new IOException($"Record not found on file {FileDefinition.FileName}");
                }

                // Load properties
                foreach (var propertyName in fields.Keys)
                {
                    var propertyValue = fields[propertyName];
                    record.SetValue(propertyName, propertyValue);
                }

                // Update
                file.Rewrite(record);

                file.Close();
            }

            return Task.FromResult<IList<ValidationResult>>(null);
        }
    }

}