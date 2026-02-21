using FluentValidation.Results;

namespace BuildingBlocks.Validation;

public static class ValidationResultExtensions
{
    public static IDictionary<string, string[]> ToValidationProblems(this ValidationResult result)
    {
        return result.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());
    }
}
