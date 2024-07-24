using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextSwitcher : MonoBehaviour
{
    public CustomFontRenderer Text;
    public float TextTime;
	public List<string> TextList;

	private float _textTimer;
    private int _currTextIndex;

	private void Start()
	{
		_currTextIndex = 0;
		Text.SetText(TextList[_currTextIndex]);
	}

	private void Update()
    {
		_textTimer += Time.deltaTime;
		if(_textTimer > TextTime)
		{
			_currTextIndex += 1;
			if(_currTextIndex >= TextList.Count)
			{
				_currTextIndex = 0;
			}

			_textTimer = 0f;
			Text.SetText(TextList[_currTextIndex]);
		}
    }
}
