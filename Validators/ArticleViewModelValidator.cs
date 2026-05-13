using FluentValidation;
using elasticsearch_netcore.ViewModels;

namespace elasticsearch_netcore.Validators
{
    public class ArticleViewModelValidator : AbstractValidator<ArticleViewModel>
    {
        public ArticleViewModelValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(500).WithMessage("Title must not exceed 500 characters");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Content is required");

            RuleFor(x => x.Abstract)
                .MaximumLength(1000).WithMessage("Abstract must not exceed 1000 characters");

            RuleFor(x => x.Shares)
                .GreaterThanOrEqualTo(0).WithMessage("Shares must be non-negative");
        }
    }
}
