using Microsoft.AspNetCore.OpenApi;

namespace ExpensoServer.Common.Extensions;

public static class OpenApiExtensions
{
    public static OpenApiOptions CustomSchemaIds(this OpenApiOptions config,
        Func<Type, string?> typeSchemaTransformer,
        bool includeValueTypes = false)
    {
        return config.AddSchemaTransformer((schema, context, _) =>
        {
            if ((!includeValueTypes &&
                 (context.JsonTypeInfo.Type.IsValueType ||
                  context.JsonTypeInfo.Type == typeof(string) ||
                  context.JsonTypeInfo.Type == typeof(string))) || schema.Annotations == null ||
                !schema.Annotations.TryGetValue("x-schema-id", out var _))
                return Task.CompletedTask;

            var transformedTypeName = typeSchemaTransformer(context.JsonTypeInfo.Type);

            schema.Annotations["x-schema-id"] = transformedTypeName;

            schema.Title = transformedTypeName;

            return Task.CompletedTask;
        });
    }
}