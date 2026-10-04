namespace AnimalClassifier.Tests.ErrorHandling
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.ErrorHandling;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Abstractions;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.AspNetCore.Routing;

    public class DomainExceptionFilterTests
    {
        private const string Message = "Written for the user.";

        public static TheoryData<DomainException, int> ExpectedStatusCodes => new()
        {
            { new RequestRefusedException(Message), StatusCodes.Status400BadRequest },
            { new NotFoundException(Message), StatusCodes.Status404NotFound },
            { new AuthenticationFailedException(Message), StatusCodes.Status401Unauthorized },
            { new ServiceBusyException(Message), StatusCodes.Status503ServiceUnavailable }
        };

        [Theory]
        [MemberData(nameof(ExpectedStatusCodes))]
        public void ADomainException_IsAnsweredWithItsStatusAndMessage(DomainException exception, int statusCode)
        {
            var context = Filter(exception);

            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.True(context.ExceptionHandled);
            Assert.Equal(statusCode, result.StatusCode);
            Assert.Equal(Message, Assert.IsType<MessageResponse>(result.Value).Message);
        }

        // Its message describes the inside of the app, so it must end as a
        // server error rather than be read out to the caller.
        [Fact]
        public void AnyOtherException_IsLeftUnhandled()
        {
            var context = Filter(new InvalidOperationException("Sequence contains no elements."));

            Assert.False(context.ExceptionHandled);
            Assert.Null(context.Result);
        }

        private static ExceptionContext Filter(Exception exception)
        {
            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            var context = new ExceptionContext(actionContext, []) { Exception = exception };

            new DomainExceptionFilter().OnException(context);

            return context;
        }
    }
}
