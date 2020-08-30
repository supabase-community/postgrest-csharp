using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using static Postgrest.Constants;

namespace Postgrest
{

    public class QueryFilter
    {
        public string Property { get; private set; }
        public Operator Op { get; private set; }
        public object Criteria { get; private set; }

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

        public QueryFilter(string property, Operator op, Range range)
        {
            switch (op)
            {
                case Operator.Overlap:
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
