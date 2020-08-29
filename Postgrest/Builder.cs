using System;
using System.Collections.Generic;
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
    public class Builder<T> where T : BaseModel, new()
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

        public Builder(string baseUrl, ClientAuthorization authorization, ClientOptions options = null)
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

        public Builder<T> Filter(string columnName, Operator op, string criteria)
        {
            filters.Add(new QueryFilter(columnName, op, criteria));
            return this;
        }

        public Builder<T> Match(Dictionary<string, string> query)
        {
            return this;
        }

        public Builder<T> Order(string column, Ordering ordering, NullPosition nullPosition = NullPosition.First)
        {
            orderers.Add(new QueryOrderer(null, column, ordering, nullPosition));
            return this;
        }

        public Builder<T> Order(string foreignTable, string column, Ordering ordering, NullPosition nullPosition = NullPosition.First)
        {
            orderers.Add(new QueryOrderer(foreignTable, column, ordering, nullPosition));
            return this;
        }

        public Builder<T> Range(int from)
        {
            rangeFrom = from;
            return this;
        }

        public Builder<T> Range(int from, int to)
        {
            rangeFrom = from;
            rangeTo = to;
            return this;
        }

        public Builder<T> Select(string columnQuery)
        {
            method = HttpMethod.Get;
            this.columnQuery = columnQuery;
            return this;
        }

        public Builder<T> Limit(int limit, string foreignTableName = null)
        {
            this.limit = limit;
            this.limitForeignKey = foreignTableName;
            return this;
        }

        public Builder<T> Offset(int offset, string foreignTableName = null)
        {
            this.offset = offset;
            this.offsetForeignKey = foreignTableName;
            return this;
        }

        public Task<ModeledResponse<T>> Insert(T model, InsertOptions options = null)
        {
            method = HttpMethod.Post;
            if (options == null)
                options = new InsertOptions();

            var headers = new Dictionary<string, string>
            {
                { "Prefer", options.Upsert ? "return=representation,resolution=merge-duplicates" : "return=representation"}
            };

            var request = Send<T>(method, model, headers);

            Clear();

            return request;
        }

        public Task<ModeledResponse<T>> Update(T model)
        {
            method = HttpMethod.Patch;
            filters.Add(new QueryFilter(model.PrimaryKeyColumn, Operator.Equals, Helpers.GetPropertyValue<string>(model, model.PrimaryKeyColumn)));

            var headers = new Dictionary<string, string>
            {
                { "Prefer", "return=representation"}
            };

            var request = Send<T>(method, model, headers);

            Clear();

            return request;
        }

        public Task Delete()
        {
            method = HttpMethod.Delete;

            var request = Send(method, null, null);

            Clear();

            return request;
        }

        public Task Delete(T model)
        {
            method = HttpMethod.Delete;
            Filter(model.PrimaryKeyColumn, Operator.Equals, Helpers.GetPropertyValue<string>(model, model.PrimaryKeyColumn));
            var request = Send(method, null, null);
            Clear();
            return request;
        }

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
                catch (Exception e)
                {
                    tsc.SetException(e);
                }
            });

            return tsc.Task;
        }

        public Task<ModeledResponse<T>> Get()
        {
            var request = Send<T>(method, null, null);
            Clear();
            return request;
        }


        public string GenerateUrl()
        {
            var builder = new UriBuilder($"{BaseUrl}/{tableName}");
            var query = HttpUtility.ParseQueryString(builder.Query);

            foreach (var param in options.QueryParams)
            {
                query[param.Key] = param.Value;
            }

            foreach (var filter in filters)
            {
                var attr = filter.Op.GetAttribute<MapToAttribute>();
                if (attr is MapToAttribute asAttribute)
                {
                    switch (filter.Op)
                    {
                        case Operator.Like:
                        case Operator.ILike:
                            query[filter.Property] = $"{asAttribute.Mapping}.{filter.Criteria.Replace(" % ", " * ")}";
                            break;
                        default:
                            query[filter.Property] = $"{asAttribute.Mapping}.{filter.Criteria}";
                            break;
                    }
                }
            }

            foreach (var orderer in orderers)
            {
                var attr = orderer.NullPosition.GetAttribute<MapToAttribute>();
                if (attr is MapToAttribute asAttribute)
                {
                    var key = !string.IsNullOrEmpty(orderer.ForeignTable) ? $"{orderer.ForeignTable}.order" : "order";
                    query[key] = $"{orderer.Column}.{orderer.Ordering}.{asAttribute.Mapping}";
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

        public Dictionary<string, string> PrepareRequestData(object data) => JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(data));

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

        private Task<BaseResponse> Send(HttpMethod method, object data, Dictionary<string, string> headers = null)
        {
            return Helpers.MakeRequest(method, GenerateUrl(), PrepareRequestData(data), PrepareRequestHeaders(headers));
        }

        private Task<ModeledResponse<T>> Send<T>(HttpMethod method, object data, Dictionary<string, string> headers = null)
        {
            return Helpers.MakeRequest<T>(method, GenerateUrl(), PrepareRequestData(data), PrepareRequestHeaders(headers));
        }
    }
}
