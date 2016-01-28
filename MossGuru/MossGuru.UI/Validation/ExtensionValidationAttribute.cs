using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MossGuru.UI.Validation
{
  public class ExtensionValidationAttribute : ValidationAttribute
  {
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
      var val = value as string;
      if (val == null)
        return ValidationResult.Success;

      val = Regex.Replace(val, @"\s+", "");
      var valArray = val.Split(',');
      if (valArray.Any(extension => !Regex.IsMatch(extension, @"^[a-zA-Z]+$")))
      {
        return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
      }

      return ValidationResult.Success;
    }
  }
}
