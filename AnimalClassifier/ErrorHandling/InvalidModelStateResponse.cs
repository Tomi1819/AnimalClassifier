namespace AnimalClassifier.ErrorHandling
{
    using AnimalClassifier.Core.Common.Models;
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Answers a request whose body or query the model binding refused, such
    /// as a number out of its range, with the first thing wrong with it, in
    /// the same shape as every other failure.
    /// </summary>
    public static class InvalidModelStateResponse
    {
        public static IActionResult Create(ActionContext context)
        {
            if (IsLargerThanAllowed(context.HttpContext))
            {
                return new ObjectResult(new MessageResponse { Message = ErrorMessages.RequestTooLarge })
                {
                    StatusCode = StatusCodes.Status413PayloadTooLarge
                };
            }

            var firstError = context.ModelState.Values
                .SelectMany(entry => entry.Errors)
                .Select(error => error.ErrorMessage)
                .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));

            return new BadRequestObjectResult(new MessageResponse { Message = firstError ?? ErrorMessages.InvalidRequest });
        }

        // The server stops reading a body once it passes the limit, and when
        // the form is what was reading it, that reaches here as a form that
        // could not be read, with the server's own words for why.
        private static bool IsLargerThanAllowed(HttpContext context) =>
            context.Request.ContentLength > context.Features.Get<IHttpMaxRequestBodySizeFeature>()?.MaxRequestBodySize;
    }
}
