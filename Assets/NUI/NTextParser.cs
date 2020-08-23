using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NUI
{
    public static class NTextParser
    {
        private static bool _tagNoParsing;
        private static Color32 _htmlTopColor;
        private static Color32 _htmlBottomColor;
        private static Color32? _underlineColor;
        private static Color32? _strikethroughColor;
        private static FontStyle _htmlFontStyle;
        private static int _htmlFontSize;
        private static int _spriteIndex;
        private static float _spriteScale;
        private static NVerticalAlign _spriteAlign;
        private static Color32 _spriteColor;
        private static int _animLength;
        private static int _animFrame;
        private static string _linkParam;

        private static XmlTagAttribute[] XmlAttribute = new XmlTagAttribute[8]; // To remove...
        private static char[] RichTextTag = new char[128];
        private static List<Color32> ColorStack = new List<Color32>();
        private static Stack<Color32?> UnderlineColorStack = new Stack<Color32?>();
        private static Stack<Color32?> StrikethroughColorStack = new Stack<Color32?>();
        private static Stack<FontStyle> FontStyleStack = new Stack<FontStyle>();
        private static Stack<string> LinkParamStack = new Stack<string>();
        private static Stack<int> FontSizeStack = new Stack<int>();


        public static List<NRichTextElement> Parse(string content, NTextGenerationSettings settings, List<NRichTextElement> elements, Dictionary<int, NRichTextElement> custom)
        {
            ListElementCleanup(elements, custom);

            int startIndex = 0;
            if (settings.richText)
            {
                _tagNoParsing = false;
                _htmlTopColor = settings.color;
                _htmlBottomColor = settings.GradientColor ? settings.BottomColor : settings.color;
                _underlineColor = null;
                _strikethroughColor = null;
                _htmlFontStyle = settings.fontStyle;
                _htmlFontSize = settings.fontSize;
                _spriteScale = settings.DefaultSpriteScale;
                _spriteAlign = settings.DefaultSpriteAlign;
                _animLength = settings.DefaultAnimLength < 1 ? 1 : settings.DefaultAnimLength;
                _animFrame = settings.DefaultAnimFrame < 1 ? 1 : settings.DefaultAnimFrame;
                _linkParam = null;

                ColorStack.Clear();
                UnderlineColorStack.Clear();
                StrikethroughColorStack.Clear();
                FontStyleStack.Clear();
                FontSizeStack.Clear();

                for (int i = 0; i < content.Length; i++)
                {
                    char unicode = content[i];
                    if (null != custom && custom.ContainsKey(unicode))
                    {
                        if (i > startIndex)
                        {
                            var element = TextElementPool.Get();
                            element.Text = content.Substring(startIndex, i - startIndex);
                            element.TopColor = settings.color;
                            element.BottomColor = settings.GradientColor ? settings.BottomColor : settings.color;
                            element.FontStyle = settings.fontStyle;
                            element.FontSize = settings.fontSize;

                            elements.Add(element);
                        }
                        elements.Add(custom[unicode]);
                        startIndex = i + 1;
                    }
                    else if (unicode == '<')
                    {
                        int tagEnd = 0;
                        if (ValidateHtmlTag(content, i + 1, out tagEnd, settings))
                        {
                            if (i > startIndex)
                            {
                                var element = TextElementPool.Get();
                                element.Text = content.Substring(startIndex, i - startIndex);
                                element.TopColor = _htmlTopColor;
                                element.BottomColor = _htmlBottomColor;
                                element.FontStyle = _htmlFontStyle;
                                element.FontSize = _htmlFontSize;
                                element.LinkParam = _linkParam;
                                element.StrikethroughColor = _strikethroughColor == null ? (Color32?)null : _strikethroughColor;
                                element.UnderlineColor = _underlineColor == null ? (Color32?)null : _underlineColor;
                                elements.Add(element);
                            }

                            if ((TagHashCode)XmlAttribute[0].nameHashCode == TagHashCode.SPRITE)
                            {
                                var element = TextElementPool.Get();
                                element.SpriteIndex = _spriteIndex;
                                element.SpriteScale = _spriteScale;
                                element.SpriteAlign = _spriteAlign;
                                element.TopColor = _spriteColor;
                                element.BottomColor = _spriteColor;
                                element.AnimLength = _animLength;
                                element.AnimFrame = _animFrame;
                                element.StrikethroughColor = _strikethroughColor == null ? (Color32?)null : _strikethroughColor;
                                element.UnderlineColor = _underlineColor == null ? (Color32?)null : _underlineColor;
                                elements.Add(element);
                            }

                            _htmlTopColor = ColorStack.Count > 0 ? ColorStack[ColorStack.Count - 1] : (Color32)settings.color;
                            _htmlBottomColor = ColorStack.Count > 1 ? ColorStack[ColorStack.Count - 2] : (settings.GradientColor ? (Color32)settings.BottomColor : (Color32)settings.color);
                            _underlineColor = UnderlineColorStack.Count > 0 ? UnderlineColorStack.Peek() : null;
                            _strikethroughColor = StrikethroughColorStack.Count > 0 ? StrikethroughColorStack.Peek() : null;
                            _htmlFontSize = FontSizeStack.Count > 0 ? FontSizeStack.Peek() : settings.fontSize;
                            _htmlFontStyle = FontStyleStack.Count > 0 ? FontStyleStack.Peek() : settings.fontStyle;
                            _linkParam = LinkParamStack.Count > 0 ? LinkParamStack.Peek() : null;

                            i = tagEnd;
                            startIndex = i + 1;
                        }
                    }
                }

                if (content.Length > startIndex)
                {
                    var element = TextElementPool.Get();
                    element.Text = content.Substring(startIndex, content.Length - startIndex);
                    element.TopColor = _htmlTopColor;
                    element.BottomColor = _htmlBottomColor;
                    element.FontStyle = _htmlFontStyle;
                    element.FontSize = _htmlFontSize;
                    element.LinkParam = _linkParam;
                    element.StrikethroughColor = _strikethroughColor == null ? (Color32?)null : _strikethroughColor;
                    element.UnderlineColor = _underlineColor == null ? (Color32?)null : _underlineColor;
                    elements.Add(element);
                }
            }
            else if (content.Length > 0)
            {
                for (int i = 0; i < content.Length; i++)
                {
                    char unicode = content[i];
                    if (null != custom && custom.ContainsKey(unicode))
                    {
                        if (i > startIndex)
                        {
                            var element = TextElementPool.Get();
                            element.Text = content.Substring(startIndex, i - startIndex);
                            element.TopColor = settings.color;
                            element.BottomColor = settings.GradientColor ? settings.BottomColor : settings.color;
                            element.FontStyle = settings.fontStyle;
                            element.FontSize = settings.fontSize;
                            elements.Add(element);
                        }

                        elements.Add(custom[unicode]);

                        startIndex = i + 1;
                    }
                }

                if (content.Length > startIndex)
                {
                    var element = TextElementPool.Get();
                    element.Text = content.Substring(startIndex, content.Length - startIndex);
                    element.TopColor = settings.color;
                    element.BottomColor = settings.GradientColor ? settings.BottomColor : settings.color;
                    element.FontStyle = settings.fontStyle;
                    element.FontSize = settings.fontSize;
                    elements.Add(element);
                }
            }

            return elements;
        }


        private static bool ValidateHtmlTag(string chars, int startIndex, out int endIndex, NTextGenerationSettings settings)
        {
            int tagCharCount = 0;
            byte attributeFlag = 0;

            TagValueType valueType = TagValueType.None;

            int attributeIndex = 0;
            XmlAttribute[0].nameHashCode = 0;
            XmlAttribute[0].valueType = TagValueType.None;
            XmlAttribute[0].valueHashCode = 0;
            XmlAttribute[0].valueStartIndex = 0;
            XmlAttribute[0].valueLength = 0;

            // Clear attribute name hash codes
            XmlAttribute[1].nameHashCode = 0;
            XmlAttribute[2].nameHashCode = 0;
            XmlAttribute[3].nameHashCode = 0;
            XmlAttribute[4].nameHashCode = 0;
            XmlAttribute[5].nameHashCode = 0;
            XmlAttribute[6].nameHashCode = 0;
            XmlAttribute[7].nameHashCode = 0;

            endIndex = startIndex;
            bool isTagSet = false;
            bool isValidHtmlTag = false;

            for (int i = startIndex; i < chars.Length && chars[i] != 0 && tagCharCount < RichTextTag.Length && chars[i] != '<'; i++)
            {
                char unicode = chars[i];

                if (unicode == '>') // ASCII Code of End HTML tag '>'
                {
                    isValidHtmlTag = true;
                    endIndex = i;
                    RichTextTag[tagCharCount] = (char)0;
                    break;
                }

                RichTextTag[tagCharCount] = unicode;
                tagCharCount += 1;

                if (attributeFlag == 1)
                {
                    if (valueType == TagValueType.None)
                    {
                        // Check for attribute type
                        if (unicode == '+' || unicode == '-' || unicode == '.' || (unicode >= '0' && unicode <= '9'))
                        {
                            valueType = TagValueType.NumericalValue;
                            XmlAttribute[attributeIndex].valueType = TagValueType.NumericalValue;
                            XmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            XmlAttribute[attributeIndex].valueLength += 1;
                        }
                        else if (unicode == '#')
                        {
                            valueType = TagValueType.ColorValue;
                            XmlAttribute[attributeIndex].valueType = TagValueType.ColorValue;
                            XmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            XmlAttribute[attributeIndex].valueLength += 1;
                        }
                        else if (unicode == '"')
                        {
                            valueType = TagValueType.StringValue;
                            XmlAttribute[attributeIndex].valueType = TagValueType.StringValue;
                            XmlAttribute[attributeIndex].valueStartIndex = tagCharCount;
                        }
                        else
                        {
                            valueType = TagValueType.StringValue;
                            XmlAttribute[attributeIndex].valueType = TagValueType.StringValue;
                            XmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            XmlAttribute[attributeIndex].valueHashCode = (XmlAttribute[attributeIndex].valueHashCode << 5) + XmlAttribute[attributeIndex].valueHashCode ^ (int)ToUpperASCIIFast(unicode);
                            XmlAttribute[attributeIndex].valueLength += 1;
                        }
                    }
                    else
                    {
                        if (valueType == TagValueType.NumericalValue)
                        {
                            // Check for termination of numerical value.
                            if (unicode == ' ')
                            {
                                attributeFlag = 2;
                                valueType = TagValueType.None;
                                attributeIndex += 1;
                                XmlAttribute[attributeIndex].nameHashCode = 0;
                                XmlAttribute[attributeIndex].valueType = TagValueType.None;
                                XmlAttribute[attributeIndex].valueHashCode = 0;
                                XmlAttribute[attributeIndex].valueStartIndex = 0;
                                XmlAttribute[attributeIndex].valueLength = 0;
                            }
                            else if (attributeFlag != 2)
                            {
                                XmlAttribute[attributeIndex].valueLength += 1;
                            }
                        }
                        else if (valueType == TagValueType.ColorValue)
                        {
                            if (unicode != ' ')
                            {
                                XmlAttribute[attributeIndex].valueLength += 1;
                            }
                            else
                            {
                                attributeFlag = 2;
                                valueType = TagValueType.None;
                                attributeIndex += 1;
                                XmlAttribute[attributeIndex].nameHashCode = 0;
                                XmlAttribute[attributeIndex].valueType = TagValueType.None;
                                XmlAttribute[attributeIndex].valueHashCode = 0;
                                XmlAttribute[attributeIndex].valueStartIndex = 0;
                                XmlAttribute[attributeIndex].valueLength = 0;
                            }
                        }
                        else if (valueType == TagValueType.StringValue)
                        {
                            // Compute HashCode value for the named tag.
                            if (unicode != '"' && unicode != ' ')
                            {
                                XmlAttribute[attributeIndex].valueHashCode = (XmlAttribute[attributeIndex].valueHashCode << 5) + XmlAttribute[attributeIndex].valueHashCode ^ (int)ToUpperASCIIFast(unicode);
                                XmlAttribute[attributeIndex].valueLength += 1;
                            }
                            else
                            {
                                attributeFlag = 2;
                                valueType = TagValueType.None;
                                attributeIndex += 1;
                                XmlAttribute[attributeIndex].nameHashCode = 0;
                                XmlAttribute[attributeIndex].valueType = TagValueType.None;
                                XmlAttribute[attributeIndex].valueHashCode = 0;
                                XmlAttribute[attributeIndex].valueStartIndex = 0;
                                XmlAttribute[attributeIndex].valueLength = 0;
                            }
                        }
                    }
                }

                if (unicode == '=')
                    attributeFlag = 1;

                // Compute HashCode for the name of the attribute
                if (attributeFlag == 0 && unicode == ' ')
                {
                    if (isTagSet)
                        return false;

                    isTagSet = true;
                    attributeFlag = 2;

                    valueType = TagValueType.None;
                    attributeIndex += 1;
                    XmlAttribute[attributeIndex].nameHashCode = 0;
                    XmlAttribute[attributeIndex].valueType = TagValueType.None;
                    XmlAttribute[attributeIndex].valueHashCode = 0;
                    XmlAttribute[attributeIndex].valueStartIndex = 0;
                    XmlAttribute[attributeIndex].valueLength = 0;
                }

                if (attributeFlag == 0)
                    XmlAttribute[attributeIndex].nameHashCode = (XmlAttribute[attributeIndex].nameHashCode << 5) + XmlAttribute[attributeIndex].nameHashCode ^ (int)ToUpperASCIIFast(unicode);

                if (attributeFlag == 2 && unicode == ' ')
                    attributeFlag = 0;
            }

            if (!isValidHtmlTag)
                return false;

            // Special handling of the no parsing tag </noparse> </NOPARSE> tag
            if (_tagNoParsing && ((TagHashCode)XmlAttribute[0].nameHashCode != TagHashCode.SLASH_NO_PARSE))
            {
                return false;
            }
            if ((TagHashCode)XmlAttribute[0].nameHashCode == TagHashCode.SLASH_NO_PARSE)
            {
                _tagNoParsing = false;
                return true;
            }

            switch ((TagHashCode)XmlAttribute[0].nameHashCode)
            {
                case TagHashCode.BOLD: // <b>
                    FontStyleStack.Push(FontStyle.Bold);
                    return true;

                case TagHashCode.SLASH_BOLD: // </b>
                    if (FontStyleStack.Count > 0)
                        _htmlFontStyle = FontStyleStack.Pop();
                    else
                        return false;
                    return true;

                case TagHashCode.ITALIC: // <i>
                    FontStyleStack.Push(FontStyle.Italic);
                    return true;

                case TagHashCode.SLASH_ITALIC: // </i>
                    if (FontStyleStack.Count > 0)
                        _htmlFontStyle = FontStyleStack.Pop();
                    else
                        return false;
                    return true;

                case TagHashCode.STRIKETHROUGH: // <s>
                    if (XmlAttribute[1].nameHashCode == (uint)TagHashCode.COLOR)
                    {
                        Color32 strikethroughColor;
                        if (TextGeneratorUtilities.AttributeToColor(RichTextTag, XmlAttribute[1], out strikethroughColor))
                        {
                            strikethroughColor.a = _htmlTopColor.a < strikethroughColor.a ? _htmlTopColor.a : strikethroughColor.a;
                            StrikethroughColorStack.Push(strikethroughColor);
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    StrikethroughColorStack.Push(null);
                    return true;

                case TagHashCode.SLASH_STRIKETHROUGH: // </s>
                    if (StrikethroughColorStack.Count > 0)
                        _strikethroughColor = StrikethroughColorStack.Pop();
                    else
                        return false;
                    return true;

                case TagHashCode.UNDERLINE: // <u>
                    if (XmlAttribute[1].nameHashCode == (uint)TagHashCode.COLOR)
                    {
                        Color32 underlineColor;
                        if (TextGeneratorUtilities.AttributeToColor(RichTextTag, XmlAttribute[1], out underlineColor))
                        {
                            underlineColor.a = _htmlTopColor.a < underlineColor.a ? _htmlTopColor.a : underlineColor.a;
                            UnderlineColorStack.Push(underlineColor);
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    UnderlineColorStack.Push(null);
                    return true;

                case TagHashCode.SLASH_UNDERLINE: // </u>
                    if (UnderlineColorStack.Count > 0)
                        _underlineColor = UnderlineColorStack.Pop();
                    else
                        return false;
                    return true;

                case TagHashCode.SIZE: // <size=>
                    float value = 0;
                    if (!TextGeneratorUtilities.ConvertToFloat(RichTextTag, XmlAttribute[0].valueStartIndex, XmlAttribute[0].valueLength, out value))
                        return false;

                    if (RichTextTag[5] == '+' || RichTextTag[5] == '-') // <size=+00>
                        FontSizeStack.Push((int)(settings.fontSize + value));
                    else // <size=00.0>
                        FontSizeStack.Push((int)value);
                    return true;


                case TagHashCode.SLASH_SIZE: // </size>
                    if (FontSizeStack.Count > 0)
                        _htmlFontSize = FontSizeStack.Pop();
                    else
                        return false;
                    return true;

                case TagHashCode.LINK: // <link="name">
                    if (XmlAttribute[0].valueLength <= 0)
                        return false;

                    LinkParamStack.Push(new string(RichTextTag, XmlAttribute[0].valueStartIndex, XmlAttribute[0].valueLength));
                    return true;

                case TagHashCode.SLASH_LINK: // </link>
                    if (LinkParamStack.Count > 0)
                        _linkParam = LinkParamStack.Pop();
                    else
                        return false;
                    return true;

                case TagHashCode.COLOR: // <color> <color=#FF00FF> or <color=#FF00FF00>
                    Color32 htmlColor;
                    if (!TextGeneratorUtilities.AttributeToColor(RichTextTag, XmlAttribute[0], out htmlColor))
                        return false;

                    ColorStack.Add(htmlColor);
                    ColorStack.Add(htmlColor);
                    return true;

                case TagHashCode.SLASH_COLOR: // </color>
                    if (ColorStack.Count > 1)
                    {
                        _htmlBottomColor = ColorStack[ColorStack.Count - 1];
                        ColorStack.RemoveAt(ColorStack.Count - 1);
                        _htmlTopColor = ColorStack[ColorStack.Count - 1];
                        ColorStack.RemoveAt(ColorStack.Count - 1);
                        return true;
                    }

                    return false;

                case TagHashCode.GRADIENT: //<gradient>
                    Color32 topColor;
                    if ((TagHashCode)XmlAttribute[1].nameHashCode != TagHashCode.TOP || !TextGeneratorUtilities.AttributeToColor(RichTextTag, XmlAttribute[1], out topColor))
                        return false;

                    Color32 bottomColor;
                    if ((TagHashCode)XmlAttribute[2].nameHashCode != TagHashCode.BOTTOM || !TextGeneratorUtilities.AttributeToColor(RichTextTag, XmlAttribute[2], out bottomColor))
                        return false;

                    ColorStack.Add(topColor);
                    ColorStack.Add(bottomColor);
                    return true;

                case TagHashCode.SLASH_GRADIENT: // </gradient>
                    if (ColorStack.Count > 1)
                    {
                        _htmlBottomColor = ColorStack[ColorStack.Count - 1];
                        ColorStack.RemoveAt(ColorStack.Count - 1);
                        _htmlTopColor = ColorStack[ColorStack.Count - 1];
                        ColorStack.RemoveAt(ColorStack.Count - 1);
                        return true;
                    }

                    return false;

                case TagHashCode.SPRITE: // <sprite=x>
                    _spriteScale = settings.DefaultSpriteScale;
                    _spriteAlign = settings.DefaultSpriteAlign;
                    _animLength = settings.DefaultAnimLength < 1 ? 1 : settings.DefaultAnimLength;
                    _animFrame = settings.DefaultAnimFrame < 1 ? 1 : settings.DefaultAnimFrame;
                    _spriteColor = Color.white;

                    if ((TagHashCode)XmlAttribute[1].nameHashCode == TagHashCode.INDEX) // <sprite index=>
                    {
                        if (XmlAttribute[1].valueType == TagValueType.NumericalValue)
                        {
                            int lastIndex = 0;
                            _spriteIndex = (int)TextGeneratorUtilities.ConvertToFloat(RichTextTag, XmlAttribute[1].valueStartIndex, XmlAttribute[1].valueLength, out lastIndex);
                            if (lastIndex <= XmlAttribute[1].valueStartIndex)
                                return false;
                            if (_spriteIndex < 0 || _spriteIndex >= settings.SpriteLength)
                                return false;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }

                    for (int attrIndex = 2; attrIndex < 8; attrIndex++)
                    {
                        if ((TagHashCode)XmlAttribute[attrIndex].nameHashCode == TagHashCode.SCALE) // scale
                        {
                            int lastIndex = 0;
                            var spriteScale = TextGeneratorUtilities.ConvertToFloat(RichTextTag, XmlAttribute[attrIndex].valueStartIndex, XmlAttribute[attrIndex].valueLength, out lastIndex);
                            if (lastIndex <= XmlAttribute[attrIndex].valueStartIndex)
                                return false;
                            _spriteScale = spriteScale;
                        }
                        else if ((TagHashCode)XmlAttribute[attrIndex].nameHashCode == TagHashCode.ALIGN) // Align
                        {
                            switch ((TagHashCode)XmlAttribute[attrIndex].valueHashCode)
                            {
                                case TagHashCode.TOP:
                                    _spriteAlign = NVerticalAlign.Top;
                                    break;
                                case TagHashCode.MIDDLE:
                                    _spriteAlign = NVerticalAlign.Middle;
                                    break;
                                case TagHashCode.BOTTOM:
                                    _spriteAlign = NVerticalAlign.Bottom;
                                    break;
                                default:
                                    return false;
                            }
                        }
                        else if ((TagHashCode)XmlAttribute[attrIndex].nameHashCode == TagHashCode.COLOR) // Color
                        {
                            if (!TextGeneratorUtilities.AttributeToColor(RichTextTag, XmlAttribute[attrIndex], out _spriteColor))
                                return false;
                        }
                        else if ((TagHashCode)XmlAttribute[attrIndex].nameHashCode == TagHashCode.ANIM_LENGTH)
                        {
                            int lastIndex = 0;
                            _animLength = (int)TextGeneratorUtilities.ConvertToFloat(RichTextTag, XmlAttribute[attrIndex].valueStartIndex, XmlAttribute[attrIndex].valueLength, out lastIndex);
                            if (lastIndex <= XmlAttribute[attrIndex].valueStartIndex)
                                return false;
                            if (_animLength < 0 || _spriteIndex + _animLength >= settings.SpriteLength)
                                return false;
                        }
                        else if ((TagHashCode)XmlAttribute[attrIndex].nameHashCode == TagHashCode.ANIM_FRAME)
                        {
                            int lastIndex = 0;
                            _animFrame = (int)TextGeneratorUtilities.ConvertToFloat(RichTextTag, XmlAttribute[attrIndex].valueStartIndex, XmlAttribute[attrIndex].valueLength, out lastIndex);
                            if (lastIndex <= XmlAttribute[attrIndex].valueStartIndex)
                                return false;
                            if (_animFrame < 0)
                                return false;
                        }
                    }

                    return true;

                case TagHashCode.NO_PARSE: // <noparse>
                    _tagNoParsing = true;
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Table used to convert character to uppercase.
        /// </summary>
        const string k_LookupStringU = "-------------------------------- !-#$%&-()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[-]^_`ABCDEFGHIJKLMNOPQRSTUVWXYZ{|}~-";

        /// <summary>
        /// Get uppercase version of this ASCII character.
        /// </summary>
        public static char ToUpperFast(char c)
        {
            if (c > k_LookupStringU.Length - 1)
                return c;

            return k_LookupStringU[c];
        }

        /// <summary>
        /// Get uppercase version of this ASCII character.
        /// </summary>
        public static uint ToUpperASCIIFast(uint c)
        {
            if (c > k_LookupStringU.Length - 1)
                return c;

            return k_LookupStringU[(int)c];
        }

        private static void ListElementCleanup(List<NRichTextElement> elements, Dictionary<int, NRichTextElement> custom)
        {
            foreach (var element in elements)
            {
                if (null == custom || !custom.ContainsValue(element))
                    TextElementPool.Release(element);
            }
            elements.Clear();
        }
    }
}