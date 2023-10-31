﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.Svg;

public static class SvgAttributes
{
    public static readonly HashSet<string> Allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "accent-height",
        "accumulate",
        "additive",
        "alignment-baseline",
        "ascent",
        "attributeName",
        "attributeType",
        "azimuth",
        "baseFrequency",
        "baseline-shift",
        "baseProfile",
        "begin",
        "bias",
        "by",
        "calcMode",
        "class",
        "clip",
        "clip-path",
        "clipPathUnits",
        "clip-rule",
        "color",
        "color-interpolation",
        "color-interpolation-filters",
        "color-profile",
        "color-rendering",
        "cx",
        "cy",
        "d",
        "diffuseConstant",
        "direction",
        "display",
        "divisor",
        "dur",
        "dx",
        "dy",
        "edgeMode",
        "elevation",
        "end",
        "exponent",
        "fill",
        "fill-opacity",
        "fill-rule",
        "filter",
        "flood-color",
        "flood-opacity",
        "font-family",
        "font-size",
        "font-size-adjust",
        "font-stretch",
        "font-style",
        "font-variant",
        "font-weight",
        "fr",
        "from",
        "fx",
        "fy",
        "g1",
        "g2",
        "glyph-name",
        "glyphRef",
        "gradientTransform",
        "gradientUnits",
        "height",
        "href",
        "id",
        "image-rendering",
        "in",
        "in2",
        "intercept",
        "k",
        "k1",
        "k2",
        "k3",
        "k4",
        "kernelMatrix",
        "kernelUnitLength",
        "kerning",
        "keyPoints",
        "keySplines",
        "keyTimes",
        "lang",
        "lengthAdjust",
        "letter-spacing",
        "lighting-color",
        "limitingConeAngle",
        "local",
        "marker-end",
        "markerHeight",
        "marker-mid",
        "marker-start",
        "markerUnits",
        "markerWidth",
        "mask",
        "maskContentUnits",
        "maskUnits",
        "max",
        "media",
        "method",
        "min",
        "mode",
        "name",
        "numOctaves",
        "offset",
        "opacity",
        "operator",
        "order",
        "orient",
        "orientation",
        "origin",
        "overflow",
        "paint-order",
        "path",
        "pathLength",
        "patternContentUnits",
        "patternTransform",
        "patternUnits",
        "pointer-events",
        "points",
        "pointsAtX",
        "pointsAtY",
        "pointsAtZ",
        "preserveAlpha",
        "preserveAspectRatio",
        "primitiveUnits",
        "r",
        "radius",
        "refX",
        "refY",
        "repeatCount",
        "repeatDur",
        "restart",
        "result",
        "rotate",
        "rx",
        "ry",
        "scale",
        "seed",
        "shape-rendering",
        "specularConstant",
        "specularExponent",
        "spreadMethod",
        "startOffset",
        "stdDeviation",
        "stitchTiles",
        "stop-color",
        "stop-opacity",
        "strikethrough-position",
        "strikethrough-thickness",
        "stroke",
        "stroke-dasharray",
        "stroke-dashoffset",
        "stroke-linecap",
        "stroke-linejoin",
        "stroke-miterlimit",
        "stroke-opacity",
        "stroke-width",
        "style",
        "surfaceScale",
        "systemLanguage",
        "tabindex",
        "tableValues",
        "targetX",
        "targetY",
        "text-anchor",
        "text-decoration",
        "textLength",
        "text-rendering",
        "to",
        "transform",
        "transform-origin",
        "type",
        "u1",
        "u2",
        "underline-position",
        "underline-thickness",
        "unicode",
        "unicode-bidi",
        "values",
        "vector-effect",
        "version",
        "vert-adv-y",
        "vert-origin-x",
        "vert-origin-y",
        "viewBox",
        "viewTarget",
        "visibility",
        "width",
        "word-spacing",
        "wrap",
        "writing-mode",
        "x",
        "x1",
        "x2",
        "xChannelSelector",
        "xlink:href",
        "xml:lang",
        "xml:space",
        "xmlns",
        "xmlns:xlink",
        "y",
        "y1",
        "y2",
        "yChannelSelector",
        "z",
        "zoomAndPan"
    };

    public static readonly HashSet<string> Urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "href",
        "src"
    };
}
