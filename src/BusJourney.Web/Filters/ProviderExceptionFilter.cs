using BusJourney.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BusJourney.Web.Filters;

/// <summary>
/// Turns <see cref="ProviderException"/> into a user-friendly response: an error page for page requests,
/// a <see cref="ProblemDetails"/> body for JSON requests (location autocomplete).
/// </summary>
public sealed class ProviderExceptionFilter(
    IModelMetadataProvider metadataProvider,
    ILogger<ProviderExceptionFilter> logger) : IExceptionFilter
{
    private const string GenericMessage = "Şu anda seferlere ulaşılamıyor. Lütfen biraz sonra tekrar deneyin.";

    private static readonly Dictionary<string, string> MessagesByStatus = new()
    {
        ["InvalidDepartureDate"] = "Seçilen tarih geçersiz.",
        ["InvalidRoute"] = "Bu güzergahta aktif sefer bulunmuyor.",
        ["InvalidLocation"] = "Seçilen lokasyon geçersiz.",
        [ProviderException.TimeoutStatus] = "İşlem zaman aşımına uğradı, lütfen tekrar deneyin.",
    };

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ProviderException exception)
        {
            return;
        }

        logger.LogError(exception, "Provider request failed with status {Status}", exception.Status);

        var message = MessagesByStatus.GetValueOrDefault(exception.Status)
            ?? exception.UserMessage
            ?? GenericMessage;

        context.Result = WantsJson(context.HttpContext.Request)
            ? new ObjectResult(new ProblemDetails { Title = message, Status = StatusCodes.Status502BadGateway })
            {
                StatusCode = StatusCodes.Status502BadGateway,
            }
            : new ViewResult
            {
                ViewName = "ProviderError",
                StatusCode = StatusCodes.Status502BadGateway,
                ViewData = new ViewDataDictionary<string>(metadataProvider, context.ModelState) { Model = message },
            };

        context.ExceptionHandled = true;
    }

    private static bool WantsJson(HttpRequest request) =>
        request.Headers.Accept.Any(value => value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
}
