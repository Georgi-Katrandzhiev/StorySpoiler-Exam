using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;

namespace StorySpoiler
{
    [TestFixture]
    public class StorySpoilerTests
    {
        private RestClient client;
        private string? createdStoryId;
        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("georgi_k_123", "georgi123456789");

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private static string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new { username, password });

            var response = loginClient.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            return json.GetProperty("accessToken").GetString() ?? string.Empty;
        }

        [Test, Order(1)]
        public void CreateStorySpoiler_ShouldReturnCreated()
        {
            var newSpoiler = new
            {
                title = "Test Story",
                description = "This is a test spoiler",
                url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(newSpoiler);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            createdStoryId = json.GetProperty("storyId").GetString();
            Assert.That(createdStoryId, Is.Not.Null);

            var message = json.GetProperty("msg").GetString();
            Assert.That(message, Is.EqualTo("Successfully created!"));
        }

        [Test, Order(2)]
        public void EditStoryTitle_ShouldReturnOk()
        {
            var updatedSpoiler = new
            {
                title = "Updated Story",
                description = "This is an updated spoiler",
                url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{createdStoryId}", Method.Put);
            request.AddJsonBody(updatedSpoiler);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            var message = json.GetProperty("msg").GetString();
            Assert.That(message, Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllStorySpoilers_ShouldReturnList()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Is.Not.Empty);
        }

        [Test, Order(4)]
        public void DeleteStorySpoiler_ShouldReturnOk()
        {
            var request = new RestRequest($"/api/Story/Delete/{createdStoryId}", Method.Delete);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Deleted successfully!"));
        }

        [Test, Order(5)]
        public void CreateStorySpoiler_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var spoiler = new
            {
                title = "",
                description = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(spoiler);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void Edit_NonExistingStorySpoiler_ShouldReturnNotFound()
        {
            var fakeId = "1-2-3";

            var editedSpoiler = new
            {
                title = "Edited Title",
                description = "Edited Description",
                url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{fakeId}", Method.Put);
            request.AddJsonBody(editedSpoiler);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No spoilers..."));
        }

        [Test, Order(7)]
        public void Delete_NonExistingStorySpoiler_ShouldReturnBadRequest()
        {
            var fakeId = "1-2-3";

            var request = new RestRequest($"/api/Story/Delete/{fakeId}", Method.Delete);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to delete this story spoiler!"));
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            client?.Dispose();
        }
    }
}