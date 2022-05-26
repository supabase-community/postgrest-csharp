using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using static Postgrest.Constants;

namespace Postgrest
{
    public class QueryFilter
    {
        /// <summary>
        /// String value to be subsituted for a null criterion
        /// </summary>
        public const string NullVal = "null";

        public string Property { get; private set; }
        public Operator Op { get; private set; }
        public object Criteria { get; private set; }

        /// <summary>
        /// Contructor to use single value filtering.
        /// </summary>
        /// <param name="property">Column name</param>
        /// <param name="op">Operation: And, Equals, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual, NotEqual, Is, Adjacent, Not, Like, ILike</param>
        /// <param name="criteria"></param>
        public QueryFilter(string property, Operator op, object criteria)
        {
            switch (op)
            {
                case Operator.And:
                case Operator.Equals:
                case Operator.GreaterThan:
                case Operator.GreaterThanOrEqual:
                case Operator.LessThan:
                case Operator.LessThanOrEqual:
                case Operator.NotEqual:
                case Operator.Is:
                case Operator.Adjacent:
                case Operator.Not:
                case Operator.Like:
                case Operator.ILike:
                    Property = property;
                    Op = op;
                    Criteria = criteria;
                    break;
                default:
                    throw new Exception("Advanced filters require a constructor with more specific arguments");
            }

        }

        /// <summary>
        /// Constructor to use multiple values as for filtering.
        /// </summary>
        /// <param name="property">Column name</param>
        /// <param name="op">Operation: In, Contains, ContainedIn, or Overlap</param>
        /// <param name="criteria"></param>
        public QueryFilter(string property, Operator op, List<object> criteria)
        {
            switch (op)
            {
                case Operator.In:
                case Operator.Contains:
                case Operator.ContainedIn:
                case Operator.Overlap:
                    Property = property;
                    Op = op;
                    Criteria = criteria;
                    break;
                default:
                    throw new Exception("List constructor must be used with filter that accepts an array of arguments.");
            }
        }

        /// <summary>
        /// Constructor to use multiple values as for filtering (using a dictionary).
        /// </summary>
        /// <param name="property">Column name</param>
        /// <param name="op">Operation: In, Contains, ContainedIn, or Overlap</param>
        /// <param name="criteria"></param>
        public QueryFilter(string property, Operator op, Dictionary<string, object> criteria)
        {
            switch (op)
            {
                case Operator.In:
                case Operator.Contains:
                case Operator.ContainedIn:
                case Operator.Overlap:
                    Property = property;
                    Op = op;
                    Criteria = criteria;
                    break;
                default:
                    throw new Exception("List constructor must be used with filter that accepts an array of arguments.");
            }
        }

        /// <summary>
        /// Constructor for Full Text Search.
        /// </summary>
        /// <param name="property">Column Name</param>
        /// <param name="op">Operation: FTS, PHFTS, PLFTS, WFTS</param>
        /// <param name="fullTextSearchConfig"></param>
        public QueryFilter(string property, Operator op, FullTextSearchConfig fullTextSearchConfig)
        {
            switch (op)
            {
                case Operator.FTS:
                case Operator.PHFTS:
                case Operator.PLFTS:
                case Operator.WFTS:
                    Property = property;
                    Op = op;
                    Criteria = fullTextSearchConfig;
                    break;
                default:
                    throw new Exception("Constructor must be called with a full text search operator");
            }
        }

        /// <summary>
        /// Constructor for Range Queries.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="op">Operator: Overlap, StrictlyLeft, StrictlyRight, NotRightOf, NotLeftOf, Adjacent</param>
        /// <param name="range"></param>
        public QueryFilter(string property, Operator op, IntRange range)
        {
            switch (op)
            {
                case Operator.Overlap:
                case Operator.Contains:
                case Operator.ContainedIn:
                case Operator.StrictlyLeft:
                case Operator.StrictlyRight:
                case Operator.NotRightOf:
                case Operator.NotLeftOf:
                case Operator.Adjacent:
                    Property = property;
                    Op = op;
                    Criteria = range;
                    break;
                default:
                    throw new Exception("Constructor must be called with a filter that accepts a range");
            }
        }

        /// <summary>
        /// Constructor to enable `AND` and `OR` Queries by allowing nested QueryFilters.
        /// </summary>
        /// <param name="op">Operation: And, Or</param>
        /// <param name="filters"></param>
        public QueryFilter(Operator op, List<QueryFilter> filters)
        {
            switch (op)
            {
                case Operator.Or:
                case Operator.And:
                    Op = op;
                    Criteria = filters;
                    break;
                default:
                    throw new Exception("Contructor can only be used with `or` or `and` filters");
            }
        }

        /// <summary>
        /// Constructor to enable `NOT` functionality
        /// </summary>
        /// <param name="op">Operation: Not.</param>
        /// <param name="filter"></param>
        public QueryFilter(Operator op, QueryFilter filter)
        {
            switch (op)
            {
                case Operator.Not:
                    Op = op;
                    Criteria = filter;
                    break;
                default:
                    throw new Exception("Contructor can only be used with `not` filter");
            }
        }
    }

    /// <summary>
    /// Configuration Object for Full Text Search.
    /// API Reference: http://postgrest.org/en/v7.0.0/api.html?highlight=full%20text%20search#full-text-search
    /// </summary>
    public class FullTextSearchConfig
    {
        [JsonProperty("queryText")]
        public string QueryText { get; private set; }

        [JsonProperty("config")]
        public string Config { get; private set; } = "english";

        public FullTextSearchConfig(string queryText, string config)
        {
            QueryText = queryText;
            Config = config;
        }
    }
}
