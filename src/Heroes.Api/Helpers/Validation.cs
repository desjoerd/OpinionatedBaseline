using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Heroes.Api.Helpers;

public static class Validation
{
    public static bool TryValidate<T>(T instance, [NotNullWhen(false)] out IDictionary<string, string[]>? errors)
        where T : notnull
    {
        var validationContext = new ValidationContext(instance);
        var validationResult = new List<ValidationResult>();
        if (!Validator.TryValidateObject(instance, validationContext, validationResult))
        {
            errors = validationResult.GroupBy(x => x.MemberNames.FirstOrDefault() ?? string.Empty)
                .ToDictionary(x => x.Key, x => x.Select(y => y.ErrorMessage!).ToArray());
            return false;
        }

        errors = null;
        return true;
    }
}