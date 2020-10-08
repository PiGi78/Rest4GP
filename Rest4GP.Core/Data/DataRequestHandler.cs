using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Rest4GP.Core.Data.Entities;
using Rest4GP.Core.Parameters;

namespace Rest4GP.Core.Data
{


    /// <summary>
    /// Rest request for data management
    /// </summary>
    public abstract class DataRequestHandler : IRestRequestHandler
    {


        #region constructors


        /// <summary>
        /// Creates a new instance of DataRequestHandler
        /// </summary>
        /// <param name="root">Root of the request to handle</param>
        /// <param name="dataContext">Data context</param>
        /// <param name="memoryCache">Cache</param>
        /// <param name="options">Options of the data request</param>
        public DataRequestHandler(string root, IDataContext dataContext, IMemoryCache memoryCache, DataRequestOptions options)
        {
            if (string.IsNullOrEmpty(root)) throw new ArgumentNullException(nameof(root));
            HandleRoot = root;
            DataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
            Cache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            Options = options ?? new DataRequestOptions();
        }


        #endregion


        /// <summary>
        /// Data context
        /// </summary>
        protected IDataContext DataContext { get; }


        /// <summary>
        /// Cache for the handler
        /// </summary>
        protected IMemoryCache Cache { get; }


        /// <summary>
        /// Options
        /// </summary>
        protected DataRequestOptions Options { get; }


        /// <summary>
        /// Gets the options for the Json serialization
        /// </summary>
        /// <returns>
        /// Options for the Json serialization
        /// </returns>
        protected JsonSerializerOptions GetJsonSerializerOptions() 
        {
            var result = new JsonSerializerOptions();
            if (Options.PropertyNameSerializationRule == PropertyNameSerializationRules.CamelCase)
            {
                result.DictionaryKeyPolicy =  JsonNamingPolicy.CamelCase;
                result.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            };

            // Check for enums
            if (Options.EnumSerializationRule == EnumSerializationRules.String)
            {
                if (Options.PropertyNameSerializationRule == PropertyNameSerializationRules.CamelCase)
                {
                    result.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                }
                else
                {
                    result.Converters.Add(new JsonStringEnumConverter());
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if the handler can manage the request
        /// </summary>
        /// <param name="request">Request to check</param>
        /// <returns>True if the request can be handled, elsewhere false</returns>
        public virtual async Task<bool> CanHandleAsync(RestRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // Check for the root
            if (!request.Root.Equals(HandleRoot, StringComparison.InvariantCultureIgnoreCase)) return false;

            // If metada, then true
            if (request.IsMetadata()) return true;

            // Load entity manager
            var entityManager = await GetEntityManagerForRequestAsync(request);

            // We can handle the request only if we have an entity manager
            return entityManager != null;
        }


        /// <summary>
        /// HTTP of the root to handle
        /// </summary>
        public string HandleRoot { get; set; }


        /// <summary>
        /// Handles a request and returns the response
        /// </summary>
        /// <param name="request">Request to handle</param>
        /// <returns>Response to the request</returns>
        public virtual async Task<RestResponse> HandleRequestAsync(RestRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // Load manager
            var manager = await GetEntityManagerForRequestAsync(request);
            if (manager == null) 
            {
                // If no manager, could be the metadata of all managers
                if (request.IsMetadata())
                {
                    var metatada = (await GetCachedMetadataAsync()).Select(y => new {
                        Name = y.EntityMetadata.Name,
                        Description = y.EntityMetadata.Description,
                        IsReadOnly = y.EntityMetadata.IsReadOnly
                    });
                    return new RestResponse {
                        StatusCode = (int)HttpStatusCode.OK,
                        Content = JsonSerializer.Serialize(metatada, GetJsonSerializerOptions())
                    };
                }
                return null;
            }

            // If metadata request, returns it
            if (request.IsMetadata())
            {
                return new RestResponse {
                    StatusCode = (int)HttpStatusCode.OK,
                    Content = JsonSerializer.Serialize(manager.EntityMetadata, GetJsonSerializerOptions())
                };
            }

            // Manage the request
            RestResponse result = null;
            switch (request.Method)
            {
                // Get -> Read
                case RestMethods.Get:
                    var restParams = Options.ParametersConverter.ToRestParameters(request.OriginalRequest.QueryString.Value);
                    if (restParams == null) restParams = new RestParameters();
                    if (restParams != null)
                    {
                        var data = await manager.FetchEntitiesAsync(restParams);
                        if (data != null)
                        {
                            result = new RestResponse {
                                StatusCode = (int)HttpStatusCode.OK,
                                Content = JsonSerializer.Serialize(data, GetJsonSerializerOptions())
                            };
                        }
                    }
                    break;
                // Post -> Insert
                case RestMethods.Post:
                    var insProperties = await ExtractPropertiesAsync(request, manager.EntityMetadata);
                    if (insProperties != null &&
                        insProperties.Keys.Count > 0)
                    {
                        var insResult = await manager.InsertEntityAsync(insProperties);
                        if (insResult != null)
                        {
                            result = new RestResponse {
                                StatusCode = (int)HttpStatusCode.OK,
                                Content = JsonSerializer.Serialize(insResult, GetJsonSerializerOptions())
                            };
                        }
                    }
                    break;
                // Put/Patch -> Update
                case RestMethods.Patch:
                case RestMethods.Put:
                    var updProperties = await ExtractPropertiesAsync(request, manager.EntityMetadata);
                    if (updProperties != null &&
                        updProperties.Keys.Count > 0)
                    {
                        var updResult = await manager.UpdateEntityAsync(updProperties);
                        if (updResult != null)
                        {
                            if (updResult.Count == 0)
                            {
                                result = new RestResponse {
                                    StatusCode = (int)HttpStatusCode.OK
                                };
                            }
                            else
                            {
                                result = new RestResponse {
                                    StatusCode = (int)HttpStatusCode.BadRequest,
                                    Content = JsonSerializer.Serialize(updResult.ToList(), GetJsonSerializerOptions())
                                };
                            }
                        }
                    }
                    break;
                // Delete -> Delete
                case RestMethods.Delete:
                    var delProperties = await ExtractPropertiesAsync(request, manager.EntityMetadata);
                    if (delProperties != null &&
                        delProperties.Keys.Count > 0)
                    {
                        var delResult = await manager.DeleteEntityAsync(delProperties);
                        if (delResult != null)
                        {
                            if (delResult.Count == 0)
                            {
                                result = new RestResponse {
                                    StatusCode = (int)HttpStatusCode.OK
                                };
                            }
                            else
                            {
                                result = new RestResponse {
                                    StatusCode = (int)HttpStatusCode.BadRequest,
                                    Content = JsonSerializer.Serialize(delResult.ToList(), GetJsonSerializerOptions())
                                };
                            }
                        }
                    }
                    break;
                default:
                    break;
            }

            return result;
        }


        /// <summary>
        /// Extract the properties of the entity from the request
        /// </summary>
        /// <param name="request">Request from which extract properties</param>
        /// <param name="metadata">Entity metadata</param>
        /// <returns>Properties keys and values</returns>
        private async Task<IDictionary<string, object>> ExtractPropertiesAsync(RestRequest request, EntityMetadata metadata)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));


            // Convert content to dictionary
            var content = await request.GetContentAsync();
            var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            
            var result = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var property in metadata.Fields)
            {
                if (root.TryGetProperty(property.Name, out JsonElement element))
                {
                    switch (property.Type)
                    {
                        case FieldDataTypes.String:
                            var strValue = element.GetString();
                            result.Add(property.Name, strValue);
                            break;
                        case FieldDataTypes.Date:
                        case FieldDataTypes.DateTime:
                        case FieldDataTypes.Time:
                            if (element.TryGetDateTime(out DateTime dateValue))
                            {
                                result.Add(property.Name, dateValue);
                            }
                            break;
                        case FieldDataTypes.Numeric:
                            if (element.TryGetDecimal(out decimal decValue))
                            {
                                result.Add(property.Name, decValue);
                            }
                            break;
                        default:
                            continue;
                    }
                }
                // for PUT verb, all missing value are set to null (replacing all values)
                // for PATCH verb, missing value will mantain the current DB value
                else
                {
                    if (request.Method == RestMethods.Put)
                    {
                        result.Add(property.Name, null);
                    }
                }
            }

            return result;
        }


        /// <summary>
        /// Gets the entity manager for the given request
        /// </summary>
        /// <remarks>
        /// Clients can ask for entity UserAccount and we will look for entity called:
        /// - ) UserAccount
        /// - ) USER_ACCOUNT
        /// </remarks>
        /// <param name="request">Request</param>
        /// <returns>Entity manager for the given request, null if not found</returns>
        protected virtual async Task<IEntityManager> GetEntityManagerForRequestAsync(RestRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // Entity name
            var entityName = request.EntityName;
            if (string.IsNullOrEmpty(entityName)) return null;

            // Managers
            var managers = await GetCachedMetadataAsync();

            // Check for exact match
            var result = managers.Where(x => x.EntityMetadata.Name.Equals(entityName, StringComparison.InvariantCultureIgnoreCase))
                                 .SingleOrDefault();
            if (result != null) return result;

            // Check for the same name without underscores (es: client ask fro UserAccount and the entity is USER_ACCOUNT)
            result = managers
                    .Where(x => GetNameWithoutSpecialChars(x.EntityMetadata.Name).Equals(entityName, StringComparison.InvariantCultureIgnoreCase))
                    .SingleOrDefault();

            // result
            return result;
        }


        /// <summary>
        /// Removes special chars from name
        /// </summary>
        /// <param name="name">Name from where remove special chars</param>
        /// <returns>Name without special chars</returns>
        private string GetNameWithoutSpecialChars(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return name.Replace("-", string.Empty).Replace("_", string.Empty);
        }


        /// <summary>
        /// Load metadata using the cache
        /// </summary>
        /// <returns>List of all managed entities</returns>
        protected virtual async Task<List<IEntityManager>> GetCachedMetadataAsync()
        {
            var cacheKey = $"RestDataRequest_Metadata_{this.HandleRoot}";
            return await Cache.GetOrCreateAsync<List<IEntityManager>>(
                    cacheKey, 
                    entry => {
                        entry.SlidingExpiration = Options.MetadataCacheDelay;
                        return DataContext.FetchEntityManagersAsync();
                    });
        }


        /// <summary>
        /// Handles the http get method
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="manager">Entity manager</param>
        /// <returns>Response</returns>
        protected virtual async Task<RestResponse> HandleGetMethod(RestRequest request, IEntityManager manager)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (manager == null) throw new ArgumentNullException(nameof(manager));

            // Load the response from the manager
            var restParams = Options.ParametersConverter.ToRestParameters(request.OriginalRequest.QueryString.Value);
            var result = await manager.FetchEntitiesAsync(restParams);

            // Empty data
            if (result == null) 
            {
                return new RestResponse {
                            StatusCode = (int)HttpStatusCode.OK,
                            Content = "{ \"data\": [], \"count\": 0 }"
                        };
            }
            
            // JSON with data
            return new RestResponse {
                        StatusCode = (int)HttpStatusCode.OK,
                        Content = $"{{ \"data\": {JsonSerializer.Serialize(result.Entities, GetJsonSerializerOptions())}, \"count\": {result.TotalCount} }}"
                    };

        }




    }
}