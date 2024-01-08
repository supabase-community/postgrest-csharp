using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Exceptions;
using Postgrest.Extensions;
using Postgrest.Interfaces;
using Postgrest.Linq;
using Postgrest.Models;
using Postgrest.Responses;
using Supabase.Core.Attributes;
using Supabase.Core.Extensions;
using static Postgrest.Constants;

namespace Postgrest
{
    /// <summary>
    /// Class created from a model derived from `BaseModel` that can generate query requests to a Postgrest Endpoint.
    /// 
    /// Representative of a `USE $TABLE` command.
    /// </summary>
    /// <typeparam name="TModel">Model derived from `BaseModel`.</typeparam>
    public class Table<TModel> : IPostgrestTable<TModel> where TModel : BaseModel, new()
    {
        /// <inheritdoc />
        public string BaseUrl { get; }

        /// <inheritdoc />
        public string TableName { get; }

        /// <inheritdoc />
        public Func<Dictionary<string, string>>? GetHeaders { get; set; }

        private readonly ClientOptions _options;
        private readonly JsonSerializerSettings _serializerSettings;

        private HttpMethod _method = HttpMethod.Get;

        #region Pending Query State

        private string? _columnQuery;

        private readonly List<QueryFilter> _filters = new();
        private readonly List<QueryOrderer> _orderers = new();
        private readonly List<string> _columns = new();

        private readonly Dictionary<object, object?> _setData = new();

        private readonly List<ReferenceAttribute> _references = new();

        private int _rangeFrom = int.MinValue;
        private int _rangeTo = int.MinValue;

        private int _limit = int.MinValue;
        private string? _limitForeignKey;

        private int _offset = int.MinValue;
        private string? _offsetForeignKey;

        private string? _onConflict;

        #endregion

        /// <summary>
        /// Typically called from the Client `new Client.Table&lt;ModelType&gt;`
        /// </summary>
        /// <param name="baseUrl">Api Endpoint (ex: "http://localhost:8000"), no trailing slash required.</param>
        /// <param name="serializerSettings"></param>
        /// <param name="options">Optional client configuration.</param>
        public Table(string baseUrl, JsonSerializerSettings serializerSettings, ClientOptions? options = null)
        {
            BaseUrl = baseUrl;

            _options = options ?? new ClientOptions();
            _serializerSettings = serializerSettings;

            foreach (var property in typeof(TModel).GetProperties())
            {
                var attrs = property.GetCustomAttributes(typeof(ReferenceAttribute), true);

                foreach (ReferenceAttribute attr in attrs)
                {
                    attr.ParseProperties(new List<ReferenceAttribute> { attr });
                    _references.Add(attr);
                }
            }

            TableName = FindTableName();
        }

        /// <inheritdoc />
        public Table<TModel> Filter<TCriterion>(Expression<Func<TModel, object>> predicate, Operator op,
            TCriterion? criterion)
        {
            var visitor = new SelectExpressionVisitor();
            visitor.Visit(predicate);

            if (visitor.Columns.Count == 0)
                throw new ArgumentException("Expected predicate to return a reference to a Model column.");

            if (visitor.Columns.Count > 1)
                throw new ArgumentException("Only one column should be returned from the predicate.");

            return Filter(visitor.Columns.First(), op, criterion);
        }

        /// <inheritdoc />
        public Table<TModel> Filter<TCriterion>(string columnName, Operator op, TCriterion? criterion)
        {
            switch (criterion)
            {
                case null:
                    switch (op)
                    {
                        case Operator.Equals:
                        case Operator.Is:
                            _filters.Add(new QueryFilter(columnName, Operator.Is, QueryFilter.NullVal));
                            break;
                        case Operator.Not:
                        case Operator.NotEqual:
                            _filters.Add(new QueryFilter(columnName, Operator.Not,
                                new QueryFilter(columnName, Operator.Is, QueryFilter.NullVal)));
                            break;
                        default:
                            throw new PostgrestException(
                                    "NOT filters must use the `Equals`, `Is`, `Not` or `NotEqual` operators")
                                { Reason = FailureHint.Reason.InvalidArgument };
                    }

                    return this;
                case string stringCriterion:
                    _filters.Add(new QueryFilter(columnName, op, stringCriterion));
                    return this;
                case int intCriterion:
                    _filters.Add(new QueryFilter(columnName, op, intCriterion));
                    return this;
                case float floatCriterion:
                    _filters.Add(new QueryFilter(columnName, op, floatCriterion));
                    return this;
                case List<object> listCriteria:
                    _filters.Add(new QueryFilter(columnName, op, listCriteria));
                    return this;
                case Dictionary<string, object> dictCriteria:
                    _filters.Add(new QueryFilter(columnName, op, dictCriteria));
                    return this;
                case IntRange rangeCriteria:
                    _filters.Add(new QueryFilter(columnName, op, rangeCriteria));
                    return this;
                case FullTextSearchConfig fullTextSearchCriteria:
                    _filters.Add(new QueryFilter(columnName, op, fullTextSearchCriteria));
                    return this;
                default:
                    throw new PostgrestException(
                        "Unknown criterion type, is it of type `string`, `int`, `float`, `List`, `Dictionary<string, object>`, `FullTextSearchConfig`, or `Range`?")
                    {
                        Reason = FailureHint.Reason.InvalidArgument
                    };
            }
        }

        /// <inheritdoc />
        public Table<TModel> Not(QueryFilter filter)
        {
            _filters.Add(new QueryFilter(Operator.Not, filter));
            return this;
        }

        /// <inheritdoc />
        public Table<TModel> Not<TCriterion>(string columnName, Operator op, TCriterion? criterion) =>
            Not(new QueryFilter(columnName, op, criterion));

        /// <inheritdoc />
        public Table<TModel> Not<TCriterion>(Expression<Func<TModel, object>> predicate, Operator op,
            TCriterion? criterion)
        {
            var visitor = new SelectExpressionVisitor();
            visitor.Visit(predicate);

            if (visitor.Columns.Count == 0)
                throw new ArgumentException("Expected predicate to return a reference to a Model column.");

            if (visitor.Columns.Count > 1)
                throw new ArgumentException("Only one column should be returned from the predicate.");

            return Not(new QueryFilter(visitor.Columns.First(), op, criterion));
        }

        /// <inheritdoc />
        public Table<TModel> Not<TCriterion>(string columnName, Operator op, List<TCriterion> criteria) =>
            Not(new QueryFilter(columnName, op, criteria.Cast<object>().ToList()));

        /// <inheritdoc />
        public Table<TModel> Not<TCriterion>(Expression<Func<TModel, object>> predicate, Operator op,
            List<TCriterion> criteria)
        {
            var visitor = new SelectExpressionVisitor();
            visitor.Visit(predicate);

            if (visitor.Columns.Count == 0)
                throw new ArgumentException("Expected predicate to return a reference to a Model column.");

            if (visitor.Columns.Count > 1)
                throw new ArgumentException("Only one column should be returned from the predicate.");

            return Not(new QueryFilter(visitor.Columns.First(), op, criteria.Cast<object>().ToList()));
        }

        /// <inheritdoc />
        public Table<TModel> Not(string columnName, Operator op, Dictionary<string, object> criteria) =>
            Not(new QueryFilter(columnName, op, criteria));

        /// <inheritdoc />
        public Table<TModel> Not(Expression<Func<TModel, object>> predicate, Operator op,
            Dictionary<string, object> criteria)
        {
            var visitor = new SelectExpressionVisitor();
            visitor.Visit(predicate);

            if (visitor.Columns.Count == 0)
                throw new ArgumentException("Expected predicate to return a reference to a Model column.");

            if (visitor.Columns.Count > 1)
                throw new ArgumentException("Only one column should be returned from the predicate.");

            return Not(new QueryFilter(visitor.Columns.First(), op, criteria));
        }


        /// <inheritdoc />
        public Table<TModel> And(List<QueryFilter> filters)
        {
            _filters.Add(new QueryFilter(Operator.And, filters));
            return this;
        }

        /// <inheritdoc />
        public Table<TModel> Or(List<QueryFilter> filters)
        {
            _filters.Add(new QueryFilter(Operator.Or, filters));
            return this;
        }

        /// <inheritdoc />
        public Table<TModel> Match(TModel model)
        {
            foreach (var kvp in model.PrimaryKey)
            {
                _filters.Add(new QueryFilter(kvp.Key.ColumnName, Operator.Equals, kvp.Value));
            }

            return this;
        }

        /// <inheritdoc />
        public Table<TModel> Match(Dictionary<string, string> query)
        {
            foreach (var param in query)
            {
                _filters.Add(new QueryFilter(param.Key, Operator.Equals, param.Value));
            }

            return this;
        }

        /// <inheritdoc />
        public Table<TModel> Order(Expression<Func<TModel, object>> predicate, Ordering ordering,
            NullPosition nullPosition = NullPosition.First)
        {
            var visitor = new SelectExpressionVisitor();
            visitor.Visit(predicate);

            if (visitor.Columns.Count == 0)
                throw new ArgumentException("Expected predicate to return a reference to a Model column.");

            if (visitor.Columns.Count > 1)
                throw new ArgumentException("Only one column should be returned from the predicate.");

            return Order(visitor.Columns.First(), ordering, nullPosition);
        }


        /// <inheritdoc />
        public Table<TModel> Order(string column, Ordering ordering, NullPosition nullPosition = NullPosition.First)
        {
            _orderers.Add(new QueryOrderer(null, column, ordering, nullPosition));
            return this;
        }

        /// <inheritdoc />
        public Table<TModel> Order(string foreignTable, string column, Ordering ordering,
            NullPosition nullPosition = NullPosition.First)
        {
            _orderers.Add(new QueryOrderer(foreignTable, column, ordering, nullPosition));
            return this;
        }

        /// <inheritdoc />
        public Table<TModel> Range(int from)
        {
            _rangeFrom = from;
            return this;
        }

        /// <inheritdoc />
        public Table<TModel> Range(int from, int to)
        {
            _rangeFrom = from;
            _rangeTo = to;
            return this;
        }

        /// <inheritdoc />
        public Table<TModel> Select(string columnQuery)
        {
            _method = HttpMethod.Get;
            _columnQuery = columnQuery;
            return this;
        }

        /// <inheritdoc />
        public Table<TModel> Select(Expression<Func<TModel, object[]>> predicate)
        {
            var visitor = new SelectExpressionVisitor();
            visitor.Visit(predicate);

            if (visitor.Columns.Count == 0)
                throw new ArgumentException(
                    "Unable to find column(s) to select from the given predicate, did you return an array of Model Properties?");

            return Select(string.Join(",", visitor.Columns));
        }

        /// <inheritdoc />
        public Table<TModel> Where(Expression<Func<TModel, bool>> predicate)
        {
            var visitor = new WhereExpressionVisitor();
            visitor.Visit(predicate);

            if (visitor.Filter == null)
                throw new ArgumentException(
                    "Unable to parse the supplied predicate, did you return a predicate where each left hand of the condition is a Model property?");

            if (visitor.Filter.Op == Operator.Equals && visitor.Filter.Criteria == null)
                _filters.Add(new QueryFilter(visitor.Filter.Property!, Operator.Is, QueryFilter.NullVal));
            else if (visitor.Filter.Op == Operator.NotEqual && visitor.Filter.Criteria == null)
                _filters.Add(new QueryFilter(visitor.Filter.Property!, Operator.Not,
                    new QueryFilter(visitor.Filter.Property!, Operator.Is, QueryFilter.NullVal)));
            else
                _filters.Add(visitor.Filter);

            return this;
        }


        /// <inheritdoc />
        public Table<TModel> Limit(int limit, string? foreignTableName = null)
        {
            _limit = limit;
            _limitForeignKey = foreignTableName;
            return this;
        }


        /// <inheritdoc />
        public Table<TModel> OnConflict(string columnName)
        {
            _onConflict = columnName;
            return this;
        }

        /// <inheritdoc />
        public Table<TModel> OnConflict(Expression<Func<TModel, object>> predicate)
        {
            var visitor = new SelectExpressionVisitor();
            visitor.Visit(predicate);

            if (visitor.Columns.Count == 0)
                throw new ArgumentException("Expected predicate to return a reference to a Model column.");

            if (visitor.Columns.Count > 1)
                throw new ArgumentException("Only one column should be returned from the predicate.");

            OnConflict(visitor.Columns.First());

            return this;
        }


        /// <inheritdoc />
        public Table<TModel> Columns(string[] columns)
        {
            foreach (var column in columns)
                _columns.Add(column);

            return this;
        }


        /// <inheritdoc />
        public Table<TModel> Columns(Expression<Func<TModel, object[]>> predicate)
        {
            var visitor = new SelectExpressionVisitor();
            visitor.Visit(predicate);

            if (visitor.Columns.Count == 0)
                throw new ArgumentException("Expected predicate to return an array of references to a Model column.");

            return Columns(visitor.Columns.ToArray());
        }


        /// <inheritdoc />
        public Table<TModel> Offset(int offset, string? foreignTableName = null)
        {
            _offset = offset;
            _offsetForeignKey = foreignTableName;
            return this;
        }


        /// <inheritdoc />
        public Task<ModeledResponse<TModel>> Insert(TModel model, QueryOptions? options = null,
            CancellationToken cancellationToken = default) => PerformInsert(model, options, cancellationToken);


        /// <inheritdoc />
        public Task<ModeledResponse<TModel>> Insert(ICollection<TModel> models, QueryOptions? options = null,
            CancellationToken cancellationToken = default) => PerformInsert(models, options, cancellationToken);


        /// <inheritdoc />
        public Task<ModeledResponse<TModel>> Upsert(TModel model, QueryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new QueryOptions();

            // Enforce Upsert
            options.Upsert = true;

            return PerformInsert(model, options, cancellationToken);
        }


        /// <inheritdoc />
        public Task<ModeledResponse<TModel>> Upsert(ICollection<TModel> model, QueryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new QueryOptions();

            // Enforce Upsert
            options.Upsert = true;

            return PerformInsert(model, options, cancellationToken);
        }


        /// <inheritdoc />
        public Table<TModel> Set(Expression<Func<TModel, object>> keySelector, object? value)
        {
            var visitor = new SetExpressionVisitor();
            visitor.Visit(keySelector);

            if (visitor.Column == null || visitor.ExpectedType == null)
                throw new ArgumentException(
                    "Expression should return a KeyValuePair with a key of a Model Property and a value.");

            if (value == null)
            {
                if (Nullable.GetUnderlyingType(visitor.ExpectedType) == null)
                    throw new ArgumentException(
                        $"Expected Value to be of Type: {visitor.ExpectedType.Name}, instead received: {null}.");
            }
            else if (!visitor.ExpectedType.IsInstanceOfType(value))
            {
                throw new ArgumentException(string.Format("Expected Value to be of Type: {0}, instead received: {1}.",
                    visitor.ExpectedType.Name, value.GetType().Name));
            }

            _setData.Add(visitor.Column, value);

            return this;
        }


        /// <inheritdoc />
        public Table<TModel> Set(Expression<Func<TModel, KeyValuePair<object, object?>>> keyValuePairExpression)
        {
            var visitor = new SetExpressionVisitor();
            visitor.Visit(keyValuePairExpression);

            if (visitor.Column == null || visitor.Value == default)
                throw new ArgumentException(
                    "Expression should return a KeyValuePair with a key of a Model Property and a value.");

            _setData.Add(visitor.Column, visitor.Value);

            return this;
        }


        /// <inheritdoc />
        public Task<ModeledResponse<TModel>> Update(QueryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new QueryOptions();

            if (_setData.Keys.Count == 0)
                throw new ArgumentException("No data has been set to update, was `Set` called?");

            _method = new HttpMethod("PATCH");

            var request = Send<TModel>(_method, _setData, options.ToHeaders(), cancellationToken, isUpdate: true);

            Clear();

            return request;
        }


        /// <inheritdoc />
        public Task<ModeledResponse<TModel>> Update(TModel model, QueryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new QueryOptions();

            _method = new HttpMethod("PATCH");

            Match(model);

            var request = Send<TModel>(_method, model, options.ToHeaders(), cancellationToken, isUpdate: true);

            Clear();

            return request;
        }


        /// <inheritdoc />
        public Task Delete(QueryOptions? options = null, CancellationToken cancellationToken = default)
        {
            options ??= new QueryOptions();

            _method = HttpMethod.Delete;

            var request = Send(_method, null, options.ToHeaders(), cancellationToken);

            Clear();

            return request;
        }


        /// <inheritdoc />
        public Task<ModeledResponse<TModel>> Delete(TModel model, QueryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new QueryOptions();

            _method = HttpMethod.Delete;

            Match(model);

            var request = Send<TModel>(_method, null, options.ToHeaders(), cancellationToken);
            Clear();
            return request;
        }


        /// <inheritdoc />
        public async Task<int> Count(CountType type, CancellationToken cancellationToken = default)
        {
            _method = HttpMethod.Head;

            var attr = type.GetAttribute<MapToAttribute>();

            var headers = new Dictionary<string, string>
            {
                { "Prefer", $"count={attr?.Mapping}" }
            };

            var request = Send(_method, null, headers, cancellationToken);
            Clear();

            var response = await request;
            var countStr = response.ResponseMessage?.Content.Headers.GetValues("Content-Range").FirstOrDefault();

            // Returns X-Y/COUNT [0-3/4]
            return int.Parse(countStr?.Split('/')[1] ?? throw new InvalidOperationException());
        }


        /// <inheritdoc />
        public async Task<TModel?> Single(CancellationToken cancellationToken = default)
        {
            _method = HttpMethod.Get;
            var headers = new Dictionary<string, string>
            {
                { "Accept", "application/vnd.pgrst.object+json" },
                { "Prefer", "return=representation" }
            };

            var request = Send<TModel>(_method, null, headers, cancellationToken);

            Clear();

            try
            {
                var result = await request;
                return result.Models.FirstOrDefault();
            }
            catch (PostgrestException e)
            {
                if (e.Response!.StatusCode == HttpStatusCode.NotAcceptable)
                    return null;

                throw;
            }
        }

        /// <inheritdoc />
        public Task<ModeledResponse<TModel>> Get(CancellationToken cancellationToken = default)
        {
            var request = Send<TModel>(_method, null, null, cancellationToken);
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

            foreach (var param in _options.QueryParams)
                query.Add(param.Key, param.Value);

            if (_options.Headers.TryGetValue("apikey", out var header))
                query.Add("apikey", header);

            if (_columns.Count > 0)
                query["columns"] = string.Join(",", _columns);

            foreach (var parsedFilter in _filters.Select(PrepareFilter))
                query.Add(parsedFilter.Key, parsedFilter.Value);

            if (_orderers.Count > 0)
            {
                var order = new StringBuilder();

                foreach (var orderer in _orderers)
                {
                    var nullPosAttr = orderer.NullPosition.GetAttribute<MapToAttribute>();
                    var orderingAttr = orderer.Ordering.GetAttribute<MapToAttribute>();

                    if (nullPosAttr == null || orderingAttr == null) continue;

                    if (order.Length > 0)
                        order.Append(",");

                    var selector = !string.IsNullOrEmpty(orderer.ForeignTable)
                        ? orderer.ForeignTable + "(" + orderer.Column + ")"
                        : orderer.Column;
                    
                    order.Append($"{selector}.{orderingAttr.Mapping}.{nullPosAttr.Mapping}");
                }

                query.Add("order", order.ToString());
            }

            if (!string.IsNullOrEmpty(_columnQuery))
                query["select"] = Regex.Replace(_columnQuery!, @"\s", "");

            if (_references.Count > 0)
            {
                query["select"] ??= "*";

                foreach (var reference in _references)
                {
                    if ((_method == HttpMethod.Get && !reference.IncludeInQuery) ||
                        (_method == HttpMethod.Post && reference.IgnoreOnInsert) ||
                        (_method == HttpMethod.Post && reference.IgnoreOnUpdate)) continue;

                    var columns = string.Join(",", reference.Columns.ToArray());

                    if (!string.IsNullOrEmpty(reference.ForeignKey))
                    {
                        if (reference.UseInnerJoin)
                            query["select"] += $",{reference.ColumnName}:{reference.ForeignKey}!inner({columns})";
                        else
                            query["select"] += $",{reference.ColumnName}:{reference.ForeignKey}({columns})";
                    }
                    else
                    {
                        if (reference.UseInnerJoin)
                            query["select"] += $",{reference.TableName}!inner({columns})";
                        else
                            query["select"] += $",{reference.TableName}({columns})";
                    }
                }
            }

            if (!string.IsNullOrEmpty(_onConflict))
                query["on_conflict"] = _onConflict;

            if (_limit != int.MinValue)
            {
                var key = _limitForeignKey != null ? $"{_limitForeignKey}.limit" : "limit";
                query[key] = _limit.ToString();
            }

            if (_offset != int.MinValue)
            {
                var key = _offsetForeignKey != null ? $"{_offsetForeignKey}.offset" : "offset";
                query[key] = _offset.ToString();
            }

            builder.Query = query.ToString();
            return builder.Uri.ToString();
        }

        /// <summary>
        /// Transforms an object into a string mapped list/dictionary using `JsonSerializerSettings`.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="isInsert"></param>
        /// <param name="isUpdate"></param>
        /// <param name="isUpsert"></param>
        /// <returns></returns>
        private object? PrepareRequestData(object? data, bool isInsert = false, bool isUpdate = false,
            bool isUpsert = false)
        {
            if (data == null) return new Dictionary<string, string>();

            // Specified in constructor;
            var resolver = (PostgrestContractResolver)_serializerSettings.ContractResolver!;

            resolver.SetState(isInsert, isUpdate, isUpsert);

            var serialized = JsonConvert.SerializeObject(data, _serializerSettings);

            resolver.SetState();

            // Check if data is a Collection for the Insert Bulk case
            if (data is ICollection<TModel>)
                return JsonConvert.DeserializeObject<List<object>>(serialized, _serializerSettings);

            return JsonConvert.DeserializeObject<Dictionary<string, object>>(serialized, _serializerSettings);
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
            var asAttribute = filter.Op.GetAttribute<MapToAttribute>();
            var strBuilder = new StringBuilder();

            if (asAttribute == null)
                return new KeyValuePair<string, string>();

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

                        return new KeyValuePair<string, string>(asAttribute.Mapping,
                            $"({strBuilder.ToString().Trim(',')})");
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
                        return new KeyValuePair<string, string>(filter.Property,
                            $"{asAttribute.Mapping}.{likeCriteria.Replace("%", "*")}");
                    }

                    break;
                case Operator.In:
                    if (filter.Criteria is List<object> inCriteria && filter.Property != null)
                    {
                        foreach (var item in inCriteria)
                            strBuilder.Append($"\"{item}\",");

                        return new KeyValuePair<string, string>(filter.Property,
                            $"{asAttribute.Mapping}.({strBuilder.ToString().Trim(',')})");
                    }
                    else if (filter.Criteria is Dictionary<string, object> dictCriteria && filter.Property != null)
                    {
                        return new KeyValuePair<string, string>(filter.Property,
                            $"{asAttribute.Mapping}.{JsonConvert.SerializeObject(dictCriteria)}");
                    }

                    break;
                case Operator.Contains:
                case Operator.ContainedIn:
                case Operator.Overlap:
                    switch (filter.Criteria)
                    {
                        case List<object> listCriteria when filter.Property != null:
                        {
                            foreach (var item in listCriteria)
                                strBuilder.Append($"{item},");

                            return new KeyValuePair<string, string>(filter.Property,
                                $"{asAttribute.Mapping}.{{{strBuilder.ToString().Trim(',')}}}");
                        }
                        case Dictionary<string, object> dictCriteria when filter.Property != null:
                            return new KeyValuePair<string, string>(filter.Property,
                                $"{asAttribute.Mapping}.{JsonConvert.SerializeObject(dictCriteria)}");
                        case IntRange rangeCriteria when filter.Property != null:
                            return new KeyValuePair<string, string>(filter.Property,
                                $"{asAttribute.Mapping}.{rangeCriteria.ToPostgresString()}");
                    }

                    break;
                case Operator.StrictlyLeft:
                case Operator.StrictlyRight:
                case Operator.NotRightOf:
                case Operator.NotLeftOf:
                case Operator.Adjacent:
                    if (filter.Criteria is IntRange rangeCriterion && filter.Property != null)
                    {
                        return new KeyValuePair<string, string>(filter.Property,
                            $"{asAttribute.Mapping}.{rangeCriterion.ToPostgresString()}");
                    }

                    break;
                case Operator.FTS:
                case Operator.PHFTS:
                case Operator.PLFTS:
                case Operator.WFTS:
                    if (filter.Criteria is FullTextSearchConfig searchConfig && filter.Property != null)
                    {
                        return new KeyValuePair<string, string>(filter.Property,
                            $"{asAttribute.Mapping}({searchConfig.Config}).{searchConfig.QueryText}");
                    }

                    break;
                default:
                    return new KeyValuePair<string, string>(filter.Property ?? "",
                        $"{asAttribute.Mapping}.{filter.Criteria}");
            }

            return new KeyValuePair<string, string>();
        }


        /// <inheritdoc />
        public void Clear()
        {
            _columnQuery = null;

            _filters.Clear();
            _orderers.Clear();
            _columns.Clear();
            _setData.Clear();

            _rangeFrom = int.MinValue;
            _rangeTo = int.MinValue;

            _limit = int.MinValue;
            _limitForeignKey = null;

            _offset = int.MinValue;
            _offsetForeignKey = null;

            _onConflict = null;
        }


        /// <summary>
        /// Performs an INSERT Request.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private Task<ModeledResponse<TModel>> PerformInsert(object data, QueryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            _method = HttpMethod.Post;
            options ??= new QueryOptions();

            if (!string.IsNullOrEmpty(options.OnConflict))
                OnConflict(options.OnConflict!);

            var request = Send<TModel>(_method, data, options.ToHeaders(), cancellationToken, isInsert: true,
                isUpsert: options.Upsert);

            Clear();

            return request;
        }

        private Task<BaseResponse> Send(HttpMethod method, object? data, Dictionary<string, string>? headers = null,
            CancellationToken cancellationToken = default, bool isInsert = false,
            bool isUpdate = false, bool isUpsert = false)
        {
            var requestHeaders = Helpers.PrepareRequestHeaders(method, headers, _options, _rangeFrom, _rangeTo);

            if (GetHeaders != null)
            {
                requestHeaders = GetHeaders().MergeLeft(requestHeaders);
            }

            var url = GenerateUrl();
            var preparedData = PrepareRequestData(data, isInsert, isUpdate, isUpsert);

            Hooks.Instance.NotifyOnRequestPreparedHandlers(this, _options, method, url, _serializerSettings,
                preparedData, requestHeaders);

            Debugger.Instance.Log(this,
                $"Request [{method}] at {DateTime.Now.ToLocalTime()}\n" +
                $"Headers:\n\t{JsonConvert.SerializeObject(requestHeaders)}\n" +
                $"Data:\n\t{JsonConvert.SerializeObject(preparedData)}");

            return Helpers.MakeRequest(_options, method, url, _serializerSettings, preparedData, requestHeaders,
                cancellationToken);
        }

        private Task<ModeledResponse<TU>> Send<TU>(HttpMethod method, object? data,
            Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default,
            bool isInsert = false,
            bool isUpdate = false, bool isUpsert = false) where TU : BaseModel, new()
        {
            var requestHeaders = Helpers.PrepareRequestHeaders(method, headers, _options, _rangeFrom, _rangeTo);

            if (GetHeaders != null)
                requestHeaders = GetHeaders().MergeLeft(requestHeaders);

            var url = GenerateUrl();
            var preparedData = PrepareRequestData(data, isInsert, isUpdate, isUpsert);

            Hooks.Instance.NotifyOnRequestPreparedHandlers(this, _options, method, url, _serializerSettings,
                preparedData, requestHeaders);

            Debugger.Instance.Log(this,
                $"Request [{method}] at {DateTime.Now.ToLocalTime()}\n" +
                $"Headers:\n\t{JsonConvert.SerializeObject(requestHeaders)}\n" +
                $"Data:\n\t{JsonConvert.SerializeObject(preparedData)}");

            return Helpers.MakeRequest<TU>(_options, method, url, _serializerSettings, preparedData, requestHeaders,
                GetHeaders, cancellationToken);
        }

        private static string FindTableName(object? obj = null)
        {
            var type = obj == null ? typeof(TModel) : obj is Type t ? t : obj.GetType();
            var attr = Attribute.GetCustomAttribute(type, typeof(TableAttribute));

            if (attr is TableAttribute tableAttr)
            {
                return tableAttr.Name;
            }

            return type.Name;
        }
    }
}