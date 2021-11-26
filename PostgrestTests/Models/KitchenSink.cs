using System;
using System.Collections.Generic;
using Postgrest;
using Postgrest.Attributes;
using Postgrest.Models;

namespace PostgrestTests.Models
{
    [Table("kitchen_sink")]
    public class KitchenSink : BaseModel
    {
        [Column("string_value")]
        public string StringValue { get; set; }

        [Column("int_value")]
        public int IntValue { get; set; }

        [Column("float_value")]
        public float FloatValue { get; set; }

        [Column("double_value")]
        public double DoubleValue { get; set; }

        [Column("datetime_value")]
        public DateTime DateTimeValue { get; set; }

        [Column("list_of_strings")]
        public List<string> ListOfStrings { get; set; }

        [Column("list_of_datetimes")]
        public List<DateTime> ListOfDateTimes { get; set; }

        [Column("list_of_ints")]
        public List<int> ListOfInts { get; set; }

        [Column("list_of_floats")]
        public List<float> ListOfFloats { get; set; }

        [Column("int_range")]
        public IntRange IntRange { get; set; }
    }
}
