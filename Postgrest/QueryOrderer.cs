using static Postgrest.Constants;

namespace Postgrest
{
    public class QueryOrderer
    {
        public string ForeignTable { get; }
        public string Column { get; }
        public Ordering Ordering { get; }
        public NullPosition NullPosition { get; }

        public QueryOrderer(string foreignTable, string column, Ordering ordering, NullPosition nullPosition)
        {
            ForeignTable = foreignTable;
            Column = column;
            Ordering = ordering;
            NullPosition = nullPosition;
        }
    }
}
