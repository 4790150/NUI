using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace NUI
{
    public enum NVerticalAlign : byte
    {
        Bottom,
        Middle,
        Top
    }

    public class NTextElement
    {
        public string Text;
        public string LinkParam;
        public int FontSize;
        public FontStyle FontStyle;
        public Color32 TopColor;
        public Color32 BottomColor;
        public Color32? UnderlineColor;
        public Color32? StrikethroughColor;

        public int SpriteIndex;
        public float SpriteScale;
        public NVerticalAlign SpriteAlign;
        public int AnimLength;
        public int AnimFrame;

        public int CustomCharTag;
    }

    public class NTextGlyph
    {
        public char Char;
        public UIVertex[] VertexQuad = new UIVertex[4];
        public float MinX;
        public float MinY;
        public float Width;
        public float Height;
        public float Advance;
        public Color32? UnderlineColor;
        public Color32? StrikethroughColor;
        public int SpriteIndex;

        public int CustomCharTag;

        public bool IsImage()
        {
            return Char == '\0' && Advance > 0;
        }
    }

    public class NTextAnim
    {
        public int StartSpriteIndex;
        public int LastSpriteIndex;
        public int AnimLength;
        public int AnimFrame;
    }

    public class NTextLine
    {
        public float Width;
        public float Height;

        public float BaseLine;

        public float OffsetX;
        public float OffsetY;

        public int ParagraphIndent;

        public int startCharIdx;
        public int endCharIdx;
    }

    public class NTextLink
    {
        public string Param;
        public int GlyphStart;
        public int GlyphEnd;
        public Vector2 LeftTop;
        public Vector2 LeftBottom;
        public Vector2 RightTop;
        public Vector2 RightBottom;
    }

    public class NText : Text, IPointerDownHandler, IPointerUpHandler, ICanvasRaycastFilter
    {
        public Action<string> OnLink;

        public float RealTextWidth
        {
            get
            {
                Populate();
                return cachedTextGenerator.RealTextWidth;
            }
        }

        public float RealTextHeight
        {
            get
            {
                Populate();
                return cachedTextGenerator.RealTextHeight;
            }
        }

        public string RealText
        {
            get
            {
                Populate();
                return cachedTextGenerator.RealTextBuilder.ToString();
            }
        }

        public Dictionary<int, Texture> ImageTexture = new Dictionary<int, Texture>();

        private Dictionary<Texture, NTextImage> _richImages = new Dictionary<Texture, NTextImage>();

        [SerializeField]
        private bool _gradientColor;
        public bool GradientColor
        {
            get { return _gradientColor; }
            set
            {
                if (_gradientColor != value)
                {
                    _gradientColor = value;
                    SetAllDirty();
                }
            }
        }

        [SerializeField]
        private Color _bottomColor;
        public Color BottomColor
        {
            get { return _bottomColor; }
            set
            {
                if (_bottomColor != value)
                {
                    _bottomColor = value;
                    SetAllDirty();
                }
            }
        }

        [SerializeField]
        private bool _outline;
        public bool Outline
        {
            get { return _outline; }
            set
            {
                if (_outline != value)
                {
                    _outline = value;
                    SetAllDirty();
                }
            }
        }

        [SerializeField]
        private Color _outlineColor = Color.black;
        public Color OutlineColor
        {
            get { return _outlineColor; }
            set
            {
                if (_outlineColor != value)
                {
                    _outlineColor = value;
                    SetAllDirty();
                }
            }
        }

        [SerializeField]
        private float _outlineSize = 1f;
        public float OutlineSize
        {
            get { return _outlineSize; }
            set
            {
                if (!_outlineSize.Equals(value))
                {
                    _outlineSize = value;
                    SetAllDirty();
                }
            }
        }

        private static Material sDefaultEffectMaterial;
        protected static Material defaultEffectMaterial
        {
            get
            {
                if (null == sDefaultEffectMaterial)
                    sDefaultEffectMaterial = Resources.Load<Material>("Text");
                return sDefaultEffectMaterial;
            }
        }

        protected static Queue<Material> EffectMaterialPool = new Queue<Material>();

        //[SerializeField]
        //private bool _shadow;
        //public bool Shadow
        //{
        //    get { return _shadow; }
        //    set
        //    {
        //        if (_shadow != value)
        //        {
        //            _shadow = value;
        //            SetAllDirty();
        //        }
        //    }
        //}

        //[SerializeField]
        //private Vector2 _shadowDistance;
        //public Vector2 ShadowDistance
        //{
        //    get { return _shadowDistance; }
        //    set
        //    {
        //        if (_shadowDistance == value)
        //            return;

        //        _shadowDistance = value;
        //        SetAllDirty();
        //    }
        //}

        //[SerializeField]
        //private Color _shadowColor;
        //public Color ShadowColor
        //{
        //    get { return _shadowColor; }
        //    set
        //    {
        //        if (_shadowColor == value)
        //            return;

        //        _shadowColor = value;
        //        SetAllDirty();
        //    }
        //}

        [SerializeField]
        private NTextSpritePackage _spritePackage;

        public NTextSpritePackage SpritePackage
        {
            get { return _spritePackage; }
            set
            {
                if (NTextSpritePackage.Compare(_spritePackage, value))
                    return;

                _spritePackage = value;
                SetAllDirty();
            }
        }

        [SerializeField]
        private Sprite[] _sprites;
        public Sprite[] Sprites
        {
            get { return _sprites; }
            set
            {
                if (_sprites == value)
                    return;

                _sprites = value;
                SetAllDirty();
            }
        }

        [SerializeField]
        private int _defaultAnimLength = 1;
        public int DefaultAnimLength
        {
            get { return _defaultAnimLength; }
            set
            {
                if (_defaultAnimLength == value)
                    return;

                _defaultAnimLength = value;
                SetAllDirty();
            }
        }

        [SerializeField]
        private int _defaultAnimFrame = 1;
        public int DefaultAnimFrame
        {
            get { return _defaultAnimFrame; }
            set
            {
                if (_defaultAnimFrame == value)
                    return;

                _defaultAnimFrame = value;
                SetAllDirty();
            }
        }

        [SerializeField]
        private float _defaultSpriteScale = 1.0f;
        public float DefaultSpriteScale
        {
            get { return _defaultSpriteScale; }
            set
            {
                if (_defaultSpriteScale == value)
                    return;

                _defaultSpriteScale = value;
                SetAllDirty();
            }
        }

        [SerializeField]
        private NVerticalAlign _defaultSpriteAlign = NVerticalAlign.Bottom;
        public NVerticalAlign DefaultSpriteAlign
        {
            get { return _defaultSpriteAlign; }
            set
            {
                if (_defaultSpriteAlign == value)
                    return;

                _defaultSpriteAlign = value;
                SetAllDirty();
            }
        }


        [SerializeField]
        private int _paragraphIndent;
        public int ParagraphIndent
        {
            get { return _paragraphIndent; }
            set
            {
                if (_paragraphIndent == value)
                    return;

                _paragraphIndent = value;
                SetAllDirty();
            }
        }

        [Tooltip(@"Horizontal Overflow:Wrap && Vertical Overflow:Truncate")]
        [SerializeField] private bool _overflowEllipsis;
        public bool OverflowEllipsis
        {
            get { return _overflowEllipsis; }
            set
            {
                if (_overflowEllipsis == value)
                    return;

                _overflowEllipsis = value;
                SetAllDirty();
            }
        }

        [SerializeField] private float _maxOverflowWidth;

        public float MaxOverflowWidth
        {
            get { return _maxOverflowWidth; }
            set
            {
                if (_maxOverflowWidth.Equals(value))
                    return;

                _maxOverflowWidth = value;
                SetAllDirty();
            }
        }

        private bool m_HasGenerated;
        private string _lastText;
        private NTextGenerationSettings _lastSettings;
        public new NTextGenerator cachedTextGenerator = new NTextGenerator();
        public Dictionary<int, NTextElement> CustomElements = new Dictionary<int, NTextElement>();
        private List<NTextElement> textElements = new List<NTextElement>();
        private static List<NTextElement> s_textElements = new List<NTextElement>();

        protected NText()
        {
            useLegacyMeshGeneration = false;
        }

        private static int imageCount = 0;
        private static NGameObjectPool _imagePool;

        protected static NGameObjectPool ImagePool
        {
            get
            {
                if (null == _imagePool)
                {
                    var objectPool = new GameObject("NTextImagePool") { hideFlags = HideFlags.HideAndDontSave };
                    if (Application.isPlaying)
                        GameObject.DontDestroyOnLoad(objectPool);
                    _imagePool = new NGameObjectPool(objectPool);
                }

                return _imagePool;
            }
        }

        private void CreateRichImage()
        {
            List<NTextImage> imageList = NListPool<NTextImage>.Get();

#if UNITY_EDITOR
            if (0 == _richImages.Count)
            {
                var childTransform = transform.GetComponentsInChildren<NTextImage>();
                imageList.AddRange(childTransform);
            }
            else
#endif
            {
                foreach (var pair in _richImages)
                    imageList.Add(pair.Value);
                _richImages.Clear();
            }

            foreach (var glyph in ImgGlyphs)
            {
                var texture = ImageTexture[glyph.Value.SpriteIndex];

                if (_richImages.ContainsKey(texture))
                {
                    _richImages[texture].ImgGlyphs.Add(glyph.Value);
                }
                else
                {
                    if (imageList.Count > 0)
                    {
                        var richImage = imageList[imageList.Count - 1];
                        imageList.RemoveAt(imageList.Count - 1);
                        richImage.ImgGlyphs.Clear();

                        var childRectTransform = richImage.GetComponent<RectTransform>();
                        childRectTransform.SetParent(transform);
                        childRectTransform.localPosition = Vector3.zero;
                        childRectTransform.localScale = Vector3.one;
                        childRectTransform.localRotation = Quaternion.identity;
                        childRectTransform.anchorMin = Vector2.zero;
                        childRectTransform.anchorMax = Vector2.one;
                        childRectTransform.sizeDelta = Vector2.zero;
                        childRectTransform.offsetMin = Vector2.zero;
                        childRectTransform.offsetMax = Vector2.one;


                        richImage.ImgGlyphs.Add(glyph.Value);
                        richImage.Texture = texture;
                        _richImages.Add(richImage.Texture, richImage);
                        richImage.SetAllDirty();
                    }
                    else
                    {
#if UNITY_EDITOR
                        var imgGo = new GameObject("NTextImage" + imageCount++, typeof(NTextImage));
                        imgGo.hideFlags = HideFlags.HideAndDontSave;
                        imgGo.layer = gameObject.layer;
#else
                        var imgGo = ImagePool.PopObject();
                        if (null == imgGo)
                        {
                            imgGo = new GameObject("NTextImage" + ImageCount++, typeof(NTextImage));
                            imgGo.hideFlags = HideFlags.HideAndDontSave;
                            imgGo.layer = gameObject.layer;
                        }
#endif

                        var childRectTransform = imgGo.GetComponent<RectTransform>();
                        childRectTransform.SetParent(transform);
                        childRectTransform.localPosition = Vector3.zero;
                        childRectTransform.localScale = Vector3.one;
                        childRectTransform.localRotation = Quaternion.identity;
                        childRectTransform.anchorMin = Vector2.zero;
                        childRectTransform.anchorMax = Vector2.one;
                        childRectTransform.sizeDelta = Vector2.zero;
                        childRectTransform.offsetMin = Vector2.zero;
                        childRectTransform.offsetMax = Vector2.one;

                        var richImage = imgGo.GetComponent<NTextImage>();
                        richImage.ImgGlyphs.Add(glyph.Value);
                        richImage.Texture = texture;
                        _richImages.Add(richImage.Texture, richImage);
                        richImage.SetAllDirty();
                    }
                }
            }

#if UNITY_EDITOR
            for (int i = 0; i < imageList.Count; i++)
            {
                imageList[i].ImgGlyphs.Clear();
                imageList[i].Texture = null;
                DestroyImmediate(imageList[i].gameObject);
            }
#else
            for (int i = 0; i < imageList.Count; i++)
            {
                imageList[i].ImgGlyphs.Clear();
                imageList[i].Texture = null;
                ImagePool.PushObject(imageList[i].gameObject);
            }
#endif
            NListPool<NTextImage>.Release(imageList);
        }

        public override void SetAllDirty()
        {
            base.SetAllDirty();
            foreach (var pair in _richImages)
                pair.Value.SetAllDirty();
        }

        public List<NTextGlyph> Characters
        {
            get { return cachedTextGenerator.characters; }
        }

        public Dictionary<int, NTextGlyph> ImgGlyphs
        {
            get { return cachedTextGenerator.ImgGlyphs; }
        }

        private List<NTextGlyph> TxtGlyphs
        {
            get { return cachedTextGenerator.TxtGlyphs; }
        }

        private List<NTextGlyph> EffectGlyphs
        {
            get { return cachedTextGenerator.EffectGlyphs; }
        }

        private Dictionary<int, NTextAnim> AnimGlyphs
        {
            get { return cachedTextGenerator.AnimGlyphs; }
        }

        private List<NTextLink> Links
        {
            get { return cachedTextGenerator.Links; }
        }

        public List<NTextLine> lines
        {
            get { return cachedTextGenerator.lines; }
        }


        public List<NTextGlyph> characters
        {
            get { return cachedTextGenerator.characters; }
        }

        public int characterCountVisible { get { return characters.Count; } }
        
        public NTextGenerationSettings GetGenerationSettings(TextGenerationSettings settings)
        {
            NTextGenerationSettings generationSettings = new NTextGenerationSettings
            {
                generationExtents = settings.generationExtents,
                fontSize = settings.fontSize,
                resizeTextMinSize = settings.resizeTextMinSize,
                resizeTextMaxSize = settings.resizeTextMaxSize,
                textAnchor = settings.textAnchor,
                alignByGeometry = settings.alignByGeometry,
                scaleFactor = settings.scaleFactor,
                color = settings.color,
                font = settings.font,
                pivot = settings.pivot,
                richText = settings.richText,
                lineSpacing = settings.lineSpacing,
                fontStyle = settings.fontStyle,
                resizeTextForBestFit = settings.resizeTextForBestFit,
                updateBounds = settings.updateBounds,
                horizontalOverflow = settings.horizontalOverflow,
                verticalOverflow = settings.verticalOverflow,

                Sprites = null == _spritePackage ? _sprites : _spritePackage.Sprites,
                DefaultAnimLength = null == _spritePackage ? _defaultAnimLength : _spritePackage.DefaultAnimLength,
                DefaultAnimFrame = null == _spritePackage ? _defaultAnimFrame : _spritePackage.DefaultAnimFrame,
                DefaultSpriteScale = _defaultSpriteScale,
                DefaultSpriteAlign = _defaultSpriteAlign,
                GradientColor = GradientColor,
                BottomColor = BottomColor,
                Outline = Outline,
                OutlineColor = OutlineColor,
                OutlineSize = OutlineSize,
                //Shadow = Shadow,
                //ShadowColor = ShadowColor,
                //ShadowDistance = ShadowDistance,
                ParagraphIndent = ParagraphIndent,
                OverflowEllipsis = OverflowEllipsis,
                maxOverflowWidth = MaxOverflowWidth,
            };
            return generationSettings;
        }

        protected bool Populate()
        {
            var settings = GetGenerationSettings(GetGenerationSettings(rectTransform.rect.size));

            return _dataDirty = Populate(text, settings, textElements) | _dataDirty;
        }

        protected bool Populate(string text, NTextGenerationSettings settings, List<NTextElement> elements)
        {
            if (m_HasGenerated && settings.Equals(_lastSettings) && text == _lastText)
                return false;

            ImageTexture.Clear();
            var sprites = null == _spritePackage ? _sprites : _spritePackage.Sprites;
            for (int i = 0; i < sprites.LengthSafe(); i++)
            {
                if (null != sprites[i])
                    ImageTexture[i] = sprites[i].texture;
            }

            NTextParser.Parse(text, settings, elements, CustomElements);

            m_DisableFontTextureRebuiltCallback = true;
            cachedTextGenerator.PopulateAlways(elements, settings);
            m_DisableFontTextureRebuiltCallback = false;

            _lastText = text;
            _lastSettings = settings;
            m_HasGenerated = true;
            return true;
        }

        protected void ProcessMaterial()
        {
            if (Outline && canvas)
            {
                if (null == m_Material)
                    m_Material = defaultEffectMaterial;

                if (m_Material == defaultEffectMaterial)
                {
                    if (OutlineColor != defaultEffectMaterial.GetColor("_OutlineColor") ||
                        !OutlineSize.Equals(defaultEffectMaterial.GetFloat("_OutlineSize")))
                    {
                        if (EffectMaterialPool.Count > 0)
                            m_Material = EffectMaterialPool.Dequeue();
                        else
                            m_Material = new Material(defaultEffectMaterial);
                        m_Material.SetFloat("_OutlineSize", _outlineSize);
                        m_Material.SetColor("_OutlineColor", _outlineColor);
                    }
                }
                else
                {
                    if (OutlineColor == defaultEffectMaterial.GetColor("_OutlineColor") &&
                        OutlineSize.Equals(defaultEffectMaterial.GetFloat("_OutlineSize")))
                    {
                        m_Material = defaultEffectMaterial;
                    }
                    else
                    {
                        m_Material.SetFloat("_OutlineSize", _outlineSize);
                        m_Material.SetColor("_OutlineColor", _outlineColor);
                    }
                }

                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            }
            else
            {
                if (null != m_Material)
                {
                    if (m_Material != defaultEffectMaterial)
                        EffectMaterialPool.Enqueue(m_Material);

                    m_Material = null;
                }
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (Populate())
            {
                ProcessMaterial();

                if (this.gameObject.activeInHierarchy && this.enabled && !_coAnimation)
                    StartCoroutine(UpdateSprite());
            }

            vh.Clear();
            foreach (var glyph in TxtGlyphs)
                vh.AddUIVertexQuad(glyph.VertexQuad);
            foreach (var glyph in EffectGlyphs)
                vh.AddUIVertexQuad(glyph.VertexQuad);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _coAnimation = false;
        }

        protected void OnDistroy()
        {
            foreach (var img in _richImages)
            {
#if UNITY_EDITOR
                DestroyImmediate(img.Value.gameObject);
#else
                ImagePool.PushGameObject(img.Value.gameObject);
#endif
            }
            _richImages.Clear();
        }

        private bool _dataDirty = false;

        protected bool _coAnimation = false;
        protected IEnumerator UpdateSprite()
        {
            _coAnimation = true;

            var sprites = null == _spritePackage ? _sprites : _spritePackage.Sprites;
            if (sprites.LengthSafe() == 0)
            {
                _coAnimation = false;
                yield break;
            }

            while (true)
            {
                yield return null;

                foreach (var pair in AnimGlyphs)
                {
                    var glyphIndex = pair.Key;
                    var anim = pair.Value;
                    if (anim.AnimFrame > 0 && Time.frameCount % anim.AnimFrame == 0)
                    {
                        ++anim.LastSpriteIndex;
                        if (anim.LastSpriteIndex > anim.StartSpriteIndex + anim.AnimLength)
                            anim.LastSpriteIndex = anim.StartSpriteIndex;

                        if (glyphIndex < Characters.Count)
                        {
                            NTextGlyph imgGlyph = Characters[glyphIndex];

                            UIVertex vertex0 = imgGlyph.VertexQuad[0];
                            UIVertex vertex1 = imgGlyph.VertexQuad[1];
                            UIVertex vertex2 = imgGlyph.VertexQuad[2];
                            UIVertex vertex3 = imgGlyph.VertexQuad[3];

                            Sprite sprite = sprites[anim.LastSpriteIndex];
                            imgGlyph.SpriteIndex = anim.LastSpriteIndex;

                            var uv = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);

                            vertex3.uv0 = new Vector2(uv.x, uv.y);
                            vertex0.uv0 = new Vector2(uv.x, uv.w);
                            vertex1.uv0 = new Vector2(uv.z, uv.w);
                            vertex2.uv0 = new Vector2(uv.z, uv.y);

                            imgGlyph.VertexQuad[0] = vertex0;
                            imgGlyph.VertexQuad[1] = vertex1;
                            imgGlyph.VertexQuad[2] = vertex2;
                            imgGlyph.VertexQuad[3] = vertex3;

                            _dataDirty = true;
                        }
                    }
                }

                if (_dataDirty)
                {
                    CreateRichImage();
                }

                if (0 == AnimGlyphs.Count)
                {
                    _coAnimation = false;
                    yield break;
                }
            }
        }

        public override float preferredWidth
        {
            get
            {
                Vector2 extents = rectTransform.rect.size;
                var settings = GetGenerationSettings(GetGenerationSettings(extents));
                settings.horizontalOverflow = HorizontalWrapMode.Overflow;
                settings.verticalOverflow = VerticalWrapMode.Overflow;

                Populate(text, settings, s_textElements);
                return cachedTextGenerator.RealTextWidth;
            }
        }

        public override float preferredHeight
        {
            get
            {
                Vector2 extents = rectTransform.rect.size;
                var settings = GetGenerationSettings(GetGenerationSettings(extents));
                //settings.horizontalOverflow = HorizontalWrapMode.Overflow;
                settings.verticalOverflow = VerticalWrapMode.Overflow;

                Populate(text, settings, s_textElements);
                return cachedTextGenerator.RealTextHeight;
            }
        }

        private int _linkClick = -1;
        public virtual void OnPointerDown(PointerEventData eventData)
        {
            Vector2 _eventPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, eventData.position, eventData.pressEventCamera, out _eventPosition);

            for (var linkIndex = 0; linkIndex < Links.Count; linkIndex++)
            {
                var link = Links[linkIndex];
                if (_eventPosition.y < link.LeftBottom.y && _eventPosition.y > link.RightTop.y ||
                    (_eventPosition.y < link.LeftTop.y && _eventPosition.y > link.LeftBottom.y && _eventPosition.x > link.LeftTop.x && _eventPosition.x < (link.RightTop.y < link.LeftBottom.y ? float.MaxValue : link.RightTop.x)) ||
                    (_eventPosition.y < link.RightTop.y && _eventPosition.y > link.RightBottom.y && _eventPosition.x < link.RightTop.x && _eventPosition.x > (link.RightTop.y < link.LeftBottom.y ? float.MinValue : link.LeftTop.x)))
                {
                    for (int glyphIndex = link.GlyphStart; glyphIndex <= link.GlyphEnd; glyphIndex++)
                    {
                        if (glyphIndex < Characters.Count)
                        {
                            var txtGlyph = Characters[glyphIndex];

                            txtGlyph.VertexQuad[0].color = new Color32(
                                (byte)(255 - txtGlyph.VertexQuad[0].color.r),
                                (byte)(255 - txtGlyph.VertexQuad[0].color.g),
                                (byte)(255 - txtGlyph.VertexQuad[0].color.b),
                                txtGlyph.VertexQuad[0].color.a);
                            txtGlyph.VertexQuad[1].color = new Color32(
                                (byte)(255 - txtGlyph.VertexQuad[1].color.r),
                                (byte)(255 - txtGlyph.VertexQuad[1].color.g),
                                (byte)(255 - txtGlyph.VertexQuad[1].color.b),
                                txtGlyph.VertexQuad[1].color.a);
                            txtGlyph.VertexQuad[2].color = new Color32(
                                (byte)(255 - txtGlyph.VertexQuad[2].color.r),
                                (byte)(255 - txtGlyph.VertexQuad[2].color.g),
                                (byte)(255 - txtGlyph.VertexQuad[2].color.b),
                                txtGlyph.VertexQuad[2].color.a);
                            txtGlyph.VertexQuad[3].color = new Color32(
                                (byte)(255 - txtGlyph.VertexQuad[3].color.r),
                                (byte)(255 - txtGlyph.VertexQuad[3].color.g),
                                (byte)(255 - txtGlyph.VertexQuad[3].color.b),
                                txtGlyph.VertexQuad[3].color.a);
                        }
                    }

                    _linkClick = linkIndex;
                    UpdateGeometry();
                    break;
                }
            }
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (_linkClick >= 0 && _linkClick < Links.Count)
            {
                var link = Links[_linkClick];
                for (int glyphIndex = link.GlyphStart; glyphIndex <= link.GlyphEnd; glyphIndex++)
                {
                    if (glyphIndex < Characters.Count)
                    {
                        var txtGlyph = Characters[glyphIndex];
                        txtGlyph.VertexQuad[0].color = new Color32((byte)(255 - txtGlyph.VertexQuad[0].color.r), (byte)(255 - txtGlyph.VertexQuad[0].color.g), (byte)(255 - txtGlyph.VertexQuad[0].color.b), txtGlyph.VertexQuad[0].color.a);
                        txtGlyph.VertexQuad[1].color = new Color32((byte)(255 - txtGlyph.VertexQuad[1].color.r), (byte)(255 - txtGlyph.VertexQuad[1].color.g), (byte)(255 - txtGlyph.VertexQuad[1].color.b), txtGlyph.VertexQuad[1].color.a);
                        txtGlyph.VertexQuad[2].color = new Color32((byte)(255 - txtGlyph.VertexQuad[2].color.r), (byte)(255 - txtGlyph.VertexQuad[2].color.g), (byte)(255 - txtGlyph.VertexQuad[2].color.b), txtGlyph.VertexQuad[2].color.a);
                        txtGlyph.VertexQuad[3].color = new Color32((byte)(255 - txtGlyph.VertexQuad[3].color.r), (byte)(255 - txtGlyph.VertexQuad[3].color.g), (byte)(255 - txtGlyph.VertexQuad[3].color.b), txtGlyph.VertexQuad[3].color.a);
                    }
                }

                UpdateGeometry();

                if (null != OnLink)
                    OnLink.Invoke(link.Param);
            }

            _linkClick = -1;
        }

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (!raycastTarget)
                return false;

            Vector2 eventPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, sp, eventCamera, out eventPosition);
            var rect = rectTransform.rect;
            if (eventPosition.x < rect.xMin || eventPosition.x > rect.xMax || eventPosition.y < rect.yMin || eventPosition.y > rect.yMax)
                return false;

            return true;
        }
    }
}