using FluentValidation;
using elasticsearch_netcore.ViewModels;

namespace elasticsearch_netcore.Validators
{
    public class AuthorViewModelValidator : AbstractValidator<AuthorViewModel>
    {
        public AuthorViewModelValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

            RuleFor(x => x.LastName)
                .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");
        }
    }
}
