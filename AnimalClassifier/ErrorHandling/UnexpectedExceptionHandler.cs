namespace AnimalClassifier.ErrorHandling
{
    using AnimalClassifier.Core.Common.Models;
    using Microsoft.AspNetCore.Diagnostics;

    /// <summary>
    /// Answers whatever got past <see cref="DomainExceptionFilter"/>, which is
    /// everything no service meant for the caller, in the same shape as every
    /// other failure, so that the frontend always has a message to show.
    ///
    /// The exception's own message describes the inside of the app, so it is
    /// logged and never sent.
    /// </summary>
    public class UnexpectedExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<UnexpectedExceptionHandler> logger;

        public UnexpectedExceptionHandler(ILogger<UnexpectedExceptionHandler> logger)
        {
            this.logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (statusCode, message) = Describe(exception);

            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(new MessageResponse { Message = message }, cancellationToken);

            return true;
        }

        // A request Kestrel could not read, such as one larger than allowed,
        // is the caller's doing and carries its own status. Anything else is
        // a fault in the app.
        private (int StatusCode, string Message) Describe(Exception exception)
        {
            if (exception is BadHttpRequestException badRequest)
            {
                logger.LogInformation(exception, "Refused a request that could not be read.");

                return (badRequest.StatusCode, badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge
                    ? ErrorMessages.RequestTooLarge
                    : ErrorMessages.InvalidRequest);
            }

            logger.LogError(exception, "A request failed with an unexpected exception.");

            return (StatusCodes.Status500InternalServerError, ErrorMessages.UnexpectedError);
        }
    }
}
