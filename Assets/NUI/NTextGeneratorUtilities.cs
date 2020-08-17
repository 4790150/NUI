// Unity C# reference source
using UnityEngine;

namespace NUI
{
    /// <summary>
    /// Rich Text Tags and Attribute definitions and their respective HashCode values.
    /// </summary>
    public enum TagHashCode : int
    {
        // Rich Text Tags
        None = 0,
        BOLD = 66,                          // <b>
        SLASH_BOLD = 1613,                  // </b>
        ITALIC = 73,                        // <i>
        SLASH_ITALIC = 1606,                // </i>
        UNDERLINE = 85,                     // <u>
        SLASH_UNDERLINE = 1626,             // </u>
        STRIKETHROUGH = 83,                 // <s>
        SLASH_STRIKETHROUGH = 1628,         // </s>
        COLOR = 81999901,                   // <color>
        SLASH_COLOR = 1909026194,           // </color>
        A = 65,                             // <a>
        SLASH_A = 1614,                     // </a>
        SIZE = 3061285,                     // <size>
        SLASH_SIZE = 58429962,              // </size>
        LINK = 2656128,                     // <link>
        SLASH_LINK = 57686191,              // </link>
        NO_PARSE = -408011596,              // <noparse>
        SLASH_NO_PARSE = -294095813,        // </noparse>
        ALIGN = 75138797,                   // <align>
        SLASH_ALIGN = 1916026786,           // </align>
        GRADIENT = -1999759898,             // <gradient>
        SLASH_GRADIENT = -1854491959,       // </gradient>
        SCALE = 100553336,                  // <scale>
        SLASH_SCALE = 1928413879,           // </scale>

        // Attributes
        SPRITE = -991527447,                // <sprite>
        INDEX = 84268030,               // <sprite index=7>
        ANIM_LENGTH = 1324370071,       // <sprite animLength=3>
        ANIM_FRAME = -1639155434,       // <sprite animFrame=5>

        // Named Colors
        RED = 91635,
        GREEN = 87065851,
        BLUE = 2457214,
        YELLOW = -882444668,
        ORANGE = -1108587920,
        BLACK = 81074727,
        WHITE = 105680263,
        PURPLE = -1250222130,
        AQUA = 2284356,
        BROWN = 81017702,
        CYAN = 2504597,
        DARKBLUE = -1960309910,
        FUCHSIA = -1002715645,
        GREY = 2638345,
        LIGHTBLUE = 341063360,
        LIME = 2656045,
        MAGENTA = -1812576107,
        MAROON = -1355621936,
        NAVY = 2876352,
        OLIVE = 95492953,
        SLIVER = -959314505,
        TEAL = 2947772,


        TOP = 89515,
        BOTTOM = -1625036945,
        MIDDLE = -1366021203,

        TRUE = 2932022,
        FALSE = 85422813,
    }

    /// <summary>
    /// Defines the type of value used by a rich text tag or tag attribute.
    /// </summary>
    enum TagValueType
    {
        None = 0x0,
        NumericalValue = 0x1,
        StringValue = 0x2,
        ColorValue = 0x4,
    }

    struct XmlTagAttribute
    {
        public int nameHashCode;
        public TagValueType valueType;
        public int valueStartIndex;
        public int valueLength;
        public int valueHashCode;
    }

    static class TextGeneratorUtilities
    {
        public static bool AttributeToColor(char[] richTextTag, XmlTagAttribute attr, out Color32 color)
        {
            color = Color.white;

            // <color=#FFF> 3 Hex (short hand)
            if (richTextTag[attr.valueStartIndex] == '#' && attr.valueLength == 4)
            {
                color = HexCharsToColor(richTextTag, attr.valueStartIndex, attr.valueLength);
                return true;
            }
            // <color=#FFF7> 4 Hex (short hand)
            else if (richTextTag[attr.valueStartIndex] == '#' && attr.valueLength == 5)
            {
                color = HexCharsToColor(richTextTag, attr.valueStartIndex, attr.valueLength);
                return true;
            }
            // <color=#FF00FF> 3 Hex pairs
            else if (richTextTag[attr.valueStartIndex] == '#' && attr.valueLength == 7)
            {
                color = HexCharsToColor(richTextTag, attr.valueStartIndex, attr.valueLength);
                return true;
            }
            // <color=#FF00FF00> 4 Hex pairs
            else if (richTextTag[attr.valueStartIndex] == '#' && attr.valueLength == 9)
            {
                color = HexCharsToColor(richTextTag, attr.valueStartIndex, attr.valueLength);
                return true;
            }

            // <color=name>
            switch ((TagHashCode)attr.valueHashCode)
            {
                case TagHashCode.RED: // <color=red>
                    color = Color.red;
                    return true;
                case TagHashCode.BLUE: // <color=blue>
                    color = Color.blue;
                    return true;
                case TagHashCode.BLACK: // <color=black>
                    color = Color.black;
                    return true;
                case TagHashCode.GREEN: // <color=green>
                    color = Color.green;
                    return true;
                case TagHashCode.WHITE: // <color=white>
                    color = Color.white;
                    return true;
                case TagHashCode.ORANGE: // <color=orange>
                    color = new Color32(255, 0xa5, 0, 255);
                    return true;
                case TagHashCode.PURPLE: // <color=purple>
                    color = new Color32(0x80, 00, 0x80, 255);
                    return true;
                case TagHashCode.YELLOW: // <color=yellow>
                    color = Color.yellow;
                    return true;
                case TagHashCode.AQUA:
                    color = Color.cyan;
                    return true;
                case TagHashCode.BROWN:
                    color = new Color32(0xa5, 0x2a, 0x2a, 255);
                    return true;
                case TagHashCode.CYAN:
                    color = Color.cyan;
                    return true;
                case TagHashCode.DARKBLUE:
                    color = new Color32(0, 0, 0xa0, 255);
                    return true;
                case TagHashCode.FUCHSIA:
                    color = Color.magenta;
                    return true;
                case TagHashCode.LIGHTBLUE:
                    color = new Color32(0xad, 0xd8, 0xe6, 255);
                    return true;
                case TagHashCode.LIME:
                    color = Color.green;
                    return true;
                case TagHashCode.MAGENTA:
                    color = Color.magenta;
                    return true;
                case TagHashCode.MAROON:
                    color = new Color32(0x80, 0, 0, 255);
                    return true;
                case TagHashCode.NAVY:
                    color = new Color32(0, 0, 0x80, 255);
                    return true;
                case TagHashCode.OLIVE:
                    color = new Color32(0x80, 0x80, 0, 255);
                    return true;
                case TagHashCode.SLIVER:
                    color = new Color32(0xc0, 0xc0, 0xc0, 255);
                    return true;
                case TagHashCode.TEAL:
                    color = new Color32(0, 0x80, 0x80, 255);
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Method to convert Hex Color values to Color32
        /// </summary>
        /// <param name="hexChars"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Color32 HexCharsToColor(char[] hexChars, int startIndex, int length)
        {
            if (length == 4)
            {
                byte r = (byte)(HexToInt(hexChars[1]) * 16 + HexToInt(hexChars[1]));
                byte g = (byte)(HexToInt(hexChars[2]) * 16 + HexToInt(hexChars[2]));
                byte b = (byte)(HexToInt(hexChars[3]) * 16 + HexToInt(hexChars[3]));

                return new Color32(r, g, b, 255);
            }
            if (length == 5)
            {
                byte r = (byte)(HexToInt(hexChars[startIndex + 1]) * 16 + HexToInt(hexChars[startIndex + 1]));
                byte g = (byte)(HexToInt(hexChars[startIndex + 2]) * 16 + HexToInt(hexChars[startIndex + 2]));
                byte b = (byte)(HexToInt(hexChars[startIndex + 3]) * 16 + HexToInt(hexChars[startIndex + 3]));
                byte a = (byte)(HexToInt(hexChars[startIndex + 4]) * 16 + HexToInt(hexChars[startIndex + 4]));

                return new Color32(r, g, b, a);
            }
            if (length == 7)
            {
                byte r = (byte)(HexToInt(hexChars[startIndex + 1]) * 16 + HexToInt(hexChars[startIndex + 2]));
                byte g = (byte)(HexToInt(hexChars[startIndex + 3]) * 16 + HexToInt(hexChars[startIndex + 4]));
                byte b = (byte)(HexToInt(hexChars[startIndex + 5]) * 16 + HexToInt(hexChars[startIndex + 6]));

                return new Color32(r, g, b, 255);
            }
            if (length == 9)
            {
                byte r = (byte)(HexToInt(hexChars[startIndex + 1]) * 16 + HexToInt(hexChars[startIndex + 2]));
                byte g = (byte)(HexToInt(hexChars[startIndex + 3]) * 16 + HexToInt(hexChars[startIndex + 4]));
                byte b = (byte)(HexToInt(hexChars[startIndex + 5]) * 16 + HexToInt(hexChars[startIndex + 6]));
                byte a = (byte)(HexToInt(hexChars[startIndex + 7]) * 16 + HexToInt(hexChars[startIndex + 8]));

                return new Color32(r, g, b, a);
            }

            return new Color32(255, 255, 255, 255);
        }

        /// <summary>
        /// Method to convert Hex to Int
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static int HexToInt(char hex)
        {
            switch (hex)
            {
                case '0':
                    return 0;
                case '1':
                    return 1;
                case '2':
                    return 2;
                case '3':
                    return 3;
                case '4':
                    return 4;
                case '5':
                    return 5;
                case '6':
                    return 6;
                case '7':
                    return 7;
                case '8':
                    return 8;
                case '9':
                    return 9;
                case 'A':
                    return 10;
                case 'B':
                    return 11;
                case 'C':
                    return 12;
                case 'D':
                    return 13;
                case 'E':
                    return 14;
                case 'F':
                    return 15;
                case 'a':
                    return 10;
                case 'b':
                    return 11;
                case 'c':
                    return 12;
                case 'd':
                    return 13;
                case 'e':
                    return 14;
                case 'f':
                    return 15;
            }
            return 15;
        }

        /// <summary>
        /// Extracts a float value from char[] assuming we know the position of the start, end and decimal point.
        /// </summary>
        /// <param name="chars"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static bool ConvertToFloat(char[] chars, int startIndex, int length, out float value)
        {
            int lastIndex = 0;
            value = ConvertToFloat(chars, startIndex, length, out lastIndex);
            return lastIndex > startIndex;
        }

        /// <summary>
        /// Extracts a float value from char[] given a start index and length.
        /// </summary>
        /// <param name="chars"></param> The Char[] containing the numerical sequence.
        /// <param name="startIndex"></param> The index of the start of the numerical sequence.
        /// <param name="length"></param> The length of the numerical sequence.
        /// <param name="lastIndex"></param> Index of the last character in the validated sequence.
        /// <returns></returns>
        public static float ConvertToFloat(char[] chars, int startIndex, int length, out int lastIndex)
        {
            int endIndex = startIndex + length;

            bool isIntegerValue = true;
            float decimalPointMultiplier = 0;

            // Set value multiplier checking the first character to determine if we are using '+' or '-'
            int valueSignMultiplier = 1;
            if (chars[startIndex] == '+')
            {
                valueSignMultiplier = 1;
                startIndex += 1;
            }
            else if (chars[startIndex] == '-')
            {
                valueSignMultiplier = -1;
                startIndex += 1;
            }

            float value = 0;

            for (int i = startIndex; i < endIndex; i++)
            {
                uint c = chars[i];

                if (c >= '0' && c <= '9' || c == '.')
                {
                    if (c == '.')
                    {
                        isIntegerValue = false;
                        decimalPointMultiplier = 0.1f;
                        continue;
                    }

                    //Calculate integer and floating point value
                    if (isIntegerValue)
                        value = value * 10 + (c - 48);
                    else
                    {
                        value = value + (c - 48) * decimalPointMultiplier;
                        decimalPointMultiplier *= 0.1f;
                    }
                }
                else
                {
                    lastIndex = i;
                    return value * valueSignMultiplier;
                }
            }

            lastIndex = endIndex;
            return value * valueSignMultiplier;
        }

        public static int LengthSafe(this System.Array self)
        {
            if (null == self) return 0;
            return self.Length;
        }
    }
}