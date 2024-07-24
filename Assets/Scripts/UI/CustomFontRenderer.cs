using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Wozware.CrystalColumns;
using Image = UnityEngine.UI.Image;


[System.Serializable]
public struct CustomFontCharacter
{
    public Sprite CharacterSprite;
    public string Character;

	public CustomFontCharacter(Sprite characterSprite, string character)
    {
        CharacterSprite = characterSprite;
        Character = character;
    }
}

public class CustomFontRenderer : MonoBehaviour
{
	public CustomFont Font;
    public TextureFormat Format;
    public int Scale;
    public bool CenterSpacing;
	public bool HasStretchParent;
	public bool RenderAtStart = true;
	public bool IgnoreSameChar = false;
	public bool OverrideParent;
	public RectTransform OverrideParentTransform;
    public int Spacing;
    public int LineSpacing;
    [Multiline] public string Text;
	public bool AutoSizeParent = false;
	public float ParentPaddingX = 0;
	public float ParentPaddingY = 0;
	public bool Logging;
	public bool TestBool;

    [SerializeField] private Texture2D _texture;

	private RectTransform _transform;
	private RectTransform _parentTransform;
	private Image _img;

	private int _lastWidth = 0;
	private int _lastHeight = 0;
	private int _centerSpacing = 0;
	private Dictionary<string, CustomFontCharacter> _characters = new Dictionary<string, CustomFontCharacter>();
	private List<CustomFontCharacter> _chars = new List<CustomFontCharacter>();
	private List<string> _lastChars = new List<string>();

	private bool _initialized = false;

	private void Awake()
	{
		
	}

	private void Start()
    {
		_transform = GetComponent<RectTransform>();
		if (HasStretchParent)
		{
			_transform = transform.parent.GetComponent<RectTransform>();
		}
		_img = GetComponent<Image>();

		_centerSpacing = Spacing / 2;
		for(int i = 0; i < 999; i++)
		{
			_lastChars.Add("");
		}

		// cache the characters
		_characters = Font.Characters;
		_initialized = true;
		if (RenderAtStart)
		{
			RenderFont();
		}

		if (AutoSizeParent)
		{
			if(OverrideParent)
			{
				OverrideParentTransform.sizeDelta = new Vector2(_transform.sizeDelta.x + ParentPaddingX, _transform.sizeDelta.y + ParentPaddingY);
				return;
			}

			_parentTransform = _transform.parent.GetComponent<RectTransform>();
			_parentTransform.sizeDelta = new Vector2(_transform.sizeDelta.x + ParentPaddingX, _transform.sizeDelta.y + ParentPaddingY);
		}
	}

	public void SetText(string text, bool test = false)
	{
		Text = text;
		RenderFont();
		if(test)
			Debug.Log(Text);

		if(AutoSizeParent)
		{
			if (OverrideParent)
			{
				OverrideParentTransform.sizeDelta = new Vector2(_transform.sizeDelta.x + ParentPaddingX, _transform.sizeDelta.y + ParentPaddingY);
				return;
			}

			if(_parentTransform != null)
			{
				_parentTransform.sizeDelta = new Vector2(_transform.sizeDelta.x + ParentPaddingX, _transform.sizeDelta.y + ParentPaddingY);
			}
		}
	}

    public void RenderFont()
    {
		if(!_initialized)
		{
			return;
		}

		float startTime = Time.realtimeSinceStartup;
		int currWidth = 0;
		int currHeight = 0;
		int maxWidth = 0;
		int maxHeight = 0;
		int prevHeight = 0;
		int i = 0;
		int x = 0;
		int y = 0;

		_chars = new List<CustomFontCharacter>();

		if(Text.Length <= 0)
		{
			return;
		}

		// go through all text characters
		// determines the width/height and characters to add
		for (i = 0; i < Text.Length; i++)
		{
			string character = Text[i].ToString();

			// if the character is a new line, add a newline font char
			if (character == "\n")
			{
				_chars.Add(new CustomFontCharacter(null, "\n"));
				// reset the width and height
				currWidth = 0;
				currHeight = 0;
				// increase max height by line spacing
				maxHeight += LineSpacing;
				continue;
			}

			// check if valid character
			if (!_characters.ContainsKey(character))
			{
				Debug.LogWarning($"Invalid character {character} in font text.");
				continue;
			}

			Rect r = _characters[character].CharacterSprite.rect;

			// increment current width by size and spacing
			currWidth += (int)r.size.x;
			currWidth += Spacing;

			// prev height is the size now
			prevHeight = ((int)r.size.y);

			// check if max width needs to be expanded
			if (currWidth > maxWidth)
			{
				// update max width to curr width
				maxWidth = currWidth;
			}

			// check if max height needs to be expanded
			if (currHeight < prevHeight)
			{
				// increase max height by the difference
				// between prev height and current
				maxHeight += prevHeight - currHeight;
				// curr height is now the prev
				currHeight = prevHeight;
			}

			// finally add the character
			_chars.Add(Font.Characters[character]);
		}

		// if the max width or height has changed
		// then regenerate texture (saves processing)
		if (_lastWidth != maxWidth || _lastHeight != maxHeight)
		{
			_texture = new Texture2D(maxWidth, maxHeight, Format, false);
			_texture.filterMode = FilterMode.Point;
			_transform.sizeDelta = new Vector2(maxWidth * Scale, maxHeight * Scale);
			Color[] fillPixels = new Color[maxWidth * maxHeight];
			for (i = 0; i < fillPixels.Length; i++)
			{
				fillPixels[i] = Color.clear;
			}
			_texture.SetPixels(fillPixels);
		}

		// update the textures last width/height
		_lastWidth = maxWidth;
		_lastHeight = maxHeight;

		Rect texRect = new Rect(Vector2.zero, new Vector2(maxWidth, maxHeight));

		// whether or not to center the texture
		int defaultWidthInterval = CenterSpacing ? _centerSpacing : 0;
		int widthInterval = defaultWidthInterval;

		int heightInterval = 0;
		int lastHeight = 0;
		bool freshLine = false;

		// loop through added characters
		// to start generating the pixels
		for (i = 0; i < _chars.Count; i++)
		{
			// check for newline
			if (_chars[i].Character == "\n")
			{
				// reset width interval
				widthInterval = defaultWidthInterval;
				// expand height interval by last and spacing
				heightInterval += lastHeight + LineSpacing;
				// mark this as a new line
				freshLine = true;
				continue;
			}

			CustomFontCharacter customChar = _chars[i];
			Sprite s = customChar.CharacterSprite;
			Rect r = s.rect;

			int w = (int)r.size.x;
			int h = (int)r.size.y;
			int rh = (int)r.size.y;

			// if the prev character was larger
			// and not a new line
			if (h < lastHeight && !freshLine)
			{
				// preserve the prev height
				h = lastHeight;
			}

			int rx = (int)r.x;
			int ry = (int)r.y;

			/*
			// only generate pixels for characters that changed
			if (_lastChars[i] != _chars[i].Character)
			{


				// update last character at that index
				_lastChars[i] = _chars[i].Character;
			}
			*/

			for (y = 0; y < h; y++)
			{
				for (x = 0; x < w; x++)
				{
					// if the prev char was larger
					// the smaller char now shouldn't generate pixels there
					if (y >= rh)
					{
						continue;
					}

					// set pixels for that char
					_texture.SetPixel(x + widthInterval, y + heightInterval, s.texture.GetPixel(x + rx, y + ry));
				}
			}

			// increment the width by the last width and spacing
			widthInterval += w + Spacing;
			// update last height
			lastHeight = h;
			// no longer a new line
			freshLine = false;
		}

		_texture.Apply();

		_img.sprite = Sprite.Create(_texture, texRect, Vector2.zero);
		if(Logging)
		{
			float endTime = Time.realtimeSinceStartup - startTime;
			Debug.Log($"Time to render font: {endTime}");
		}

	}

}
