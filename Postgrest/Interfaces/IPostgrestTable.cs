using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Postgrest.Models;
using Postgrest.Responses;
using Supabase.Core.Interfaces;
namespace Postgrest.Interfaces
{

	/// <summary>
	/// Client interface for Postgrest
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IPostgrestTable<T> : IGettableHeaders where T : BaseModel, new()
	{
		/// <summary>
		/// API Base Url for subsequent calls.
		/// </summary>
		string BaseUrl { get; }

		/// <summary>
		/// Name of the Table parsed by the Model.
		/// </summary>
		string TableName { get; }

		/// <summary>
		/// Generates the encoded URL with defined query parameters that will be sent to the Postgrest API.
		/// </summary>
		string GenerateUrl();

		/// <summary>
		/// Adds an AND Filter to the current query args.
		/// </summary>
		/// <param name="filters"></param>
		/// <returns></returns>
		Table<T> And(List<QueryFilter> filters);
		
		/// <summary>
		/// Clears currently defined query values.
		/// </summary>
		void Clear();
		
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
		Table<T> Columns(string[] columns);
		
		/// <summary>
		/// By using the columns query parameter it’s possible to specify the payload keys that will be inserted and ignore the rest of the payload.
		/// 
		/// The rest of the JSON keys will be ignored.
		/// Using this also has the side-effect of being more efficient for Bulk Insert since PostgREST will not process the JSON and it’ll send it directly to PostgreSQL.
		/// 
		/// See: https://postgrest.org/en/stable/api.html#specifying-columns
		/// </summary>
		/// <param name="predicate"></param>
		/// <returns></returns>
		Table<T> Columns(Expression<Func<T, object[]>> predicate);
		
		/// <summary>
		/// Returns ONLY a count from the specified query.
		///
		/// See: https://postgrest.org/en/v7.0.0/api.html?highlight=count
		/// </summary>
		/// <param name="type">The kind of count.</param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		Task<int> Count(Constants.CountType type, CancellationToken cancellationToken = default);
		
		/// <summary>
		/// Executes a delete request using the defined query params on the current instance.
		/// </summary>
		/// <returns></returns>
		Task Delete(QueryOptions? options = null, CancellationToken cancellationToken = default);
		
		/// <summary>
		/// Executes a delete request using the model's primary key as the filter for the request.
		/// </summary>
		/// <param name="model"></param>
		/// <param name="options"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		Task<ModeledResponse<T>> Delete(T model, QueryOptions? options = null, CancellationToken cancellationToken = default);
		
		/// <summary>
		/// Add a Filter to a query request
		/// </summary>
		/// <param name="columnName">Column Name in Table.</param>
		/// <param name="op">Operation to perform.</param>
		/// <param name="criterion">Value to filter with, must be a `string`, `List&lt;object&gt;`, `Dictionary&lt;string, object&gt;`, `FullTextSearchConfig`, or `Range`</param>
		/// <returns></returns>
		Table<T> Filter(string columnName, Constants.Operator op, object? criterion);
	
		/// <summary>
		/// Add a filter to a query request using a predicate to select column.
		/// </summary>
		/// <param name="predicate">Expects a columns from the Model to be returned</param>
		/// <param name="op">Operation to perform.</param>
		/// <param name="criterion">Value to filter with, must be a `string`, `List&lt;object&gt;`, `Dictionary&lt;string, object&gt;`, `FullTextSearchConfig`, or `Range`</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		Table<T> Filter(Expression<Func<T, object>> predicate, Constants.Operator op, object? criterion);
		
		/// <summary>
		/// Executes the query using the defined filters on the current instance.
		/// </summary>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		Task<ModeledResponse<T>> Get(CancellationToken cancellationToken = default);
		
		/// <summary>
		/// Executes a BULK INSERT query using the defined query params on the current instance.
		/// </summary>
		/// <param name="models"></param>
		/// <param name="options"></param>
		/// <param name="cancellationToken"></param>
		/// <returns>A typed model response from the database.</returns>
		Task<ModeledResponse<T>> Insert(ICollection<T> models, QueryOptions? options = null, CancellationToken cancellationToken = default);
		
		/// <summary>
		/// Executes an INSERT query using the defined query params on the current instance.
		/// </summary>
		/// <param name="model"></param>
		/// <param name="options"></param>
		/// <param name="cancellationToken"></param>
		/// <returns>A typed model response from the database.</returns>
		Task<ModeledResponse<T>> Insert(T model, QueryOptions? options = null, CancellationToken cancellationToken = default);
		
		/// <summary>
		/// Sets a limit with an optional foreign table reference. 
		/// </summary>
		/// <param name="limit"></param>
		/// <param name="foreignTableName"></param>
		/// <returns></returns>
		Table<T> Limit(int limit, string? foreignTableName = null);
		
		/// <summary>
		/// Finds all rows whose columns match the specified `query` object.
		/// </summary>
		/// <param name="query">The object to filter with, with column names as keys mapped to their filter values.</param>
		/// <returns></returns>
		Table<T> Match(Dictionary<string, string> query);
		
		/// <summary>
		/// Fills in query parameters based on a given model's primary key(s).
		/// </summary>
		/// <param name="model">A model with a primary key column</param>
		/// <returns></returns>
		Table<T> Match(T model);
		
		/// <summary>
		/// Adds a NOT filter to the current query args.
		/// </summary>
		/// <param name="filter"></param>
		/// <returns></returns>
		Table<T> Not(QueryFilter filter);
		
		/// <summary>
		/// Adds a NOT filter to the current query args.
		/// </summary>
		/// <param name="columnName"></param>
		/// <param name="op"></param>
		/// <param name="criteria"></param>
		/// <returns></returns>
		Table<T> Not(string columnName, Constants.Operator op, Dictionary<string, object> criteria);
		
		/// <summary>
		/// Adds a NOT filter to the current query args.
		/// Allows queries like:
		/// <code>
		/// await client.Table&lt;User&gt;().Not("status", Operators.In, new List&lt;string&gt; {"AWAY", "OFFLINE"}).Get();
		/// </code>
		/// </summary>
		/// <param name="columnName"></param>
		/// <param name="op"></param>
		/// <param name="criteria"></param>
		/// <returns></returns>
		Table<T> Not(string columnName, Constants.Operator op, List<object> criteria);
		
		/// <summary>
		/// Adds a NOT filter to the current query args.
		///
		/// Allows queries like:
		/// <code>
		/// await client.Table&lt;User&gt;().Not("status", Operators.Equal, "OFFLINE").Get();
		/// </code>
		/// </summary>
		/// <param name="columnName"></param>
		/// <param name="op"></param>
		/// <param name="criterion"></param>
		/// <returns></returns>
		Table<T> Not(string columnName, Constants.Operator op, string criterion);
		
		/// <summary>
		/// Sets an offset with an optional foreign table reference.
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="foreignTableName"></param>
		/// <returns></returns>
		Table<T> Offset(int offset, string? foreignTableName = null);
		
		/// <summary>
		/// By specifying the onConflict query parameter, you can make UPSERT work on a column(s) that has a UNIQUE constraint.
		/// </summary>
		/// <param name="columnName"></param>
		/// <returns></returns>
		Table<T> OnConflict(string columnName);
		
		/// <summary>
		/// Set an onConflict query parameter for UPSERTing on a column that has a UNIQUE constraint using a linq predicate.
		/// </summary>
		/// <param name="predicate">Expects a column from the model to be returned.</param>
		/// <returns></returns>
		Table<T> OnConflict(Expression<Func<T, object>> predicate);
		
		/// <summary>
		/// Adds a OR Filter to the current query args.
		/// </summary>
		/// <param name="filters"></param>
		/// <returns></returns>
		Table<T> Or(List<QueryFilter> filters);

		/// <summary>
		/// Adds an ordering to the current query args.
		/// 
		/// NOTE: If multiple orderings are required, chain this function with another call to <see>
		///     <cref>Order(Expression{Func{T,object}},Ordering,NullPosition)</cref>
		/// </see>
		/// .
		/// </summary>
		/// <param name="column">Column Name</param>
		/// <param name="ordering"></param>
		/// <param name="nullPosition"></param>
		/// <returns></returns>
		Table<T> Order(string column, Constants.Ordering ordering, Constants.NullPosition nullPosition = Constants.NullPosition.First);

		/// <summary>
		/// Adds an ordering to the current query args using a predicate function.
		/// 
		/// NOTE: If multiple orderings are required, chain this function with another call to <see>
		///     <cref>Order(Expression{Func{T,object}},Ordering,NullPosition)</cref>
		/// </see>
		/// .
		/// </summary>
		/// <param name="predicate"></param>
		/// <param name="ordering">>Expects a columns from the Model to be returned</param>
		/// <param name="nullPosition"></param>
		/// <returns></returns>
		Table<T> Order(Expression<Func<T, object>> predicate, Constants.Ordering ordering, Constants.NullPosition nullPosition = Constants.NullPosition.First);

		/// <summary>
		/// Adds an ordering to the current query args.
		/// 
		/// NOTE: If multiple orderings are required, chain this function with another call to <see>
		///     <cref>Order(Expression{Func{T,object}},Ordering,NullPosition)</cref>
		/// </see>
		/// .
		/// </summary>
		/// <param name="foreignTable"></param>
		/// <param name="column"></param>
		/// <param name="ordering"></param>
		/// <param name="nullPosition"></param>
		/// <returns></returns>
		Table<T> Order(string foreignTable, string column, Constants.Ordering ordering, Constants.NullPosition nullPosition = Constants.NullPosition.First);
	
		/// <summary>
		/// Sets a FROM range, similar to a `StartAt` query.
		/// </summary>
		/// <param name="from"></param>
		/// <returns></returns>
		Table<T> Range(int from);
		
		/// <summary>
		/// Sets a bounded range to the current query.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns></returns>
		Table<T> Range(int from, int to);
		
		/// <summary>
		/// Select columns for query. 
		/// </summary>
		/// <param name="columnQuery"></param>
		/// <returns></returns>
		Table<T> Select(string columnQuery);
		
		/// <summary>
		/// Select columns using a predicate function.
		/// 
		/// For example: 
		///		`Table&lt;Movie&gt;().Select(x => new[] { x.Id, x.Name, x.CreatedAt }).Get();`
		/// </summary>
		/// <param name="predicate">Expects an array of columns from the Model to be returned.</param>
		/// <returns></returns>
		Table<T> Select(Expression<Func<T, object[]>> predicate);
		
		/// <summary>
		/// Filter a query based on a predicate function. 
		/// 
		/// Note: Chaining multiple <see cref="Where(Expression{Func{T, bool}})"/> calls will
		/// be parsed as an "AND" query.
		/// 
		/// Examples:
		///		`Table&lt;Movie&gt;().Where(x =&gt; x.Name == "Top Gun").Get();`
		///		`Table&lt;Movie&gt;().Where(x =&gt; x.Name == "Top Gun" || x.Name == "Mad Max").Get();`
		///		`Table&lt;Movie&gt;().Where(x =&gt; x.Name.Contains("Gun")).Get();`
		///		`Table&lt;Movie&gt;().Where(x =&gt; x.CreatedAt &lt;= new DateTime(2022, 08, 21)).Get();`
		///		`Table&lt;Movie&gt;().Where(x =&gt; x.Id > 5 &amp;&amp; x.Name.Contains("Max")).Get();`
		/// </summary>
		/// <param name="predicate"></param>
		/// <returns></returns>
		Table<T> Where(Expression<Func<T, bool>> predicate);
		
		/// <summary>
		/// Executes a query that expects to have a single object returned, rather than returning list of models
		/// it will return a single model.
		/// </summary>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		Task<T?> Single(CancellationToken cancellationToken = default);
		
		/// <summary>
		/// Specifies a key and value to be updated. Should be combined with filters/where clauses.
		/// 
		/// Can be called multiple times to set multiple values.
		/// </summary>
		/// <param name="keySelector"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		Table<T> Set(Expression<Func<T, object>> keySelector, object? value);
		
		/// <summary>
		/// Specifies a KeyValuePair to be updated. Should be combined with filters/where clauses.
		/// 
		/// Can be called multiple times to set multiple values.
		/// </summary>
		/// <param name="keyValuePairExpression"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		Table<T> Set(Expression<Func<T, KeyValuePair<object, object?>>> keyValuePairExpression);
		
		/// <summary>
		/// Calls an Update function after `Set` has been called.
		/// </summary>
		/// <param name="options"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		Task<ModeledResponse<T>> Update(QueryOptions? options = null, CancellationToken cancellationToken = default);
	
		/// <summary>
		/// Executes an UPDATE query using the defined query params on the current instance.
		/// </summary>
		/// <param name="model"></param>
		/// <param name="options"></param>
		/// <param name="cancellationToken"></param>
		/// <returns>A typed response from the database.</returns>
		Task<ModeledResponse<T>> Update(T model, QueryOptions? options = null, CancellationToken cancellationToken = default);
		
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
		Task<ModeledResponse<T>> Upsert(ICollection<T> model, QueryOptions? options = null, CancellationToken cancellationToken = default);
		
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
		Task<ModeledResponse<T>> Upsert(T model, QueryOptions? options = null, CancellationToken cancellationToken = default);
	}
}
