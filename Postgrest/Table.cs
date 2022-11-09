using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Extensions;
using Postgrest.Interfaces;
using Postgrest.Models;
using Postgrest.Responses;
using static Postgrest.Constants;

namespace Postgrest
{
    /// <summary>
    /// Class created from a model derived from `BaseModel` that can generate query requests to a Postgrest Endpoint.
    /// 
    /// Representative of a `USE $TABLE` command.
    /// </summary>
    /// <typeparam name="T">Model derived from `BaseModel`.</typeparam>
    public class Table<T> : IPostgrestTable<T> where T : BaseModel, new()
    {
        public string BaseUrl { get; }

        /// <summary>
        /// Name of the Table parsed by the Model.
        /// </summary>
        public string TableName { get; }

        private ClientOptions options;
        private JsonSerializerSettings serializerSettings;

        private HttpMethod method = HttpMethod.Get;

        private string? columnQuery;

        private List<QueryFilter> filters = new List<QueryFilter>();
        private List<QueryOrderer> orderers = new List<QueryOrderer>();
        private List<string> columns = new List<string>();

        private List<ReferenceAttribute> references = new List<ReferenceAttribute>();

        private int rangeFrom = int.MinValue;
        private int rangeTo = int.MinValue;

        private int limit = int.MinValue;
        private string? limitForeignKey;

        private int offset = int.MinValue;
        private string? offsetForeignKey;

        private string? onConflict;

        /// <summary>
        /// Typically called from the Client `new Client.Table<ModelType>`
        /// </summary>
        /// <param name="baseUrl">Api Endpoint (ex: "http://localhost:8000"), no trailing slash required.</param>
        /// <param name="options">Optional client configuration.</param>
        public Table(string baseUrl, ClientOptions? options = null)
        {
            BaseUrl = baseUrl;

            options ??= new ClientOptions();

            this.options = options;

            serializerSettings = StatelessClient.SerializerSettings(options);

            foreach (var property in typeof(T).GetProperties())
            {
                var attrs = property.GetCustomAttributes(typeof(ReferenceAttribute), true);

                if (attrs.Length > 0)
                    references.Add((ReferenceAttribute)attrs.First());
            }

            TableName = FindTableName();
        }

        /// <summary>
        /// Constructor that specifies the serializer settings
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <param name="options"></param>
        /// <param name="serializerSettings"></param>
        public Table(string baseUrl, ClientOptions options, JsonSerializerSettings serializerSettings) : this(baseUrl, options)
        {
            this.serializerSettings = serializerSettings;
        }

        /// <summary>
        /// Add a Filter to a query request
        /// </summary>
        /// <param name="columnName">Column Name in Table.</param>
        /// <param name="op">Operation to perform.</param>
        /// <param name="criterion">Value to filter with, must be a `string`, `List<object>`, `Dictionary<string, object>`, `FullTextSearchConfig`, or `Range`</param>
        /// <returns></returns>
        public Table<T> Filter(string columnName, Operator op, object criterion)
        {
            if (criterion == null)
            {
                switch (op)
                {
                    case Operator.Equals:
                    case Operator.Is:
                        filters.Add(new QueryFilter(columnName, Operator.Is, QueryFilter.NullVal));
                        break;
                    case Operator.Not:
                    case Operator.NotEqual:
                        filters.Add(new QueryFilter(columnName, Operator.Not, new QueryFilter(columnName, Operator.Is, QueryFilter.NullVal)));
                        break;
                    default:
                        throw new Exception("NOT filters must use the `Equals`, `Is`, `Not` or `NotEqual` operators");
                }
                return this;
            }
            else if (criterion is string stringCriterion)
            {
                filters.Add(new QueryFilter(columnName, op, stringCriterion));
                return this;
            }
            else if (criterion is int intCriterion)
            {
                filters.Add(new QueryFilter(columnName, op, intCriterion));
                return this;
            }
            else if (criterion is float floatCriterion)
            {
                filters.Add(new QueryFilter(columnName, op, floatCriterion));
                return this;
            }
            else if (criterion is List<object> listCriteria)
            {
                filters.Add(new QueryFilter(columnName, op, listCriteria));
                return this;
            }
            else if (criterion is Dictionary<string, object> dictCriteria)
            {
                filters.Add(new QueryFilter(columnName, op, dictCriteria));
                return this;
            }
            else if (criterion is IntRange rangeCriteria)
            {
                filters.Add(new QueryFilter(columnName, op, rangeCriteria));
                return this;
            }
            else if (criterion is FullTextSearchConfig fullTextSearchCriteria)
            {
                filters.Add(new QueryFilter(columnName, op, fullTextSearchCriteria));
                return this;
            }

            throw new Exception("Unknown criterion type, is it of type `string`, `int`, `float`, `List`, `Dictionary<string, object>`, `FullTextSearchConfig`, or `Range`?");
        }

        /// <summary>
        /// Adds a NOT filter to the current query args.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public Table<T> Not(QueryFilter filter)
        {
            filters.Add(new QueryFilter(Operator.Not, filter));
            return this;
        }

        /// <summary>
        /// Adds a NOT filter to the current query args.
        ///
        /// Allows queries like:
        /// <code>
        /// await client.Table<User>().Not("status", Operators.Equal, "OFFLINE").Get();
        /// </code>
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="op"></param>
        /// <param name="criterion"></param>
        /// <returns></returns>
        public Table<T> Not(string columnName, Operator op, string criterion) => Not(new QueryFilter(columnName, op, criterion));

        /// <summary>
        /// Adds a NOT filter to the current query args.
        /// Allows queries like:
        /// <code>
        /// await client.Table<User>().Not("status", Operators.In, new List<string> {"AWAY", "OFFLINE"}).Get();
        /// </code>
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="op"></param>
        /// <param name="criteria"></param>
        /// <returns></returns>
        public Table<T> Not(string columnName, Operator op, List<object> criteria) => Not(new QueryFilter(columnName, op, criteria));

        /// <summary>
        /// Adds a NOT filter to the current query args.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="op"></param>
        /// <param name="criteria"></param>
        /// <returns></returns>
        public Table<T> Not(string columnName, Operator op, Dictionary<string, object> criteria) => Not(new QueryFilter(columnName, op, criteria));

        /// <summary>
        /// Adds an AND Filter to the current query args.
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        public Table<T> And(List<QueryFilter> filters)
        {
            this.filters.Add(new QueryFilter(Operator.And, filters));
            return this;
        }

        /// <summary>
        /// Adds a NOT Filter to the current query args.
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        public Table<T> Or(List<QueryFilter> filters)
        {
            this.filters.Add(new QueryFilter(Operator.Or, filters));
            return this;
        }

        /// <summary>
        /// Fills in query parameters based on a given model's primary key(s).
        /// </summary>
        /// <param name="model">A model with a primary key column</param>
        /// <returns></returns>
        public Table<T> Match(T model)
        {
            foreach (var kvp in model.PrimaryKey)
            {
                filters.Add(new QueryFilter(kvp.Key.ColumnName, Operator.Equals, kvp.Value));
            }

            return this;
        }

        /// <summary>
        /// Finds all rows whose columns match the specified `query` object.
        /// </summary>
        /// <param name="query">The object to filter with, with column names as keys mapped to their filter values.</param>
        /// <returns></returns>
        public Table<T> Match(Dictionary<string, string> query)
        {
            foreach (var param in query)
            {
                filters.Add(new QueryFilter(param.Key, Operator.Equals, param.Value));
            }

            return this;
        }

        /// <summary>
        /// Adds an ordering to the current query args.
        /// </summary>
        /// <param name="column">Column Name</param>
        /// <param name="ordering"></param>
        /// <param name="nullPosition"></param>
        /// <returns></returns>
        public Table<T> Order(string column, Ordering ordering, NullPosition nullPosition = NullPosition.First)
        {
            orderers.Add(new QueryOrderer(null, column, ordering, nullPosition));
            return this;
        }

        /// <summary>
        /// Adds an ordering to the current query args.
        /// </summary>
        /// <param name="foreignTable"></param>
        /// <param name="column"></param>
        /// <param name="ordering"></param>
        /// <param name="nullPosition"></param>
        /// <returns></returns>
        public Table<T> Order(string foreignTable, string column, Ordering ordering, NullPosition nullPosition = NullPosition.First)
        {
            orderers.Add(new QueryOrderer(foreignTable, column, ordering, nullPosition));
            return this;
        }

        /// <summary>
        /// Sets a FROM range, similar to a `StartAt` query.
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        public Table<T> Range(int from)
        {
            rangeFrom = from;
            return this;
        }

        /// <summary>
        /// Sets a bounded range to the current query.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public Table<T> Range(int from, int to)
        {
            rangeFrom = from;
            rangeTo = to;
            return this;
        }

        /// <summary>
        /// Select columns for query. 
        /// </summary>
        /// <param name="columnQuery"></param>
        /// <returns></returns>
        public Table<T> Select(string columnQuery)
        {
            method = HttpMethod.Get;
            this.columnQuery = columnQuery;
            return this;
        }


        /// <summary>
        /// Sets a limit with an optional foreign table reference. 
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="foreignTableName"></param>
        /// <returns></returns>
        public Table<T> Limit(int limit, string? foreignTableName = null)
        {
            this.limit = limit;
            this.limitForeignKey = foreignTableName;
            return this;
        }

        /// <summary>
        /// By specifying the onConflict query parameter, you can make UPSERT work on a column(s) that has a UNIQUE constraint.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public Table<T> OnConflict(string columnName)
        {
            onConflict = columnName;
            return this;
        }

        /// <summary>
        /// By using the columns query parameter it’s possible to specify the payload keys that will be inserted and ignore the rest of the payload.
        /// 
        /// The rest of the JSON keys will be ignored.
        /// Using this also has the side-effect of being more efficient for Bulk Insert since PostgREST will not process the JSON and it’ll send it directly to PostgreSQL.
        /// 
        /// See: https://postgrest.org/en/stable/api.html#specifying-columns
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public Table<T> Columns(string[] columns)
        {
            foreach (var column in columns)
                this.columns.Add(column);

            return this;
        }

        /// <summary>
        /// Sets an offset with an optional foreign table reference.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="foreignTableName"></param>
        /// <returns></returns>
        public Table<T> Offset(int offset, string? foreignTableName = null)
        {
            this.offset = offset;
            this.offsetForeignKey = foreignTableName;
            return this;
        }

        /// <summary>
        /// Executes an INSERT query using the defined query params on the current instance.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>A typed model response from the database.</returns>
        public Task<ModeledResponse<T>> Insert(T model, QueryOptions? options = null, CancellationToken cancellationToken = default) => PerformInsert(model, options, cancellationToken);

        /// <summary>
        /// Executes a BULK INSERT query using the defined query params on the current instance.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>A typed model response from the database.</returns>
        public Task<ModeledResponse<T>> Insert(ICollection<T> models, QueryOptions? options = null, CancellationToken cancellationToken = default) => PerformInsert(models, options, cancellationToken);

        /// <summary>
        /// Executes an UPSERT query using the defined query params on the current instance.
        /// 
        /// By default the new record is returned. Set QueryOptions.ReturnType to Minimal if you don't need this value.
        /// By specifying the QueryOptions.OnConflict parameter, you can make UPSERT work on a column(s) that has a UNIQUE constraint.
        /// QueryOptions.DuplicateResolution.IgnoreDuplicates Specifies if duplicate rows should be ignored and not inserted.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<ModeledResponse<T>> Upsert(T model, QueryOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (options == null)
            {
                options = new QueryOptions();
            }

            // Enforce Upsert
            options.Upsert = true;

            return PerformInsert(model, options, cancellationToken);
        }

        /// <summary>
        /// Executes an UPSERT query using the defined query params on the current instance.
        ///
        /// By default the new record is returned. Set QueryOptions.ReturnType to Minimal if you don't need this value.
        /// By specifying the QueryOptions.OnConflict parameter, you can make UPSERT work on a column(s) that has a UNIQUE constraint.
        /// QueryOptions.DuplicateResolution.IgnoreDuplicates Specifies if duplicate rows should be ignored and not inserted.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<ModeledResponse<T>> Upsert(ICollection<T> model, QueryOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (options == null)
            {
                options = new QueryOptions();
            }

            // Enforce Upsert
            options.Upsert = true;

            return PerformInsert(model, options, cancellationToken);
        }

        /// <summary>
        /// Executes an UPDATE query using the defined query params on the current instance.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>A typed response from the database.</returns>
        public Task<ModeledResponse<T>> Update(T model, QueryOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (options == null)
            {
                options = new QueryOptions();
            }

            method = new HttpMethod("PATCH");

            Match(model);

            var request = Send<T>(method, model, options.ToHeaders(), cancellationToken, isUpdate: true);

            Clear();

            return request;
        }

        /// <summary>
        /// Executes a delete request using the defined query params on the current instance.
        /// </summary>
        /// <returns></returns>
        public Task Delete(QueryOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (options == null)
            {
                options = new QueryOptions();
            }

            method = HttpMethod.Delete;

            var request = Send(method, null, options.ToHeaders(), cancellationToken);

            Clear();

            return request;
        }

        /// <summary>
        /// Executes a delete request using the model's primary key as the filter for the request.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<ModeledResponse<T>> Delete(T model, QueryOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (options == null)
            {
                options = new QueryOptions();
            }

            method = HttpMethod.Delete;

            Match(model);

            var request = Send<T>(method, null, options.ToHeaders(), cancellationToken);
            Clear();
            return request;
        }

        /// <summary>
        /// Returns ONLY a count from the specified query.
        ///
        /// See: https://postgrest.org/en/v7.0.0/api.html?highlight=count
        /// </summary>
        /// <param name="type"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<int> Count(CountType type, CancellationToken cancellationToken = default)
        {
            var tsc = new TaskCompletionSource<int>();

            Task.Run(async () =>
            {
                method = HttpMethod.Head;

                var attr = type.GetAttribute<MapToAttribute>();

                var headers = new Dictionary<string, string> {
                    { "Prefer", $"count={attr?.Mapping}" }
                };

                var request = Send(method, null, headers, cancellationToken);
                Clear();

                try
                {
                    var response = await request;
                    var countStr = response.ResponseMessage?.Content.Headers.GetValues("Content-Range").FirstOrDefault();
                    if (!string.IsNullOrEmpty(countStr) && countStr!.Contains("/"))
                    {
                        // Returns X-Y/COUNT [0-3/4]
                        tsc.SetResult(int.Parse(countStr.Split('/')[1]));
                    }
                    tsc.SetException(new Exception("Failed to parse response."));
                }
                catch (Exception ex)
                {
                    tsc.SetException(ex);
                }
            });

            return tsc.Task;
        }

        /// <summary>
        /// Executes a query that expects to have a single object returned, rather than returning list of models
        /// it will return a single model.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<T?> Single(CancellationToken cancellationToken = default)
        {
            var tsc = new TaskCompletionSource<T?>();

            Task.Run(async () =>
            {
                method = HttpMethod.Get;
                var headers = new Dictionary<string, string>
                {
                    { "Accept", "application/vnd.pgrst.object+json" },
                    { "Prefer", "return=representation"}
                };

                var request = Send<T>(method, null, headers, cancellationToken);

                Clear();

                try
                {
                    var result = await request;
                    tsc.SetResult(result.Models.FirstOrDefault());
                }
                catch (RequestException e)
                {
                    // No rows returned
                    if (e.Response.StatusCode == System.Net.HttpStatusCode.NotAcceptable)
                        tsc.SetResult(null);
                    else
                        tsc.SetException(e);
                }
                catch (Exception e)
                {
                    tsc.SetException(e);
                }
            });

            return tsc.Task;
        }

        /// <summary>
        /// Executes the query using the defined filters on the current instance.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<ModeledResponse<T>> Get(CancellationToken cancellationToken = default)
        {
            var request = Send<T>(method, null, null, cancellationToken);
            Clear();
            return request;
        }

        /// <summary>
        /// Generates the encoded URL with defined query parameters that will be sent to the Postgrest API.
        /// </summary>
        /// <returns></returns>
        public string GenerateUrl()
        {
            var builder = new UriBuilder($"{BaseUrl}/{TableName}");
            var query = HttpUtility.ParseQueryString(builder.Query);

            foreach (var param in options.QueryParams)
            {
                query.Add(param.Key, param.Value);
            }

            if (options.Headers.ContainsKey("apikey"))
            {
                query.Add("apikey", options.Headers["apikey"]);
            }

            if (columns.Count > 0)
            {
                query["columns"] = string.Join(",", columns);
            }

            foreach (var filter in filters)
            {
                var parsedFilter = PrepareFilter(filter);
                query.Add(parsedFilter.Key, parsedFilter.Value);
            }

            foreach (var orderer in orderers)
            {
                var nullPosAttr = orderer.NullPosition.GetAttribute<MapToAttribute>();
                var orderingAttr = orderer.Ordering.GetAttribute<MapToAttribute>();
                if (nullPosAttr is MapToAttribute nullPosAsAttribute && orderingAttr is MapToAttribute orderingAsAttribute)
                {
                    var key = !string.IsNullOrEmpty(orderer.ForeignTable) ? $"{orderer.ForeignTable}.order" : "order";
                    query.Add(key, $"{orderer.Column}.{orderingAsAttribute.Mapping}.{nullPosAsAttribute.Mapping}");
                }
            }

            if (!string.IsNullOrEmpty(columnQuery))
            {
                query["select"] = Regex.Replace(columnQuery, @"\s", "");
            }

            if (references.Count > 0)
            {
                if (query["select"] == null)
                    query["select"] = "*";

                foreach (var reference in references)
                {
                    if (reference.IncludeInQuery)
                    {
                        var columns = string.Join(",", reference.Columns.ToArray());

                        if (reference.ShouldFilterTopLevel)
                        {
                            query["select"] = query["select"] + $",{reference.TableName}!inner({columns})";
                        }
                        else
                        {
                            query["select"] = query["select"] + $",{reference.TableName}({columns})";
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(onConflict))
            {
                query["on_conflict"] = onConflict;
            }

            if (limit != int.MinValue)
            {
                var key = limitForeignKey != null ? $"{limitForeignKey}.limit" : "limit";
                query[key] = limit.ToString();
            }

            if (offset != int.MinValue)
            {
                var key = offsetForeignKey != null ? $"{offsetForeignKey}.offset" : "offset";
                query[key] = offset.ToString();
            }

            builder.Query = query.ToString();
            return builder.Uri.ToString();
        }

        /// <summary>
        /// Transforms an object into a string mapped list/dictionary using `JsonSerializerSettings`.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal object? PrepareRequestData(object? data, bool isInsert = false, bool isUpdate = false)
        {
            if (data == null) return new Dictionary<string, string>();

            PostgrestContractResolver? resolver = (PostgrestContractResolver)serializerSettings.ContractResolver!; // Specified in constructor;

            resolver.SetState(isInsert, isUpdate);

            var serialized = JsonConvert.SerializeObject(data, serializerSettings);

            resolver.SetState();

            // Check if data is a Collection for the Insert Bulk case
            if (data is ICollection<T>)
            {
                return JsonConvert.DeserializeObject<List<object>>(serialized);
            }
            else
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(serialized, serializerSettings);
            }
        }

        /// <summary>
        /// Transforms the defined filters into the expected Postgrest format.
        ///
        /// See: http://postgrest.org/en/v7.0.0/api.html#operators
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        internal KeyValuePair<string, string> PrepareFilter(QueryFilter filter)
        {
            var attr = filter.Op.GetAttribute<MapToAttribute>();
            if (attr is MapToAttribute asAttribute)
            {
                var strBuilder = new StringBuilder();
                switch (filter.Op)
                {
                    case Operator.Or:
                    case Operator.And:
                        if (filter.Criteria is List<QueryFilter> subFilters)
                        {
                            var list = new List<KeyValuePair<string, string>>();
                            foreach (var subFilter in subFilters)
                                list.Add(PrepareFilter(subFilter));

                            foreach (var preppedFilter in list)
                                strBuilder.Append($"{preppedFilter.Key}.{preppedFilter.Value},");

                            return new KeyValuePair<string, string>(asAttribute.Mapping, $"({strBuilder.ToString().Trim(',')})");
                        }
                        break;
                    case Operator.Not:
                        if (filter.Criteria is QueryFilter notFilter)
                        {
                            var prepped = PrepareFilter(notFilter);
                            return new KeyValuePair<string, string>(prepped.Key, $"not.{prepped.Value}");
                        }
                        break;
                    case Operator.Like:
                    case Operator.ILike:
                        if (filter.Criteria is string likeCriteria && filter.Property != null)
                        {
                            return new KeyValuePair<string, string>(filter.Property, $"{asAttribute.Mapping}.{likeCriteria.Replace("%", "*")}");
                        }
                        break;
                    case Operator.In:
                        if (filter.Criteria is List<object> inCriteria && filter.Property != null)
                        {
                            foreach (var item in inCriteria)
                                strBuilder.Append($"\"{item}\",");

                            return new KeyValuePair<string, string>(filter.Property, $"{asAttribute.Mapping}.({strBuilder.ToString().Trim(',')})");
                        }
                        else if (filter.Criteria is Dictionary<string, object> dictCriteria && filter.Property != null)
                        {
                            return new KeyValuePair<string, string>(filter.Property, $"{asAttribute.Mapping}.{JsonConvert.SerializeObject(dictCriteria)}");
                        }
                        break;
                    case Operator.Contains:
                    case Operator.ContainedIn:
                    case Operator.Overlap:
                        if (filter.Criteria is List<object> listCriteria && filter.Property != null)
                        {
                            foreach (var item in listCriteria)
                                strBuilder.Append($"{item},");

                            return new KeyValuePair<string, string>(filter.Property, $"{asAttribute.Mapping}.{{{strBuilder.ToString().Trim(',')}}}");
                        }
                        else if (filter.Criteria is Dictionary<string, object> dictCriteria && filter.Property != null)
                        {
                            return new KeyValuePair<string, string>(filter.Property, $"{asAttribute.Mapping}.{JsonConvert.SerializeObject(dictCriteria)}");
                        }
                        else if (filter.Criteria is IntRange rangeCriteria && filter.Property != null)
                        {
                            return new KeyValuePair<string, string>(filter.Property, $"{asAttribute.Mapping}.{rangeCriteria.ToPostgresString()}");
                        }
                        break;
                    case Operator.StrictlyLeft:
                    case Operator.StrictlyRight:
                    case Operator.NotRightOf:
                    case Operator.NotLeftOf:
                    case Operator.Adjacent:
                        if (filter.Criteria is IntRange rangeCritera && filter.Property != null)
                        {
                            return new KeyValuePair<string, string>(filter.Property, $"{asAttribute.Mapping}.{rangeCritera.ToPostgresString()}");
                        }
                        break;
                    case Operator.FTS:
                    case Operator.PHFTS:
                    case Operator.PLFTS:
                    case Operator.WFTS:
                        if (filter.Criteria is FullTextSearchConfig searchConfig && filter.Property != null)
                        {
                            return new KeyValuePair<string, string>(filter.Property, $"{asAttribute.Mapping}({searchConfig.Config}).{searchConfig.QueryText}");
                        }
                        break;
                    default:
                        return new KeyValuePair<string, string>(filter.Property ?? "", $"{asAttribute.Mapping}.{filter.Criteria}");
                }
            }
            return new KeyValuePair<string, string>();
        }

        /// <summary>
        /// Clears currently defined query values.
        /// </summary>
        public void Clear()
        {
            columnQuery = null;

            filters.Clear();
            orderers.Clear();
            columns.Clear();

            rangeFrom = int.MinValue;
            rangeTo = int.MinValue;

            limit = int.MinValue;
            limitForeignKey = null;

            offset = int.MinValue;
            offsetForeignKey = null;

            onConflict = null;
        }


        /// <summary>
        /// Performs an INSERT Request.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private Task<ModeledResponse<T>> PerformInsert(object data, QueryOptions? options = null, CancellationToken cancellationToken = default)
        {
            method = HttpMethod.Post;
            if (options == null)
                options = new QueryOptions();

            if (!string.IsNullOrEmpty(options.OnConflict))
            {
                OnConflict(options.OnConflict!);
            }

            var request = Send<T>(method, data, options.ToHeaders(), cancellationToken, isInsert: true);

            Clear();

            return request;
        }

        private Task<BaseResponse> Send(HttpMethod method, object? data, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default, bool isInsert = false, bool isUpdate = false)
        {
            var requestHeaders = Helpers.PrepareRequestHeaders(method, headers, options, rangeFrom, rangeTo);
            var preparedData = PrepareRequestData(data, isInsert, isUpdate);
            return Helpers.MakeRequest(options, method, GenerateUrl(), serializerSettings, preparedData, requestHeaders, cancellationToken);
        }

        private Task<ModeledResponse<U>> Send<U>(HttpMethod method, object? data, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default, bool isInsert = false, bool isUpdate = false) where U : BaseModel, new()
        {
            var requestHeaders = Helpers.PrepareRequestHeaders(method, headers, options, rangeFrom, rangeTo);
            var preparedData = PrepareRequestData(data, isInsert, isUpdate);
            return Helpers.MakeRequest<U>(options, method, GenerateUrl(), serializerSettings, preparedData, requestHeaders, cancellationToken);
        }

        internal static string FindTableName(object? obj = null)
        {
            var type = obj == null ? typeof(T) : obj is Type t ? t : obj.GetType();
            var attr = Attribute.GetCustomAttribute(type, typeof(TableAttribute));

            if (attr is TableAttribute tableAttr)
            {
                return tableAttr.Name;
            }

            return type.Name;
        }
    }
}
