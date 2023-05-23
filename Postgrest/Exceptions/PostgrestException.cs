using System;
using System.Net.Http;

namespace Postgrest.Exceptions;

/// <summary>
/// Errors from Postgrest are wrapped by this exception
/// </summary>
public class PostgrestException : Exception
{
	public PostgrestException(string? message) : base(message) { }
	public PostgrestException(string? message, Exception? innerException) : base(message, innerException) { }

	public HttpResponseMessage? Response { get; internal set; }

	public string? Content { get; internal set; }

	public int StatusCode { get; internal set; }

	public FailureHint.Reason Reason { get; internal set; }

	public void AddReason()
	{
		Reason = FailureHint.DetectReason(this);
	}

}