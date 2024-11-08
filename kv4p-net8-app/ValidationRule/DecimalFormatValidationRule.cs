using System.Globalization;
using System.Windows.Controls;

namespace kv4p_net8_app.ValidationRule;

public class DecimalFormatValidationRule : System.Windows.Controls.ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        if (value is string input && float.TryParse(input, out var frequency))
        {
            // Ensure the input matches the "123.456" format and is within the range 144.000 - 148.000
            if (decimal.TryParse(input, out var parsed) && parsed.ToString("F3") == input)
            {
                if (frequency >= 144.000 && frequency <= 148.000)
                {
                    return ValidationResult.ValidResult;
                }
                return new ValidationResult(
                    false,
                    "Frequency must be between 144.000 and 148.000."
                );
            }
            return new ValidationResult(false, "Input must have three decimal places.");
        }
        return new ValidationResult(false, "Invalid input.");
    }
}
