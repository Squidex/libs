// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI.SemanticKernel;

#pragma warning disable MA0048 // File name must match type name
public delegate string PromptRerankerPrompt(string question, string answer);
#pragma warning restore MA0048 // File name must match type name

public sealed class PromptRerankerOptions
{
    public PromptRerankerPrompt? Prompt { get; set; }

    internal string GetPrompt(string question, string answer)
    {
        var result = Prompt?.Invoke(question, answer);

        if (string.IsNullOrEmpty(result))
        {
            result = $"""
You are a language model designed to evaluate the responses of this documentation query system.
You will use a rating scale of 0 to 10, 0 being poorest response and 10 being the best.
Responses with "not specified" or "no specific mention" or "rephrase question" or "unclear" or no documents returned or empty response are considered poor responses.
Responses where the question appears to be answered are considered good.
Responses that contain detailed answers are considered the best.
Also, use your own judgement in analyzing if the question asked is actually answered in the response.
Remember that a response that contains a request to “rephrase the question” is usually a non-response.
Please rate the question/response pair entered.
Only respond with the rating. No explanation necessary. Only integers.

Question:
{question}

Response:
{answer}
""";
        }

        return result;
    }
}
