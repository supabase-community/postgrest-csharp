using static Supabase.Postgrest.Constants;
#pragma warning disable CS1591

namespace Supabase.Postgrest
{

	public class QueryOrderer
	{
		public string? ForeignTable { get; }
		public string Column { get; }
		public Constants.Ordering Ordering { get; }
		public Constants.NullPosition NullPosition { get; }

		public QueryOrderer(string? foreignTable, string column, Constants.Ordering ordering, Constants.NullPosition nullPosition)
		{
			ForeignTable = foreignTable;
			Column = column;
			Ordering = ordering;
			NullPosition = nullPosition;
		}
	}
}
