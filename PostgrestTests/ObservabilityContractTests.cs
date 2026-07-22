using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using static Supabase.Postgrest.Constants;

namespace PostgrestTests
{
    /// <summary>
    /// Contract tests for the diagnostics the SDK emits through System.Diagnostics
    /// (ActivitySource/Meter "Supabase.Postgrest") and for the sanitization rule: telemetry must
    /// never contain a query string, a column filter value, row data, or other PII/secret.
    /// </summary>
    [TestClass]
    public class ObservabilityContractTests
    {
        private const string SecretFilterValue = "secret-filter-value-42";

        private readonly List<Activity> _activities = new();
        private readonly List<KeyValuePair<double, Dictionary<string, object?>>> _measurements = new();
        private ActivityListener _activityListener = null!;
        private MeterListener _meterListener = null!;
        private WireMockServer _server = null!;
        private Client _client = null!;

        [Table("todos")]
        private class Todo : BaseModel
        {
            [PrimaryKey("id", false)] public int Id { get; set; }
            [Column("name")] public string? Name { get; set; }
        }

        [TestInitialize]
        public void TestInitializer()
        {
            _server = WireMockServer.Start();
            _client = new Client(_server.Url!, new ClientOptions());

            _activityListener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == PostgrestDiagnostics.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = activity => _activities.Add(activity)
            };
            ActivitySource.AddActivityListener(_activityListener);

            _meterListener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == PostgrestDiagnostics.SourceName)
                        listener.EnableMeasurementEvents(instrument);
                }
            };
            _meterListener.SetMeasurementEventCallback<double>((_, value, tags, _) =>
            {
                var tagValues = new Dictionary<string, object?>();
                foreach (var tag in tags)
                    tagValues[tag.Key] = tag.Value;
                _measurements.Add(new KeyValuePair<double, Dictionary<string, object?>>(value, tagValues));
            });
            _meterListener.Start();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _activityListener.Dispose();
            _meterListener.Dispose();
            _server.Stop();
        }

        [TestMethod("The HTTP span records the request URL without its query string")]
        public async Task HttpSpanRecordsSanitizedUrl()
        {
            MockTodosOk();
            await FilteredGet();
            var httpSpan = SingleHttpSpan();
            Assert.AreEqual($"{_server.Url}/todos", httpSpan.GetTagItem("url.full"),
                "the query string carries column filters and their values and must never be recorded");
        }

        [TestMethod("The HTTP span follows OpenTelemetry HTTP client conventions and tags the operation")]
        public async Task HttpSpanRecordsMethodStatusAndOperation()
        {
            MockTodosOk();
            await FilteredGet();
            var httpSpan = SingleHttpSpan();
            Assert.AreEqual(ActivityKind.Client, httpSpan.Kind);
            Assert.AreEqual("GET", httpSpan.GetTagItem("http.request.method"));
            Assert.AreEqual(200, httpSpan.GetTagItem("http.response.status_code"));
            Assert.AreEqual("select", httpSpan.GetTagItem("db.operation"));
        }

        [TestMethod("A failed request marks the span as an error")]
        public async Task FailedRequestMarksTheSpanAsError()
        {
            _server.Given(Request.Create().WithPath("/todos").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(500).WithBody("{\"message\":\"boom\"}"));
            await Assert.ThrowsAsync<Supabase.Postgrest.Exceptions.PostgrestException>(FilteredGet);
            var httpSpan = SingleHttpSpan();
            Assert.AreEqual(ActivityStatusCode.Error, httpSpan.Status);
            Assert.AreEqual(500, httpSpan.GetTagItem("http.response.status_code"));
        }

        [TestMethod("The request duration histogram is recorded per HTTP request")]
        public async Task RequestDurationMetricIsRecorded()
        {
            MockTodosOk();
            await FilteredGet();
            _meterListener.RecordObservableInstruments();
            Assert.AreEqual(1, _measurements.Count);
            var measurement = _measurements.Single();
            Assert.IsTrue(measurement.Key > 0);
            Assert.AreEqual(200, measurement.Value["http.response.status_code"]);
            Assert.AreEqual("/todos", measurement.Value["url.path"]);
            Assert.AreEqual("select", measurement.Value["db.operation"]);
        }

        [TestMethod("Telemetry never contains the filter value")]
        public async Task TelemetryDoesNotLeakFilterValues()
        {
            MockTodosOk();
            await FilteredGet();
            var recorded = _activities
                .SelectMany(a => a.TagObjects)
                .Select(tag => tag.Value?.ToString() ?? "")
                .Concat(_measurements.SelectMany(m => m.Value.Values).Select(v => v?.ToString() ?? ""))
                .Concat(_activities.Select(a => a.DisplayName));
            Assert.IsFalse(recorded.Any(value => value.Contains(SecretFilterValue)),
                "no span name, tag, or metric dimension may contain a column filter value");
        }

        private Task<Supabase.Postgrest.Responses.ModeledResponse<Todo>> FilteredGet() =>
            _client.Table<Todo>().Filter("name", Operator.Equals, SecretFilterValue).Get();

        private void MockTodosOk() =>
            _server.Given(Request.Create().WithPath("/todos").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("[]"));

        private Activity SingleHttpSpan() =>
            _activities.Single(a => a.OperationName == "GET /todos");
    }
}
