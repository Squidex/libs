﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;
using System.Text;

namespace Squidex.Assets;

public sealed class ResizeOptions : IOptions
{
    public IEnumerable<KeyValuePair<string, string>>? ExtraParameters { get; set; }

    public ImageFormat? Format { get; set; }

    public ResizeMode Mode { get; set; }

    public int? TargetWidth { get; set; }

    public int? TargetHeight { get; set; }

    public int? Quality { get; set; }

    public float? FocusX { get; set; }

    public float? FocusY { get; set; }

    public string? Background { get; set; }

    public string? WatermarkUrl { get; set; }

    public float WatermarkOpacity { get; set; } = 1;

    public bool KeepMetadata { get; set; }

    public WatermarkAnchor WatermarkAnchor { get; set; }

    internal bool IsResize
    {
        get => TargetWidth > 0 || TargetHeight > 0 || Quality > 0;
    }

    public IEnumerable<(string, string)> ToParameters()
    {
        if (Mode != default)
        {
            yield return ("mode", Mode.ToString());
        }

        if (Format != null)
        {
            yield return ("format", Format.ToString()!);
        }

        if (TargetWidth != null)
        {
            yield return ("targetWidth", TargetWidth.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (TargetHeight != null)
        {
            yield return ("targetHeight", TargetHeight.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (Quality != null)
        {
            yield return ("quality", Quality.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (FocusX != null)
        {
            yield return ("focusX", FocusX.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (FocusY != null)
        {
            yield return ("focusY", FocusY.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (Background != null)
        {
            yield return ("background", Background);
        }

        if (WatermarkAnchor != default)
        {
            yield return ("watermarkAnchor", WatermarkAnchor.ToString());
        }

        if (WatermarkOpacity < 1)
        {
            yield return ("watermarkOpacity", WatermarkOpacity.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(WatermarkUrl))
        {
            yield return ("watermark", WatermarkUrl);
        }

        if (ExtraParameters != null)
        {
            foreach (var kvp in ExtraParameters)
            {
                yield return (kvp.Key, kvp.Value);
            }
        }
    }

    public static ResizeOptions Parse(Dictionary<string, string> parameters)
    {
        var result = new ResizeOptions();

        bool TryParseEnum<T>(string key, out T value) where T : struct
        {
            value = default(T);

            return parameters.TryGetValue(key, out var temp) && Enum.TryParse<T>(temp, true, out value);
        }

        bool TryParseInt(string key, out int value)
        {
            value = 0;

            return parameters.TryGetValue(key, out var temp) && int.TryParse(temp, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        bool TryParseFloat(string key, out float value)
        {
            value = 0;

            return parameters.TryGetValue(key, out var temp) && float.TryParse(temp, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        if (TryParseEnum<ResizeMode>("mode", out var mode))
        {
            result.Mode = mode;
        }

        if (TryParseEnum<ImageFormat>("format", out var format))
        {
            result.Format = format;
        }

        if (TryParseInt("targetWidth", out var targetWidth))
        {
            result.TargetWidth = targetWidth;
        }

        if (TryParseInt("targetHeight", out var targetHeight))
        {
            result.TargetHeight = targetHeight;
        }

        if (TryParseInt("quality", out var quality))
        {
            result.Quality = quality;
        }

        if (TryParseFloat("focusX", out var focusX))
        {
            result.FocusX = focusX;
        }

        if (TryParseFloat("focusY", out var focusY))
        {
            result.FocusY = focusY;
        }

        if (parameters.TryGetValue("background", out var background))
        {
            result.Background = background;
        }

        if (parameters.TryGetValue("watermark", out var watermark))
        {
            result.WatermarkUrl = watermark;
        }

        if (parameters.TryGetValue("watermarkAnchor", out var a) && Enum.TryParse<WatermarkAnchor>(a, out var anchor))
        {
            result.WatermarkAnchor = anchor;
        }

        if (TryParseFloat("watermarkOpacity", out var watermarkOpacity))
        {
            result.WatermarkOpacity = watermarkOpacity;
        }

        return result;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append(TargetWidth);
        sb.Append('_');
        sb.Append(TargetHeight);
        sb.Append('_');
        sb.Append(Mode);

        if (Quality.HasValue)
        {
            sb.Append('_');
            sb.Append(Quality);
        }

        if (FocusX.HasValue)
        {
            sb.Append("_focusX_");
            sb.Append(FocusX);
        }

        if (FocusY.HasValue)
        {
            sb.Append("_focusY_");
            sb.Append(FocusY);
        }

        if (Format != null)
        {
            sb.Append("_format_");
            sb.Append(Format.ToString());
        }

        if (!string.IsNullOrWhiteSpace(Background))
        {
            sb.Append("_background_");
            sb.Append(Background);
        }

        if (WatermarkAnchor != default)
        {
            sb.Append("_wa_");
            sb.Append(WatermarkAnchor);
        }

        if (!string.IsNullOrWhiteSpace(WatermarkUrl))
        {
            sb.Append("_wm_");
            sb.Append(WatermarkUrl);
        }

        if (WatermarkOpacity < 1)
        {
            sb.Append("_wo_");
            sb.Append(WatermarkOpacity);
        }

        return sb.ToString();
    }
}
