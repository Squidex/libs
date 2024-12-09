// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Squidex.Text;

namespace Squidex.Flows;

public sealed class FlowStepRegistry
{
    public Dictionary<string, FlowStepDescriptor> Steps { get; } = [];

    public FlowStepRegistry Add<T>() where T : IFlowStep
    {
        return Add(typeof(T));
    }

    private FlowStepRegistry Add(Type stepType)
    {
        var metadata = stepType.GetCustomAttribute<FlowStepAttribute>();

        if (metadata == null)
        {
            return this;
        }

        var name = GetStepName(stepType);

        var definition =
            new FlowStepDescriptor
            {
                Type = stepType,
                Description = metadata.Description,
                Display = metadata.Display,
                IconColor = metadata.IconColor,
                IconImage = metadata.IconImage,
                ReadMore = metadata.ReadMore,
                Title = metadata.Title,
            };

        foreach (var property in stepType.GetProperties())
        {
            if (property.CanRead && property.CanWrite)
            {
                var stepProperty = new FlowStepPropertyDescriptor
                {
                    Name = property.Name.ToCamelCase()
                };

                var display = property.GetCustomAttribute<DisplayAttribute>();

                if (!string.IsNullOrWhiteSpace(display?.Name))
                {
                    stepProperty.Display = display.Name;
                }
                else
                {
                    stepProperty.Display = property.Name;
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

                stepProperty.IsFormattable |= property.GetCustomAttribute<ExpressionAttribute>() != null;
                stepProperty.IsScript |= property.GetCustomAttribute<ScriptAttribute>() != null;

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
        }

        Steps[name] = definition;

        return this;
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
