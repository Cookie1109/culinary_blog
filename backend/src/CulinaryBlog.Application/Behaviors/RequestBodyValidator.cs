// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Behaviors;

/// <summary>Represents the request body validator.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TBody">The body type.</typeparam>
internal abstract class RequestBodyValidator<TRequest, TBody> : AbstractValidator<TRequest>
    where TRequest : IRequestWithBody<TBody>
    where TBody : class
{
    /// <summary>Initializes a new instance of the <see cref="RequestBodyValidator{TRequest, TBody}"/> class.</summary>
    /// <param name="validator">The validator supplied to this operation.</param>
    protected RequestBodyValidator(IValidator<TBody> validator)
    {
        this.RuleFor(request => request.Body).NotNull().DependentRules(() =>
            this.RuleFor(request => request.Body).CustomAsync(async (body, context, cancellationToken) =>
            {
                var result = await validator.ValidateAsync(body, cancellationToken).ConfigureAwait(false);
                foreach (var failure in result.Errors)
                {
                    // Preserve the public API's field names rather than leaking the command wrapper.
                    context.AddFailure(failure);
                }
            }));
    }
}
