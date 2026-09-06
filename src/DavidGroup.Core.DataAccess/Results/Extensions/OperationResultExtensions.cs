using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DavidGroup.Core.DataAccess.Results.Extensions;

/// <summary>
/// Provides extension methods for converting <see cref="OperationResult"/> instances
/// to ASP.NET Core <see cref="ActionResult"/> instances.
/// </summary>
public static class OperationResultExtensions
{
    /// <summary>
    /// Converts an <see cref="OperationResult"/> to an appropriate HTTP action result
    /// based on its success state and error messages.
    /// </summary>
    /// <param name="result">The operation result to convert.</param>
    /// <param name="controller">The controller used to create the HTTP action result.</param>
    /// <returns>
    /// An <see cref="ActionResult"/> representing the outcome of the operation.
    /// Successful results return <c>200 OK</c>; known failures are mapped to their
    /// corresponding HTTP status codes.
    /// </returns>
    public static ActionResult ToActionResult(this OperationResult result, ControllerBase controller)
    {
        if (result.Succeeded)
            return controller.Ok(result);

        if (result.Messages.Any(m => m.Message == ErrorMessages.NotFound))
            return controller.NotFound(result);
        if (result.Messages.Any(m => m.Message == ErrorMessages.AlreadyExists))
            return controller.Conflict(result);
        if (result.Messages.Any(m => m.Message == ErrorMessages.InvalidInput))
            return controller.BadRequest(result);

        if (result.Messages.Any(m => m.Message == ErrorMessages.Forbidden))
            return controller.Forbid();
        if (result.Messages.Any(m => m.Message == ErrorMessages.SomethingWentWrong))
            return controller.StatusCode(StatusCodes.Status500InternalServerError, result);

        return controller.BadRequest(result);
    }
}
