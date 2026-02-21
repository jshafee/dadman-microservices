using FluentValidation;

namespace Ordering.Application;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CatalogItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
