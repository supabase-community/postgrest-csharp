using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using PostgrestTests.Models;
using Supabase.Postgrest;

namespace PostgrestTests
{
    [TestClass]
    public class EnumSerializationTests
    {
        private readonly Movie movie = new Movie { Name = "Reservoir Dogs", Status = MovieStatus.OffDisplay };
        private readonly Todo todo = new Todo { UserId = 1, Status = Todo.TodoStatus.IN_PROGRESS };

        private static string Serialize(object model, bool serializeEnumsAsStrings = false) =>
            JsonConvert.SerializeObject(model,
                Client.SerializerSettings(new ClientOptions { SerializeEnumsAsStrings = serializeEnumsAsStrings }));

        [TestMethod(DisplayName = "Enum write: serializes as its underlying integer by default (SerializeEnumsAsStrings off)")]
        public void GivenEnumPropertyWithoutJsonConverterAttribute_AndFlagDisabled_ShouldSerializeAsInteger() =>
            Assert.Contains("\"status\":1", Serialize(movie));

        [TestMethod(DisplayName = "Enum write: serializes as its string name when SerializeEnumsAsStrings is enabled")]
        public void GivenEnumPropertyWithoutJsonConverterAttribute_AndFlagEnabled_ShouldSerializeAsString() =>
            Assert.Contains("\"status\":\"OffDisplay\"", Serialize(movie, serializeEnumsAsStrings: true));

        [TestMethod(DisplayName = "Enum write: round-trips through serialize/deserialize when SerializeEnumsAsStrings is enabled")]
        public void GivenEnumPropertyWithoutJsonConverterAttribute_AndFlagEnabled_ShouldRoundTrip()
        {
            var settings = Client.SerializerSettings(new ClientOptions { SerializeEnumsAsStrings = true });
            var json = JsonConvert.SerializeObject(movie, settings);
            var roundTripped = JsonConvert.DeserializeObject<Movie>(json, settings);
            Assert.AreEqual(MovieStatus.OffDisplay, roundTripped!.Status);
        }

        [TestMethod(DisplayName = "Enum write: respects an explicit [EnumMember] mapping on a type-level [JsonConverter], regardless of SerializeEnumsAsStrings")]
        public void GivenEnumTypeWithJsonConverterAndEnumMember_ShouldSerializeMappedValue() =>
            Assert.Contains("\"status\":\"IN PROGRESS\"", Serialize(todo));
    }
}
