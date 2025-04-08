using Avancira.Application.Identity.Roles.Dtos;
using FluentValidation;
public class CreateOrUpdateRoleValidator : AbstractValidator<CreateOrUpdateRoleDto>
{
    public CreateOrUpdateRoleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Role name is required.");
    }
}
