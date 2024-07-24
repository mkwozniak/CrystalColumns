using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CustomFont", menuName = "ScriptableObjects/CustomFont", order = 3)]
public class CustomFont : ScriptableObject
{
	public List<CustomFontCharacter> CharacterList;

	public Dictionary<string, CustomFontCharacter> Characters = new Dictionary<string, CustomFontCharacter>();

	public void Initialize()
	{
		for (int i = 0; i < CharacterList.Count; i++)
		{
			CustomFontCharacter c = CharacterList[i];
			Characters.Add(CharacterList[i].Character, CharacterList[i]);
		}
	}
}