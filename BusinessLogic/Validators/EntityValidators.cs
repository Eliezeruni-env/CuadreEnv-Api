using FluentValidation;
using Onion.Domain.Products;
using Onion.Domain;

namespace Onion.BussinesLogic.Validators
{
    public class ProductValidator : AbstractValidator<Product>
    {
        public ProductValidator()
        {
            RuleFor(x => x.Description).NotEmpty();
            RuleFor(x => x.Cost).GreaterThan(0);
            RuleFor(x => x.MinimumQuantity).GreaterThanOrEqualTo(0);
        }
    }

    public class CompanyValidator : AbstractValidator<Company>
    {
        public CompanyValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
