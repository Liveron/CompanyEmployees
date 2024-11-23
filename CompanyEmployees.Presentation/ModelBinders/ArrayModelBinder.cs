using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel;
using System.Reflection;

namespace CompanyEmployees.Presentation.ModelBinders;

public class ArrayModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (!bindingContext.ModelMetadata.IsEnumerableType)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        string providedValue = bindingContext.ValueProvider
            .GetValue(bindingContext.ModelName).ToString();

        if (string.IsNullOrEmpty(providedValue))
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        Type genericType = bindingContext.ModelType
            .GetTypeInfo().GenericTypeArguments.First();
        TypeConverter converter = TypeDescriptor.GetConverter(genericType);

        object?[] obectArray = providedValue.Split(",", StringSplitOptions.RemoveEmptyEntries)
            .Select(x => converter.ConvertFromString(x.Trim())).ToArray();

        var guidArray = Array.CreateInstance(genericType, obectArray.Length);
        obectArray.CopyTo(guidArray, 0);
        bindingContext.Model = guidArray;

        bindingContext.Result = ModelBindingResult.Success(bindingContext.Model);
        return Task.CompletedTask;
    }
}
