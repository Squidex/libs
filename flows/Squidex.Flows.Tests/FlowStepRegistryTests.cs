// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

#pragma warning disable CA1041 // Provide ObsoleteAttribute message
#pragma warning disable CS0612 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete

namespace Squidex.Flows;

public class FlowStepRegistryTests
{
    private readonly FlowStepRegistry sut;

    public FlowStepRegistryTests()
    {
        sut = new FlowStepRegistry(Options.Create(new FlowOptions
        {
            Steps = [typeof(CombinedStep), typeof(ObsoleteStep)],
        }));
    }

    public enum StepEnum
    {
        Yes,
        No,
    }

    [FlowStep(
        Title = "CombinedStep",
        IconImage = "<svg></svg>",
        IconColor = "#1e5470",
        Display = "Step display",
        Description = "Step description.",
        ReadMore = "https://www.readmore.com/")]
    public record CombinedStep : FlowStep
    {
        [Required]
        [Display(Name = "Url Name", Description = "Url Description")]
        [Editor(FlowStepEditor.Url)]
        [Expression]
        public Uri Url { get; set; }

        [Editor("Custom")]
        [Script]
        public string Script { get; set; }

        [Editor(FlowStepEditor.Text)]
        public string Text { get; set; }

        [Editor(FlowStepEditor.TextArea)]
        public string TextMultiline { get; set; }

        [Editor(FlowStepEditor.Password)]
        public string Password { get; set; }

        [Editor(FlowStepEditor.Text)]
        public StepEnum Enum { get; set; }

        [Editor(FlowStepEditor.Text)]
        public StepEnum? EnumOptional { get; set; }

        [Editor(FlowStepEditor.Text)]
        public bool Boolean { get; set; }

        [Editor(FlowStepEditor.Text)]
        public bool? BooleanOptional { get; set; }

        [Editor(FlowStepEditor.Text)]
        public int Number { get; set; }

        [Editor(FlowStepEditor.Text)]
        public int? NumberOptional { get; set; }

        [Editor(FlowStepEditor.Text)]
        public int? My12Monkeys { get; set; }

        [Computed]
        public string Computed { get; set; }

        public override ValueTask<FlowStepResult> ExecuteAsync(FlowExecutionContext executionContext, CancellationToken ct)
        {
            return default;
        }
    }

    [FlowStep(
        Title = "ObsoleteStep",
        IconImage = "<svg></svg>",
        IconColor = "#1e5470",
        Display = "Step display",
        Description = "Step description.",
        ReadMore = "https://www.readmore.com/")]
    [Obsolete("Obsolete step")]
    public sealed record ObsoleteStep : FlowStep
    {
        [Editor(FlowStepEditor.Text)]
        [Obsolete]
        public string Text1 { get; set; }

        [Editor(FlowStepEditor.Text)]
        [Obsolete("Use property 3")]
        public string Text2 { get; set; }

        public override ValueTask<FlowStepResult> ExecuteAsync(FlowExecutionContext executionContext, CancellationToken ct)
        {
            return default;
        }
    }

    [Fact]
    public void Should_create_definition()
    {
        var expected = new FlowStepDescriptor
        {
            Type = typeof(CombinedStep),
            Title = "CombinedStep",
            IconImage = "<svg></svg>",
            IconColor = "#1e5470",
            Display = "Step display",
            Description = "Step description.",
            ReadMore = "https://www.readmore.com/",
        };

        expected.Properties.Add(new FlowStepPropertyDescriptor
        {
            Name = "url",
            Display = "Url Name",
            Description = "Url Description",
            Editor = "Url",
            IsFormattable = true,
            IsRequired = true,
        });

        expected.Properties.Add(new FlowStepPropertyDescriptor
        {
            Name = "script",
            Display = "Script",
            Description = null,
            Editor = "Custom",
            IsRequired = false,
            IsScript = true,
        });

        expected.Properties.Add(new FlowStepPropertyDescriptor
        {
            Name = "text",
            Display = "Text",
            Description = null,
            Editor = FlowStepEditor.Text,
            IsRequired = false,
        });

        expected.Properties.Add(new FlowStepPropertyDescriptor
        {
            Name = "textMultiline",
            Display = "Text Multiline",
            Description = null,
            Editor = FlowStepEditor.TextArea,
            IsRequired = false,
        });

        expected.Properties.Add(new FlowStepPropertyDescriptor
        {
            Name = "password",
            Display = "Password",
            Description = null,
            Editor = FlowStepEditor.Password,
            IsRequired = false,
        });

        expected.Properties.Add(new FlowStepPropertyDescriptor
        {
            Name = "enum",
            Display = "Enum",
            Description = null,
            Editor = FlowStepEditor.Dropdown,
            IsRequired = false,
            Options = ["Yes", "No"],
        });

        expected.Properties.Add(new FlowStepPropertyDescriptor
        {
            Name = "enumOptional",
            Display = "Enum Optional",
            Description = null,
            Editor = FlowStepEditor.Dropdown,
            IsRequired = false,
            Options = ["Yes", "No"],
        });

        expected.Properties.Add(new FlowStepPropertyDescriptor
        {
            Name = "boolean",
            Display = "Boolean",
            Description = null,
            Editor = FlowStepEditor.Checkbox,
            IsRequired = false,
        });

        expected.Properties.Add(new FlowStepPropertyDescriptor
        {
            Name = "booleanOptional",
            Display = "Boolean Optional",
            Description = null,
            Editor = FlowStepEditor.Checkbox,
            IsRequired = false,
        });

        expected.Properties.Add(new FlowStepPropertyDescriptor
        {
            Name = "number",
            Display = "Number",
            Description = null,
            Editor = FlowStepEditor.Number,
            IsRequired = true,
        });

        expected.Properties.Add(new FlowStepPropertyDescriptor
        {
            Name = "numberOptional",
            Display = "Number Optional",
            Description = null,
            Editor = FlowStepEditor.Number,
            IsRequired = false,
        });

        expected.Properties.Add(new FlowStepPropertyDescriptor
        {
            Name = "my12Monkeys",
            Display = "My 12 Monkeys",
            Description = null,
            Editor = FlowStepEditor.Number,
            IsRequired = false,
        });

        var currentDefinition = sut.Steps["Combined"];

        currentDefinition.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Should_create_definition_for_obsolete_types()
    {
        var expected = new FlowStepDescriptor
        {
            Type = typeof(ObsoleteStep),
            Title = "ObsoleteStep",
            IconImage = "<svg></svg>",
            IconColor = "#1e5470",
            Display = "Step display",
            Description = "Step description.",
            ReadMore = "https://www.readmore.com/",
            IsObsolete = true,
            ObsoleteReason = "Obsolete step",
        };

        expected.Properties.Add(new FlowStepPropertyDescriptor
        {
            Name = "text1",
            Display = "Text 1",
            Description = null,
            Editor = FlowStepEditor.Text,
            IsRequired = false,
            IsObsolete = true,
            ObsoleteReason = null,
        });

        expected.Properties.Add(new FlowStepPropertyDescriptor
        {
            Name = "text2",
            Display = "Text 2",
            Description = null,
            Editor = FlowStepEditor.Text,
            IsRequired = false,
            IsObsolete = true,
            ObsoleteReason = "Use property 3",
        });

        var currentDefinition = sut.Steps["Obsolete"];

        currentDefinition.Should().BeEquivalentTo(expected);
    }
}
