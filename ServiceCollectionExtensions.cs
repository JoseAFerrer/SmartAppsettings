using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SmartAppsettings;

// TODO: it would be nice if the library could also check other classes, which could be injected through params. This is useful if the developers cannot modify the class they want to check to add the interface. This is very cool!
// TODO: it would be great if the user didn't have to actually use the method manually and it would happen automatically. This is very cool!
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Checks for null or empty configurations added to the services. It looks at all classes that have inherited the interface ISmartAppsettings and checks two levels of properties and enumerables.
    /// </summary>
    /// <param name="services"></param>
    public static void CheckForEmptyOrNullConfigurations(this IServiceCollection services)
    {
        var configObjects = services.Where(x => x.ImplementationInstance is ISmartAppsettings).Select(x => x.ImplementationInstance);

        foreach (var configObject in configObjects)
        {
            if (configObject is null)
            {
                Console.WriteLine("[INJECTION WARNING] Some custom configuration objects are null.");
                continue;
            }

            var configProperties = configObject.GetType().GetProperties();
            foreach (var property in configProperties)
            {
                var propValue = property.GetValue(configObject);
                if (propValue is null) FindRelevantNamesAndThrowException(property, configObject);

                var isString = property.PropertyType == typeof(string);
                if (isString && string.IsNullOrWhiteSpace(propValue as string)) FindRelevantNamesAndThrowException(property, configObject);

                var isArray = property.PropertyType.BaseType == typeof(Array);
                if (!isArray) continue;

                var valueAsArray = propValue as Array;
                foreach (var element in valueAsArray!) FindEmptyValuesInArrayAndThrowException(element, property, configObject);
            }
        }
    }

    private static void FindEmptyValuesInArrayAndThrowException(object? arrayElement, PropertyInfo property, object configObject)
    {
        var objectsAndValues = arrayElement?.GetType().GetProperties()
            .Select(x => new {PropInfo = x, PropValue = x.GetValue(arrayElement)});

        if (objectsAndValues != null &&
            objectsAndValues.Any(x => x.PropInfo.PropertyType == typeof(string) && string.IsNullOrWhiteSpace(x.PropValue as string)))
            FindRelevantNamesAndThrowException(property, configObject);
    }

    private static void FindRelevantNamesAndThrowException(PropertyInfo property, object configObject)
    {
        var propName = property.Name;
        var className = configObject.GetType().Name;
        var errorMessage = $"[INJECTION ERROR] Property {propName} of class {className} was injected null or empty!";
        throw new ArgumentException(errorMessage);
    }
}