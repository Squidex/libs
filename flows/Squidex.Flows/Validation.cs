// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows;

#pragma warning disable MA0048 // File name must match type name

public delegate void AddError(string path, ValidationErrorType type, string message = "");

public delegate void AddStepError(string path, string message);

public enum ValidationErrorType
{
    NoSteps,
    NoStartStep,
    InvalidNextStepId,
    InvalidStep,
    InvalidStepId,
    InvalidProperty,
}
