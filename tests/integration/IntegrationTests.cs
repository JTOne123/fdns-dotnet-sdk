using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using Foundation.Sdk.Data;
using RichardSzalay.MockHttp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Foundation.Sdk.IntegrationTests
{
    // TODO: Add xunit attribute to load the Json test data the right way, like with a [JsonData] attribute on the test methods

    public class ObjectServiceIntegrationTests : IClassFixture<ObjectIntegrationFixture>
    {
        ObjectIntegrationFixture _fixture;

        public ObjectServiceIntegrationTests(ObjectIntegrationFixture fixture)
        {
            this._fixture = fixture;
        }

        private FileInfo[] GetFiles(string folder)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;

            var path = Path.Combine(basePath, "Resources", folder);
            DirectoryInfo dir = new DirectoryInfo(path);
            var jsonFiles = dir.GetFiles("*.json").OrderBy(n => n.Name).ToArray();
            return jsonFiles;
        }

        [Fact]
        public void Run()
        {
            var jsonFiles1 = GetFiles("crud");
            var jsonFiles2 = GetFiles("search");
            
            var jsonFiles = new List<FileInfo>();

            jsonFiles.AddRange(jsonFiles1.ToList());
            jsonFiles.AddRange(jsonFiles2.ToList());

            int passCount = 0;
            int failCount = 0;
            int skipCount = 0;
            int totalCount = 0;
            
            foreach (var jsonFile in jsonFiles)
            {
                int count = 1;
                var json = File.ReadAllText(jsonFile.FullName);
                JArray array = JArray.Parse(json);

                foreach (JToken token in array.Children())
                {
                    var testType = token["type"].ToString();
                    var testPurpose = token["purpose"].ToString();
                    var outcome = "SKIP";
                    var failMessage = string.Empty;
                    var failType = string.Empty;

                    try
                    {
                        if (testType == "get-by-primitive-id")
                        {
                            string id = token["id"].ToString();
                            string data = token["data"] != null ? token["data"].ToString() : string.Empty;
                            JToken expected = token["expected"];
                            int status = int.Parse(expected["status"].ToString());
                            string expectedData = (status >= 200 && status <= 299) ? expected["value"].ToString() : string.Empty;

                            foreach (var service in _fixture.Services)
                            {
                                if (token["data"] != null)
                                {
                                    var insertResult = service.InsertAsync(id, data).Result;
                                    Assert.Equal(201, insertResult.Status);
                                }

                                var retrievalResult = service.GetAsync(id).Result;
                                Assert.Equal(status, retrievalResult.Status);

                                if (token["data"] != null)
                                {
                                    Assert.Equal(expectedData, retrievalResult.Value);
                                }

                                outcome = "PASS";
                            }
                        }
                        else if (testType == "get-by-objectid")
                        {
                            string data = token["data"] != null ? token["data"].ToString() : string.Empty;
                            JToken expected = token["expected"];
                            int status = int.Parse(expected["status"].ToString());
                            string expectedData = (status >= 200 && status <= 299) ? expected["value"].ToString() : string.Empty;

                            foreach (var service in _fixture.Services)
                            {
                                string id = string.Empty;

                                if (token["data"] != null)
                                {
                                    var insertResult = service.InsertAsync(data).Result;
                                    Assert.Equal(201, insertResult.Status);

                                    JObject insertedObject = JObject.Parse(insertResult.Value);
                                    id = insertedObject["_id"]["$oid"].ToString();
                                }

                                var retrievalResult = service.GetAsync(id).Result;
                                Assert.Equal(status, retrievalResult.Status);

                                if (token["data"] != null)
                                {
                                    Assert.Equal(expectedData.Replace("%%%", "{ \"$oid\" : \"" + id + "\" }"), retrievalResult.Value);
                                }

                                outcome = "PASS";
                            }
                        }
                        else if (testType == "insert-with-primitive-id")
                        {
                            string id = token["id"].ToString();
                            string data = token["data"] != null ? token["data"].ToString() : string.Empty;
                            JToken expected = token["expected"];
                            int status = int.Parse(expected["status"].ToString());
                            string expectedData = (status >= 200 && status <= 299) ? expected["value"].ToString() : string.Empty;

                            foreach (var service in _fixture.Services)
                            {
                                if (token["data"] != null)
                                {
                                    var insertResult = service.InsertAsync(id, data).Result;
                                    Assert.Equal(status, insertResult.Status);
                                }
                                outcome = "PASS";
                            }
                        }
                        else if (testType == "insert-with-objectid")
                        {
                            string data = token["data"] != null ? token["data"].ToString() : string.Empty;
                            JToken expected = token["expected"];
                            int status = int.Parse(expected["status"].ToString());
                            string expectedData = (status >= 200 && status <= 299) ? expected["value"].ToString() : string.Empty;

                            foreach (var service in _fixture.Services)
                            {
                                string id = string.Empty;

                                if (token["data"] != null)
                                {
                                    var insertResult = service.InsertAsync(data).Result;
                                    Assert.Equal(status, insertResult.Status);

                                    if (insertResult.IsSuccess)
                                    {
                                        JObject insertedObject = JObject.Parse(insertResult.Value);
                                        id = insertedObject["_id"]["$oid"].ToString();

                                        var retrievalResult = service.GetAsync(id).Result;
                                        Assert.Equal(200, retrievalResult.Status);

                                        if (token["data"] != null)
                                        {
                                            Assert.Equal(expectedData.Replace("%%%", "{ \"$oid\" : \"" + id + "\" }"), retrievalResult.Value);
                                        }
                                    }                                   
                                }

                                outcome = "PASS";
                            }
                        }
                        else if (testType == "replace-with-primitive-id")
                        {
                            string id = token["id"].ToString();
                            JToken[] dataTokens = token["data"].ToArray();

                            JToken expected = token["expected"];
                            int status = int.Parse(expected["status"].ToString());
                            string expectedData = (status >= 200 && status <= 299) ? expected["value"].ToString() : string.Empty;

                            foreach (var service in _fixture.Services)
                            {
                                var insertResult = service.InsertAsync(id, dataTokens[0].ToString()).Result;
                                Assert.Equal(201, insertResult.Status);

                                var replaceResult = service.ReplaceAsync(id, dataTokens[1].ToString()).Result;
                                Assert.Equal(status, replaceResult.Status);
                            }

                            outcome = "PASS";
                        }
                        else if (testType == "upsert-fail-primitive-id")
                        {
                            string id = token["id"].ToString();
                            string data = token["data"] != null ? token["data"].ToString() : string.Empty;
                            JToken expected = token["expected"];
                            int status = int.Parse(expected["status"].ToString());
                            string expectedData = (status >= 200 && status <= 299) ? expected["value"].ToString() : string.Empty;

                            foreach (var service in _fixture.Services)
                            {
                                var replaceResult = service.ReplaceAsync(id, data.ToString()).Result;
                                Assert.Equal(status, replaceResult.Status);
                            }

                            outcome = "PASS";
                        }
                        else if (testType == "insert-fail-duplicate-id")
                        {
                            string id = token["id"] != null? token["id"].ToString() : string.Empty;
                            string data = token["data"] != null ? token["data"].ToString() : string.Empty;
                            JToken expected = token["expected"];
                            int expectedStatus = int.Parse(expected["status"].ToString());
                            string expectedData = (expectedStatus >= 200 && expectedStatus <= 299) ? expected["value"].ToString() : string.Empty;

                            foreach (var service in _fixture.Services)
                            {
                                var insertResult1 = service.InsertAsync(string.IsNullOrEmpty(id) ? null : id, data.ToString()).Result;
                                Assert.Equal(201, insertResult1.Status);

                                var insertResult2 = service.InsertAsync(string.IsNullOrEmpty(id) ? null : id, data.ToString()).Result;
                                Assert.Equal(expectedStatus, insertResult2.Status);
                            }

                            outcome = "PASS";
                        }
                        else if (testType == "delete-by-primitive-id")
                        {
                            string id = token["id"].ToString();
                            string data = token["data"] != null ? token["data"].ToString() : string.Empty;
                            JToken expected = token["expected"];
                            int expectedStatus = int.Parse(expected["status"].ToString());

                            foreach (var service in _fixture.Services)
                            {
                                if (token["data"] != null)
                                {
                                    var insertResult = service.InsertAsync(id, data).Result;
                                    Assert.Equal(201, insertResult.Status);

                                    var deleteResult = service.DeleteAsync(id).Result;
                                    Assert.Equal(expectedStatus, deleteResult.Status);
                                }
                                outcome = "PASS";
                            }
                        }
                        else if (testType == "delete-by-objectid")
                        {
                            string data = token["data"] != null ? token["data"].ToString() : string.Empty;
                            JToken expected = token["expected"];
                            int expectedStatus = int.Parse(expected["status"].ToString());

                            foreach (var service in _fixture.Services)
                            {
                                if (token["data"] != null)
                                {
                                    var insertResult = service.InsertAsync(data).Result;
                                    Assert.Equal(201, insertResult.Status);

                                    JObject obj = JObject.Parse(insertResult.Value);
                                    string id = obj["_id"]["$oid"].ToString();

                                    var deleteResult = service.DeleteAsync(id).Result;
                                    Assert.Equal(expectedStatus, deleteResult.Status);
                                }
                                outcome = "PASS";
                            }
                        }
                        else if (testType == "crud-combo-test-objectid")
                        {
                            JToken[] dataTokens = token["data"].ToArray();

                            foreach (var service in _fixture.Services)
                            {
                                var firstData = dataTokens[0].ToString();
                                var secondData = dataTokens[1].ToString();

                                var insertResult = service.InsertAsync(firstData).Result;
                                Assert.Equal(201, insertResult.Status);

                                JObject obj = JObject.Parse(insertResult.Value);
                                string id = obj["_id"]["$oid"].ToString();
                                
                                string insertedValue = insertResult.Value;

                                var getResult1 = service.GetAsync(id).Result;
                                Assert.Equal(200, getResult1.Status);
                                Assert.Equal(insertedValue, getResult1.Value);

                                var replaceResult = service.ReplaceAsync(id, secondData).Result;
                                Assert.Equal(200, replaceResult.Status);

                                Assert.NotEqual(insertedValue, replaceResult.Value);

                                var deleteResult = service.DeleteAsync(id).Result;
                                Assert.Equal(200, deleteResult.Status);

                                var getResult2 = service.GetAsync(id).Result;
                                Assert.Equal(404, getResult2.Status);

                                outcome = "PASS";
                            }
                        }
                        else if (testType == "crud-combo-test-primitive-id")
                        {
                            JToken[] dataTokens = token["data"].ToArray();
                            string expectedValue = token["expected"]["value"].ToString();
                            string id = token["id"].ToString();

                            foreach (var service in _fixture.Services)
                            {
                                var firstData = dataTokens[0].ToString();
                                var secondData = dataTokens[1].ToString();

                                var insertResult = service.InsertAsync(id, firstData).Result;
                                Assert.Equal(201, insertResult.Status);
                                
                                string insertedValue = insertResult.Value;

                                var getResult1 = service.GetAsync(id).Result;
                                Assert.Equal(200, getResult1.Status);
                                Assert.Equal(insertedValue, getResult1.Value);

                                var replaceResult = service.ReplaceAsync(id, secondData).Result;
                                Assert.Equal(200, replaceResult.Status);

                                Assert.NotEqual(insertedValue, replaceResult.Value);
                                Assert.Equal(expectedValue, replaceResult.Value);

                                var deleteResult = service.DeleteAsync(id).Result;
                                Assert.Equal(200, deleteResult.Status);

                                var getResult2 = service.GetAsync(id).Result;
                                Assert.Equal(404, getResult2.Status);

                                outcome = "PASS";
                            }
                        }
                        else if (testType == "find" || testType == "search")
                        {
                            JToken[] dataTokens = token["data"].ToArray();
                            JToken[] actionTokens = token["expected"].ToArray();

                            foreach (var service in _fixture.Services)
                            {
                                var deleteCollectionResult = service.DeleteCollectionAsync().Result;

                                System.Threading.Thread.Sleep(200);

                                foreach (var dataToken in dataTokens)
                                {
                                    var insertResult = service.InsertAsync(dataToken.ToString()).Result;
                                    Assert.Equal(201, insertResult.Status);
                                }

                                foreach (var actionToken in actionTokens)
                                {
                                    var expression = actionToken["expression"].ToString();
                                    var start = actionToken["start"] != null ? actionToken["start"].ToString() : "0";
                                    var limit = actionToken["limit"] != null ? actionToken["limit"].ToString() : "-1";
                                    var expectedStatus = actionToken["status"].ToString();
                                    var expectedTitlesArray = actionToken["titles"].ToArray();
                                    var expectedTitles = new List<string>();

                                    foreach(var titleToken in expectedTitlesArray)
                                    {
                                        var jvalue = ((JValue)titleToken); 
                                        var title = jvalue.Value.ToString();
                                        expectedTitles.Add(title);
                                    }

                                    var findResult = testType == "find" ? 
                                        service.FindAsync(expression, int.Parse(start), int.Parse(limit), "title", System.ComponentModel.ListSortDirection.Descending, null).Result :
                                        service.SearchAsync(expression, int.Parse(start), int.Parse(limit), "title", System.ComponentModel.ListSortDirection.Descending, null).Result;

                                    Assert.Equal(expectedStatus, findResult.Status.ToString());

                                    var foundItems = findResult.Value.Items;

                                    foreach (var foundItem in foundItems)
                                    {
                                        JObject foundObject = JObject.Parse(foundItem);
                                        string foundItemTitle = foundObject["title"].ToString();
                                        Assert.Contains(foundItemTitle, expectedTitles);
                                    }

                                    Assert.Equal(expectedTitles.Count, foundItems.Count);
                                }
                            }

                            outcome = "PASS";
                        }
                        else if (testType == "search")
                        {

                        }
                    }
                    catch (Exception ex)
                    {
                        outcome = "FAIL";
                        failMessage = ex.Message;
                        failType = ex.GetType().ToString();
                    }

                    string testDescription = $"{outcome} : {jsonFile.Name} : T#{count.ToString("D2")} : {testType} : {testPurpose}";
                    if (outcome == "FAIL")
                    {
                        testDescription += ". Fail type: " + failType + ". Reason: " + failMessage;
                    }
                    Console.WriteLine(testDescription);
                    
                    if (outcome.StartsWith("FAIL")) failCount++;
                    if (outcome.StartsWith("PASS")) passCount++;
                    if (outcome.StartsWith("SKIP")) skipCount++;

                    count++;
                    totalCount++;
                }
            }

            Console.WriteLine();
            string testOutcomeDescription = $"Total tests: {totalCount}. Passed: {passCount}. Failed: {failCount}. Skipped: {skipCount}.";
            Console.WriteLine(testOutcomeDescription);

            Assert.Equal(0, failCount);
        }
    }

    public class ObjectIntegrationFixture : IDisposable
    {
        private ILogger<MongoService> MongoBookLogger { get; set; }
        private ILogger<HttpObjectService> HttpBookLogger { get; set; }
        private HttpClient Client { get; set; }
        private IHttpClientFactory ClientFactory { get; set; }
        private IObjectService MongoBookService { get; set; }
        private IObjectService HttpBookService { get; set; }
        private IMongoClient MongoClient { get; set; }
        public List<IObjectService> Services { get; private set; } = new List<IObjectService>();

        public JsonSerializerSettings JsonSerializerSettings { get; set; } = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public ObjectIntegrationFixture()
        {
            Client = new HttpClient();
            Client.BaseAddress = new Uri("http://localhost:9090/api/1.0/");

            var mock = new Mock<IHttpClientFactory>();
            mock.CallBase = true;
            mock.Setup(x => x.CreateClient($"unittests-{Common.OBJECT_SERVICE_NAME}")).Returns(Client);

            ClientFactory = mock.Object;
            MongoClient = new MongoClient("mongodb://localhost:27017");

            MongoBookLogger = new Mock<ILogger<MongoService>>().Object;
            HttpBookLogger = new Mock<ILogger<HttpObjectService>>().Object;

            var guid1 = System.Guid.NewGuid().ToString().Replace("-", string.Empty);
            var guid2 = System.Guid.NewGuid().ToString().Replace("-", string.Empty);

            MongoBookService = new MongoService(MongoClient, "bookstore", "booksMongo" + guid1, MongoBookLogger);
            HttpBookService = new HttpObjectService("unittests", "bookstore", "booksHttp" + guid2, ClientFactory, HttpBookLogger);

            Services.Add(MongoBookService);
            Services.Add(HttpBookService);

            foreach (var service in Services)
            {
                service.DeleteCollectionAsync();
            }
        }

        public void Dispose()
        {
            foreach (var service in Services)
            {
                service.DeleteCollectionAsync();
            }

            Client.Dispose();
        }
    }
}