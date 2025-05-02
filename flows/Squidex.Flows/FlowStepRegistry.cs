// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Options;
using Squidex.Text;

namespace Squidex.Flows;

public sealed class FlowStepRegistry : IFlowStepRegistry
{
    private readonly Dictionary<string, FlowStepDescriptor> steps = [];

    public IReadOnlyDictionary<string, FlowStepDescriptor> Steps => steps;

    public FlowStepRegistry(IOptions<FlowOptions> options)
    {
        foreach (var type in options.Value.Steps)
        {
            Add(type);
        }
    }

    private FlowStepRegistry Add(Type stepType)
    {
        var metadata = stepType.GetCustomAttribute<FlowStepAttribute>();

        if (metadata == null)
        {
            return this;
        }

        var stepName = GetStepName(stepType);
        var stepObsolete = stepType.GetCustomAttribute<ObsoleteAttribute>();

        var definition =
            new FlowStepDescriptor
            {
                Type = stepType,
                Description = metadata.Description,
                Display = metadata.Display,
                IconColor = metadata.IconColor,
                IconImage = metadata.IconImage,
                IsObsolete = stepObsolete != null,
                ObsoleteReason = stepObsolete?.Message,
                ReadMore = metadata.ReadMore,
                Title = metadata.Title,
            };

        foreach (var property in stepType.GetProperties())
        {
            if (!property.CanRead || !property.CanWrite)
            {
                continue;
            }

            if (property.GetCustomAttribute<ComputedAttribute>() != null)
            {
                continue;
            }

            var propertyObsolete = property.GetCustomAttribute<ObsoleteAttribute>();

            var stepProperty = new FlowStepPropertyDescriptor
            {
                Name = property.Name.ToCamelCase(),
                IsFormattable = property.GetCustomAttribute<ExpressionAttribute>() != null,
                IsObsolete = propertyObsolete != null,
                IsScript = property.GetCustomAttribute<ScriptAttribute>() != null,
                ObsoleteReason = propertyObsolete?.Message,
            };

            var display = property.GetCustomAttribute<DisplayAttribute>();

            if (!string.IsNullOrWhiteSpace(display?.Name))
            {
                stepProperty.Display = display.Name;
            }
            else
            {
                stepProperty.Display = GetDisplayName(property);
            }

            if (!string.IsNullOrWhiteSpace(display?.Description))
            {
                stepProperty.Description = display.Description;
            }

            var type = GetType(property);

            if (!IsNullable(property.PropertyType))
            {
                stepProperty.IsRequired |= GetDataAttribute<RequiredAttribute>(property) != null;
                stepProperty.IsRequired |= type.IsValueType && !IsBoolean(type) && !type.IsEnum;
            }

            if (type.IsEnum)
            {
                var values = Enum.GetNames(type);

                stepProperty.Options = values;
                stepProperty.Editor = FlowStepEditor.Dropdown;
            }
            else if (IsBoolean(type))
            {
                stepProperty.Editor = FlowStepEditor.Checkbox;
            }
            else if (IsNumericType(type))
            {
                stepProperty.Editor = FlowStepEditor.Number;
            }
            else
            {
                stepProperty.Editor = GetEditor(property);
            }

            definition.Properties.Add(stepProperty);
        }

        steps[stepName] = definition;

        return this;
    }

    private static string GetDisplayName(PropertyInfo property)
    {
        var sb = new StringBuilder();
        var pv = (char)0;
        foreach (var c in property.Name)
        {
            if ((char.IsNumber(pv) || char.IsLower(pv)) && char.IsUpper(c))
            {
                sb.Append(' ');
            }
            else if (!char.IsNumber(pv) && char.IsNumber(c))
            {
                sb.Append(' ');
            }

            sb.Append(c);
            pv = c;
        }

        return sb.ToString();
    }

    private static T? GetDataAttribute<T>(PropertyInfo property) where T : ValidationAttribute
    {
        var result = property.GetCustomAttribute<T>();

        result?.IsValid(null);

        return result;
    }

    private static string GetEditor(PropertyInfo property)
    {
        return property.GetCustomAttribute<EditorAttribute>()?.Editor ?? FlowStepEditor.Text;
    }

    private static bool IsNullable(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    private static bool IsBoolean(Type type)
    {
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Boolean:
                return true;
            default:
                return false;
        }
    }

    private static bool IsNumericType(Type type)
    {
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Single:
                return true;
            default:
                return false;
        }
    }

    private static Type GetType(PropertyInfo property)
    {
        var type = property.PropertyType;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            type = type.GetGenericArguments()[0];
        }

        return type;
    }

    private static string GetStepName(Type type)
    {
        var typeName = type.Name;

        string[] suffixes = ["FlowStep", "Step"];
        foreach (var suffix in suffixes)
        {
            if (typeName.EndsWith(suffix, StringComparison.Ordinal))
            {
                typeName = typeName[..^suffix.Length];
                break;
            }
        }

        return typeName;
    }
}
