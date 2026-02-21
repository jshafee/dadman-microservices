using FluentValidation;

namespace Catalog.Application;

public class CreateCatalogItemValidator : AbstractValidator<CreateCatalogItemCommand>
{
    public CreateCatalogItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}
