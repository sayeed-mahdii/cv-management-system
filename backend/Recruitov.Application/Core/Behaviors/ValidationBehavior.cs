using ErrorOr;
using FluentValidation;
using MediatR;

namespace Recruitov.Application.Core.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var errors = failures
            .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
            .ToList();

        return CreateErrorOrResult(errors);
    }

    private static TResponse CreateErrorOrResult(List<Error> errors)
    {
        var responseType = typeof(TResponse);
        var innerType = responseType.GetGenericArguments().First();
        var errorOrType = typeof(ErrorOr<>).MakeGenericType(innerType);

        var fromErrorsMethod = errorOrType.GetMethod("From", [typeof(List<Error>)]);
        var result = fromErrorsMethod!.Invoke(null, [errors]);

        return (TResponse)result!;
    }
}
