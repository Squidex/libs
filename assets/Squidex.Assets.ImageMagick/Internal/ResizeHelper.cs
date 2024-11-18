// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;

#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace Squidex.Assets.Internal;

public readonly record struct PointF(float X, float Y);

public readonly record struct Size(int Width, int Height);

public readonly record struct Rectangle(int X, int Y, int Width, int Height);

internal static class ResizeHelper
{
    public static (Size Size, Rectangle Rectangle) CalculateTargetLocationAndBounds(ResizeMode mode, Size sourceSize, int width, int height, PointF? centerCoordinates)
    {
        // Ensure target size is populated across both dimensions.
        // These dimensions are used to calculate the final dimensions determined by the mode algorithm.
        // If only one of the incoming dimensions is 0, it will be modified here to maintain aspect ratio.
        // If it is not possible to keep aspect ratio, make sure at least the minimum is is kept.
        const int Min = 1;

        if (width == 0 && height > 0)
        {
            width = (int)MathF.Max(Min, MathF.Round(sourceSize.Width * height / (float)sourceSize.Height));
        }

        if (height == 0 && width > 0)
        {
            height = (int)MathF.Max(Min, MathF.Round(sourceSize.Height * width / (float)sourceSize.Width));
        }

        switch (mode)
        {
            case ResizeMode.Crop:
                return CalculateCropRectangle(sourceSize, width, height, centerCoordinates);
            case ResizeMode.Pad:
                return CalculatePadRectangle(sourceSize, width, height);
            case ResizeMode.BoxPad:
                return CalculateBoxPadRectangle(sourceSize, width, height);
            case ResizeMode.Max:
                return CalculateMaxRectangle(sourceSize, width, height);
            case ResizeMode.Min:
                return CalculateMinRectangle(sourceSize, width, height);
            default:
                return (new Size(Sanitize(width), Sanitize(height)), new Rectangle(0, 0, Sanitize(width), Sanitize(height)));
        }
    }

    private static (Size Size, Rectangle Rectangle) CalculateBoxPadRectangle(
        Size source,
        int desiredWidth,
        int desiredHeight)
    {
        var sourceWidth = source.Width;
        var sourceHeight = source.Height;

        // Fractional variants for preserving aspect ratio.
        var percentHeight = MathF.Abs(desiredHeight / (float)sourceHeight);
        var percentWidth = MathF.Abs(desiredWidth / (float)sourceWidth);

        var boxPadHeight = desiredHeight > 0 ? desiredHeight : (int)MathF.Round(sourceHeight * percentWidth);
        var boxPadWidth = desiredWidth > 0 ? desiredWidth : (int)MathF.Round(sourceWidth * percentHeight);

        // Only calculate if upscaling.
        if (sourceWidth < boxPadWidth && sourceHeight < boxPadHeight)
        {
            var targetWidth = sourceWidth;
            var targetHeight = sourceHeight;

            desiredWidth = boxPadWidth;
            desiredHeight = boxPadHeight;

            var targetY = (desiredHeight - sourceHeight) / 2;
            var targetX = (desiredWidth - sourceWidth) / 2;

            // Target image width and height can be different to the rectangle width and height.
            return (
                new Size(
                    Sanitize(desiredWidth),
                    Sanitize(desiredHeight)),
                new Rectangle(
                    targetX,
                    targetY,
                    Sanitize(targetWidth),
                    Sanitize(targetHeight))
            );
        }

        // Switch to pad mode to downscale and calculate from there.
        return CalculatePadRectangle(source, desiredWidth, desiredHeight);
    }

    private static (Size Size, Rectangle Rectangle) CalculateCropRectangle(
        Size source,
        int desiredWidth,
        int desiredHeight,
        PointF? centerCoordinates)
    {
        float ratio;

        var sourceWidth = source.Width;
        var sourceHeight = source.Height;
        var targetX = 0;
        var targetY = 0;
        var targetWidth = desiredWidth;
        var targetHeight = desiredHeight;

        // Fractional variants for preserving aspect ratio.
        var percentHeight = MathF.Abs(desiredHeight / (float)sourceHeight);
        var percentWidth = MathF.Abs(desiredWidth / (float)sourceWidth);

        if (percentHeight < percentWidth)
        {
            ratio = percentWidth;

            if (centerCoordinates.HasValue)
            {
                var center = -(ratio * sourceHeight) * centerCoordinates.Value.Y;

                targetY = (int)MathF.Round(center + (desiredHeight / 2F));

                if (targetY > 0)
                {
                    targetY = 0;
                }

                if (targetY < (int)MathF.Round(desiredHeight - (sourceHeight * ratio)))
                {
                    targetY = (int)MathF.Round(desiredHeight - (sourceHeight * ratio));
                }
            }

            targetHeight = (int)MathF.Ceiling(sourceHeight * percentWidth);
        }
        else
        {
            ratio = percentHeight;

            if (centerCoordinates.HasValue)
            {
                var center = -(ratio * sourceWidth) * centerCoordinates.Value.X;

                targetX = (int)MathF.Round(center + (desiredWidth / 2F));

                if (targetX > 0)
                {
                    targetX = 0;
                }

                if (targetX < (int)MathF.Round(desiredWidth - (sourceWidth * ratio)))
                {
                    targetX = (int)MathF.Round(desiredWidth - (sourceWidth * ratio));
                }
            }
            else
            {
                // Center (X)
                targetX = (int)MathF.Round((desiredWidth - (sourceWidth * ratio)) / 2F);
            }

            targetWidth = (int)MathF.Ceiling(sourceWidth * percentHeight);
        }

        // Target image width and height can be different to the rectangle width and height.
        return (
            new Size(
                Sanitize(desiredWidth),
                Sanitize(desiredHeight)),
            new Rectangle(
                targetX,
                targetY,
                Sanitize(targetWidth),
                Sanitize(targetHeight))
        );
    }

    private static (Size Size, Rectangle Rectangle) CalculateMaxRectangle(
        Size source,
        int desiredWidth,
        int desiredHeight)
    {
        var targetWidth = desiredWidth;
        var targetHeight = desiredHeight;

        // Fractional variants for preserving aspect ratio.
        var percentHeight = MathF.Abs(desiredHeight / (float)source.Height);
        var percentWidth = MathF.Abs(desiredWidth / (float)source.Width);

        // Integers must be cast to floats to get needed precision
        var ratio = desiredHeight / (float)desiredWidth;

        var sourceRatio = source.Height / (float)source.Width;

        if (sourceRatio < ratio)
        {
            targetHeight = (int)MathF.Round(source.Height * percentWidth);
        }
        else
        {
            targetWidth = (int)MathF.Round(source.Width * percentHeight);
        }

        // Replace the size to match the rectangle.
        return (
            new Size(
                Sanitize(targetWidth),
                Sanitize(targetHeight)),
            new Rectangle(
                0,
                0,
                Sanitize(targetWidth),
                Sanitize(targetHeight))
        );
    }

    private static (Size Size, Rectangle Rectangle) CalculateMinRectangle(
        Size source,
        int desiredWidth,
        int desiredHeight)
    {
        var sourceWidth = source.Width;
        var sourceHeight = source.Height;
        var targetWidth = desiredWidth;
        var targetHeight = desiredHeight;

        // Don't upscale
        if (desiredWidth > sourceWidth || desiredHeight > sourceHeight)
        {
            return (new Size(sourceWidth, sourceHeight), new Rectangle(0, 0, sourceWidth, sourceHeight));
        }

        // Find the shortest distance to go.
        var diffWidth = sourceWidth - desiredWidth;
        var diffHeight = sourceHeight - desiredHeight;

        if (diffWidth < diffHeight)
        {
            var sourceRatio = (float)sourceHeight / sourceWidth;
            targetHeight = (int)MathF.Round(desiredWidth * sourceRatio);
        }
        else if (diffWidth > diffHeight)
        {
            var sourceRatioInverse = (float)sourceWidth / sourceHeight;
            targetWidth = (int)MathF.Round(desiredHeight * sourceRatioInverse);
        }
        else
        {
            if (desiredHeight > desiredWidth)
            {
                var percentWidth = MathF.Abs(desiredWidth / (float)sourceWidth);
                targetHeight = (int)MathF.Round(sourceHeight * percentWidth);
            }
            else
            {
                var percentHeight = MathF.Abs(desiredHeight / (float)sourceHeight);
                targetWidth = (int)MathF.Round(sourceWidth * percentHeight);
            }
        }

        // Replace the size to match the rectangle.
        return (new Size(Sanitize(targetWidth), Sanitize(targetHeight)), new Rectangle(0, 0, Sanitize(targetWidth), Sanitize(targetHeight)));
    }

    private static (Size Size, Rectangle Rectangle) CalculatePadRectangle(
        Size sourceSize,
        int desiredWidth,
        int desiredHeight)
    {
        var sourceWidth = sourceSize.Width;
        var sourceHeight = sourceSize.Height;

        var targetX = 0;
        var targetY = 0;
        var targetWidth = desiredWidth;
        var targetHeight = desiredHeight;

        // Fractional variants for preserving aspect ratio.
        var percentHeight = MathF.Abs(desiredHeight / (float)sourceHeight);
        var percentWidth = MathF.Abs(desiredWidth / (float)sourceWidth);

        if (percentHeight < percentWidth)
        {
            var ratio = percentHeight;
            targetWidth = (int)MathF.Round(sourceWidth * percentHeight);
            targetX = (int)MathF.Round((desiredWidth - (sourceWidth * ratio)) / 2F);
        }
        else
        {
            var ratio = percentWidth;
            targetHeight = (int)MathF.Round(sourceHeight * percentWidth);
            targetY = (int)MathF.Round((desiredHeight - (sourceHeight * ratio)) / 2F);
        }

        // Target image width and height can be different to the rectangle width and height.
        return (
            new Size(
                Sanitize(desiredWidth),
                Sanitize(desiredHeight)),
            new Rectangle(
                targetX,
                targetY,
                Sanitize(targetWidth),
                Sanitize(targetHeight))
        );
    }

    private static int Sanitize(int input) => Math.Max(1, input);
}
