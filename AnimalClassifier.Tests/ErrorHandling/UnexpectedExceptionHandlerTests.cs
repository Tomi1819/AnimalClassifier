namespace AnimalClassifier.Tests.ErrorHandling
{
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.ErrorHandling;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging.Abstractions;
    using System.Text.Json;

    public class UnexpectedExceptionHandlerTests
    {
        // Its message describes the inside of the app, so the caller is told
        // only that something went wrong.
        [Fact]
        public async Task AnUnexpectedException_IsAnsweredWithoutItsMessage()
        {
            var (statusCode, message) = await HandleAsync(new InvalidOperationException("Sequence contains no elements."));

            Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
            Assert.Equal(ErrorMessages.UnexpectedError, message);
        }

        // Kestrel's own refusal, such as of a body over the limit, keeps its
        // status rather than becoming a server error.
        [Fact]
        public async Task ARequestTooLarge_KeepsItsStatus()
        {
            var (statusCode, message) = await HandleAsync(
                new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge));

            Assert.Equal(StatusCodes.Status413PayloadTooLarge, statusCode);
            Assert.Equal(ErrorMessages.RequestTooLarge, message);
        }

        private static async Task<(int StatusCode, string Message)> HandleAsync(Exception exception)
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var handler = new UnexpectedExceptionHandler(NullLogger<UnexpectedExceptionHandler>.Instance);
            Assert.True(await handler.TryHandleAsync(context, exception, CancellationToken.None));

            context.Response.Body.Position = 0;
            var response = await JsonSerializer.DeserializeAsync<MessageResponse>(context.Response.Body, JsonSerializerOptions.Web);

            return (context.Response.StatusCode, response!.Message);
        }
    }
}
