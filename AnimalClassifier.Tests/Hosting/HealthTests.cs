namespace AnimalClassifier.Tests.Hosting
{
    using AnimalClassifier.Tests.Support;
    using System.Net;

    public class HealthTests : ApiTest
    {
        private const string HealthPath = "/api/health";

        public HealthTests(ApiFactory factory)
            : base(factory)
        {
        }

        // Asked by a deployment, which holds no session, and answered with the
        // verdict alone, as anyone on the internet can ask too.
        [Fact]
        public async Task WithTheDatabaseUp_TheApiSaysItIsHealthy()
        {
            var response = await Factory.CreateClient().GetAsync(HealthPath);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }
    }
}
