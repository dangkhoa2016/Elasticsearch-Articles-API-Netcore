using FluentValidation;
using elasticsearch_netcore.ViewModels;

namespace elasticsearch_netcore.Validators
{
    public class CommentViewModelValidator : AbstractValidator<CommentViewModel>
    {
        public CommentViewModelValidator()
        {
            RuleFor(x => x.Body)
                .NotEmpty().WithMessage("Comment body is required")
                .MaximumLength(2000).WithMessage("Comment must not exceed 2000 characters");

            RuleFor(x => x.User)
                .NotEmpty().WithMessage("User is required")
                .MaximumLength(100).WithMessage("User must not exceed 100 characters");

            RuleFor(x => x.ArticleId)
                .GreaterThan(0).WithMessage("Valid Article ID is required");
        }
    }
}
