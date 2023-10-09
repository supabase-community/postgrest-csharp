using Postgrest.Attributes;
using Postgrest.Models;

namespace PostgrestTests.Models;

[Table("foreign_key_test")]
public class ForeignKeyTestModel : BaseModel
{
    [PrimaryKey("id")] public int Id { get; set; }

    [Reference(typeof(Movie), foreignKey: "foreign_key_test_relation_1")]
    public Movie MovieFK1 { get; set; }

    [Reference(typeof(Movie), foreignKey: "foreign_key_test_relation_2")]
    public Movie MovieFK2 { get; set; }
}