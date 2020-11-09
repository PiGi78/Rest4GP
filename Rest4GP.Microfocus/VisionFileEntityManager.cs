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
    internal class VisionFileEntityManager : IEntityManager
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
                var keyResult = FillPrimaryKey(record, fields);

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
        /// Fill the primary key of the file
        /// </summary>
        /// <param name="record">Record to fill</param>
        /// <param name="fields">Fiels from where take the values</param>
        private IList<ValidationResult> FillPrimaryKey(IVisionRecord record, IDictionary<string, object> fields)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            var result = new List<ValidationResult>();
            var key = FileDefinition.Keys.First(x => x.IsUnique);
            foreach (var field in key.Fields)
            {
                var name = field.GetDotnetName();
                if (fields.ContainsKey(name))
                {
                    var fieldValue = fields[name];
                    record.SetValue(field.Name, fieldValue);
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
            // Check for sort
            var hasSort = (parameters?.Sort?.Fields?.Any()).GetValueOrDefault(false);
            if (hasSort)
            {
                return Task.FromResult(FetchEntitiesWithSort(parameters));
            }

            // Read for primary key
            var result = new FetchEntitiesResponse();

            // Filter as function
            var filterFunction = GetFunctionFilter(parameters);

            // Take/Skip
            var take = (parameters?.Take).GetValueOrDefault();
            var skip = (parameters?.Skip).GetValueOrDefault();

            // Record count
            var recordCount = 0;

            // Read file
            using (var file = FileSystem.GetVisionFile(FileDefinition.FileName))
            {
                var records = new List<ExpandoObject>();

                file.Open(FileOpenMode.Input);

                if (file.Start())
                {
                    IVisionRecord record;
                    while (true)
                    {
                        record = file.ReadNext();
                        if (record == null) break;

                        // Load data
                        var element = GetExpandoObjectFromRecord(record);

                        // Apply filter
                        if (filterFunction == null || filterFunction(element))
                        {
                            // another record
                            recordCount++;

                            // Skip
                            if (skip > 0 && recordCount <= skip) continue;

                            // Take
                            if (recordCount > skip &&
                                take >= (recordCount - skip))
                            {
                                records.Add(element);
                            }

                            // Take
                            if (!parameters.WithCount && take > 0 && take == (recordCount - skip)) break;

                        }
                    }
                }
                file.Close();

                // Entities
                result.Entities = new List<object>(records);
                
                // Total record count
                if (parameters.WithCount)
                {
                    result.TotalCount = recordCount;
                }
            }

            return Task.FromResult(result);
        }


        /// <summary>
        /// Fetch entites when parameters has a sort
        /// </summary>
        /// <param name="parameters">Parameters of the REST function</param>
        /// <returns>Response</returns>
        private FetchEntitiesResponse FetchEntitiesWithSort(RestParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (parameters.Sort == null) throw new ApplicationException("FetchEntitiesWithSort called without a sort");
            
            var filterFunction = GetFunctionFilter(parameters);

            // Read all records
            var records = new List<ExpandoObject>();
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

                        var element = GetExpandoObjectFromRecord(record);

                        if (filterFunction == null || filterFunction(element))
                        {
                            records.Add(element);
                        }
                    }
                }

                file.Close();
            }

            // Apply sort
            var comparer = new ExpandoComparer(parameters.Sort);
            records.Sort(comparer);

            // Total count
            var totalCount = parameters.WithCount ? records.Count : 0;


            var elements = records.AsEnumerable();

            // Skip
            if (parameters.Skip > 0) elements = elements.Skip(parameters.Skip);

            // Take
            if (parameters.Take > 0) elements = elements.Take(parameters.Take);

            // Result
            return new FetchEntitiesResponse
            {
                TotalCount = totalCount,
                Entities = new List<object>(elements)
            };
        }


        /// <summary>
        /// Get an ExpandoObject from vision record
        /// </summary>
        /// <param name="record">Vision record to convert</param>
        /// /// <returns>ExpandoObject with properties from record</returns>
        private ExpandoObject GetExpandoObjectFromRecord(IVisionRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            
            var result = new ExpandoObject();
            var dictResult = (IDictionary<string, object>)result;
            foreach (var property in FileDefinition.Fields.Where(x => !x.IsGroupField))
            {
                dictResult[property.GetDotnetName()] = record.GetValue(property.Name);
            }
            return result;
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
                var keyResult = FillPrimaryKey(record, fields);
                if (keyResult.Count > 0) keyResult.ToIOException(FileDefinition.FileName);


                file.Open(FileOpenMode.InputOutput);


                // Check for no records with the same key
                record = file.Read(record);
                if (record != null)
                {
                    throw new IOException($"Duplicate key on file {FileDefinition.FileName}");
                }

                FillRecord(record, fields);

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
                var keyResult = FillPrimaryKey(record, fields);
                if (keyResult.Count > 0) keyResult.ToIOException(FileDefinition.FileName);


                file.Open(FileOpenMode.InputOutput);


                // Lock the record
                record = file.Read(record);
                if (record == null)
                {
                    throw new IOException($"Record not found on file {FileDefinition.FileName}");
                }

                FillRecord(record, fields);

                // Update
                file.Rewrite(record);

                file.Close();
            }

            return Task.FromResult<IList<ValidationResult>>(null);
        }


        /// <summary>
        /// Fill a vision record from a given dictionary
        /// </summary>
        /// <param name="record">Record to fill</param>
        /// <param name="fields">Dictionary with propery value</param>
        private void FillRecord(IVisionRecord record, IDictionary<string, object> fields)
        {
            // Load properties
            foreach (var propertyName in fields.Keys)
            {
                var propertyValue = fields[propertyName];
                // look for the property with that name
                var field = FileDefinition.Fields
                                          .SingleOrDefault(x => x.GetDotnetName().Equals(propertyName, StringComparison.InvariantCultureIgnoreCase));
                if (field != null)
                {
                    record.SetValue(field.Name, propertyValue);
                }
            }
        }


        /// <summary>
        /// Convert the filters in a function
        /// </summary>
        /// <param name="parameters">Filters to convert</param>
        /// <returns>Function of filters</returns>
        private Func<object, bool> GetFunctionFilter(RestParameters parameters)
        {
            var applier = new VisionFilterApplier(parameters, EntityMetadata.Fields);
            return applier.GetFilterFunction();
        }
    }

}