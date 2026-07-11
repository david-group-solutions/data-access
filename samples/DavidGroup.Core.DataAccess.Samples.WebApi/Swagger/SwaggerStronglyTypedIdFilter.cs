using System.ComponentModel;
using System.Reflection;

using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace DavidGroup.Core.DataAccess.Samples.WebApi.Swagger;

public class SwaggerStronglyTypedIdFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concrete) return;

        if (!context.Type.Name.EndsWith("Id") ||
            !context.Type.IsValueType ||
            context.Type.GetCustomAttribute<TypeConverterAttribute>() is not { } attr ||
            Type.GetType(attr.ConverterTypeName) is not { } type)
        {
            return;
        }

        if (Activator.CreateInstance(type) is not TypeConverter converter) return;

        if (converter.CanConvertTo(typeof(Guid)) || converter.CanConvertTo(typeof(string)))
            concrete.Type = JsonSchemaType.String;
        else if (converter.CanConvertTo(typeof(int)) || converter.CanConvertTo(typeof(long)))
            concrete.Type = JsonSchemaType.Integer;
    }
}
