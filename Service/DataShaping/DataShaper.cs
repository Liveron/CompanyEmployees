using Contracts;
using Entities.Models;
using System.Dynamic;
using System.Reflection;

namespace Service.DataShaping;

public class DataShaper<T> : IDataShaper<T> where T : class
{
    public PropertyInfo[] Properties { get; set; }

    public DataShaper()
    {
        Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    }

    public IEnumerable<ShapedEntity> ShapeData(IEnumerable<T> entities, string fieldsString)
    {
        List<PropertyInfo> requiredProperties = GetRequiredProperties(fieldsString);

        return DataShaper<T>.FetchData(entities, requiredProperties);
    }

    public ShapedEntity ShapeData(T entity, string fieldsString)
    {
        List<PropertyInfo> requiredProperties = GetRequiredProperties(fieldsString);

        return DataShaper<T>.FetchDataForEntity(entity, requiredProperties);
    }

    private List<PropertyInfo> GetRequiredProperties(string fieldsString)
    {
        List<PropertyInfo> requiredProperties = [];

        if (!string.IsNullOrWhiteSpace(fieldsString))
        {
            string[] fields = fieldsString.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (string field in fields)
            {
                PropertyInfo? property = Properties.FirstOrDefault(pi =>
                    pi.Name.Equals(field.Trim(), StringComparison.InvariantCultureIgnoreCase));

                if (property is null)
                    continue;

                requiredProperties.Add(property);
            }
        }
        else
        {
            requiredProperties = [.. Properties];
        }

        return requiredProperties;
    }

    private static List<ShapedEntity> FetchData(IEnumerable<T> entities,
        IEnumerable<PropertyInfo> requiredProperties)
    {
        List<ShapedEntity> shapedData = [];

        foreach (T entity in entities)
        {
            ShapedEntity shapedObject = FetchDataForEntity(entity, requiredProperties);
            shapedData.Add(shapedObject);
        }

        return shapedData;
    }

    private static ShapedEntity FetchDataForEntity(T entity, IEnumerable<PropertyInfo> prequiredProperties)
    {
        var shapedObject = new ShapedEntity();

        foreach (PropertyInfo property in prequiredProperties)
        {
            object? objectPropertyValue = property.GetValue(entity);
            shapedObject.Entity.TryAdd(property.Name, objectPropertyValue);
        }

        PropertyInfo? objectProperty = entity.GetType().GetProperty("Id");
        shapedObject.Id = (Guid)objectProperty.GetValue(entity);

        return shapedObject;
    }
}
