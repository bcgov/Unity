using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Attributes
{
    public class MaxValueAttribute : ValidationAttribute
    {
        private readonly double _maxValue;        

        public MaxValueAttribute(double maxValue, string? propertyName = null)
        {
            _maxValue = maxValue;
            ErrorMessage = $"The value {propertyName} must be less than " + _maxValue;
        }

        public MaxValueAttribute(int maxValue, string? propertyName = null)
        {
            _maxValue = maxValue;
            ErrorMessage = $"The value {propertyName} must be less than " + _maxValue;
        }

        public override bool IsValid(object? value)
        {
            return Convert.ToDouble(value) <= _maxValue;
        }
    }
}
