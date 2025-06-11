// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows;

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class FlowStepAttribute : Attribute
{
    public string Title { get; set; }

    public string ReadMore { get; set; }

    public string IconImage { get; set; }

    public string IconColor { get; set; }

    public string Display { get; set; }

    public string Description { get; set; }
}
