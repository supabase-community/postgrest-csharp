using System;
using static Postgrest.Constants;

namespace Postgrest
{
    public class QueryOrderer
    {
        public string ForeignTable { get; set; }
        public string Column { get; set; }
        public Ordering Ordering { get; set; }
        public NullPosition NullPosition { get; set; }

        public QueryOrderer(string foreignTable, string column, Ordering ordering, NullPosition nullPosition)
        {
            ForeignTable = foreignTable;
            Column = column;
            Ordering = ordering;
            NullPosition = nullPosition;
        }
    }
}
