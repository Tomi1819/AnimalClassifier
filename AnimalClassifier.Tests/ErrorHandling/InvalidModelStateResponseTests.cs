namespace AnimalClassifier.Tests.ErrorHandling
{
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.ErrorHandling;
    using AnimalClassifier.Tests.Support;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Abstractions;
    using Microsoft.AspNetCore.Routing;
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

        // The server stops reading a form once it passes the limit, which the
        // binding reports in the server's own words, as a bad request.
        [Fact]
        public void AFormCutOffForItsSize_IsAnsweredAsTooLarge()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentLength = 2048;
            httpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(new RequestBodyLimit { MaxRequestBodySize = 1024 });
            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            context.ModelState.AddModelError(string.Empty, "Failed to read the request form. Request body too large.");

            var result = Assert.IsType<ObjectResult>(InvalidModelStateResponse.Create(context));

            Assert.Equal(StatusCodes.Status413PayloadTooLarge, result.StatusCode);
            Assert.Equal(ErrorMessages.RequestTooLarge, Assert.IsType<MessageResponse>(result.Value).Message);
        }

        private sealed class RequestBodyLimit : IHttpMaxRequestBodySizeFeature
        {
            public bool IsReadOnly => false;

            public long? MaxRequestBodySize { get; set; }
        }
    }
}
