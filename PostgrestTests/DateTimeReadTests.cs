using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Supabase.Postgrest;
using PostgrestTests.Models;

namespace PostgrestTests;

[TestClass]
public class DateTimeReadTests
{
    private static KitchenSink Deserialize(string json) =>
        JsonConvert.DeserializeObject<KitchenSink>(json, Client.SerializerSettings())!;

    private static DateTime ParseUtc(string wireValue) =>
        DateTimeOffset.Parse(wireValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).UtcDateTime;

    [DataTestMethod]
    [DataRow("2024-06-26T18:30:45.1234560+00:00", DisplayName = "UTC (+00:00)")]
    [DataRow("2024-06-26T20:30:45.1234560+02:00", DisplayName = "+02:00")]
    public void GivenTimestamptzWithSubSeconds_ShouldPreserveInstantAndPrecisionOnRead(string wireValue)
    {
        var model = Deserialize($"{{\"datetime_value\":\"{wireValue}\"}}");
        Assert.IsTrue(model.DateTimeValue.HasValue);
        Assert.AreEqual(ParseUtc(wireValue), model.DateTimeValue!.Value.ToUniversalTime());
    }

    [DataTestMethod]
    [DataRow("2024-06-26T18:30:45.1234560+00:00", DisplayName = "UTC (+00:00)")]
    [DataRow("2024-06-26T20:30:45.1234560+02:00", DisplayName = "+02:00")]
    public void GivenTimestamptz_ShouldNotReturnUnspecifiedKindOnRead(string wireValue)
    {
        var model = Deserialize($"{{\"datetime_value\":\"{wireValue}\"}}");
        Assert.AreNotEqual(DateTimeKind.Unspecified, model.DateTimeValue!.Value.Kind);
    }

    [DataTestMethod]
    [DataRow("2024-06-26T18:30:45.1234560+00:00", DisplayName = "UTC (+00:00)")]
    [DataRow("2024-06-26T20:30:45.1234560+02:00", DisplayName = "+02:00")]
    public void GivenTimestamptzList_ShouldPreserveEachInstantOnRead(string wireValue)
    {
        var model = Deserialize($"{{\"list_of_datetimes\":[\"{wireValue}\"]}}");
        Assert.IsNotNull(model.ListOfDateTimes);
        Assert.AreEqual(1, model.ListOfDateTimes!.Count);
        Assert.AreEqual(ParseUtc(wireValue), model.ListOfDateTimes[0].ToUniversalTime());
    }
}