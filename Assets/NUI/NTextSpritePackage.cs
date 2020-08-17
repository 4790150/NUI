using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NUI
{
    public class NTextSpritePackage : ScriptableObject
    {

        public Sprite[] Sprites;

        public int DefaultAnimLength = 1;
        public int DefaultAnimFrame = 15;

        public static bool Compare(NTextSpritePackage self, NTextSpritePackage other)
        {
            if (self == other) return true;

            return self.Sprites == other.Sprites && self.DefaultAnimLength == other.DefaultAnimLength && self.DefaultAnimFrame == other.DefaultAnimFrame;
        }
    }
}
