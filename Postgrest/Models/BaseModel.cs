﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Exceptions;
using Supabase.Postgrest.Responses;

#pragma warning disable CS1591

namespace Supabase.Postgrest.Models
{

	/// <summary>
	/// Abstract class that must be implemented by C# Postgrest Models.
	/// </summary>
	public abstract class BaseModel
	{
		[JsonIgnore]
		public string? BaseUrl { get; set; }

		[JsonIgnore]
		public ClientOptions? RequestClientOptions { get; set; }

		[JsonIgnore]
		internal Func<Dictionary<string, string>>? GetHeaders { get; set; }


		public Task<ModeledResponse<T>> Update<T>(CancellationToken cancellationToken = default) where T : BaseModel, new()
		{
			if (BaseUrl != null)
			{
				var client = new Client(BaseUrl, RequestClientOptions)
				{
					GetHeaders = GetHeaders
				};
				return client.Table<T>().Update((T)this, cancellationToken: cancellationToken);
			}

			throw new PostgrestException("`BaseUrl` should be set in the model.");
		}

		public virtual Task Delete<T>(CancellationToken cancellationToken = default) where T : BaseModel, new()
		{
			if (BaseUrl == null)
				throw new PostgrestException("`BaseUrl` should be set in the model.") { Reason = FailureHint.Reason.ServerError };

			var client = new Client(BaseUrl, RequestClientOptions)
			{
				GetHeaders = GetHeaders
			};

			return client.Table<T>().Delete((T)this, cancellationToken: cancellationToken);
		}


		[JsonIgnore]
		public string TableName
		{
			get
			{
				var attribute = Attribute.GetCustomAttribute(GetType(), typeof(TableAttribute));

				return attribute is TableAttribute tableAttr ? tableAttr.Name : GetType().Name;
			}
		}

		/// <summary>
		/// Gets the values of the PrimaryKey columns (there can be multiple) on a model's instance as defined by the [PrimaryKey] attributes on a property on the model.
		/// </summary>
		[JsonIgnore]
		public Dictionary<PrimaryKeyAttribute, object> PrimaryKey
		{
			get
			{
				var result = new Dictionary<PrimaryKeyAttribute, object>();
				var propertyInfos = GetType().GetProperties();

				foreach (var info in propertyInfos)
				{
					var hasAttr = Attribute.GetCustomAttribute(info, typeof(PrimaryKeyAttribute));

					if (hasAttr is PrimaryKeyAttribute pka)
					{
						result.Add(pka, info.GetValue(this));
					}
				}

				if (result.Count > 0)
					return result;

				throw new PostgrestException("Models must specify their Primary Key via the [PrimaryKey] Attribute") { Reason = FailureHint.Reason.InvalidArgument };
			}
		}
	}
}
