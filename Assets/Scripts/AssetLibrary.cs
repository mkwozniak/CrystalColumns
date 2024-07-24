using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wozware.CrystalColumns
{
    [CreateAssetMenu(fileName = "AssetLibrary", menuName = "ScriptableObjects/AssetLibrary", order = 3)]
    public class AssetLibrary : ScriptableObject
    {
        /// <summary>
        /// The prefab to spawn when creating a 2d temporary sound effect.
        /// </summary>
        public GameObject SoundEffectPrefab;

        /// <summary>
        /// The list of all blitz levels. Set in Unity Inspector.
        /// </summary>
        [SerializeField] private List<BlitzLevelData> blitzLevelList;

        /// <summary>
        /// The list of all music. Set in Unity Inspector.
        /// </summary>
        [SerializeField] private List<SoundContainer> musicList;

        /// <summary>
        /// The list of sound fx. Set in Unity Inspector.
        /// </summary>
        [SerializeField] private List<SoundContainer> sfxList;

        /// <summary>
        /// List of possible gems. Set in Unity Inspector.
        /// </summary>
        public List<MetaGem> PossibleGems = new List<MetaGem>();

        /// <summary>
        /// The list of gem fx. Set in Unity Inspector.
        /// </summary>
        [SerializeField] private List<GemFX> gemFXList;

        /// <summary>
        /// The externally available music dictionary.
        /// </summary>
        public Dictionary<string, AudioClip> Music = new Dictionary<string, AudioClip>();

        /// <summary>
        /// The externally available sfx dictionary.
        /// </summary>
        public Dictionary<string, AudioClip> SFX = new Dictionary<string, AudioClip>();

        /// <summary>
        /// The externally available gem fx dictionary.
        /// </summary>
        public Dictionary<string, GemFX> GemFX = new Dictionary<string, GemFX>();

        /// <summary>
        /// The externally available gem fx dictionary.
        /// </summary>
        public Dictionary<string, BlitzLevelData> BlitzLevels = new Dictionary<string, BlitzLevelData>();

        /// <summary>
        /// Loads the asset lists set from the unity inspector into their respective dictionaries.
        /// </summary>
        public void LoadAssetsToDictionaries()
		{
            foreach (SoundContainer s in musicList)
                Music[s.name] = s.clip;

            foreach (SoundContainer s in sfxList)
                SFX[s.name] = s.clip;

            foreach (GemFX fx in gemFXList)
                GemFX[fx.gemName] = fx;

            foreach (BlitzLevelData lvl in blitzLevelList)
                BlitzLevels[lvl.name] = lvl;
        }

    }
}