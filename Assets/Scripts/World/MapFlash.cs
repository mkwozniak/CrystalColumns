using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapFlash : MonoBehaviour
{
    public Tilemap tileMap;
    public float FlashSpeed;

    public float MaxAlpha;
    public float MinAlpha;

    [SerializeField] private float _currAlpha;

    private Color _currColor;

	[SerializeField] private int direction = 0;

    // Start is called before the first frame update
    void Start()
    {
		_currColor = tileMap.color;
	}

    // Update is called once per frame
    void Update()
    {
        if(direction == 0)
        {
			if (_currAlpha < MaxAlpha)
			{
				_currAlpha += Time.deltaTime * FlashSpeed;
			}
            if(_currAlpha >= MaxAlpha)
            {
                direction = 1;
            }
		}

		if (direction == 1)
		{
			if (_currAlpha > MinAlpha)
			{
				_currAlpha -= Time.deltaTime * FlashSpeed;
			}
			if (_currAlpha <= MinAlpha)
			{
				direction = 0;
			}
		}

        _currColor.a = _currAlpha;
        tileMap.color = _currColor;
    }
}
