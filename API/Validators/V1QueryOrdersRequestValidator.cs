using FluentValidation;
using Microsoft.IdentityModel.Tokens;
using Models.Dto.V1.Requests;

namespace API.Validators;
public class V1QueryOrdersRequestValidator : AbstractValidator<V1QueryOrdersRequest>
{
    public V1QueryOrdersRequestValidator()
    {
        RuleFor(x => x).Must(x => !x.Ids.IsNullOrEmpty() || !x.CustomerIds.IsNullOrEmpty()).WithMessage("CustomerId or Ids is required");
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Page number cannot be negative");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Page size cannot be negative")
            .LessThanOrEqualTo(500)
            .WithMessage("Page size should be less than 500");

        When(x => x.Page > 0, () =>
        {
            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .WithMessage("Page size is required when using pagination");
        });

        When(x => x.PageSize > 0, () =>
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Page number is required when page size is set");
        });

        When(x => x.Ids != null && x.Ids.Length > 0, () =>
        {
            RuleFor(x => x.Ids)
                .Must(ids => ids.All(id => id > 0))
                .WithMessage("All order IDs must be positive numbers")
                .Must(ids => ids.Length <= 200)
                .WithMessage("Maximum of 200 order IDs allowed per request");
        });

        When(x => x.CustomerIds != null && x.CustomerIds.Length > 0, () =>
        {
            RuleFor(x => x.CustomerIds)
                .Must(ids => ids.All(id => id > 0))
                .WithMessage("All customer IDs must be positive numbers")
                .Must(ids => ids.Length <= 200)
                .WithMessage("Maximum of 200 customer IDs allowed per request");
        });
    }
}