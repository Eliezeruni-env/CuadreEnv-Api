using FluentValidation;
using Onion.BussinesLogic.Dtos;

namespace Onion.BussinesLogic.Validators
{
    public class SaleDetailDtoValidator : AbstractValidator<SaleDetailDto>
    {
        public SaleDetailDtoValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0);
            RuleFor(x => x.Quantity).GreaterThan(0);
            RuleFor(x => x.UnitPrice).GreaterThan(0);
        }
    }

    public class SaleRequestDtoValidator : AbstractValidator<SaleRequestDto>
    {
        public SaleRequestDtoValidator()
        {
            RuleFor(x => x.Details).NotEmpty().WithMessage("Sale must contain at least one item.");
            RuleForEach(x => x.Details).SetValidator(new SaleDetailDtoValidator());
            RuleFor(x => x.Total).GreaterThanOrEqualTo(0);
            RuleFor(x => x.PaidAmount).GreaterThanOrEqualTo(0);
        }
    }
}
