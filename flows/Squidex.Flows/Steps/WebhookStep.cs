// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.ComponentModel.DataAnnotations;
using Generator.Equals;

namespace Squidex.Flows.Steps;

[FlowStep(
    Title = "Webhook",
    IconImage = "<svg xmlns='http://www.w3.org/2000/svg' height='24' viewBox='0 -960 960 960' width='24'><path d='M40-360v-240h60v80h80v-80h60v240h-60v-100h-80v100H40zm300 0v-180h-60v-60h180v60h-60v180h-60zm220 0v-180h-60v-60h180v60h-60v180h-60zm160 0v-240h140q24 0 42 18t18 42v40q0 24-18 42t-42 18h-80v80h-60zm60-140h80v-40h-80v40z'/></svg>",
    IconColor = "#3389ff",
    Display = "Send webhook",
    Description = "Invoke HTTP endpoints on a target system.",
    ReadMore = "https://en.wikipedia.org/wiki/weebhook")]
[Equatable]
public sealed partial record WebhookStep : FlowStep
{
    [Required]
    [Display(Name = "Method", Description = "The type of the request.")]
    public WebhookMethod Method { get; set; }

    [Required]
    [Display(Name = "Url", Description = "The URL to the webhook.")]
    [Expression]
    public string Uri { get; set; }

    [Expression(ExpressionFallback.Envelope)]
    [Display(Name = "Payload (Optional)", Description = "Leave it empty to use the full event as body.")]
    [Editor(FlowStepEditor.TextArea)]
    public string? Payload { get; set; }

    [Expression]
    [Display(Name = "Headers (Optional)", Description = "The message headers in the format '[Key]=[Value]', one entry per line.")]
    [Editor(FlowStepEditor.TextArea)]
    public string? Headers { get; set; }

    [Display(Name = "Payload Type", Description = "The mime type of the payload.")]
    [Editor(FlowStepEditor.Text)]
    public string? PayloadType { get; set; }

    [Display(Name = "Shared Secret", Description = "The shared secret that is used to calculate the payload signature.")]
    [Editor(FlowStepEditor.Text)]
    public string? SharedSecret { get; set; }
}
