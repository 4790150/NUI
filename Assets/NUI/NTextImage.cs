using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NUI
{
    [ExecuteInEditMode]
    public class NTextImage : MaskableGraphic
    {
        public List<NRichTextGlyph> ImgGlyphs = new List<NRichTextGlyph>();
        public Texture Texture;

        protected NTextImage()
        {
            useLegacyMeshGeneration = false;
        }

        /// <summary>
        /// Text's texture comes from the font.
        /// </summary>
        public override Texture mainTexture
        {
            get
            {
                if (Texture != null)
                    return Texture;

                if (m_Material != null)
                    return m_Material.mainTexture;

                return base.mainTexture;
            }
        }

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            toFill.Clear();

            foreach (var glyph in ImgGlyphs)
            {
                toFill.AddUIVertexQuad(glyph.VertexQuad);
            }
        }
    }
}