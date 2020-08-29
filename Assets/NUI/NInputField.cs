using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace NUI
{
    static class SetPropertyUtility
    {
        public static bool SetColor(ref Color currentValue, Color newValue)
        {
            if (currentValue.r == newValue.r && currentValue.g == newValue.g && currentValue.b == newValue.b && currentValue.a == newValue.a)
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetStruct<T>(ref T currentValue, T newValue) where T : struct
        {
            if (currentValue.Equals(newValue))
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetClass<T>(ref T currentValue, T newValue) where T : class
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
                return false;

            currentValue = newValue;
            return true;
        }
    }

    public class NInputField
        : NText,
        IUpdateSelectedHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IPointerClickHandler,
        ISubmitHandler,
        ICanvasElement,
        ILayoutElement, 
        ISelectHandler, 
        IDeselectHandler
    {
        public enum ContentType
        {
            Standard,
            Autocorrected,
            IntegerNumber,
            DecimalNumber,
            Alphanumeric,
            Name,
            EmailAddress,
            Password,
            Pin,
            Custom
        }

        public enum InputType
        {
            Standard,
            AutoCorrect,
            Password,
        }

        public enum CharacterValidation
        {
            None,
            Integer,
            Decimal,
            Alphanumeric,
            Name,
            EmailAddress
        }

        public enum LineType
        {
            SingleLine,
            MultiLineSubmit,
            MultiLineNewline
        }

        public delegate char OnValidateInput(string text, int charIndex, char addedChar);

        [Serializable]
        public class SubmitEvent : UnityEvent<string> { }

        [Serializable]
        public class OnChangeEvent : UnityEvent<string> { }

        protected TouchScreenKeyboard m_Keyboard;
        static private readonly char[] kSeparators = { ' ', '.', ',', '\t', '\r', '\n' };

        #region Exposed properties
        /// <summary>
        /// Text Text used to display the input's value.
        /// </summary>

        //[SerializeField]
        //[FormerlySerializedAs("text")]
        //protected NText m_TextComponent;

        [SerializeField]
        protected Graphic m_Placeholder;

        [SerializeField]
        private ContentType m_ContentType = ContentType.Standard;

        /// <summary>
        /// Type of data expected by the input field.
        /// </summary>
        [FormerlySerializedAs("inputType")]
        [SerializeField]
        private InputType m_InputType = InputType.Standard;

        /// <summary>
        /// The character used to hide text in password field.
        /// </summary>
        [FormerlySerializedAs("asteriskChar")]
        [SerializeField]
        private char m_AsteriskChar = '*';

        /// <summary>
        /// Keyboard type applies to mobile keyboards that get shown.
        /// </summary>
        [FormerlySerializedAs("keyboardType")]
        [SerializeField]
        private TouchScreenKeyboardType m_KeyboardType = TouchScreenKeyboardType.Default;

        [SerializeField]
        private LineType m_LineType = LineType.SingleLine;

        /// <summary>
        /// Should hide mobile input.
        /// </summary>

        [FormerlySerializedAs("hideMobileInput")]
        [SerializeField]
        private bool m_HideMobileInput = false;

        /// <summary>
        /// What kind of validation to use with the input field's data.
        /// </summary>
        [FormerlySerializedAs("validation")]
        [SerializeField]
        private CharacterValidation m_CharacterValidation = CharacterValidation.None;

        /// <summary>
        /// Maximum number of characters allowed before input no longer works.
        /// </summary>
        [FormerlySerializedAs("characterLimit")]
        [SerializeField]
        private int m_CharacterLimit = 0;

        /// <summary>
        /// Event delegates triggered when the input field submits its data.
        /// </summary>
        [FormerlySerializedAs("onSubmit")]
        [FormerlySerializedAs("m_OnSubmit")]
        [FormerlySerializedAs("m_EndEdit")]
        [SerializeField]
        private SubmitEvent m_OnEndEdit = new SubmitEvent();

        /// <summary>
        /// Event delegates triggered when the input field changes its data.
        /// </summary>
        [FormerlySerializedAs("onValueChange")]
        [FormerlySerializedAs("m_OnValueChange")]
        [SerializeField]
        private OnChangeEvent m_OnValueChanged = new OnChangeEvent();

        /// <summary>
        /// Custom validation callback.
        /// </summary>
        [FormerlySerializedAs("onValidateInput")]
        [SerializeField]
        private OnValidateInput m_OnValidateInput;

        [FormerlySerializedAs("selectionColor")]
        [SerializeField]
        private Color m_CaretColor = new Color(50f / 255f, 50f / 255f, 50f / 255f, 1f);

        [SerializeField]
        private bool m_CustomCaretColor = false;

        [FormerlySerializedAs("selectionColor")]
        [SerializeField]
        private Color m_SelectionColor = new Color(168f / 255f, 206f / 255f, 255f / 255f, 192f / 255f);

        /// <summary>
        /// Input field's value.
        /// </summary>

        //[SerializeField]
        //[FormerlySerializedAs("mValue")]
        //protected string m_Text = string.Empty;

        [SerializeField]
        [Range(0f, 4f)]
        private float m_CaretBlinkRate = 0.85f;

        [SerializeField]
        [Range(1, 5)]
        private int m_CaretWidth = 1;

        [SerializeField]
        private bool m_ReadOnly = false;

        #endregion

        protected int m_CaretPosition = 0;
        protected int m_CaretSelectPosition = 0;
        private RectTransform caretRectTrans = null;
        protected UIVertex[] m_CursorVerts = null;
        private NTextGenerator m_InputTextCache;
        private CanvasRenderer m_CachedInputRenderer;
        private bool m_PreventFontCallback = false;
        [NonSerialized] protected Mesh m_Mesh;
        private bool m_AllowInput = false;
        private bool m_ShouldActivateNextUpdate = false;
        private bool m_UpdateDrag = false;
        private bool m_DragPositionOutOfBounds = false;
        private const float kHScrollSpeed = 0.05f;
        private const float kVScrollSpeed = 0.10f;
        protected bool m_CaretVisible;
        private Coroutine m_BlinkCoroutine = null;
        private float m_BlinkStartTime = 0.0f;
        protected int m_DrawStart = 0;
        protected int m_DrawEnd = 0;
        private Coroutine m_DragCoroutine = null;
        private string m_OriginalText = "";
        private bool m_WasCanceled = false;
        private bool m_HasDoneFocusTransition = false;
        private WaitForSecondsRealtime m_WaitForSecondsRealtime;
        private bool m_TouchKeyboardAllowsInPlaceEditing = false;

        // Doesn't include dot and @ on purpose! See usage for details.
        const string kEmailSpecialCharacters = "!#$%&'*+-/=?^_`{|}~";

        private BaseInput input
        {
            get
            {
                if (EventSystem.current && EventSystem.current.currentInputModule)
                    return EventSystem.current.currentInputModule.input;
                return null;
            }
        }

        private string compositionString
        {
            get { return input != null ? input.compositionString : Input.compositionString; }
        }

        protected NInputField()
            : base()
        {
        }

        protected override void Awake()
        {
            base.Awake();
            EnforceTextHOverflow();
        }

        protected Mesh mesh
        {
            get
            {
                if (m_Mesh == null)
                    m_Mesh = new Mesh();
                return m_Mesh;
            }
        }

        //protected NTextGenerator cachedInputTextGenerator
        //{
        //    get
        //    {
        //        if (m_InputTextCache == null)
        //            m_InputTextCache = new NTextGenerator();

        //        return m_InputTextCache;
        //    }
        //}

        protected NTextGenerator cachedInputTextGenerator
        {
            get { return cachedTextGenerator; }
        }

        /// <summary>
        /// Should the mobile keyboard input be hidden.
        /// </summary>

        public bool shouldHideMobileInput
        {
            set
            {
                SetPropertyUtility.SetStruct(ref m_HideMobileInput, value);
            }
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                    case RuntimePlatform.IPhonePlayer:
                    case RuntimePlatform.tvOS:
                        return m_HideMobileInput;
                }

                return true;
            }
        }

        bool shouldActivateOnSelect
        {
            get
            {
                return Application.platform != RuntimePlatform.tvOS;
            }
        }

        static StringBuilder builder = new StringBuilder();

        public int RealTextLength;

        /// <summary>
        /// Input field's current text value.
        /// </summary>

        public override string text
        {
            get
            {
                return m_Text;
            }
            set
            {
                SetText(value);
            }
        }

        public void SetTextWithoutNotify(string input)
        {
            SetText(input, false);
        }

        void SetText(string value, bool sendCallback = true)
        {
            if (m_Text == value)
                return;
            if (value == null)
                value = "";
            value = value.Replace("\0", string.Empty); // remove embedded nulls
            if (m_LineType == LineType.SingleLine)
                value = value.Replace("\n", "").Replace("\t", "");

            RemoveAllCustom();

            // If we have an input validator, validate the input and apply the character limit at the same time.
            if (onValidateInput != null || characterValidation != CharacterValidation.None)
            {
                m_Text = "";
                RealTextLength = 0;

                OnValidateInput validatorMethod = onValidateInput ?? Validate;
                m_CaretPosition = m_CaretSelectPosition = value.Length;
                int charactersToCheck = characterLimit > 0 ? Math.Min(characterLimit, value.Length) : value.Length;
                for (int i = 0; i < charactersToCheck; ++i)
                {
                    char c = validatorMethod(m_Text, m_Text.Length, value[i]);
                    if (c != 0)
                    {
                        m_Text += c;
                        RealTextLength++;
                    }
                }
            }
            else
            {
                m_Text = characterLimit > 0 && value.Length > characterLimit ? value.Substring(0, characterLimit) : value;
                RealTextLength = m_Text.Length;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                SendOnValueChangedAndUpdateLabel();
                return;
            }
#endif

            if (m_Keyboard != null)
                m_Keyboard.text = m_Text;

            if (m_CaretPosition > RealTextLength)
                m_CaretPosition = m_CaretSelectPosition = RealTextLength;
            else if (m_CaretSelectPosition > RealTextLength)
                m_CaretSelectPosition = RealTextLength;

            if (sendCallback)
                SendOnValueChanged();
            UpdateLabel();
        }

        private int CustomRichTag = '\xe000';
        private List<NTextElement> TextElements = new List<NTextElement>();

        public bool isFocused
        {
            get { return m_AllowInput; }
        }

        public float caretBlinkRate
        {
            get { return m_CaretBlinkRate; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_CaretBlinkRate, value))
                {
                    if (m_AllowInput)
                        SetCaretActive();
                }
            }
        }

        public int caretWidth { get { return m_CaretWidth; } set { if (SetPropertyUtility.SetStruct(ref m_CaretWidth, value)) MarkGeometryAsDirty(); } }


        //public NText textComponent
        //{
        //    get { return m_TextComponent; }
        //    set
        //    {
        //        if (m_TextComponent != null)
        //        {
        //            m_TextComponent.UnregisterDirtyVerticesCallback(MarkGeometryAsDirty);
        //            m_TextComponent.UnregisterDirtyVerticesCallback(UpdateLabel);
        //            m_TextComponent.UnregisterDirtyMaterialCallback(UpdateCaretMaterial);
        //        }

        //        if (SetPropertyUtility.SetClass(ref m_TextComponent, value))
        //        {
        //            EnforceTextHOverflow();
        //            if (m_TextComponent != null)
        //            {
        //                m_TextComponent.RegisterDirtyVerticesCallback(MarkGeometryAsDirty);
        //                m_TextComponent.RegisterDirtyVerticesCallback(UpdateLabel);
        //                m_TextComponent.RegisterDirtyMaterialCallback(UpdateCaretMaterial);
        //            }
        //        }
        //    }
        //}

        public NText textComponent
        {
            get { return this; }
        }

        public Graphic placeholder { get { return m_Placeholder; } set { SetPropertyUtility.SetClass(ref m_Placeholder, value); } }

        public Color caretColor { get { return customCaretColor ? m_CaretColor : textComponent.color; } set { if (SetPropertyUtility.SetColor(ref m_CaretColor, value)) MarkGeometryAsDirty(); } }

        public bool customCaretColor { get { return m_CustomCaretColor; } set { if (m_CustomCaretColor != value) { m_CustomCaretColor = value; MarkGeometryAsDirty(); } } }


        public Color selectionColor { get { return m_SelectionColor; } set { SetPropertyUtility.SetColor(ref m_SelectionColor, value); } }

        public SubmitEvent onEndEdit { get { return m_OnEndEdit; } set { SetPropertyUtility.SetClass(ref m_OnEndEdit, value); } }

        [Obsolete("onValueChange has been renamed to onValueChanged")]
        public OnChangeEvent onValueChange { get { return m_OnValueChanged; } set { SetPropertyUtility.SetClass(ref m_OnValueChanged, value); } }

        public OnChangeEvent onValueChanged { get { return m_OnValueChanged; } set { SetPropertyUtility.SetClass(ref m_OnValueChanged, value); } }

        public OnValidateInput onValidateInput { get { return m_OnValidateInput; } set { SetPropertyUtility.SetClass(ref m_OnValidateInput, value); } }

        public int characterLimit
        {
            get { return m_CharacterLimit; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_CharacterLimit, Math.Max(0, value)))
                {
                    UpdateLabel();
                    //if (m_Keyboard != null)
                    //    m_Keyboard.characterLimit = value;
                }
            }
        }

        // Content Type related

        public ContentType contentType { get { return m_ContentType; } set { if (SetPropertyUtility.SetStruct(ref m_ContentType, value)) EnforceContentType(); } }

        public LineType lineType
        {
            get { return m_LineType; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_LineType, value))
                    SetToCustomIfContentTypeIsNot(ContentType.Standard, ContentType.Autocorrected);
            }
        }

        public TouchScreenKeyboard touchScreenKeyboard { get { return m_Keyboard; } }

        public InputType inputType { get { return m_InputType; } set { if (SetPropertyUtility.SetStruct(ref m_InputType, value)) SetToCustom(); } }

        public TouchScreenKeyboardType keyboardType { get { return m_KeyboardType; } set { if (SetPropertyUtility.SetStruct(ref m_KeyboardType, value)) SetToCustom(); } }

        public CharacterValidation characterValidation { get { return m_CharacterValidation; } set { if (SetPropertyUtility.SetStruct(ref m_CharacterValidation, value)) SetToCustom(); } }

        public bool readOnly { get { return m_ReadOnly; } set { m_ReadOnly = value; } }

        // Derived property
        public bool multiLine { get { return m_LineType == LineType.MultiLineNewline || lineType == LineType.MultiLineSubmit; } }
        // Not shown in Inspector.
        public char asteriskChar { get { return m_AsteriskChar; } set { if (SetPropertyUtility.SetStruct(ref m_AsteriskChar, value)) UpdateLabel(); } }
        public bool wasCanceled { get { return m_WasCanceled; } }

        protected void ClampPos(ref int pos)
        {
            if (pos < 0) pos = 0;
            else if (pos > RealTextLength) pos = RealTextLength;
        }

        /// <summary>
        /// Current position of the cursor.
        /// Getters are public Setters are protected
        /// </summary>

        protected int caretPositionInternal {
            get {
                return m_CaretPosition + compositionString.Length;
            }
            set {
                m_CaretPosition = value;
                ClampPos(ref m_CaretPosition);
            }
        }
        protected int caretSelectPositionInternal { get { return m_CaretSelectPosition + compositionString.Length; } set { m_CaretSelectPosition = value; ClampPos(ref m_CaretSelectPosition); } }
        private bool hasSelection { get { return caretPositionInternal != caretSelectPositionInternal; } }

#if UNITY_EDITOR
        [Obsolete("caretSelectPosition has been deprecated. Use selectionFocusPosition instead (UnityUpgradable) -> selectionFocusPosition", true)]
        public int caretSelectPosition { get { return selectionFocusPosition; } protected set { selectionFocusPosition = value; } }
#endif

        /// <summary>
        /// Get: Returns the focus position as thats the position that moves around even during selection.
        /// Set: Set both the anchor and focus position such that a selection doesn't happen
        /// </summary>

        public int caretPosition
        {
            get { return m_CaretSelectPosition + compositionString.Length; }
            set { selectionAnchorPosition = value; selectionFocusPosition = value; }
        }

        /// <summary>
        /// Get: Returns the fixed position of selection
        /// Set: If compositionString is 0 set the fixed position
        /// </summary>

        public int selectionAnchorPosition
        {
            get { return m_CaretPosition + compositionString.Length; }
            set
            {
                if (compositionString.Length != 0)
                    return;

                m_CaretPosition = value;
                ClampPos(ref m_CaretPosition);
            }
        }

        /// <summary>
        /// Get: Returns the variable position of selection
        /// Set: If compositionString is 0 set the variable position
        /// </summary>

        public int selectionFocusPosition
        {
            get { return m_CaretSelectPosition + compositionString.Length; }
            set
            {
                if (compositionString.Length != 0)
                    return;

                m_CaretSelectPosition = value;
                ClampPos(ref m_CaretSelectPosition);
            }
        }

#if UNITY_EDITOR
        // Remember: This is NOT related to text validation!
        // This is Unity's own OnValidate method which is invoked when changing values in the Inspector.
        protected override void OnValidate()
        {
            base.OnValidate();
            EnforceContentType();
            EnforceTextHOverflow();

            //This can be invoked before OnEnabled is called. So we shouldn't be accessing other objects, before OnEnable is called.
            if (!IsActive())
                return;

            ClampPos(ref m_CaretPosition);
            ClampPos(ref m_CaretSelectPosition);

            UpdateLabel();
            if (m_AllowInput)
                SetCaretActive();
        }

#endif // if UNITY_EDITOR

        protected override void OnEnable()
        {
            base.OnEnable();
            if (m_Text == null)
            {
                m_Text = string.Empty;
                RealTextLength = 0;
                RemoveAllCustom();
            }
            m_DrawStart = 0;
            m_DrawEnd = RealTextLength;

            if (m_CachedInputRenderer != null)
                m_CachedInputRenderer.SetMaterial(textComponent.GetModifiedMaterial(Graphic.defaultGraphicMaterial), Texture2D.whiteTexture);

            if (textComponent != null)
            {
                textComponent.RegisterDirtyVerticesCallback(MarkGeometryAsDirty);
                textComponent.RegisterDirtyVerticesCallback(UpdateLabel);
                textComponent.RegisterDirtyMaterialCallback(UpdateCaretMaterial);
                UpdateLabel();
            }
        }

        protected override void OnDisable()
        {
            // the coroutine will be terminated, so this will ensure it restarts when we are next activated
            m_BlinkCoroutine = null;

            DeactivateInputField();
            if (textComponent != null)
            {
                textComponent.UnregisterDirtyVerticesCallback(MarkGeometryAsDirty);
                textComponent.UnregisterDirtyVerticesCallback(UpdateLabel);
                textComponent.UnregisterDirtyMaterialCallback(UpdateCaretMaterial);
            }
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);

            if (null != m_CachedInputRenderer)
                m_CachedInputRenderer.Clear();

            if (m_Mesh)
                DestroyImmediate(m_Mesh);
            m_Mesh = null;

            base.OnDisable();
        }

        IEnumerator CaretBlink()
        {
            // Always ensure caret is initially visible since it can otherwise be confusing for a moment.
            m_CaretVisible = true;
            yield return null;

            while (isFocused && m_CaretBlinkRate > 0)
            {
                // the blink rate is expressed as a frequency
                float blinkPeriod = 1f / m_CaretBlinkRate;

                // the caret should be ON if we are in the first half of the blink period
                bool blinkState = (Time.unscaledTime - m_BlinkStartTime) % blinkPeriod < blinkPeriod / 2;
                if (m_CaretVisible != blinkState)
                {
                    m_CaretVisible = blinkState;
                    if (!hasSelection)
                        MarkGeometryAsDirty();
                }

                // Then wait again.
                yield return null;
            }
            m_BlinkCoroutine = null;
        }

        void SetCaretVisible()
        {
            if (!m_AllowInput)
                return;

            m_CaretVisible = true;
            m_BlinkStartTime = Time.unscaledTime;
            SetCaretActive();
        }

        // SetCaretActive will not set the caret immediately visible - it will wait for the next time to blink.
        // However, it will handle things correctly if the blink speed changed from zero to non-zero or non-zero to zero.
        void SetCaretActive()
        {
            if (!m_AllowInput)
                return;

            if (m_CaretBlinkRate > 0.0f)
            {
                if (m_BlinkCoroutine == null)
                    m_BlinkCoroutine = StartCoroutine(CaretBlink());
            }
            else
            {
                m_CaretVisible = true;
            }
        }

        private void UpdateCaretMaterial()
        {
            if (textComponent != null && m_CachedInputRenderer != null)
                m_CachedInputRenderer.SetMaterial(textComponent.GetModifiedMaterial(Graphic.defaultGraphicMaterial), Texture2D.whiteTexture);
        }

        protected void OnFocus()
        {
            SelectAll();
        }

        protected void SelectAll()
        {
            caretPositionInternal = RealTextLength;
            caretSelectPositionInternal = 0;
        }

        public void MoveTextEnd(bool shift)
        {
            int position = RealTextLength;

            if (shift)
            {
                caretSelectPositionInternal = position;
            }
            else
            {
                caretPositionInternal = position;
                caretSelectPositionInternal = caretPositionInternal;
            }
            UpdateLabel();
        }

        public void MoveTextStart(bool shift)
        {
            int position = 0;

            if (shift)
            {
                caretSelectPositionInternal = position;
            }
            else
            {
                caretPositionInternal = position;
                caretSelectPositionInternal = caretPositionInternal;
            }

            UpdateLabel();
        }

        static string clipboard
        {
            get
            {
                return GUIUtility.systemCopyBuffer;
            }
            set
            {
                GUIUtility.systemCopyBuffer = value;
            }
        }

        private bool InPlaceEditing()
        {
            return !TouchScreenKeyboard.isSupported || m_TouchKeyboardAllowsInPlaceEditing;
        }

        void UpdateCaretFromKeyboard()
        {
            var selectionRange = m_Keyboard.selection;

            var selectionStart = selectionRange.start;
            var selectionEnd = selectionRange.end;

            var caretChanged = false;

            if (caretPositionInternal != selectionStart)
            {
                caretChanged = true;
                caretPositionInternal = selectionStart;
            }

            if (caretSelectPositionInternal != selectionEnd)
            {
                caretSelectPositionInternal = selectionEnd;
                caretChanged = true;
            }

            if (caretChanged)
            {
                m_BlinkStartTime = Time.unscaledTime;

                UpdateLabel();
            }
        }

        /// <summary>
        /// Update the text based on input.
        /// </summary>
        // TODO: Make LateUpdate a coroutine instead. Allows us to control the update to only be when the field is active.
        protected virtual void LateUpdate()
        {
            // Only activate if we are not already activated.
            if (m_ShouldActivateNextUpdate)
            {
                if (!isFocused)
                {
                    ActivateInputFieldInternal();
                    m_ShouldActivateNextUpdate = false;
                    return;
                }

                // Reset as we are already activated.
                m_ShouldActivateNextUpdate = false;
            }

            AssignPositioningIfNeeded();

            if (!isFocused || InPlaceEditing())
                return;

            if (m_Keyboard != null && !m_Keyboard.active)
            {
                if (!m_ReadOnly)
                    text = m_Keyboard.text;

                if (m_Keyboard.active)
                    m_WasCanceled = true;
            }
            if (m_Keyboard == null || !m_Keyboard.active)
            {
                if (m_Keyboard != null)
                {
                    if (!m_ReadOnly)
                        text = m_Keyboard.text;

                    if (m_Keyboard.wasCanceled)
                        m_WasCanceled = true;
                }

                OnDeselect(null);
                return;
            }

            string val = m_Keyboard.text;

            if (m_Text != val)
            {
                if (m_ReadOnly)
                {
                    m_Keyboard.text = m_Text;
                }
                else
                {
                    m_Text = "";
                    RealTextLength = 0;
                    RemoveAllCustom();

                    for (int i = 0; i < val.Length; ++i)
                    {
                        char c = val[i];

                        if (c == '\r' || (int)c == 3)
                            c = '\n';

                        if (onValidateInput != null)
                            c = onValidateInput(m_Text, m_Text.Length, c);
                        else if (characterValidation != CharacterValidation.None)
                            c = Validate(m_Text, m_Text.Length, c);

                        if (lineType == LineType.MultiLineSubmit && c == '\n')
                        {
                            m_Keyboard.text = m_Text;

                            OnDeselect(null);
                            return;
                        }

                        if (c != 0)
                        {
                            m_Text += c;
                            RealTextLength++;
                        }
                    }

                    if (characterLimit > 0 && m_Text.Length > characterLimit)
                    {
                        m_Text = m_Text.Substring(0, characterLimit);
                        RealTextLength = m_Text.Length;
                    }

                    if (m_Keyboard.canGetSelection)
                    {
                        UpdateCaretFromKeyboard();
                    }
                    else
                    {
                        caretPositionInternal = caretSelectPositionInternal = RealTextLength;
                    }

                    // Set keyboard text before updating label, as we might have changed it with validation
                    // and update label will take the old value from keyboard if we don't change it here
                    if (m_Text != val)
                        m_Keyboard.text = m_Text;

                    SendOnValueChangedAndUpdateLabel();
                }
            }
            //else if (m_HideMobileInput && m_Keyboard.canSetSelection)
            //{
            //    m_Keyboard.selection = new RangeInt(caretPositionInternal, caretSelectPositionInternal - caretPositionInternal);
            //}
            else if (m_Keyboard.canGetSelection && !m_HideMobileInput)
            {
                UpdateCaretFromKeyboard();
            }


            if (m_Keyboard.done)
            {
                if (m_Keyboard.wasCanceled)
                    m_WasCanceled = true;

                OnDeselect(null);
            }
        }

        public Vector2 ScreenToLocal(Vector2 screen)
        {
            var theCanvas = textComponent.canvas;
            if (theCanvas == null)
                return screen;

            Vector3 pos = Vector3.zero;
            if (theCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                pos = textComponent.transform.InverseTransformPoint(screen);
            }
            else if (theCanvas.worldCamera != null)
            {
                Ray mouseRay = theCanvas.worldCamera.ScreenPointToRay(screen);
                float dist;
                Plane plane = new Plane(textComponent.transform.forward, textComponent.transform.position);
                plane.Raycast(mouseRay, out dist);
                pos = textComponent.transform.InverseTransformPoint(mouseRay.GetPoint(dist));
            }
            return new Vector2(pos.x, pos.y);
        }

        private int GetUnclampedCharacterLineFromPosition(Vector2 pos, NTextGenerator generator)
        {
            if (!multiLine)
                return 0;

            float yMax = textComponent.rectTransform.rect.yMax;

            // Position is above first line.
            if (pos.y > yMax)
                return -1;

            for (int i = 0; i < generator.lineCount; ++i)
            {
                float yMin = generator.lines[i].BaseLine + generator.lines[i].OffsetY;
                if (pos.y <= yMax && pos.y > yMin)
                    return i;
                yMax = yMin;
            }

            // Position is after last line.
            return generator.lineCount;
        }

        /// <summary>
        /// Given an input position in local space on the Text return the index for the selection cursor at this position.
        /// </summary>

        protected int GetCharacterIndexFromPosition(Vector2 pos)
        {
            NTextGenerator gen = textComponent.cachedTextGenerator;

            if (gen.lineCount == 0)
                return 0;

            int line = GetUnclampedCharacterLineFromPosition(pos, gen);
            if (line < 0)
                return 0;
            if (line >= gen.lineCount)
                return gen.characterCountVisible;

            int startCharIndex = gen.lines[line].startCharIdx;
            int endCharIndex = GetLineEndPosition(gen, line);

            int charIndex = endCharIndex;
            for (int i = startCharIndex; i < endCharIndex; i++)
            {
                if (i >= gen.characterCountVisible)
                    break;

                NTextGlyph charInfo = gen.characters[i];
                Vector2 charPos = charInfo.VertexQuad[0].position / textComponent.pixelsPerUnit;

                float distToCharStart = pos.x - charPos.x;
                float distToCharEnd = charPos.x + (charInfo.Advance / textComponent.pixelsPerUnit) - pos.x;
                if (distToCharStart < distToCharEnd)
                {
                    charIndex = i;
                    break;
                }
            }

            var currChar = gen.characters[charIndex];
            if (0 == currChar.CustomCharTag)
                return charIndex;

            while (charIndex > 0)
            {
                if (currChar.CustomCharTag != gen.characters[charIndex - 1].CustomCharTag)
                    return charIndex;
                charIndex--;
            }
            return charIndex;
        }

        private bool MayDrag(PointerEventData eventData)
        {
            return IsActive() &&
                   IsInteractable() &&
                   eventData.button == PointerEventData.InputButton.Left &&
                   textComponent != null &&
                   (InPlaceEditing() || m_HideMobileInput);
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            m_UpdateDrag = true;
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            Vector2 localMousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(textComponent.rectTransform, eventData.position, eventData.pressEventCamera, out localMousePos);
            caretSelectPositionInternal = GetCharacterIndexFromPosition(localMousePos) + m_DrawStart;
            MarkGeometryAsDirty();

            m_DragPositionOutOfBounds = !RectTransformUtility.RectangleContainsScreenPoint(textComponent.rectTransform, eventData.position, eventData.pressEventCamera);
            if (m_DragPositionOutOfBounds && m_DragCoroutine == null)
                m_DragCoroutine = StartCoroutine(MouseDragOutsideRect(eventData));

            eventData.Use();
        }

        IEnumerator MouseDragOutsideRect(PointerEventData eventData)
        {
            while (m_UpdateDrag && m_DragPositionOutOfBounds)
            {
                Vector2 localMousePos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(textComponent.rectTransform, eventData.position, eventData.pressEventCamera, out localMousePos);

                Rect rect = textComponent.rectTransform.rect;

                if (multiLine)
                {
                    if (localMousePos.y > rect.yMax)
                        MoveUp(true, true);
                    else if (localMousePos.y < rect.yMin)
                        MoveDown(true, true);
                }
                else
                {
                    if (localMousePos.x < rect.xMin)
                        MoveLeft(true, false);
                    else if (localMousePos.x > rect.xMax)
                        MoveRight(true, false);
                }
                UpdateLabel();
                float delay = multiLine ? kVScrollSpeed : kHScrollSpeed;
                yield return new WaitForSecondsRealtime(delay);
            }
            m_DragCoroutine = null;
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            m_UpdateDrag = false;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            EventSystem.current.SetSelectedGameObject(gameObject, eventData);

            bool hadFocusBefore = m_AllowInput;
            base.OnPointerDown(eventData);

            if (!InPlaceEditing())
            {
                if (m_Keyboard == null || !m_Keyboard.active)
                {
                    OnSelect(eventData);
                    return;
                }
            }

            // Only set caret position if we didn't just get focus now.
            // Otherwise it will overwrite the select all on focus.
            if (hadFocusBefore)
            {
                Vector2 localMousePos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(textComponent.rectTransform, eventData.position, eventData.pressEventCamera, out localMousePos);
                caretSelectPositionInternal = caretPositionInternal = GetCharacterIndexFromPosition(localMousePos) + m_DrawStart;
            }
            UpdateLabel();
            eventData.Use();
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
        }

        protected enum EditState
        {
            Continue,
            Finish
        }

        protected EditState KeyPressed(Event evt)
        {
            var currentEventModifiers = evt.modifiers;
            bool ctrl = SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX ? (currentEventModifiers & EventModifiers.Command) != 0 : (currentEventModifiers & EventModifiers.Control) != 0;
            bool shift = (currentEventModifiers & EventModifiers.Shift) != 0;
            bool alt = (currentEventModifiers & EventModifiers.Alt) != 0;
            bool ctrlOnly = ctrl && !alt && !shift;

            switch (evt.keyCode)
            {
                case KeyCode.Backspace:
                    {
                        Backspace();
                        return EditState.Continue;
                    }

                case KeyCode.Delete:
                    {
                        ForwardSpace();
                        return EditState.Continue;
                    }

                case KeyCode.Home:
                    {
                        MoveTextStart(shift);
                        return EditState.Continue;
                    }

                case KeyCode.End:
                    {
                        MoveTextEnd(shift);
                        return EditState.Continue;
                    }

                // Select All
                case KeyCode.A:
                    {
                        if (ctrlOnly)
                        {
                            SelectAll();
                            return EditState.Continue;
                        }
                        break;
                    }

                // Copy
                case KeyCode.C:
                    {
                        if (ctrlOnly)
                        {
                            if (inputType != InputType.Password)
                                clipboard = GetSelectedString();
                            else
                                clipboard = "";
                            return EditState.Continue;
                        }
                        break;
                    }

                // Paste
                case KeyCode.V:
                    {
                        if (ctrlOnly)
                        {
                            Append(clipboard);
                            UpdateLabel();
                            return EditState.Continue;
                        }
                        break;
                    }

                // Cut
                case KeyCode.X:
                    {
                        if (ctrlOnly)
                        {
                            if (inputType != InputType.Password)
                                clipboard = GetSelectedString();
                            else
                                clipboard = "";
                            Delete();
                            UpdateTouchKeyboardFromEditChanges();
                            SendOnValueChangedAndUpdateLabel();
                            return EditState.Continue;
                        }
                        break;
                    }

                case KeyCode.LeftArrow:
                    {
                        MoveLeft(shift, ctrl);
                        return EditState.Continue;
                    }

                case KeyCode.RightArrow:
                    {
                        MoveRight(shift, ctrl);
                        return EditState.Continue;
                    }

                case KeyCode.UpArrow:
                    {
                        MoveUp(shift);
                        return EditState.Continue;
                    }

                case KeyCode.DownArrow:
                    {
                        MoveDown(shift);
                        return EditState.Continue;
                    }

                // Submit
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    {
                        if (lineType != LineType.MultiLineNewline)
                        {
                            return EditState.Finish;
                        }
                        break;
                    }

                case KeyCode.Escape:
                    {
                        m_WasCanceled = true;
                        return EditState.Finish;
                    }
            }

            char c = evt.character;
            // Dont allow return chars or tabulator key to be entered into single line fields.
            if (!multiLine && (c == '\t' || c == '\r' || c == 10))
                return EditState.Continue;

            // Convert carriage return and end-of-text characters to newline.
            if (c == '\r' || (int)c == 3)
                c = '\n';

            if (IsValidChar(c))
            {
                Append(c);
            }

            if (c == 0)
            {
                if (compositionString.Length > 0)
                {
                    UpdateLabel();
                }
            }
            return EditState.Continue;
        }

        private bool IsValidChar(char c)
        {
            // Delete key on mac
            if ((int)c == 127)
                return false;
            // Accept newline and tab
            if (c == '\t' || c == '\n')
                return true;

            return textComponent.font.HasCharacter(c);
        }

        /// <summary>
        /// Handle the specified event.
        /// </summary>
        private Event m_ProcessingEvent = new Event();

        public void ProcessEvent(Event e)
        {
            KeyPressed(e);
        }

        public virtual void OnUpdateSelected(BaseEventData eventData)
        {
            if (!isFocused)
                return;

            bool consumedEvent = false;
            while (Event.PopEvent(m_ProcessingEvent))
            {
                if (m_ProcessingEvent.rawType == EventType.KeyDown)
                {
                    consumedEvent = true;
                    var shouldContinue = KeyPressed(m_ProcessingEvent);
                    if (shouldContinue == EditState.Finish)
                    {
                        DeactivateInputField();
                        break;
                    }
                }

                switch (m_ProcessingEvent.type)
                {
                    case EventType.ValidateCommand:
                    case EventType.ExecuteCommand:
                        switch (m_ProcessingEvent.commandName)
                        {
                            case "SelectAll":
                                SelectAll();
                                consumedEvent = true;
                                break;
                        }
                        break;
                }
            }

            if (consumedEvent)
                UpdateLabel();

            eventData.Use();
        }

        private string GetSelectedString()
        {
            if (!hasSelection)
                return "";

            int startPos = CharetPositionToTextIndex(caretPositionInternal);
            int endPos = CharetPositionToTextIndex(caretSelectPositionInternal);

            // Ensure pos is always less then selPos to make the code simpler
            if (startPos > endPos)
            {
                int temp = startPos;
                startPos = endPos;
                endPos = temp;
            }

            return m_Text.Substring(startPos, endPos - startPos);
        }

        private int FindtNextWordBegin()
        {
            if (caretSelectPositionInternal + 1 >= RealTextLength)
                return RealTextLength;

            //int spaceLoc = m_Text.IndexOfAny(kSeparators, caretSelectPositionInternal + 1);
            int spaceLoc = caretSelectPositionInternal;

            var currChar = cachedInputTextGenerator.characters[spaceLoc];
            while (++spaceLoc < cachedInputTextGenerator.characters.Count)
            {
                var nextChar = cachedInputTextGenerator.characters[spaceLoc];
                if (nextChar.CustomCharTag == 0 && kSeparators.Contains(nextChar.Char))
                    return spaceLoc + 1;
                if (currChar.CustomCharTag != nextChar.CustomCharTag)
                    return spaceLoc;
            }

            return spaceLoc;
        }

        private void MoveRight(bool shift, bool ctrl)
        {
            if (hasSelection && !shift)
            {
                // By convention, if we have a selection and move right without holding shift,
                // we just place the cursor at the end.
                caretPositionInternal = caretSelectPositionInternal = Mathf.Max(caretPositionInternal, caretSelectPositionInternal);
                return;
            }

            int position;
            if (ctrl)
                position = FindtNextWordBegin();
            else
            {
                position = caretSelectPositionInternal;
                if (position < cachedInputTextGenerator.characterCount)
                {
                    var currChar = cachedInputTextGenerator.characters[position];
                    while (++position < cachedInputTextGenerator.characterCount)
                    {
                        var nextChar = cachedInputTextGenerator.characters[position];
                        if (0 == currChar.CustomCharTag || currChar.CustomCharTag != nextChar.CustomCharTag)
                            break;
                    }
                }
            }

            if (shift)
                caretSelectPositionInternal = position;
            else
                caretSelectPositionInternal = caretPositionInternal = position;
        }

        private int FindtPrevWordBegin()
        {
            if (caretSelectPositionInternal - 2 < 0)
                return 0;

            //int spaceLoc = m_Text.LastIndexOfAny(kSeparators, caretSelectPositionInternal - 2);

            int spaceLoc = caretSelectPositionInternal - 1;

            var currChar = cachedInputTextGenerator.characters[spaceLoc];
            while (--spaceLoc >= 0)
            {
                var prevChar = cachedInputTextGenerator.characters[spaceLoc];
                if (prevChar.CustomCharTag == 0 && kSeparators.Contains(prevChar.Char))
                    return spaceLoc + 1;
                if (currChar.CustomCharTag != prevChar.CustomCharTag)
                    return spaceLoc + 1;
            }

            return spaceLoc;
        }

        private void MoveLeft(bool shift, bool ctrl)
        {
            if (hasSelection && !shift)
            {
                // By convention, if we have a selection and move left without holding shift,
                // we just place the cursor at the start.
                caretPositionInternal = caretSelectPositionInternal = Mathf.Min(caretPositionInternal, caretSelectPositionInternal);
                return;
            }

            int position;
            if (ctrl)
                position = FindtPrevWordBegin();
            else
            {
                position = caretSelectPositionInternal - 1;
                if (position >= 0)
                {
                    var currChar = cachedInputTextGenerator.characters[position];
                    if (0 != currChar.CustomCharTag)
                    {
                        while (position - 1 >= 0)
                        {
                            var prevChar = cachedInputTextGenerator.characters[position - 1];
                            if (currChar.CustomCharTag != prevChar.CustomCharTag)
                                break;
                            position--;
                        }
                    }
                }
            }

            if (shift)
                caretSelectPositionInternal = position;
            else
                caretSelectPositionInternal = caretPositionInternal = position;
        }

        private int DetermineCharacterLine(int charPos, NTextGenerator generator)
        {
            for (int i = 0; i < generator.lineCount - 1; ++i)
            {
                if (generator.lines[i + 1].startCharIdx > charPos)
                    return i;
            }

            return generator.lineCount - 1;
        }

        /// <summary>
        ///  Use cachedInputTextGenerator as the y component for the TRichTextGlyph is not required
        /// </summary>

        private int LineUpCharacterPosition(int originalPos, bool goToFirstChar)
        {
            if (originalPos >= cachedInputTextGenerator.characters.Count)
                return 0;

            NTextGlyph originChar = cachedInputTextGenerator.characters[originalPos];
            int originLine = DetermineCharacterLine(originalPos, cachedInputTextGenerator);

            // We are on the last line return last character
            if (originLine <= 0)
                return goToFirstChar ? 0 : originalPos;


            int endCharIdx = cachedInputTextGenerator.lines[originLine].startCharIdx - 1;

            int position = endCharIdx;
            for (int i = cachedInputTextGenerator.lines[originLine - 1].startCharIdx; i < endCharIdx; ++i)
            {
                if (cachedInputTextGenerator.characters[i].VertexQuad[0].position.x >= originChar.VertexQuad[0].position.x)
                {
                    position = i;
                    break;
                }
            }

            var currChar = cachedInputTextGenerator.characters[position];
            if (0 == currChar.CustomCharTag)
                return position;

            while (position - 1 >= 0)
            {
                var prevChar = cachedInputTextGenerator.characters[position - 1];
                if (currChar.CustomCharTag != prevChar.CustomCharTag)
                    break;
                position--;
            }

            return position;
        }

        /// <summary>
        ///  Use cachedInputTextGenerator as the y component for the TRichTextGlyph is not required
        /// </summary>

        private int LineDownCharacterPosition(int originalPos, bool goToLastChar)
        {
            if (originalPos >= cachedInputTextGenerator.characterCountVisible)
                return RealTextLength;

            NTextGlyph originChar = cachedInputTextGenerator.characters[originalPos];
            int originLine = DetermineCharacterLine(originalPos, cachedInputTextGenerator);

            // We are on the last line return last character
            if (originLine + 1 >= cachedInputTextGenerator.lineCount)
                return goToLastChar ? RealTextLength : originalPos;

            // Need to determine end line for next line.
            int endCharIdx = GetLineEndPosition(cachedInputTextGenerator, originLine + 1);
            int position = endCharIdx;
            for (int i = cachedInputTextGenerator.lines[originLine + 1].startCharIdx; i < endCharIdx; ++i)
            {
                if (cachedInputTextGenerator.characters[i].VertexQuad[0].position.x >= originChar.VertexQuad[0].position.x)
                {
                    position = i;
                    break;
                }
            }

            if (position < cachedInputTextGenerator.characterCount)
            {
                var currChar = cachedInputTextGenerator.characters[position];
                if (0 == currChar.CustomCharTag)
                    return position;

                while (++position < cachedInputTextGenerator.characterCount)
                {
                    var nnextChar = cachedInputTextGenerator.characters[position];
                    if (currChar.CustomCharTag != nnextChar.CustomCharTag)
                        break;
                }
            }

            return position;
        }

        private void MoveDown(bool shift)
        {
            MoveDown(shift, true);
        }

        private void MoveDown(bool shift, bool goToLastChar)
        {
            if (hasSelection && !shift)
            {
                // If we have a selection and press down without shift,
                // set caret position to end of selection before we move it down.
                caretPositionInternal = caretSelectPositionInternal = Mathf.Max(caretPositionInternal, caretSelectPositionInternal);
            }

            int position = multiLine ? LineDownCharacterPosition(caretSelectPositionInternal, goToLastChar) : RealTextLength;

            if (shift)
                caretSelectPositionInternal = position;
            else
                caretPositionInternal = caretSelectPositionInternal = position;
        }

        private void MoveUp(bool shift)
        {
            MoveUp(shift, true);
        }

        private void MoveUp(bool shift, bool goToFirstChar)
        {
            if (hasSelection && !shift)
            {
                // If we have a selection and press up without shift,
                // set caret position to start of selection before we move it up.
                caretPositionInternal = caretSelectPositionInternal = Mathf.Min(caretPositionInternal, caretSelectPositionInternal);
            }

            int position = multiLine ? LineUpCharacterPosition(caretSelectPositionInternal, goToFirstChar) : 0;

            if (shift)
                caretSelectPositionInternal = position;
            else
                caretSelectPositionInternal = caretPositionInternal = position;
        }

        private void Delete()
        {
            if (m_ReadOnly)
                return;

            if (caretPositionInternal == caretSelectPositionInternal)
                return;

            var txtCaretPosition = CharetPositionToTextIndex(caretPositionInternal);
            var txtCaretSelectPosition = CharetPositionToTextIndex(caretSelectPositionInternal);
            if (caretPositionInternal < caretSelectPositionInternal)
            {
                //m_Text = m_Text.Substring(0, txtCaretPosition) + m_Text.Substring(txtCaretSelectPosition, m_Text.Length - txtCaretSelectPosition);
                DeleteChar(txtCaretPosition, txtCaretSelectPosition);
                caretSelectPositionInternal = caretPositionInternal;
            }
            else
            {
                //m_Text = m_Text.Substring(0, txtCaretSelectPosition) + m_Text.Substring(txtCaretPosition, m_Text.Length - txtCaretPosition);
                DeleteChar(txtCaretSelectPosition, txtCaretPosition);
                caretPositionInternal = caretSelectPositionInternal;
            }
        }

        private void ForwardSpace()
        {
            if (m_ReadOnly)
                return;

            if (hasSelection)
            {
                Delete();
                UpdateTouchKeyboardFromEditChanges();
                SendOnValueChangedAndUpdateLabel();
            }
            else
            {
                var realCaret = CharetPositionToTextIndex(caretPositionInternal);
                if (realCaret < m_Text.Length)
                {
                    //m_Text = m_Text.Remove(realCaret, 1);
                    DeleteChar(realCaret, realCaret + 1);
                    UpdateTouchKeyboardFromEditChanges();
                    SendOnValueChangedAndUpdateLabel();
                }
            }
        }

        private void Backspace()
        {
            if (hasSelection)
            {
                Delete();
                UpdateTouchKeyboardFromEditChanges();
                SendOnValueChangedAndUpdateLabel();
            }
            else
            {
                if (caretPositionInternal > 0)
                {
                    var realCaret = CharetPositionToTextIndex(caretPositionInternal);
                    var c = m_Text[realCaret - 1];
                    var length = 1;
                    NTextElement e = null;
                    if (CustomElements.TryGetValue((int)c, out e))
                    {
                        length = Mathf.Max(1, null == e.Text ? 0 : e.Text.Length);
                    }

                    //m_Text = m_Text.Remove(realCaret - 1, 1);
                    DeleteChar(realCaret - 1, realCaret);
                    caretSelectPositionInternal = caretPositionInternal = caretPositionInternal - length;

                    UpdateTouchKeyboardFromEditChanges();
                    SendOnValueChangedAndUpdateLabel();
                }
            }
        }

        // Insert the character and update the label.
        private void Insert(char c)
        {
            if (m_ReadOnly)
                return;

            string replaceString = c.ToString();
            Delete();

            // Can't go past the character limit
            if (characterLimit > 0 && m_Text.Length >= characterLimit)
                return;

            //m_Text = m_Text.Insert( CharetPositionToTextIndex(m_CaretPosition), replaceString);
            InsertStr(replaceString, CharetPositionToTextIndex(m_CaretPosition));
            caretSelectPositionInternal = caretPositionInternal += replaceString.Length;

            UpdateTouchKeyboardFromEditChanges();
            SendOnValueChanged();
        }

        public void InsertChar(char c)
        {
            if (m_ReadOnly)
                return;

            string replaceString = c.ToString();
            Delete();

            // Can't go past the character limit
            if (characterLimit > 0 && m_Text.Length >= characterLimit)
                return;

            //m_Text = m_Text.Insert(CharetPositionToTextIndex(m_CaretPosition), replaceString);
            InsertStr(replaceString, CharetPositionToTextIndex(m_CaretPosition));
            caretSelectPositionInternal = caretPositionInternal += replaceString.Length;

            UpdateTouchKeyboardFromEditChanges();
            SendOnValueChangedAndUpdateLabel();
        }

        public void Append(char c, int selectionAnchorPosition)
        {

            Delete();
            // Can't go past the character limit
            if (characterLimit > 0 && m_Text.Length >= characterLimit)
                return;

            //m_Text = m_Text + c.ToString();
            InsertStr(c.ToString(), m_Text.Length);
            caretSelectPositionInternal = caretPositionInternal = RealTextLength;

            UpdateTouchKeyboardFromEditChanges();
            SendOnValueChangedAndUpdateLabel();
        }

        private void UpdateTouchKeyboardFromEditChanges()
        {
            // Update the TouchKeyboard's text from edit changes
            // if in-place editing is allowed
            if (m_Keyboard != null && InPlaceEditing())
            {
                m_Keyboard.text = m_Text;
            }
        }

        private void SendOnValueChangedAndUpdateLabel()
        {
            SendOnValueChanged();
            UpdateLabel();
        }

        private void SendOnValueChanged()
        {
            if (onValueChanged != null)
                onValueChanged.Invoke(m_Text);
        }

        /// <summary>
        /// Submit the input field's text.
        /// </summary>

        protected void SendOnSubmit()
        {
            if (onEndEdit != null)
                onEndEdit.Invoke(m_Text);
        }

        /// <summary>
        /// Append the specified text to the end of the current.
        /// </summary>

        protected virtual void Append(string input)
        {
            if (m_ReadOnly)
                return;

            if (!InPlaceEditing())
                return;

            for (int i = 0, imax = input.Length; i < imax; ++i)
            {
                char c = input[i];

                if (c >= ' ' || c == '\t' || c == '\r' || c == 10 || c == '\n')
                {
                    Append(c);
                }
            }
        }

        private const int k_MaxTextLength = UInt16.MaxValue / 4 - 1;

        protected virtual void Append(char input)
        {
            // We do not currently support surrogate pairs
            if (char.IsSurrogate(input))
                return;

            if (m_ReadOnly || m_Text.Length >= k_MaxTextLength)
                return;

            if (!InPlaceEditing())
                return;

            // If we have an input validator, validate the input first
            int insertionPoint = Math.Min(selectionFocusPosition, selectionAnchorPosition);
            if (onValidateInput != null)
                input = onValidateInput(m_Text, insertionPoint, input);
            else if (characterValidation != CharacterValidation.None)
                input = Validate(m_Text, insertionPoint, input);

            // If the input is invalid, skip it
            if (input == 0)
                return;

            // Append the character and update the label
            Insert(input);
        }

        /// <summary>
        /// Update the visual text Text.
        /// </summary>
        protected void UpdateLabel()
        {
            if (textComponent != null && textComponent.font != null && !m_PreventFontCallback)
            {
                // TTextGenerator.Populate invokes a callback that's called for anything
                // that needs to be updated when the data for that font has changed.
                // This makes all Text components that use that font update their vertices.
                // In turn, this makes the InputField that's associated with that Text component
                // update its label by calling this UpdateLabel method.
                // This is a recursive call we want to prevent, since it makes the InputField
                // update based on font data that didn't yet finish executing, or alternatively
                // hang on infinite recursion, depending on whether the cached value is cached
                // before or after the calculation.
                //
                // This callback also occurs when assigning text to our Text component, i.e.,
                // m_TextComponent.text = processed;

                m_PreventFontCallback = true;

                string fullText;
                if (compositionString.Length > 0)
                    fullText = m_Text.Substring(0, CharetPositionToTextIndex(m_CaretPosition)) + compositionString + m_Text.Substring(CharetPositionToTextIndex(m_CaretPosition));
                else
                    fullText = m_Text;

                string processed;
                if (inputType == InputType.Password)
                    processed = new string(asteriskChar, fullText.Length);
                else
                    processed = fullText;

                bool isEmpty = string.IsNullOrEmpty(fullText);

                if (m_Placeholder != null)
                    m_Placeholder.enabled = isEmpty;

                // If not currently editing the text, set the visible range to the whole text.
                // The UpdateLabel method will then truncate it to the part that fits inside the Text area.
                // We can't do this when text is being edited since it would discard the current scroll,
                // which is defined by means of the m_DrawStart and m_DrawEnd indices.
                if (!m_AllowInput)
                {
                    m_DrawStart = 0;
                    m_DrawEnd = RealTextLength;
                }

                //if (!isEmpty)
                {
                    // Determine what will actually fit into the given line
                    Vector2 extents = textComponent.rectTransform.rect.size;

                    var settings = textComponent.GetGenerationSettings(textComponent.GetGenerationSettings(extents));
                    settings.generateOutOfBounds = true;

                    cachedInputTextGenerator.PopulateAlways(NTextParser.Parse(processed, settings, TextElements, CustomElements), settings);

                    SetDrawRangeToContainCaretPosition(caretSelectPositionInternal);

                    processed = processed.Substring( CharetPositionToTextIndex(m_DrawStart, true), Mathf.Min(CharetPositionToTextIndex(m_DrawEnd), processed.Length) - CharetPositionToTextIndex(m_DrawStart, true));

                    //
                    ImageTexture.Clear();
                    var sprites = null == SpritePackage ? Sprites : SpritePackage.Sprites;
                    for (int i = 0; i < sprites.LengthSafe(); i++)
                    {
                        if (null != sprites[i])
                            ImageTexture[i] = sprites[i].texture;
                    }

                    SetCaretVisible();
                }
                //m_TextComponent.CustomElements = CustomElements;
                //m_TextComponent.text = processed;
                MarkGeometryAsDirty();
                SetAllDirty();
                m_PreventFontCallback = false;
            }
        }

        private bool IsSelectionVisible()
        {
            if (m_DrawStart > caretPositionInternal || m_DrawStart > caretSelectPositionInternal)
                return false;

            if (m_DrawEnd < caretPositionInternal || m_DrawEnd < caretSelectPositionInternal)
                return false;

            return true;
        }

        private static int GetLineStartPosition(NTextGenerator gen, int line)
        {
            line = Mathf.Clamp(line, 0, gen.lines.Count - 1);
            return gen.lines[line].startCharIdx;
        }

        private static int GetLineEndPosition(NTextGenerator gen, int line)
        {
            line = Mathf.Max(line, 0);
            if (line + 1 < gen.lines.Count)
                return gen.lines[line + 1].startCharIdx - 1;
            return gen.characterCountVisible;
        }

        private void SetDrawRangeToContainCaretPosition(int caretPos)
        {
            if (cachedInputTextGenerator.lineCount <= 0)
                return;

            // the extents gets modified by the pixel density, so we need to use the generated extents since that will be in the same 'space' as
            // the values returned by the TextGenerator.lines[x].height for instance.
            Vector2 extents = textComponent.rectTransform.rect.size;

            if (multiLine)
            {
                var lines = cachedInputTextGenerator.lines;
                int caretLine = DetermineCharacterLine(caretPos, cachedInputTextGenerator);

                if (caretPos > m_DrawEnd)
                {
                    // Caret comes after drawEnd, so we need to move drawEnd to a later line end that comes after caret.
                    m_DrawEnd = GetLineEndPosition(cachedInputTextGenerator, caretLine);
                    float bottomY = lines[caretLine].BaseLine + lines[caretLine].OffsetY;
                    //if (caretLine == lines.Count - 1)
                    //{
                    //    bottomY += lines[caretLine].leading;
                    //}

                    int startLine = caretLine;
                    while (startLine > 0)
                    {
                        float topY = lines[startLine - 1].BaseLine + lines[startLine - 1].Height;
                        if (topY - bottomY > extents.y)
                            break;
                        startLine--;
                    }
                    m_DrawStart = GetLineStartPosition(cachedInputTextGenerator, startLine);
                }
                else
                {
                    if (caretPos < m_DrawStart)
                    {
                        // Caret comes before drawStart, so we need to move drawStart to an earlier line start that comes before caret.
                        m_DrawStart = GetLineStartPosition(cachedInputTextGenerator, caretLine);
                    }

                    int startLine = DetermineCharacterLine(m_DrawStart, cachedInputTextGenerator);
                    int endLine = startLine;

                    float topY = lines[startLine].BaseLine + lines[startLine].Height;
                    float bottomY = lines[startLine].BaseLine + lines[startLine].OffsetY;
                    //if (endLine == lines.Count - 1)
                    //{
                    //    bottomY += lines[endLine].leading;
                    //}

                    while (endLine < lines.Count - 1)
                    {
                        bottomY = lines[endLine + 1].BaseLine + lines[endLine + 1].OffsetY;

                        //if (endLine + 1 == lines.Count - 1)
                        //{
                        //    // Remove interline spacing on last line.
                        //    bottomY += lines[endLine].leading;
                        //}

                        if (topY - bottomY > extents.y)
                            break;
                        ++endLine;
                    }

                    m_DrawEnd = GetLineEndPosition(cachedInputTextGenerator, endLine);

                    while (startLine > 0)
                    {
                        topY = lines[startLine - 1].BaseLine + lines[startLine - 1].Height;
                        if (topY - bottomY > extents.y)
                            break;
                        startLine--;
                    }
                    m_DrawStart = GetLineStartPosition(cachedInputTextGenerator, startLine);
                }
            }
            else
            {
                var characters = cachedInputTextGenerator.characters;
                if (m_DrawEnd > cachedInputTextGenerator.characterCountVisible)
                    m_DrawEnd = cachedInputTextGenerator.characterCountVisible;

                float width = 0.0f;
                if (caretPos > m_DrawEnd || (caretPos == m_DrawEnd && m_DrawStart > 0))
                {
                    // fit characters from the caretPos leftward
                    m_DrawEnd = caretPos;
                    for (m_DrawStart = m_DrawEnd - 1; m_DrawStart >= 0; --m_DrawStart)
                    {
                        if (width + characters[m_DrawStart].Advance > extents.x)
                            break;

                        width += characters[m_DrawStart].Advance;
                    }
                    ++m_DrawStart;  // move right one to the last character we could fit on the left
                }
                else
                {
                    if (caretPos < m_DrawStart)
                        m_DrawStart = caretPos;

                    m_DrawEnd = m_DrawStart;
                }

                // fit characters rightward
                for (; m_DrawEnd < cachedInputTextGenerator.characterCountVisible; ++m_DrawEnd)
                {
                    width += characters[m_DrawEnd].Advance;
                    if (width > extents.x)
                        break;
                }
            }
        }

        public void ForceLabelUpdate()
        {
            UpdateLabel();
        }

        private void MarkGeometryAsDirty()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying || UnityEditor.PrefabUtility.GetPrefabObject(gameObject) != null)
                return;
#endif

            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
        }

        public override void Rebuild(CanvasUpdate update)
        {
            switch (update)
            {
                case CanvasUpdate.LatePreRender:
                    UpdateGeometry();
                    break;
                default:
                    base.Rebuild(update);
                    break;
            }
        }

        //public virtual void LayoutComplete()
        //{ }

        //public virtual void GraphicUpdateComplete()
        //{ }

        protected override void UpdateGeometry()
        {
            base.UpdateGeometry();

#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif
            // No need to draw a cursor on mobile as its handled by the devices keyboard.
            if (!shouldHideMobileInput)
                return;

            if (m_CachedInputRenderer == null && textComponent != null)
            {
                GameObject go = new GameObject(transform.name + " Input Caret", typeof(RectTransform), typeof(CanvasRenderer));
                //go.hideFlags = HideFlags.DontSave;
                go.transform.SetParent(transform);
                go.transform.SetAsFirstSibling();
                go.layer = gameObject.layer;

                caretRectTrans = go.GetComponent<RectTransform>();
                m_CachedInputRenderer = go.GetComponent<CanvasRenderer>();
                m_CachedInputRenderer.SetMaterial(textComponent.GetModifiedMaterial(Graphic.defaultGraphicMaterial), Texture2D.whiteTexture);

                // Needed as if any layout is present we want the caret to always be the same as the text area.
                go.AddComponent<LayoutElement>().ignoreLayout = true;

                AssignPositioningIfNeeded();
            }

            if (m_CachedInputRenderer == null)
                return;

            OnFillVBO(mesh);
            m_CachedInputRenderer.SetMesh(mesh);
        }

        private void AssignPositioningIfNeeded()
        {
            if (textComponent != null && caretRectTrans != null &&
                (caretRectTrans.localPosition != textComponent.rectTransform.localPosition ||
                 caretRectTrans.localRotation != textComponent.rectTransform.localRotation ||
                 caretRectTrans.localScale != textComponent.rectTransform.localScale ||
                 caretRectTrans.anchorMin != textComponent.rectTransform.anchorMin ||
                 caretRectTrans.anchorMax != textComponent.rectTransform.anchorMax ||
                 caretRectTrans.anchoredPosition != textComponent.rectTransform.anchoredPosition ||
                 caretRectTrans.sizeDelta != textComponent.rectTransform.sizeDelta ||
                 caretRectTrans.pivot != textComponent.rectTransform.pivot))
            {
                //caretRectTrans.localPosition = textComponent.rectTransform.localPosition;
                //caretRectTrans.localRotation = textComponent.rectTransform.localRotation;
                //caretRectTrans.localScale = textComponent.rectTransform.localScale;
                //caretRectTrans.anchorMin = textComponent.rectTransform.anchorMin;
                //caretRectTrans.anchorMax = textComponent.rectTransform.anchorMax;
                //caretRectTrans.anchoredPosition = textComponent.rectTransform.anchoredPosition;
                //caretRectTrans.sizeDelta = textComponent.rectTransform.sizeDelta;
                //caretRectTrans.pivot = textComponent.rectTransform.pivot;
                caretRectTrans.localPosition = Vector3.zero;
                caretRectTrans.localScale = Vector3.one;
                caretRectTrans.localRotation = Quaternion.identity;
                caretRectTrans.anchorMin = Vector2.zero;
                caretRectTrans.anchorMax = Vector2.one;
                caretRectTrans.sizeDelta = Vector2.zero;
                caretRectTrans.pivot = Vector2.zero;
            }
        }

        private void OnFillVBO(Mesh vbo)
        {
            using (var helper = new VertexHelper())
            {
                if (!isFocused)
                {
                    helper.FillMesh(vbo);
                    return;
                }

                Vector2 roundingOffset = textComponent.PixelAdjustPoint(Vector2.zero);
                if (!hasSelection)
                    GenerateCursor(helper, roundingOffset);
                else
                    GenerateHightlight(helper, roundingOffset);

                helper.FillMesh(vbo);
            }
        }

        private void GenerateCursor(VertexHelper vbo, Vector2 roundingOffset)
        {
            if (!m_CaretVisible)
                return;

            if (m_CursorVerts == null)
            {
                CreateCursorVerts();
            }

            float width = m_CaretWidth;
            int adjustedPos = Mathf.Max(0, caretPositionInternal);
            NTextGenerator gen = textComponent.cachedTextGenerator;

            if (gen == null)
                return;

            if (gen.lineCount == 0)
                return;

            //if (m_TextComponent.resizeTextForBestFit)
            //	height = gen.fontSizeUsedForBestFit / m_TextComponent.pixelsPerUnit;

            Vector2 startPosition = Vector2.zero;

            if (adjustedPos < gen.characters.Count)
            {
                NTextGlyph cursorChar = gen.characters[adjustedPos];
                startPosition.x = cursorChar.VertexQuad[0].position.x;
            }

            // Calculate startPosition
            //      if (gen.characterCountVisible + 1 > adjustedPos || adjustedPos == 0)
            //{
            //	TRichTextGlyph cursorChar = gen.characters[adjustedPos];
            //	startPosition.x = cursorChar.VertexQuad[0].position.x;
            //	startPosition.y = cursorChar.VertexQuad[0].position.y;
            //}
            startPosition.x /= textComponent.pixelsPerUnit;

            // TODO: Only clamp when Text uses horizontal word wrap.
            //if (startPosition.x > m_TextComponent.rectTransform.rect.xMax)
            //    startPosition.x = m_TextComponent.rectTransform.rect.xMax;

            int characterLine = DetermineCharacterLine(adjustedPos, gen);
            startPosition.y = gen.lines[characterLine].BaseLine + gen.lines[characterLine].Height;
            float height = gen.lines[characterLine].Height;

            if (m_DrawStart > 0)
            {
                var extents = textComponent.rectTransform.rect;

                int currentLineIndex = DetermineCharacterLine(m_DrawStart, cachedInputTextGenerator);
                if (currentLineIndex > 0)
                {
                    var lastline = cachedInputTextGenerator.lines[currentLineIndex - 1];
                    startPosition.y += extents.yMax - (lastline.BaseLine + lastline.OffsetY);
                }

                var firstChar = cachedInputTextGenerator.characters[m_DrawStart];
                startPosition.x += extents.xMin - (firstChar.VertexQuad[0].position.x + firstChar.MinX);
            }

            for (int i = 0; i < m_CursorVerts.Length; i++)
                m_CursorVerts[i].color = caretColor;

            m_CursorVerts[0].position = new Vector3(startPosition.x, startPosition.y - height, 0.0f);
            m_CursorVerts[1].position = new Vector3(startPosition.x + width, startPosition.y - height, 0.0f);
            m_CursorVerts[2].position = new Vector3(startPosition.x + width, startPosition.y, 0.0f);
            m_CursorVerts[3].position = new Vector3(startPosition.x, startPosition.y, 0.0f);

            if (roundingOffset != Vector2.zero)
            {
                for (int i = 0; i < m_CursorVerts.Length; i++)
                {
                    UIVertex uiv = m_CursorVerts[i];
                    uiv.position.x += roundingOffset.x;
                    uiv.position.y += roundingOffset.y;
                }
            }

            vbo.AddUIVertexQuad(m_CursorVerts);

            int screenHeight = Screen.height;
            // Multiple display support only when not the main display. For display 0 the reported
            // resolution is always the desktops resolution since its part of the display API,
            // so we use the standard none multiple display method. (case 741751)
            int displayIndex = textComponent.canvas.targetDisplay;
            if (displayIndex > 0 && displayIndex < Display.displays.Length)
                screenHeight = Display.displays[displayIndex].renderingHeight;

            startPosition.y = screenHeight - startPosition.y;
            input.compositionCursorPos = startPosition;
        }

        private void CreateCursorVerts()
        {
            m_CursorVerts = new UIVertex[4];

            for (int i = 0; i < m_CursorVerts.Length; i++)
            {
                m_CursorVerts[i] = UIVertex.simpleVert;
                m_CursorVerts[i].uv0 = Vector2.zero;
            }
        }

        private float SumLineHeights(int endLine, NTextGenerator generator)
        {
            if (endLine < generator.lineCount)
                return generator.lines[endLine].BaseLine + generator.lines[endLine].Height;
            //float height = 0.0f;
            //for (int i = 0; i < endLine; ++i)
            //{
            //	height += generator.lines[i].height;
            //}

            //return height;
            return 0;
        }

        private void GenerateHightlight(VertexHelper vbo, Vector2 roundingOffset)
        {
            int startChar = Mathf.Max(0, caretPositionInternal);
            int endChar = Mathf.Max(0, caretSelectPositionInternal);

            // Ensure pos is always less then selPos to make the code simpler
            if (startChar > endChar)
            {
                int temp = startChar;
                startChar = endChar;
                endChar = temp;
            }
            startChar = Mathf.Max(m_DrawStart, startChar);
            endChar = Mathf.Min(m_DrawEnd, endChar);

            endChar -= 1;
            NTextGenerator gen = textComponent.cachedTextGenerator;

            if (gen.lineCount <= 0)
                return;

            int currentLineIndex = DetermineCharacterLine(startChar, gen);

            int lastCharInLineIndex = GetLineEndPosition(gen, currentLineIndex);

            UIVertex vert = UIVertex.simpleVert;
            vert.uv0 = Vector2.zero;
            vert.color = selectionColor;

            float height = textComponent.font.lineHeight;

            //if (m_TextComponent.resizeTextForBestFit)
            //	height = gen.fontSizeUsedForBestFit / m_TextComponent.pixelsPerUnit;

            if (cachedInputTextGenerator != null && cachedInputTextGenerator.lines.Count > 0)
            {
                // TODO: deal with multiple lines with different line heights.
                height = cachedInputTextGenerator.lines[0].Height;
            }

            //if (m_TextComponent.resizeTextForBestFit && cachedInputTextGenerator != null)
            //{
            //	height = cachedInputTextGenerator.fontSizeUsedForBestFit;
            //}
            Vector3 startPos = Vector3.zero;
            if (m_DrawStart > 0)
            {
                var extents = textComponent.rectTransform.rect;
                int drawStartLine = DetermineCharacterLine(m_DrawStart, gen);
                if (drawStartLine > 0)
                {
                    var lastline = cachedInputTextGenerator.lines[drawStartLine - 1];
                    startPos.y = extents.yMax - (lastline.BaseLine + lastline.OffsetY);
                }

                var firstChar = cachedInputTextGenerator.characters[m_DrawStart];
                startPos.x = extents.xMin - (firstChar.VertexQuad[0].position.x + firstChar.MinX);
            }

            int currentChar = startChar;
            while (currentChar <= endChar && currentChar < gen.characterCount)
            {
                if (currentChar == lastCharInLineIndex || currentChar == endChar)
                {
                    NTextGlyph startCharInfo = gen.characters[startChar];
                    NTextGlyph endCharInfo = gen.characters[currentChar];
                    float lineHeights = SumLineHeights(currentLineIndex, gen);
                    float lineHeight = cachedInputTextGenerator.lines[currentLineIndex].Height;
                    Vector2 startPosition = new Vector2(startCharInfo.VertexQuad[0].position.x, lineHeights);
                    Vector2 endPosition = new Vector2((endCharInfo.VertexQuad[0].position.x + endCharInfo.Advance), startPosition.y - lineHeight);

                    // Checking xMin as well due to text generator not setting possition if char is not rendered.
                    if (endPosition.x > textComponent.rectTransform.rect.xMax || endPosition.x < textComponent.rectTransform.rect.xMin)
                        endPosition.x = textComponent.rectTransform.rect.xMax;

                    var startIndex = vbo.currentVertCount;
                    vert.position = new Vector3(startPosition.x, endPosition.y, 0.0f) + (Vector3)roundingOffset + startPos;
                    vbo.AddVert(vert);

                    vert.position = new Vector3(endPosition.x, endPosition.y, 0.0f) + (Vector3)roundingOffset + startPos;
                    vbo.AddVert(vert);

                    vert.position = new Vector3(endPosition.x, startPosition.y, 0.0f) + (Vector3)roundingOffset + startPos;
                    vbo.AddVert(vert);

                    vert.position = new Vector3(startPosition.x, startPosition.y, 0.0f) + (Vector3)roundingOffset + startPos;
                    vbo.AddVert(vert);

                    vbo.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
                    vbo.AddTriangle(startIndex + 2, startIndex + 3, startIndex + 0);

                    startChar = currentChar + 1;
                    currentLineIndex++;

                    lastCharInLineIndex = GetLineEndPosition(gen, currentLineIndex);
                }
                currentChar++;
            }
        }

        /// <summary>
        /// Validate the specified input.
        /// </summary>

        protected char Validate(string text, int pos, char ch)
        {
            // Validation is disabled
            if (characterValidation == CharacterValidation.None || !enabled)
                return ch;

            if (characterValidation == CharacterValidation.Integer || characterValidation == CharacterValidation.Decimal)
            {
                // Integer and decimal
                bool cursorBeforeDash = (pos == 0 && text.Length > 0 && text[0] == '-');
                bool dashInSelection = text.Length > 0 && text[0] == '-' && ((caretPositionInternal == 0 && caretSelectPositionInternal > 0) || (caretSelectPositionInternal == 0 && caretPositionInternal > 0));
                bool selectionAtStart = caretPositionInternal == 0 || caretSelectPositionInternal == 0;
                if (!cursorBeforeDash || dashInSelection)
                {
                    if (ch >= '0' && ch <= '9') return ch;
                    if (ch == '-' && (pos == 0 || selectionAtStart)) return ch;
                    if ((ch == '.' || ch == ',') && characterValidation == CharacterValidation.Decimal && text.IndexOfAny(new[] { '.', ',' }) == -1) return ch;
                }
            }
            else if (characterValidation == CharacterValidation.Alphanumeric)
            {
                // All alphanumeric characters
                if (ch >= 'A' && ch <= 'Z') return ch;
                if (ch >= 'a' && ch <= 'z') return ch;
                if (ch >= '0' && ch <= '9') return ch;
            }
            else if (characterValidation == CharacterValidation.Name)
            {
                if (char.IsLetter(ch))
                {
                    // Space followed by a letter -- make sure it's capitalized
                    if (char.IsLower(ch) && ((pos == 0) || (text[pos - 1] == ' ')))
                        return char.ToUpper(ch);

                    // Uppercase letters are only allowed after spaces (and apostrophes)
                    if (char.IsUpper(ch) && (pos > 0) && (text[pos - 1] != ' ') && (text[pos - 1] != '\''))
                        return char.ToLower(ch);

                    // If character was already in correct case, return it as-is.
                    // Also, letters that are neither upper nor lower case are always allowed.
                    return ch;
                }
                else if (ch == '\'')
                {
                    // Don't allow more than one apostrophe
                    if (!text.Contains("'"))
                        // Don't allow consecutive spaces and apostrophes.
                        if (!(((pos > 0) && ((text[pos - 1] == ' ') || (text[pos - 1] == '\''))) ||
                              ((pos < text.Length) && ((text[pos] == ' ') || (text[pos] == '\'')))))
                            return ch;
                }
                else if (ch == ' ')
                {
                    if (pos != 0) // Don't allow leading spaces
                    {
                        // Don't allow consecutive spaces and apostrophes.
                        if (!(((pos > 0) && ((text[pos - 1] == ' ') || (text[pos - 1] == '\''))) ||
                              ((pos < text.Length) && ((text[pos] == ' ') || (text[pos] == '\'')))))
                            return ch;
                    }
                }
            }
            else if (characterValidation == CharacterValidation.EmailAddress)
            {
                // From StackOverflow about allowed characters in email addresses:
                // Uppercase and lowercase English letters (a-z, A-Z)
                // Digits 0 to 9
                // Characters ! # $ % & ' * + - / = ? ^ _ ` { | } ~
                // Character . (dot, period, full stop) provided that it is not the first or last character,
                // and provided also that it does not appear two or more times consecutively.

                if (ch >= 'A' && ch <= 'Z') return ch;
                if (ch >= 'a' && ch <= 'z') return ch;
                if (ch >= '0' && ch <= '9') return ch;
                if (ch == '@' && text.IndexOf('@') == -1) return ch;
                if (kEmailSpecialCharacters.IndexOf(ch) != -1) return ch;
                if (ch == '.')
                {
                    char lastChar = (text.Length > 0) ? text[Mathf.Clamp(pos, 0, text.Length - 1)] : ' ';
                    char nextChar = (text.Length > 0) ? text[Mathf.Clamp(pos + 1, 0, text.Length - 1)] : '\n';
                    if (lastChar != '.' && nextChar != '.')
                        return ch;
                }
            }
            return (char)0;
        }

        public void ActivateInputField()
        {
            if (textComponent == null || textComponent.font == null || !IsActive() || !IsInteractable())
                return;

            if (isFocused)
            {
                if (m_Keyboard != null && !m_Keyboard.active)
                {
                    m_Keyboard.active = true;
                    m_Keyboard.text = m_Text;
                }
            }

            m_ShouldActivateNextUpdate = true;
        }

        private void ActivateInputFieldInternal()
        {
            if (EventSystem.current == null)
                return;

            if (EventSystem.current.currentSelectedGameObject != gameObject)
                EventSystem.current.SetSelectedGameObject(gameObject);

            if (TouchScreenKeyboard.isSupported)
            {
                if (input.touchSupported)
                {
                    TouchScreenKeyboard.hideInput = shouldHideMobileInput;
                }

                m_Keyboard = (inputType == InputType.Password) ?
                    TouchScreenKeyboard.Open(m_Text, keyboardType, false, multiLine, true, false, "") :
                    TouchScreenKeyboard.Open(m_Text, keyboardType, inputType == InputType.AutoCorrect, multiLine, false, false, "");

                // Cache the value of isInPlaceEditingAllowed, because on UWP this involves calling into native code
                // The value only needs to be updated once when the TouchKeyboard is opened.
                //m_TouchKeyboardAllowsInPlaceEditing = TouchScreenKeyboard.isInPlaceEditingAllowed;

                // Mimics OnFocus but as mobile doesn't properly support select all
                // just set it to the end of the text (where it would move when typing starts)
                MoveTextEnd(false);
            }
            else
            {
                input.imeCompositionMode = IMECompositionMode.On;
                OnFocus();
            }

            m_AllowInput = true;
            m_OriginalText = m_Text;
            m_WasCanceled = false;
            SetCaretVisible();
            UpdateLabel();
        }

        public virtual void OnSelect(BaseEventData eventData)
        {
            //base.OnSelect(eventData);

            if (shouldActivateOnSelect)
                ActivateInputField();
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            ActivateInputField();
        }

        public void DeactivateInputField()
        {
            // Not activated do nothing.
            if (!m_AllowInput)
                return;

            m_HasDoneFocusTransition = false;
            m_AllowInput = false;

            if (m_Placeholder != null)
                m_Placeholder.enabled = string.IsNullOrEmpty(m_Text);

            if (textComponent != null && IsInteractable())
            {
                if (m_WasCanceled)
                    text = m_OriginalText;

                SendOnSubmit();

                if (m_Keyboard != null)
                {
                    m_Keyboard.active = false;
                    m_Keyboard = null;
                }

                // modify by niehong
                //m_CaretPosition = m_CaretSelectPosition = 0;

                input.imeCompositionMode = IMECompositionMode.Auto;
            }

            MarkGeometryAsDirty();
        }

        public virtual void OnDeselect(BaseEventData eventData)
        {
            DeactivateInputField();
            //base.OnDeselect(eventData);
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
                return;

            if (!isFocused)
                m_ShouldActivateNextUpdate = true;
        }

        private void EnforceContentType()
        {
            switch (contentType)
            {
                case ContentType.Standard:
                    {
                        // Don't enforce line type for this content type.
                        m_InputType = InputType.Standard;
                        m_KeyboardType = TouchScreenKeyboardType.Default;
                        m_CharacterValidation = CharacterValidation.None;
                        break;
                    }
                case ContentType.Autocorrected:
                    {
                        // Don't enforce line type for this content type.
                        m_InputType = InputType.AutoCorrect;
                        m_KeyboardType = TouchScreenKeyboardType.Default;
                        m_CharacterValidation = CharacterValidation.None;
                        break;
                    }
                case ContentType.IntegerNumber:
                    {
                        m_LineType = LineType.SingleLine;
                        m_InputType = InputType.Standard;
                        m_KeyboardType = TouchScreenKeyboardType.NumberPad;
                        m_CharacterValidation = CharacterValidation.Integer;
                        break;
                    }
                case ContentType.DecimalNumber:
                    {
                        m_LineType = LineType.SingleLine;
                        m_InputType = InputType.Standard;
                        m_KeyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
                        m_CharacterValidation = CharacterValidation.Decimal;
                        break;
                    }
                case ContentType.Alphanumeric:
                    {
                        m_LineType = LineType.SingleLine;
                        m_InputType = InputType.Standard;
                        m_KeyboardType = TouchScreenKeyboardType.ASCIICapable;
                        m_CharacterValidation = CharacterValidation.Alphanumeric;
                        break;
                    }
                case ContentType.Name:
                    {
                        m_LineType = LineType.SingleLine;
                        m_InputType = InputType.Standard;
                        m_KeyboardType = TouchScreenKeyboardType.Default;
                        m_CharacterValidation = CharacterValidation.Name;
                        break;
                    }
                case ContentType.EmailAddress:
                    {
                        m_LineType = LineType.SingleLine;
                        m_InputType = InputType.Standard;
                        m_KeyboardType = TouchScreenKeyboardType.EmailAddress;
                        m_CharacterValidation = CharacterValidation.EmailAddress;
                        break;
                    }
                case ContentType.Password:
                    {
                        m_LineType = LineType.SingleLine;
                        m_InputType = InputType.Password;
                        m_KeyboardType = TouchScreenKeyboardType.Default;
                        m_CharacterValidation = CharacterValidation.None;
                        break;
                    }
                case ContentType.Pin:
                    {
                        m_LineType = LineType.SingleLine;
                        m_InputType = InputType.Password;
                        m_KeyboardType = TouchScreenKeyboardType.NumberPad;
                        m_CharacterValidation = CharacterValidation.Integer;
                        break;
                    }
                default:
                    {
                        // Includes Custom type. Nothing should be enforced.
                        break;
                    }
            }

            EnforceTextHOverflow();
        }

        void SetToCustomIfContentTypeIsNot(params ContentType[] allowedContentTypes)
        {
            if (contentType == ContentType.Custom)
                return;

            for (int i = 0; i < allowedContentTypes.Length; i++)
                if (contentType == allowedContentTypes[i])
                    return;

            contentType = ContentType.Custom;
        }

        void SetToCustom()
        {
            if (contentType == ContentType.Custom)
                return;

            contentType = ContentType.Custom;
        }

        //protected override void DoStateTransition(SelectionState state, bool instant)
        //{
        //    if (m_HasDoneFocusTransition)
        //        state = SelectionState.Highlighted;
        //    else if (state == SelectionState.Pressed)
        //        m_HasDoneFocusTransition = true;

        //    base.DoStateTransition(state, instant);
        //}

        void EnforceTextHOverflow()
        {
            if (textComponent != null)
            {
                if (multiLine)
                    textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
                else
                    textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
                textComponent.supportRichText = false;
            }
        }

        /// <summary>
        /// See ILayoutElement.CalculateLayoutInputHorizontal.
        /// </summary>
        //public virtual void CalculateLayoutInputHorizontal() { }

        /// <summary>
        /// See ILayoutElement.CalculateLayoutInputVertical.
        /// </summary>
        //public virtual void CalculateLayoutInputVertical() { }

        /// <summary>
        /// See ILayoutElement.minWidth.
        /// </summary>
        //public virtual float minWidth { get { return 0; } }

        /// <summary>
        /// Get the displayed with of all input characters.
        /// </summary>
        //public virtual float preferredWidth
        //{
        //    get
        //    {
        //        if (textComponent == null)
        //            return 0;
        //        var settings = textComponent.GetGenerationSettings(Vector2.zero);
        //        return textComponent.cachedTextGeneratorForLayout.GetPreferredWidth(m_Text, settings) / textComponent.pixelsPerUnit;
        //    }
        //}

        /// <summary>
        /// See ILayoutElement.flexibleWidth.
        /// </summary>
        //public virtual float flexibleWidth { get { return -1; } }

        /// <summary>
        /// See ILayoutElement.minHeight.
        /// </summary>
        //public virtual float minHeight { get { return 0; } }

        /// <summary>
        /// Get the height of all the text if constrained to the height of the RectTransform.
        /// </summary>
        //public virtual float preferredHeight
        //{
        //    get
        //    {
        //        if (textComponent == null)
        //            return 0;
        //        var settings = textComponent.GetGenerationSettings(new Vector2(textComponent.rectTransform.rect.size.x, 0.0f));
        //        return textComponent.cachedTextGeneratorForLayout.GetPreferredHeight(m_Text, settings) / textComponent.pixelsPerUnit;
        //    }
        //}

        /// <summary>
        /// See ILayoutElement.flexibleHeight.
        /// </summary>
        //public virtual float flexibleHeight { get { return -1; } }

        /// <summary>
        /// See ILayoutElement.layoutPriority.
        /// </summary>
        //public virtual int layoutPriority { get { return 1; } }


        public int AppendLink(string text, string linkParam = null, int fontSize = 0, FontStyle? fontStyle = null, Color32? color = null, Color32? bottomColor = null, Color32? underlineColor = null, Color32? strikethroughColor = null)
        {
            RealTextLength += text.Length;

            var element = TextElementPool.Get();
            element.Text = text;
            element.LinkParam = linkParam;
            element.TopColor = null == color ? (Color32)textComponent.color : color.Value;
            element.BottomColor = null == bottomColor ? element.TopColor : bottomColor.Value;
            element.FontSize = fontSize <= 0 ? textComponent.fontSize : fontSize;
            element.FontStyle = null == fontStyle ? textComponent.fontStyle : fontStyle.Value;
            element.StrikethroughColor = strikethroughColor;
            element.UnderlineColor = underlineColor;
            element.CustomCharTag = CustomRichTag;
            CustomElements[CustomRichTag] = element;

            m_Text += (char)CustomRichTag;
            caretSelectPositionInternal = caretPositionInternal = RealTextLength;
            UpdateLabel();
            return CustomRichTag++;
        }

        public int AppendImage(int spriteIndex, float spriteScale = 1.0f, NVerticalAlign align = NVerticalAlign.Bottom, int animLength = 0, int animFrame = 0)
        {
            RealTextLength++;

            var element = TextElementPool.Get();
            element.SpriteIndex = spriteIndex;
            element.SpriteScale = spriteScale;
            element.SpriteAlign = align;
            element.TopColor = element.BottomColor = Color.white;
            int defaultAnimLength = null == textComponent.SpritePackage ? textComponent.DefaultAnimLength : textComponent.SpritePackage.DefaultAnimLength;
            element.AnimLength = 0 == animLength ? defaultAnimLength : animLength;
            int defaultAnimFrame = null == textComponent.SpritePackage ? textComponent.DefaultAnimFrame : textComponent.SpritePackage.DefaultAnimFrame;
            element.AnimFrame = 0 == animFrame ? defaultAnimFrame : animFrame;
            element.CustomCharTag = CustomRichTag;
            CustomElements[CustomRichTag] = element;

            m_Text += (char)CustomRichTag;
            caretSelectPositionInternal = caretPositionInternal = RealTextLength;
            UpdateLabel();
            return CustomRichTag++;
        }

        public int InsertLink(string text, int txtPos, string linkParam, int fontSize = 0, FontStyle? fontStyle = null, Color32? color = null, Color32? bottomColor = null, Color32? underlineColor = null, Color32? strikethroughColor = null)
        {
            RealTextLength += text.Length;

            var element = TextElementPool.Get();
            element.Text = text;
            element.LinkParam = linkParam;
            element.TopColor = null == color ? (Color32)textComponent.color : color.Value;
            element.BottomColor = null == bottomColor ? element.TopColor : bottomColor.Value;
            element.FontSize = fontSize <= 0 ? textComponent.fontSize : fontSize;
            element.FontStyle = null == fontStyle ? textComponent.fontStyle : fontStyle.Value;
            element.StrikethroughColor = strikethroughColor;
            element.UnderlineColor = underlineColor;
            element.CustomCharTag = CustomRichTag;
            element.CustomCharTag = CustomRichTag;
            CustomElements[CustomRichTag] = element;

            txtPos = Mathf.Clamp(txtPos, 0, m_Text.Length);
            m_Text = m_Text.Insert(txtPos, ((char)CustomRichTag).ToString());
            caretSelectPositionInternal = caretPositionInternal += text.Length;
            UpdateLabel();
            return CustomRichTag++;
        }

        public int InsertImage(int spriteIndex, int txtPos, float spriteScale = 1.0f, NVerticalAlign align = NVerticalAlign.Bottom, int animLength = 0, int animFrame = 0, Color32? underlineColor = null, Color32? strikethroughColor = null)
        {
            RealTextLength++;

            var element = TextElementPool.Get();
            element.SpriteIndex = spriteIndex;
            element.SpriteScale = spriteScale;
            element.SpriteAlign = align;
            element.TopColor = element.BottomColor = Color.white;
            int defaultAnimLength = null == textComponent.SpritePackage ? textComponent.DefaultAnimLength : textComponent.SpritePackage.DefaultAnimLength;
            element.AnimLength = 0 == animLength ? defaultAnimLength : animLength;
            int defaultAnimFrame = null == textComponent.SpritePackage ? textComponent.DefaultAnimFrame : textComponent.SpritePackage.DefaultAnimFrame;
            element.AnimFrame = 0 == animFrame ? defaultAnimFrame : animFrame;
            element.CustomCharTag = CustomRichTag;
            CustomElements[CustomRichTag] = element;

            txtPos = Mathf.Clamp(txtPos, 0, m_Text.Length);
            m_Text = m_Text.Insert(txtPos, ((char)CustomRichTag).ToString());
            caretSelectPositionInternal = caretPositionInternal += 1;
            UpdateLabel();
            return CustomRichTag++;
        }

        private int CharetPositionToTextIndex(int realCaret, bool before = false)
        {
            int realIndex = 0;
            int txtCharet = 0;
            for (int i = 0; i < TextElements.Count; i++)
            {
                var e = TextElements[i];
                if (0 == e.CustomCharTag)
                {
                    var length = null == e.Text ? 0 : e.Text.Length;
                    if (realIndex + length > realCaret)
                    {
                        return txtCharet + realCaret - realIndex;
                    }
                    realIndex += length;
                    txtCharet += length;
                }
                else
                {
                    if (realIndex == realCaret)
                        return txtCharet;

                    txtCharet++;
                    realIndex += Mathf.Max(1, null == e.Text ? 0 : e.Text.Length);
                    if (realIndex > realCaret)
                    {
                        if (before)
                            return --txtCharet;
                        else
                            return txtCharet;
                    }
                }
            }

            return txtCharet;
        }

        private void InsertStr(string str, int txtPosition)
        {
            RealTextLength += str.Length;

            int index = 0;
            int txtCharet = 0;
            for (int i = 0; i < TextElements.Count; i++)
            {
                var e = TextElements[i];
                if (0 == e.CustomCharTag)
                {
                    var length = null == e.Text ? 0 : e.Text.Length;
                    if (index + length >= txtPosition)
                    {
                        int offset = txtPosition - index;
                        e.Text = e.Text.Insert(offset, str);
                        m_Text = m_Text.Insert(txtCharet + offset, str);
                        return;
                    }
                    index += length;
                    txtCharet += length;
                }
                else
                {
                    if (index == txtPosition)
                    {
                        var element = TextElementPool.Get();
                        element.Text = str;
                        element.TopColor = textComponent.color;
                        element.BottomColor = element.TopColor;
                        element.FontStyle = textComponent.fontStyle;
                        element.FontSize = textComponent.fontSize;
                        TextElements.Insert(i, element);
                        m_Text = m_Text.Insert(txtCharet, str);
                        return;
                    }

                    txtCharet++;
                    index++;
                }
            }

            {
                var element = TextElementPool.Get();
                element.Text = str;
                element.TopColor = textComponent.color;
                element.BottomColor = element.TopColor;
                element.FontStyle = textComponent.fontStyle;
                element.FontSize = textComponent.fontSize;
                TextElements.Add(element);
                m_Text += str;
            }
        }

        static int[] elementPos = new int[32];

        private void DeleteChar(int start, int end)
        {
            if (elementPos.Length < TextElements.Count * 2)
                Array.Resize(ref elementPos, TextElements.Count * 2);

            int index = 0;
            for (int i = 0; i < TextElements.Count; i++)
            {
                var e = TextElements[i];
                if (0 == e.CustomCharTag)
                {
                    var length = null == e.Text ? 0 : e.Text.Length;

                    elementPos[i * 2] = index;
                    elementPos[i * 2 + 1] = index + length;
                    index += length;
                }
                else
                {
                    elementPos[i * 2] = index;
                    elementPos[i * 2 + 1] = index + 1;
                    index++;
                }
            }

            int count = TextElements.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                var eStart = elementPos[i * 2];
                var eEnd = elementPos[i * 2 + 1];
                var e = TextElements[i];

                var length = e.CustomCharTag == 0 ? (null == e.Text ? 0 : e.Text.Length) : 1;
                if (eStart < end && eEnd >= start)
                {
                    int d_start = Mathf.Max(eStart, start);
                    int d_end = Mathf.Min(eEnd, end);
                    if (d_end - d_start == length)
                    {
                        RealTextLength -= null == e.Text ? (e.CustomCharTag == 0 ? 0 : 1) : e.Text.Length;

                        //if (CustomElements.ContainsKey(e.CustomCharTag))
                        //    CustomElements.Remove(e.CustomCharTag);

                        TextElementPool.Release(e);
                        TextElements.RemoveAt(i);
                    }
                    else
                    {
                        var text = e.Text;
                        builder.Length = 0;
                        if (d_start > eStart)
                            builder.Append( text.Substring(0, d_start - eStart) );
                        if (d_end < eEnd)
                            builder.Append( text.Substring(d_end - eStart, eEnd - d_end) );
                        e.Text = builder.ToString();

                        RealTextLength -= d_end - d_start;
                    }
                }

                if (eStart <= start)
                    break;
            }

            builder.Length = 0;
            if (start > 0)
                builder.Append(m_Text, 0, start);
            if (end < m_Text.Length)
                builder.Append(m_Text, end, m_Text.Length - end);
            m_Text = builder.ToString();
        }

        private void RemoveAllCustom()
        {
            CustomRichTag = '\xe000';
            foreach (var e in CustomElements)
                TextElementPool.Release(e.Value);
            CustomElements.Clear();
        }

        public bool IsInteractable()
        {
            return raycastTarget;
        }

        UIVertex[] vertexQuad = new UIVertex[4];
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            //if (Populate())
            //{
            ProcessMaterial();

            if (this.gameObject.activeInHierarchy && this.enabled && !_coAnimation)
                StartCoroutine(UpdateSprite());
            //}

            vh.Clear();

            Vector3 startPos = new Vector3();
            var extents = textComponent.rectTransform.rect ;
            if (m_DrawStart > 0)
            {
                int currentLineIndex = DetermineCharacterLine(m_DrawStart, cachedInputTextGenerator);
                if (currentLineIndex > 0)
                {
                    var lastline = cachedInputTextGenerator.lines[currentLineIndex - 1];
                    startPos.y = extents.yMax - (lastline.BaseLine + lastline.OffsetY);
                }

                var firstChar = cachedInputTextGenerator.characters[m_DrawStart];
                startPos.x = extents.xMin - (firstChar.VertexQuad[0].position.x + firstChar.MinX);

                for (int i = m_DrawStart; i < m_DrawEnd; i++)
                {
                    if (!cachedInputTextGenerator.characters[i].IsImage())
                    {
                        Array.Copy(cachedInputTextGenerator.characters[i].VertexQuad, vertexQuad, 4);
                        vertexQuad[0].position += startPos;
                        vertexQuad[1].position += startPos;
                        vertexQuad[2].position += startPos;
                        vertexQuad[3].position += startPos;
                        vh.AddUIVertexQuad(vertexQuad);
                    }
                }
            }
            else
            {
                for (int i = m_DrawStart; i < m_DrawEnd; i++)
                {
                    if (!cachedInputTextGenerator.characters[i].IsImage())
                    {
                        vh.AddUIVertexQuad(cachedInputTextGenerator.characters[i].VertexQuad);
                    }
                }
            }

            foreach (var glyph in cachedInputTextGenerator.EffectGlyphs)
            {
                Array.Copy(glyph.VertexQuad, vertexQuad, 4);
                vertexQuad[0].position += startPos;
                vertexQuad[1].position += startPos;
                vertexQuad[2].position += startPos;
                vertexQuad[3].position += startPos;

                if ((vertexQuad[0].position.x > extents.xMin && vertexQuad[0].position.x < extents.xMax && vertexQuad[0].position.y > extents.yMin && vertexQuad[0].position.y < extents.yMax) ||
                    (vertexQuad[2].position.x > extents.xMin && vertexQuad[2].position.x < extents.xMax && vertexQuad[2].position.y > extents.yMin && vertexQuad[2].position.y < extents.yMax) )
                {
                    Vector3 position = vertexQuad[0].position;
                    position.x = Mathf.Max(extents.xMin, position.x);
                    position.x = Mathf.Min(extents.xMax, position.x);
                    vertexQuad[0].position = position;

                    position = vertexQuad[1].position;
                    position.x = Mathf.Max(extents.xMin, position.x);
                    position.x = Mathf.Min(extents.xMax, position.x);
                    vertexQuad[1].position = position;

                    position = vertexQuad[2].position;
                    position.x = Mathf.Max(extents.xMin, position.x);
                    position.x = Mathf.Min(extents.xMax, position.x);
                    vertexQuad[2].position = position;

                    position = vertexQuad[3].position;
                    position.x = Mathf.Max(extents.xMin, position.x);
                    position.x = Mathf.Min(extents.xMax, position.x);
                    vertexQuad[3].position = position;

                    vh.AddUIVertexQuad(vertexQuad);
                }
            }
        }
    }
}