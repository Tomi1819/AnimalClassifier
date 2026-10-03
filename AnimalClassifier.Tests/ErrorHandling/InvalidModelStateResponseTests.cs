namespace AnimalClassifier.Tests.ErrorHandling
{
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Tests.Support;
    using System.Net;
    using System.Net.Http.Json;

    public class InvalidModelStateResponseTests : ApiTest
    {
        public InvalidModelStateResponseTests(ApiFactory factory)
            : base(factory)
        {
        }

        // In the shape every other failure takes, which is the one the
        // frontend reads a message from.
        [Fact]
        public async Task ARequestTheBindingRefuses_IsAnsweredWithAMessage()
        {
            var user = await SignInAsync((await RegisterAsync()).Email);

            var response = await user.GetAsync("/api/statistics/activity?days=366");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<MessageResponse>();
            Assert.Contains("days", body!.Message);
        }
    }
}
