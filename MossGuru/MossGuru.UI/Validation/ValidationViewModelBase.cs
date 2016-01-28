using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight;

namespace MossGuru.UI.Validation
{
  public abstract class ValidationViewModelBase : ViewModelBase, IDataErrorInfo
  {
    private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

    public new bool Set<T>(ref T field, T newValue = default(T), bool broadcast = false, [CallerMemberName] string propertyName = null)
    {
      _values[propertyName] = newValue;
      return base.Set(ref field, newValue, broadcast, propertyName);
    }

    protected virtual string OnValidate(string propertyName)
    {
      if (string.IsNullOrEmpty(propertyName))
      {
        throw new ArgumentException("Invalid property name", propertyName);
      }

      string error = string.Empty;
      var value = GetValue(propertyName);
      var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>(1);
      var result = Validator.TryValidateProperty(
          value,
          new ValidationContext(this, null, null)
          {
            MemberName = propertyName
          },
          results);

      if (!result)
      {
        var validationResult = results.First();
        error = validationResult.ErrorMessage;
      }

      return error;
    }

    string IDataErrorInfo.Error
    {
      get
      {
        throw new NotSupportedException("IDataErrorInfo.Error is not supported, use IDataErrorInfo.this[propertyName] instead.");
      }
    }

    string IDataErrorInfo.this[string propertyName]
    {
      get
      {
        return OnValidate(propertyName);
      }
    }

    private object GetValue(string propertyName)
    {
      object value;
      if (!_values.TryGetValue(propertyName, out value))
      {
        var propertyDescriptor = TypeDescriptor.GetProperties(GetType()).Find(propertyName, false);
        if (propertyDescriptor == null)
        {
          throw new ArgumentException("Invalid property name", propertyName);
        }

        value = propertyDescriptor.GetValue(this);
        _values.Add(propertyName, value);
      }

      return value;
    }
  }
}