using System;

namespace TOCC.IBE.Compare.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ValidationAttribute : Attribute
    {
        /// <summary>
        /// Override to implement custom validation logic. Default returns true.
        /// </summary>
        public virtual bool IsValid(object? value) => true;
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class SkipValidationAttribute : ValidationAttribute
    {
    }
}
