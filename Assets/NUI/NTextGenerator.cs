using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NUI
{
    public struct NTextGenerationSettings
    {
        /// <summary>
        ///   <para>Font to use for generation.</para>
        /// </summary>
        public Font font;
        /// <summary>
        ///   <para>The base color for the text generation.</para>
        /// </summary>
        public Color color;
        /// <summary>
        ///   <para>Font size.</para>
        /// </summary>
        public int fontSize;
        /// <summary>
        ///   <para>The line spacing multiplier.</para>
        /// </summary>
        public float lineSpacing;
        /// <summary>
        ///   <para>Allow rich text markup in generation.</para>
        /// </summary>
        public bool richText;
        /// <summary>
        ///   <para>A scale factor for the text. This is useful if the Text is on a Canvas and the canvas is scaled.</para>
        /// </summary>
        public float scaleFactor;
        /// <summary>
        ///   <para>Font style.</para>
        /// </summary>
        public FontStyle fontStyle;
        /// <summary>
        ///   <para>How is the generated text anchored.</para>
        /// </summary>
        public TextAnchor textAnchor;
        /// <summary>
        ///   <para>Use the extents of glyph geometry to perform horizontal alignment rather than glyph metrics.</para>
        /// </summary>
        public bool alignByGeometry;
        /// <summary>
        ///   <para>Should the text be resized to fit the configured bounds?</para>
        /// </summary>
        public bool resizeTextForBestFit;
        /// <summary>
        ///   <para>Minimum size for resized text.</para>
        /// </summary>
        public int resizeTextMinSize;
        /// <summary>
        ///   <para>Maximum size for resized text.</para>
        /// </summary>
        public int resizeTextMaxSize;
        /// <summary>
        ///   <para>Should the text generator update the bounds from the generated text.</para>
        /// </summary>
        public bool updateBounds;
        /// <summary>
        ///   <para>What happens to text when it reaches the bottom generation bounds.</para>
        /// </summary>
        public VerticalWrapMode verticalOverflow;
        /// <summary>
        ///   <para>What happens to text when it reaches the horizontal generation bounds.</para>
        /// </summary>
        public HorizontalWrapMode horizontalOverflow;
        /// <summary>
        ///   <para>Extents that the generator will attempt to fit the text in.</para>
        /// </summary>
        public Vector2 generationExtents;
        /// <summary>
        ///   <para>Generated vertices are offset by the pivot.</para>
        /// </summary>
        public Vector2 pivot;
        /// <summary>
        ///   <para>Continue to generate characters even if the text runs out of bounds.</para>
        /// </summary>
        public bool generateOutOfBounds;

        public Sprite[] Sprites;
        public int DefaultAnimLength;
        public int DefaultAnimFrame;
        public float DefaultSpriteScale;
        public NVerticalAlign DefaultSpriteAlign;
        public bool GradientColor;
        public Color BottomColor;
        public bool Outline;
        public Color OutlineColor;
        public float OutlineSize;
        public int ParagraphIndent;
        public bool OverflowEllipsis;
        public float maxOverflowWidth;
        public bool SingleLine;

        private bool CompareColors(Color left, Color right)
        {
            return Mathf.Approximately(left.r, right.r) && Mathf.Approximately(left.g, right.g) && Mathf.Approximately(left.b, right.b) && Mathf.Approximately(left.a, right.a);
        }

        private bool CompareVector2(Vector2 left, Vector2 right)
        {
            return Mathf.Approximately(left.x, right.x) && Mathf.Approximately(left.y, right.y);
        }

        public bool Equals(NTextGenerationSettings other)
        {
            return this.CompareColors(this.color, other.color) && this.fontSize == other.fontSize && Mathf.Approximately(this.scaleFactor, other.scaleFactor) && this.resizeTextMinSize == other.resizeTextMinSize &&
                this.resizeTextMaxSize == other.resizeTextMaxSize && Mathf.Approximately(this.lineSpacing, other.lineSpacing) && this.fontStyle == other.fontStyle && this.richText == other.richText &&
                this.textAnchor == other.textAnchor && this.alignByGeometry == other.alignByGeometry && this.resizeTextForBestFit == other.resizeTextForBestFit && this.resizeTextMinSize == other.resizeTextMinSize &&
                this.resizeTextMaxSize == other.resizeTextMaxSize && this.resizeTextForBestFit == other.resizeTextForBestFit && this.updateBounds == other.updateBounds && this.horizontalOverflow == other.horizontalOverflow &&
                this.verticalOverflow == other.verticalOverflow && this.CompareVector2(this.generationExtents, other.generationExtents) && this.CompareVector2(this.pivot, other.pivot) &&
                ReferenceEquals(this.font, other.font) && ReferenceEquals(this.Sprites, other.Sprites) && this.DefaultAnimLength == other.DefaultAnimLength && this.DefaultAnimFrame == other.DefaultAnimFrame &&
                Mathf.Approximately(this.DefaultSpriteScale, other.DefaultSpriteScale) && this.DefaultSpriteAlign == other.DefaultSpriteAlign && this.GradientColor == other.GradientColor && CompareColors(this.BottomColor, other.BottomColor) &&
                this.Outline == other.Outline && CompareColors(this.OutlineColor, other.OutlineColor) && Mathf.Approximately(this.OutlineSize, other.OutlineSize) && this.ParagraphIndent == other.ParagraphIndent &&
                this.OverflowEllipsis == other.OverflowEllipsis && Mathf.Approximately(this.maxOverflowWidth, other.maxOverflowWidth) && this.SingleLine == other.SingleLine;
        }

        public int SpriteLength { get { return null == Sprites ? 0 : Sprites.Length; } }
    }

    public class NTextGenerator
    {
        public float RealTextWidth;
        public float RealTextHeight;
        public StringBuilder RealTextBuilder = new StringBuilder();

        public List<NTextGlyph> characters = new List<NTextGlyph>();
        public readonly List<NTextGlyph> ImgGlyphs = new List<NTextGlyph>();
        public readonly List<NTextGlyph> TxtGlyphs = new List<NTextGlyph>();
        public readonly List<NTextGlyph> EffectGlyphs = new List<NTextGlyph>();
        public readonly Dictionary<int, NTextAnim> AnimGlyphs = new Dictionary<int, NTextAnim>();
        public readonly List<NTextLink> Links = new List<NTextLink>();
        public readonly List<NTextLine> lines = new List<NTextLine>();

        public int lineCount { get { return lines.Count; } }
        public int characterCount { get; private set; }
        public int characterCountVisible { get { return characterCount - 1; } }

        private float fontSpacing = 0.0f;
        private float ascent = 0.0f;

        public void PopulateAlways(List<NTextElement> elements, NTextGenerationSettings settings)
        {
            if (null == settings.font) return;

            RealTextWidth = RealTextHeight = characterCount = 0;
            RealTextBuilder.Length = 0;
            AnimGlyphs.Clear();
            Links.Clear();

            foreach (var glyph in characters)
                TextGlyphPool.Release(glyph);
            characters.Clear();
            ImgGlyphs.Clear();
            TxtGlyphs.Clear();

            foreach (var glyph in EffectGlyphs)
                TextGlyphPool.Release(glyph);
            EffectGlyphs.Clear();

            foreach (var line in lines)
                TextLinePool.Release(line);
            lines.Clear();
            var lineRect = TextLinePool.Get();
            lineRect.ParagraphIndent = settings.ParagraphIndent;
            lines.Add(lineRect);

            fontSpacing = settings.font.fontSize == 0 ? 0.0f : (settings.font.lineHeight + 1.0f) * settings.fontSize / settings.font.fontSize;
            ascent = settings.font.fontSize == 0 ? 0.0f : (settings.font.ascent + 1.0f) * settings.fontSize / settings.font.fontSize;

            var charUnderlineInfo = new CharacterInfo();
            var charPeriodInfo = new CharacterInfo();
            int tableWidth = 0;

            if (settings.font.dynamic)
            {
                settings.font.RequestCharactersInTexture(" _.\t", settings.fontSize, settings.fontStyle);
                if (settings.font.GetCharacterInfo('_', out charUnderlineInfo, settings.fontSize, settings.fontStyle))
                {
                    var uvSize = charUnderlineInfo.uvTopRight - charUnderlineInfo.uvTopLeft;
                    charUnderlineInfo.uvTopLeft = charUnderlineInfo.uvTopLeft + uvSize / 8;
                    charUnderlineInfo.uvTopRight = charUnderlineInfo.uvTopRight - uvSize / 8;
                    charUnderlineInfo.uvBottomRight = charUnderlineInfo.uvBottomRight - uvSize / 8;
                    charUnderlineInfo.uvBottomLeft = charUnderlineInfo.uvBottomLeft + uvSize / 8;
                }

                settings.font.GetCharacterInfo('.', out charPeriodInfo, settings.fontSize, settings.fontStyle);

                var charSpaceInfo = new CharacterInfo();
                if (settings.font.GetCharacterInfo(' ', out charSpaceInfo, settings.fontSize, settings.fontStyle))
                    tableWidth = 4 * charSpaceInfo.advance;

                foreach (var element in elements)
                {
                    if (!string.IsNullOrEmpty(element.Text))
                    {
                        var txt = element.Text;
                        int size = element.FontSize;
                        FontStyle style = element.FontStyle;
                        settings.font.RequestCharactersInTexture(txt, size, style);
                        RealTextBuilder.Append(txt);
                    }
                }
            }

            bool discardAfter = false;
            for (int elementIndex = 0; elementIndex < elements.Count; elementIndex++)
            {
                var element = elements[elementIndex];
                Color32 topColor = element.TopColor;
                Color32 bottomColor = element.BottomColor;

                if (!string.IsNullOrEmpty(element.Text))
                {
                    var txt = element.Text;
                    int size = element.FontSize;
                    FontStyle style = element.FontStyle;
                    Color32? underlineColor = element.UnderlineColor;
                    Color32? strikethroughColor = element.StrikethroughColor;

                    if (!string.IsNullOrEmpty(element.LinkParam))
                        Links.Add(new NTextLink { Param = element.LinkParam, GlyphStart = characters.Count });

                    CharacterInfo info;
                    for (int charIndex = 0; charIndex < txt.Length; charIndex++)
                    {
                        var ch = txt[charIndex];
                        if (ch == '\r' || ch == '\n')
                        {
                            if (settings.SingleLine)
                            {
                                CalcLineHeight(lines.Count - 1, settings);
                                discardAfter = true;
                                break;
                            }

                            var newGlyph = TextGlyphPool.Get();
                            newGlyph.Char = ch;
                            newGlyph.CustomCharTag = element.CustomCharTag;
                            characters.Add(newGlyph);
                            lineRect.endCharIdx = characters.Count;

                            if (ch == '\n')
                            {
                                lineRect = TextLinePool.Get();
                                lineRect.ParagraphIndent = settings.ParagraphIndent;
                                lineRect.startCharIdx = characters.Count;
                                lines.Add(lineRect);

                                if (!CalcLineHeight(lines.Count - 2, settings))
                                {
                                    ReleaseEndLine();
                                    ReleaseEndLine();
                                    discardAfter = true;
                                    break;
                                }
                            }
                        }
                        else if (ch == '\t')
                        {
                            var richChar = TextGlyphPool.Get();
                            richChar.Advance = (int)lineRect.Width / tableWidth * tableWidth + tableWidth - lineRect.Width;
                            richChar.CustomCharTag = element.CustomCharTag;
                            characters.Add(richChar);

                            lineRect.Width += richChar.Advance;
                            lineRect.endCharIdx = characters.Count;
                        }
                        else if (settings.font.GetCharacterInfo(ch, out info, size, style))
                        {
                            if ((settings.horizontalOverflow == HorizontalWrapMode.Wrap && lineRect.Width + info.advance > settings.generationExtents.x - lineRect.ParagraphIndent) ||
                                (settings.horizontalOverflow == HorizontalWrapMode.Overflow && settings.maxOverflowWidth > 0 && lineRect.Width + info.advance > settings.maxOverflowWidth - lineRect.ParagraphIndent))
                            {
                                if (settings.SingleLine)
                                {
                                    CalcLineHeight(lines.Count - 1, settings);
                                    discardAfter = true;
                                    break;
                                }

                                var lastLine = lineRect;
                                lineRect = TextLinePool.Get();
                                lineRect.startCharIdx = characters.Count;
                                lines.Add(lineRect);

                                if (ch <= 127 && ch != ' ')
                                {
                                    int glyphIndex = characters.Count;
                                    int idx = charIndex;
                                    while (idx > 0 && glyphIndex > lastLine.startCharIdx)
                                    {
                                        idx--;
                                        glyphIndex--;
                                        if (txt[idx] == ' ' || txt[idx] == '\t')
                                            break;
                                    }

                                    if (txt[idx] == ' ' || txt[idx] == '\t')
                                    {
                                        for (int index = glyphIndex + 1; index < characters.Count; index++)
                                        {
                                            lastLine.Width -= characters[index].Advance;
                                            lineRect.Width += characters[index].Advance;
                                        }

                                        lastLine.endCharIdx = glyphIndex + 1;
                                        lineRect.startCharIdx = glyphIndex + 1;
                                        lineRect.endCharIdx = characters.Count;
                                    }
                                }

                                if (!CalcLineHeight(lines.Count - 2, settings))
                                {
                                    ReleaseEndLine();
                                    ReleaseEndLine();
                                    discardAfter = true;
                                    break;
                                }
                            }

                            var richChar = TextGlyphPool.Get();
                            richChar.Char = ch;
                            richChar.Width = info.glyphWidth;
                            richChar.Height = info.glyphHeight;
                            richChar.MinX = info.minX;
                            richChar.MinY = info.minY;
                            richChar.Advance = info.advance;
                            richChar.UnderlineColor = underlineColor;
                            richChar.StrikethroughColor = strikethroughColor;
                            richChar.CustomCharTag = element.CustomCharTag;

                            richChar.VertexQuad[0].color = topColor;
                            richChar.VertexQuad[1].color = topColor;
                            richChar.VertexQuad[2].color = bottomColor;
                            richChar.VertexQuad[3].color = bottomColor;

                            richChar.VertexQuad[0].uv0 = info.uvTopLeft;
                            richChar.VertexQuad[1].uv0 = info.uvTopRight;
                            richChar.VertexQuad[2].uv0 = info.uvBottomRight;
                            richChar.VertexQuad[3].uv0 = info.uvBottomLeft;

                            characters.Add(richChar);

                            lineRect.Width += info.advance;
                            lineRect.endCharIdx = characters.Count;
                        }
                    }

                    if (!string.IsNullOrEmpty(element.LinkParam) && Links.Count > 0)
                        Links[Links.Count - 1].GlyphEnd = characters.Count - 1;

                    if (discardAfter)
                        break;
                }
                else
                {
                    int spriteIndex = element.SpriteIndex;
                    if (spriteIndex < 0 || spriteIndex >= settings.SpriteLength)
                        continue;

                    if (null == settings.Sprites[spriteIndex])
                        continue;

                    Sprite sprite = settings.Sprites[spriteIndex];

                    float spriteWidth = sprite.rect.width * element.SpriteScale;

                    if ((settings.horizontalOverflow == HorizontalWrapMode.Wrap && lineRect.Width + spriteWidth > settings.generationExtents.x - lineRect.ParagraphIndent) ||
                        (settings.horizontalOverflow == HorizontalWrapMode.Overflow && settings.maxOverflowWidth > 0 && lineRect.Width + spriteWidth > settings.maxOverflowWidth - lineRect.ParagraphIndent))
                    {
                        if (settings.SingleLine)
                        {
                            CalcLineHeight(lines.Count - 1, settings);
                            discardAfter = true;
                            break;
                        }

                        lineRect = TextLinePool.Get();
                        lineRect.startCharIdx = characters.Count;
                        lines.Add(lineRect);

                        if (!CalcLineHeight(lines.Count - 2, settings))
                        {
                            ReleaseEndLine();
                            ReleaseEndLine();
                            discardAfter = true;
                            break;
                        }
                    }

                    var uv = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);

                    var richChar = TextGlyphPool.Get();
                    richChar.Width = spriteWidth;
                    richChar.Height = sprite.rect.height * element.SpriteScale;
                    richChar.Advance = spriteWidth;
                    richChar.SpriteIndex = spriteIndex;
                    richChar.CustomCharTag = element.CustomCharTag;

                    if (element.SpriteAlign == NVerticalAlign.Top)
                        richChar.MinY = (ascent + charUnderlineInfo.minY - richChar.Height);
                    else if (element.SpriteAlign == NVerticalAlign.Middle)
                        richChar.MinY = (ascent + charUnderlineInfo.minY - richChar.Height) / 2;
                    else if (element.SpriteAlign == NVerticalAlign.Bottom)
                        richChar.MinY = charUnderlineInfo.minY;

                    richChar.VertexQuad[0].color = richChar.VertexQuad[1].color = richChar.VertexQuad[2].color = richChar.VertexQuad[3].color = Color.white;

                    richChar.VertexQuad[0].color = topColor;
                    richChar.VertexQuad[1].color = topColor;
                    richChar.VertexQuad[2].color = bottomColor;
                    richChar.VertexQuad[3].color = bottomColor;

                    richChar.VertexQuad[0].uv0 = new Vector2(uv.x, uv.w);
                    richChar.VertexQuad[1].uv0 = new Vector2(uv.z, uv.w);
                    richChar.VertexQuad[2].uv0 = new Vector2(uv.z, uv.y);
                    richChar.VertexQuad[3].uv0 = new Vector2(uv.x, uv.y);

                    if (element.AnimFrame > 0 && element.AnimLength > 1)
                        AnimGlyphs.Add(characters.Count, new NTextAnim { StartSpriteIndex = spriteIndex, AnimLength = element.AnimLength, AnimFrame = element.AnimFrame, LastSpriteIndex = spriteIndex });
                    characters.Add(richChar);

                    lineRect.Width += richChar.Advance;
                    lineRect.endCharIdx = characters.Count;
                }
            }

            if (!discardAfter)
            {
                if (!CalcLineHeight(lines.Count - 1, settings))
                    ReleaseEndLine();
            }

            // for InputField
            characters.Add(TextGlyphPool.Get());
            lineRect.endCharIdx = characters.Count;
            characterCount = characters.Count;

            float halfWidth = settings.generationExtents.x / 2;
            float halfHeight = settings.generationExtents.y / 2;
            Vector2 txtPivotPos = new Vector2((0.5f - settings.pivot.x) * settings.generationExtents.x, (0.5f - settings.pivot.y) * settings.generationExtents.y);
            float startY = 0;
            switch (settings.textAnchor)
            {
                case TextAnchor.UpperLeft:
                case TextAnchor.UpperCenter:
                case TextAnchor.UpperRight:
                    startY = txtPivotPos.y + halfHeight;
                    break;
                case TextAnchor.MiddleLeft:
                case TextAnchor.MiddleCenter:
                case TextAnchor.MiddleRight:
                    startY = txtPivotPos.y + (settings.verticalOverflow == VerticalWrapMode.Truncate ? Mathf.Min(halfHeight, RealTextHeight / 2f) : RealTextHeight / 2f);
                    break;
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    startY = txtPivotPos.y + (settings.verticalOverflow == VerticalWrapMode.Truncate ? Mathf.Min(halfHeight, RealTextHeight - halfHeight) : RealTextHeight - halfHeight);
                    break;
            }

            if (settings.OverflowEllipsis && lineCount > lines.Count && charPeriodInfo.index != 0 && lines.Count > 0)
            {
                lineRect = lines[lines.Count - 1];

                while (lineRect.endCharIdx > lineRect.startCharIdx && lineRect.Width + 3 * charPeriodInfo.advance > settings.generationExtents.x)
                {
                    var glyph = characters[lineRect.endCharIdx - 1];
                    //characters.RemoveAt(lineRect.endCharIdx - 1);
                    lineRect.Width -= glyph.Advance;
                    lineRect.endCharIdx--;
                }

                lineRect.Width += 3 * charPeriodInfo.advance;
                for (int i = 0; i < 3; i++)
                {
                    var glyphPeriod = TextGlyphPool.Get();
                    glyphPeriod.Char = '.';
                    glyphPeriod.Width = charPeriodInfo.glyphWidth;
                    glyphPeriod.Height = charPeriodInfo.glyphHeight;
                    glyphPeriod.MinX = charPeriodInfo.minX;
                    glyphPeriod.MinY = charPeriodInfo.minY;
                    glyphPeriod.Advance = charPeriodInfo.advance;

                    glyphPeriod.VertexQuad[0].color = settings.color;
                    glyphPeriod.VertexQuad[1].color = settings.color;
                    glyphPeriod.VertexQuad[2].color = settings.GradientColor ? settings.BottomColor : settings.color;
                    glyphPeriod.VertexQuad[3].color = settings.GradientColor ? settings.BottomColor : settings.color;

                    glyphPeriod.VertexQuad[0].uv0 = charPeriodInfo.uvTopLeft;
                    glyphPeriod.VertexQuad[1].uv0 = charPeriodInfo.uvTopRight;
                    glyphPeriod.VertexQuad[2].uv0 = charPeriodInfo.uvBottomRight;
                    glyphPeriod.VertexQuad[3].uv0 = charPeriodInfo.uvBottomLeft;
                    if (characters.Count > lineRect.endCharIdx)
                        characters[lineRect.endCharIdx] = glyphPeriod;
                    else
                        characters.Add(glyphPeriod);
                    lineRect.endCharIdx++;
                }
            }

            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                var thisLineRect = lines[lineIndex];
                thisLineRect.BaseLine = startY + thisLineRect.BaseLine;

                switch (settings.textAnchor)
                {
                    case TextAnchor.UpperLeft:
                    case TextAnchor.MiddleLeft:
                    case TextAnchor.LowerLeft:
                        thisLineRect.OffsetX = txtPivotPos.x - halfWidth + thisLineRect.ParagraphIndent;
                        break;
                    case TextAnchor.UpperCenter:
                    case TextAnchor.MiddleCenter:
                    case TextAnchor.LowerCenter:
                        thisLineRect.OffsetX = txtPivotPos.x - thisLineRect.Width / 2f + thisLineRect.ParagraphIndent;
                        break;
                    case TextAnchor.UpperRight:
                    case TextAnchor.MiddleRight:
                    case TextAnchor.LowerRight:
                        thisLineRect.OffsetX = txtPivotPos.x + halfWidth - thisLineRect.Width + thisLineRect.ParagraphIndent;
                        break;
                }

                float lineWidth = 0.0f;
                NTextGlyph glyphUnderline = null;
                NTextGlyph glyphStrikethrough = null;
                var imgPivot = new Vector2(0.5f, 0.5f);
                for (int glyphIndex = thisLineRect.startCharIdx; glyphIndex < thisLineRect.endCharIdx; glyphIndex++)
                {
                    var glyph = characters[glyphIndex];
                    if (glyph.IsImage())
                        ImgGlyphs.Add(glyph);
                    else
                        TxtGlyphs.Add(glyph);

                    float minX = thisLineRect.OffsetX + lineWidth + glyph.MinX;
                    float minY = thisLineRect.BaseLine + glyph.MinY;
                    if (glyph.IsImage())
                    {
                        minX += (settings.pivot.x - imgPivot.x) * settings.generationExtents.x;
                        minY += (settings.pivot.y - imgPivot.y) * settings.generationExtents.y;
                    }
                    float maxX = minX + glyph.Width;
                    float maxY = minY + glyph.Height;

                    UIVertex vertex0 = glyph.VertexQuad[0];
                    UIVertex vertex1 = glyph.VertexQuad[1];
                    UIVertex vertex2 = glyph.VertexQuad[2];
                    UIVertex vertex3 = glyph.VertexQuad[3];

                    vertex0.position = new Vector3(minX, maxY, 0);
                    vertex1.position = new Vector3(maxX, maxY, 0);
                    vertex2.position = new Vector3(maxX, minY, 0);
                    vertex3.position = new Vector3(minX, minY, 0);

                    // 描边
                    if (settings.Outline && glyph.Char != (char)0)
                    {
                        // retrieve UV minimum and maximum
                        Vector2 uvMin = Min(vertex0.uv0, vertex2.uv0);
                        Vector2 uvMax = Max(vertex0.uv0, vertex2.uv0);

                        // store UV min. and max. in the tangent of each vertex
                        Vector2 size = (uvMax - uvMin) * (2 << 12);
                        Vector2 packUvBounds = new Vector2(Mathf.Floor(size.x) + uvMin.x, Mathf.Floor(size.y) + uvMin.y);

                        // build two vectors
                        Vector2 vecUvX = vertex1.uv0 - vertex0.uv0;
                        Vector2 vecUvY = vertex2.uv0 - vertex1.uv0;
                        Vector2 vecX = vertex1.position - vertex0.position;
                        Vector2 vecY = vertex2.position - vertex1.position;

                        float vecXLen = Mathf.Abs(vecX.x);
                        float vecYLen = Mathf.Abs(vecY.y);
                        Vector2 xFactor = vecUvX / vecXLen;
                        Vector2 yFactor = vecUvY / vecYLen;

                        vertex0.position += BoardLeftUp;
                        vertex0.uv0 += (-xFactor - yFactor) * m_size;
                        vertex0.uv1 = packUvBounds;

                        vertex1.position += BoardRightUp;
                        vertex1.uv0 += (xFactor - yFactor) * m_size;
                        vertex1.uv1 = packUvBounds;

                        vertex2.position += BoardRightDown;
                        vertex2.uv0 += (xFactor + yFactor) * m_size;
                        vertex2.uv1 = packUvBounds;

                        vertex3.position += BoardLeftDown;
                        vertex3.uv0 += (-xFactor + yFactor) * m_size;
                        vertex3.uv1 = packUvBounds;
                    }

                    glyph.VertexQuad[0] = vertex0;
                    glyph.VertexQuad[1] = vertex1;
                    glyph.VertexQuad[2] = vertex2;
                    glyph.VertexQuad[3] = vertex3;

                    // <u> 下划线
                    if (glyph.UnderlineColor.HasValue)
                    {
                        if ((null == glyphUnderline || !ColorEquals(glyph.UnderlineColor.Value, glyphUnderline.VertexQuad[0].color)) && 0 != charUnderlineInfo.index)
                        {
                            minY = thisLineRect.BaseLine + charUnderlineInfo.minY;
                            maxY = minY + charUnderlineInfo.glyphHeight;

                            glyphUnderline = TextGlyphPool.Get();
                            glyphUnderline.VertexQuad[0].position = new Vector3(minX, maxY, 0);
                            glyphUnderline.VertexQuad[1].position = new Vector3(maxX, maxY, 0);
                            glyphUnderline.VertexQuad[2].position = new Vector3(maxX, minY, 0);
                            glyphUnderline.VertexQuad[3].position = new Vector3(minX, minY, 0);
                            glyphUnderline.VertexQuad[0].color = glyph.UnderlineColor.Value;
                            glyphUnderline.VertexQuad[1].color = glyph.UnderlineColor.Value;
                            glyphUnderline.VertexQuad[2].color = glyph.UnderlineColor.Value;
                            glyphUnderline.VertexQuad[3].color = glyph.UnderlineColor.Value;

                            glyphUnderline.VertexQuad[0].uv0 = charUnderlineInfo.uvTopLeft;
                            glyphUnderline.VertexQuad[1].uv0 = charUnderlineInfo.uvTopRight;
                            glyphUnderline.VertexQuad[2].uv0 = charUnderlineInfo.uvBottomRight;
                            glyphUnderline.VertexQuad[3].uv0 = charUnderlineInfo.uvBottomLeft;
                            glyphUnderline.VertexQuad[0].uv1 = charUnderlineInfo.uvTopLeft;
                            glyphUnderline.VertexQuad[1].uv1 = charUnderlineInfo.uvTopRight;
                            glyphUnderline.VertexQuad[2].uv1 = charUnderlineInfo.uvBottomRight;
                            glyphUnderline.VertexQuad[3].uv1 = charUnderlineInfo.uvBottomLeft;
                        }

                        if (null != glyphUnderline)
                        {
                            glyphUnderline.VertexQuad[1].position = new Vector3(maxX, glyphUnderline.VertexQuad[1].position.y, 0);
                            glyphUnderline.VertexQuad[2].position = new Vector3(maxX, glyphUnderline.VertexQuad[2].position.y, 0);
                        }
                    }
                    else if (null != glyphUnderline)
                    {
                        EffectGlyphs.Add(glyphUnderline);
                        glyphUnderline = null;
                    }

                    // <s>删除线
                    if (glyph.StrikethroughColor.HasValue)
                    {
                        if ((null == glyphStrikethrough || !ColorEquals(glyph.StrikethroughColor.Value, glyphStrikethrough.VertexQuad[0].color)) && 0 != charUnderlineInfo.index)
                        {
                            minY = thisLineRect.BaseLine + (ascent / 2 + charUnderlineInfo.minY);
                            maxY = minY + charUnderlineInfo.glyphHeight;

                            glyphStrikethrough = TextGlyphPool.Get();
                            glyphStrikethrough.VertexQuad[0].position = new Vector3(minX, maxY, 0);
                            glyphStrikethrough.VertexQuad[1].position = new Vector3(maxX, maxY, 0);
                            glyphStrikethrough.VertexQuad[2].position = new Vector3(maxX, minY, 0);
                            glyphStrikethrough.VertexQuad[3].position = new Vector3(minX, minY, 0);
                            glyphStrikethrough.VertexQuad[0].color = glyph.StrikethroughColor.Value;
                            glyphStrikethrough.VertexQuad[1].color = glyph.StrikethroughColor.Value;
                            glyphStrikethrough.VertexQuad[2].color = glyph.StrikethroughColor.Value;
                            glyphStrikethrough.VertexQuad[3].color = glyph.StrikethroughColor.Value;

                            glyphStrikethrough.VertexQuad[0].uv0 = charUnderlineInfo.uvTopLeft;
                            glyphStrikethrough.VertexQuad[1].uv0 = charUnderlineInfo.uvTopRight;
                            glyphStrikethrough.VertexQuad[2].uv0 = charUnderlineInfo.uvBottomRight;
                            glyphStrikethrough.VertexQuad[3].uv0 = charUnderlineInfo.uvBottomLeft;
                            glyphStrikethrough.VertexQuad[0].uv1 = charUnderlineInfo.uvTopLeft;
                            glyphStrikethrough.VertexQuad[1].uv1 = charUnderlineInfo.uvTopRight;
                            glyphStrikethrough.VertexQuad[2].uv1 = charUnderlineInfo.uvBottomRight;
                            glyphStrikethrough.VertexQuad[3].uv1 = charUnderlineInfo.uvBottomLeft;
                        }

                        if (null != glyphStrikethrough)
                        {
                            glyphStrikethrough.VertexQuad[1].position = new Vector3(maxX, glyphStrikethrough.VertexQuad[1].position.y, 0);
                            glyphStrikethrough.VertexQuad[2].position = new Vector3(maxX, glyphStrikethrough.VertexQuad[2].position.y, 0);
                        }
                    }
                    else if (null != glyphStrikethrough)
                    {
                        EffectGlyphs.Add(glyphStrikethrough);
                        glyphStrikethrough = null;
                    }

                    lineWidth += glyph.Advance;
                }

                if (null != glyphUnderline)
                    EffectGlyphs.Add(glyphUnderline);
                if (null != glyphStrikethrough)
                    EffectGlyphs.Add(glyphStrikethrough);
            }

            foreach (var link in Links)
            {
                if (link.GlyphStart < characters.Count && link.GlyphEnd < characters.Count)
                {
                    var glyphStart = characters[link.GlyphStart];
                    var glyphEnd = characters[link.GlyphEnd];
                    link.LeftTop = glyphStart.VertexQuad[0].position;
                    link.LeftBottom = glyphStart.VertexQuad[3].position;
                    link.RightTop = glyphEnd.VertexQuad[1].position;
                    link.RightBottom = glyphEnd.VertexQuad[2].position;
                }
            }
        }

        private bool CalcLineHeight(int lineIndex, NTextGenerationSettings settings)
        {
            if (lineIndex >= lines.Count)
                return false;

            float lastLineOffsetY = lineIndex > 0 ? lines[lineIndex - 1].OffsetY : 0f;
            float lastBaseLine = lineIndex > 0 ? lines[lineIndex - 1].BaseLine : 0f;
            var thisLineRect = lines[lineIndex];
            thisLineRect.Height = ascent;

            if (thisLineRect.Width + thisLineRect.ParagraphIndent > RealTextWidth)
                RealTextWidth = thisLineRect.Width + thisLineRect.ParagraphIndent;

            var lineSpacingMax = 0 == lineIndex ? ascent : fontSpacing;
            for (var glyphIndex = thisLineRect.startCharIdx; glyphIndex < thisLineRect.endCharIdx && glyphIndex < characters.Count; glyphIndex++)
            {
                var glyph = characters[glyphIndex];
                if (Mathf.Abs(glyph.Height) + glyph.MinY - lastLineOffsetY > lineSpacingMax)
                    lineSpacingMax = Mathf.Abs(glyph.Height) + glyph.MinY - lastLineOffsetY;
                if (Mathf.Abs(glyph.Height) + glyph.MinY > thisLineRect.Height)
                    thisLineRect.Height = Mathf.Abs(glyph.Height) + glyph.MinY;
                if (glyph.MinY < thisLineRect.OffsetY)
                    thisLineRect.OffsetY = glyph.MinY;
            }

            thisLineRect.BaseLine = lastBaseLine - (0 == lineIndex ? lineSpacingMax : lineSpacingMax * settings.lineSpacing);

            if (!settings.generateOutOfBounds && settings.verticalOverflow == VerticalWrapMode.Truncate && thisLineRect.BaseLine + thisLineRect.OffsetY < -settings.generationExtents.y)
                return false;  // 剩下的内容丢弃

            RealTextHeight = -thisLineRect.BaseLine - thisLineRect.OffsetY;
            return true;
        }

        private void ReleaseEndLine()
        {
            var endCharIdx = characters.Count;
            var line = lines[lines.Count - 1];
            for (int i = endCharIdx - 1; i >= line.startCharIdx; i--)
            {
                TextGlyphPool.Release(characters[i]);
            }
            characters.RemoveRange(line.startCharIdx, endCharIdx - line.startCharIdx);

            TextLinePool.Release(line);
            lines.RemoveAt(lines.Count - 1);
        }
 


        public const float m_size = 3.0f;
        private static readonly Vector3 BoardLeftUp = new Vector3(-m_size, m_size, 0);
        private static readonly Vector3 BoardRightUp = new Vector3(m_size, m_size, 0);
        private static readonly Vector3 BoardRightDown = new Vector3(m_size, -m_size, 0);
        private static readonly Vector3 BoardLeftDown = new Vector3(-m_size, -m_size, 0);

        private static Vector2 Min(Vector2 a, Vector2 b)
        {
            return new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
        }
        private static Vector2 Max(Vector2 a, Vector2 b)
        {
            return new Vector2(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        public static bool ColorEquals(Color32 a, Color32 b)
        {
            return a.a == b.a && a.b == b.b && a.g == b.g && a.r == b.r;
        }
    }
}