namespace AnimalClassifier.ErrorHandling
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Common.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;

    /// <summary>
    /// Answers a failure the services report with the status that fits it and
    /// the message it carries. Every controller shares it, so none of them
    /// catches these for itself.
    ///
    /// Any other exception is left alone and ends as a server error, because
    /// its message was not written for the caller.
    /// </summary>
    public class DomainExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is not DomainException exception)
            {
                return;
            }

            context.Result = new ObjectResult(new MessageResponse { Message = exception.Message })
            {
                StatusCode = StatusCodeFor(exception)
            };
            context.ExceptionHandled = true;
        }

        // A refusal, such as a wrong current password, is a bad request
        // rather than a 401, which would tell the frontend that the session
        // itself had ended.
        private static int StatusCodeFor(DomainException exception) => exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            AuthenticationFailedException => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status400BadRequest
        };
    }
}
