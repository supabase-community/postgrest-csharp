using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Extensions;
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
    public class Table<T> where T : BaseModel, new()
    {
        public string BaseUrl { get; private set; }

        private ClientAuthorization authorization;
        private ClientOptions options;

        private HttpMethod method = HttpMethod.Get;

        private string tableName;
        private string columnQuery;

        private List<QueryFilter> filters = new List<QueryFilter>();
        private List<QueryOrderer> orderers = new List<QueryOrderer>();

        private int rangeFrom = int.MinValue;
        private int rangeTo = int.MinValue;

        private int limit = int.MinValue;
        private string limitForeignKey;

        private int offset = int.MinValue;
        private string offsetForeignKey;

        /// <summary>
        /// Typically called from the Client Singleton using `Client.Instance.Builder<T>`
        /// </summary>
        /// <param name="baseUrl">Api Endpoint (ex: "http://localhost:8000"), no trailing slash required.</param>
        /// <param name="authorization">Authorization Information.</param>
        /// <param name="options">Optional client configuration.</param>
        public Table(string baseUrl, ClientAuthorization authorization, ClientOptions options = null)
        {
            BaseUrl = baseUrl;

            if (options == null)
                options = new ClientOptions();

            this.options = options;
            this.authorization = authorization;

            var attr = Attribute.GetCustomAttribute(typeof(T), typeof(TableAttribute));
            if (attr is TableAttribute tableAttr)
            {
                tableName = tableAttr.Name;
            }
            else
            {
                tableName = typeof(T).Name;
            }
        }

        /// <summary>
        /// Add a Filter to a query request
        /// </summary>
        /// <param name="columnName">Column Name in Table.</param>
        /// <param name="op">Operation to perform.</param>
        /// <param name="criterion">Value to filter with, must be a `string`, `List<object>`, `Dictionary<string, object>`, or `Range`</string></object></param>
        /// <returns></returns>
        public Table<T> Filter(string columnName, Operator op, object criterion)
        {
            if (criterion == null)
            {
                switch (op)
                {
                    case Operator.Equals:
                    case Operator.Is:
                        filters.Add(new QueryFilter(columnName, Operator.Is, QueryFilter.NULL_VAL));
                        break;
                    case Operator.Not:
                    case Operator.NotEqual:
                        filters.Add(new QueryFilter(columnName, Operator.Not, new QueryFilter(columnName, Operator.Is, QueryFilter.NULL_VAL)));
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
            else if (criterion is Range rangeCriteria)
            {
                filters.Add(new QueryFilter(columnName, op, rangeCriteria));
                return this;
            }
            else if (criterion is FullTextSearchConfig fullTextSearchCriteria)
            {
                filters.Add(new QueryFilter(columnName, op, fullTextSearchCriteria));
                return this;
            }

            throw new Exception("Unknown criterion type, is it of type `string`, `List`, `Dictionary<string, object>`, `FullTextSearchConfig`, or `Range`?");
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
            filters.Add(new QueryFilter(Operator.And, filters));
            return this;
        }

        /// <summary>
        /// Adds a NOT Filter to the current query args.
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        public Table<T> Or(List<QueryFilter> filters)
        {
            filters.Add(new QueryFilter(Operator.Or, filters));
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
        public Table<T> Limit(int limit, string foreignTableName = null)
        {
            this.limit = limit;
            this.limitForeignKey = foreignTableName;
            return this;
        }


        /// <summary>
        /// Sets an offset with an optional foreign table reference.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="foreignTableName"></param>
        /// <returns></returns>
        public Table<T> Offset(int offset, string foreignTableName = null)
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
        /// <returns>A typed model response from the database.</returns>
        public Task<ModeledResponse<T>> Insert(T model, InsertOptions options = null) => PerformInsert(model, options);

        /// <summary>
        /// Executes a BULK INSERT query using the defined query params on the current instance.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="options"></param>
        /// <returns>A typed model response from the database.</returns>
        public Task<ModeledResponse<T>> Insert(ICollection<T> models, InsertOptions options = null) => PerformInsert(models, options);

        /// <summary>
        /// Executes an UPDATE query using the defined query params on the current instance.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>A typed response from the database.</returns>
        public Task<ModeledResponse<T>> Update(T model)
        {
            method = HttpMethod.Patch;

            filters.Add(new QueryFilter(model.PrimaryKeyColumn, Operator.Equals, model.PrimaryKeyValue.ToString()));

            var headers = new Dictionary<string, string>
            {
                { "Prefer", "return=representation"}
            };

            var request = Send<T>(method, model, headers);

            Clear();

            return request;
        }

        /// <summary>
        /// Executes a delete request using the defined query params on the current instance.
        /// </summary>
        /// <returns></returns>
        public Task Delete()
        {
            method = HttpMethod.Delete;

            var request = Send(method, null, null);

            Clear();

            return request;
        }

        /// <summary>
        /// Executes a delete request using the model's primary key as the filter for the request.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public Task Delete(T model)
        {
            method = HttpMethod.Delete;
            Filter(model.PrimaryKeyColumn, Operator.Equals, model.PrimaryKeyValue.ToString());
            var request = Send(method, null, null);
            Clear();
            return request;
        }

        /// <summary>
        /// Executes a query that expects to have a single object returned, rather than returning list of models
        /// it will return a single model.
        /// </summary>
        /// <returns></returns>
        public Task<T> Single()
        {
            var tsc = new TaskCompletionSource<T>();

            Task.Run(async () =>
            {
                method = HttpMethod.Get;
                var headers = new Dictionary<string, string>
                {
                    { "Accept", "application/vnd.pgrst.object+json" },
                    { "Prefer", "return=representation"}
                };

                var request = Send<T>(method, null, headers);

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
        /// <returns></returns>
        public Task<ModeledResponse<T>> Get()
        {
            var request = Send<T>(method, null, null);
            Clear();
            return request;
        }

        /// <summary>
        /// Generates the encoded URL with defined query parameters that will be sent to the Postgrest API.
        /// </summary>
        /// <returns></returns>
        public string GenerateUrl()
        {
            var builder = new UriBuilder($"{BaseUrl}/{tableName}");
            var query = HttpUtility.ParseQueryString(builder.Query);

            foreach (var param in options.QueryParams)
            {
                query.Add(param.Key, param.Value);
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

            if (authorization.Type == ClientAuthorization.AuthorizationType.ApiKey)
            {
                query["apikey"] = authorization.ApiKey;
            }

            if (!string.IsNullOrEmpty(columnQuery))
            {
                query["select"] = Regex.Replace(columnQuery, @"\s", "");
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
        /// Transforms an object into a string mapped dictionary using `Client.Instance.SerializerSettings`.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public object PrepareRequestData(object data)
        {
            // Check if data is a Collection for the Insert Bulk case
            if (data is ICollection<T>)
                return JsonConvert.DeserializeObject<ICollection<T>>(JsonConvert.SerializeObject(data, Client.Instance.SerializerSettings));

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(data, Client.Instance.SerializerSettings));
        }

        /// <summary>
        /// Prepares the request with appropriate HTTP headers expected by Postgrest.
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        public Dictionary<string, string> PrepareRequestHeaders(Dictionary<string, string> headers = null)
        {
            if (headers == null)
                headers = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(options.Schema))
            {
                if (method == HttpMethod.Get)
                    headers.Add("Accept-Profile", options.Schema);
                else
                    headers.Add("Content-Profile", options.Schema);
            }

            if (authorization != null)
            {
                switch (authorization.Type)
                {
                    case ClientAuthorization.AuthorizationType.ApiKey:
                        headers.Add("apikey", authorization.ApiKey);
                        break;
                    case ClientAuthorization.AuthorizationType.Token:
                        headers.Add("Authorization", $"Bearer {authorization.Token}");
                        break;
                    case ClientAuthorization.AuthorizationType.Basic:
                        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{authorization.Username}:{authorization.Password}"));
                        headers.Add("Authorization", $"Basic {header}");
                        break;
                }
            }

            if (rangeFrom != int.MinValue)
            {
                headers.Add("Range-Unit", "items");
                headers.Add("Range", $"{rangeFrom}-{(rangeTo != int.MinValue ? rangeTo.ToString() : null)}");
            }

            return headers;
        }

        /// <summary>
        /// Transforms the defined filters into the expected Postgrest format.
        ///
        /// See: http://postgrest.org/en/v7.0.0/api.html#operators
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public KeyValuePair<string, string> PrepareFilter(QueryFilter filter)
        {
            var attr = filter.Op.GetAttribute<MapToAttribute>();
            if (attr is MapToAttribute asAttribute)
            {
                var str = "";
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
                                str += $"{preppedFilter.Key}.{preppedFilter.Value},";

                            return new KeyValuePair<string, string>(asAttribute.Mapping, $"({str.Trim(',')})");
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
                        if (filter.Criteria is string likeCriteria)
                        {
                            return new KeyValuePair<string, string>(filter.Property, $"{asAttribute.Mapping}.{likeCriteria.Replace("%", "*")}");
                        }
                        break;
                    case Operator.In:
                        if (filter.Criteria is List<object> inCriteria)
                        {
                            foreach (var item in inCriteria)
                                str += $"\"{item}\",";

                            str = str.Trim(',');
                            return new KeyValuePair<string, string>(filter.Property, $"{asAttribute.Mapping}.({str})");
                        }
                        else if (filter.Criteria is Dictionary<string, object> dictCriteria)
                        {
                            return new KeyValuePair<string, string>(filter.Property, $"{asAttribute.Mapping}.{JsonConvert.SerializeObject(dictCriteria)}");
                        }
                        break;
                    case Operator.Contains:
                    case Operator.ContainedIn:
                    case Operator.Overlap:
                        if (filter.Criteria is List<object> listCriteria)
                        {
                            foreach (var item in listCriteria)
                                str += $"{item},";
                            str = str.Trim(',');

                            return new KeyValuePair<string, string>(filter.Property, $"{asAttribute.Mapping}.{{{str}}}");
                        }
                        else if (filter.Criteria is Dictionary<string, object> dictCriteria)
                        {
                            return new KeyValuePair<string, string>(filter.Property, $"{asAttribute.Mapping}.{JsonConvert.SerializeObject(dictCriteria)}");
                        }
                        else if (filter.Criteria is Range rangeCriteria)
                        {
                            return new KeyValuePair<string, string>(filter.Property, $"{asAttribute.Mapping}.{rangeCriteria.ToPostgresString()}");
                        }
                        break;
                    case Operator.StrictlyLeft:
                    case Operator.StrictlyRight:
                    case Operator.NotRightOf:
                    case Operator.NotLeftOf:
                    case Operator.Adjacent:
                        if (filter.Criteria is Range rangeCritera)
                        {
                            return new KeyValuePair<string, string>(filter.Property, $"{asAttribute.Mapping}.{rangeCritera.ToPostgresString()}");
                        }
                        break;
                    case Operator.FTS:
                    case Operator.PHFTS:
                    case Operator.PLFTS:
                    case Operator.WFTS:
                        if (filter.Criteria is FullTextSearchConfig searchConfig)
                        {
                            return new KeyValuePair<string, string>(filter.Property, $"{asAttribute.Mapping}({searchConfig.Config}).{searchConfig.QueryText}");
                        }
                        break;
                    default:
                        return new KeyValuePair<string, string>(filter.Property, $"{asAttribute.Mapping}.{filter.Criteria}");
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

            rangeFrom = int.MinValue;
            rangeTo = int.MinValue;

            limit = int.MinValue;
            limitForeignKey = null;

            offset = int.MinValue;
            offsetForeignKey = null;
        }

        /// <summary>
        /// Performs an INSERT Request.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private Task<ModeledResponse<T>> PerformInsert(object data, InsertOptions options = null)
        {
            method = HttpMethod.Post;
            if (options == null)
                options = new InsertOptions();

            var headers = new Dictionary<string, string>
            {
                { "Prefer", options.Upsert ? "return=representation,resolution=merge-duplicates" : "return=representation"}
            };

            var request = Send<T>(method, data, headers);

            Clear();

            return request;
        }

        private Task<BaseResponse> Send(HttpMethod method, object data, Dictionary<string, string> headers = null)
        {
            return Helpers.MakeRequest(method, GenerateUrl(), PrepareRequestData(data), PrepareRequestHeaders(headers));
        }

        private Task<ModeledResponse<U>> Send<U>(HttpMethod method, object data, Dictionary<string, string> headers = null) where U : BaseModel, new()
        {
            return Helpers.MakeRequest<U>(method, GenerateUrl(), PrepareRequestData(data), PrepareRequestHeaders(headers));
        }
    }
}
