namespace AnimalClassifier.ErrorHandling
{
    using AnimalClassifier.Core.Common.Models;
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
            var firstError = context.ModelState.Values
                .SelectMany(entry => entry.Errors)
                .Select(error => error.ErrorMessage)
                .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));

            return new BadRequestObjectResult(new MessageResponse { Message = firstError ?? ErrorMessages.InvalidRequest });
        }
    }
}
