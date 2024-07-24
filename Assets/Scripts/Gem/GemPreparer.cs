using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.ParticleSystem;

namespace Wozware.CrystalColumns
{
	public class GemPreparer : MonoBehaviour
	{
		// hidden reference
		[HideInInspector] public World World;

		#region Members
		// ********************************** //

		/// <summary>
		/// The player reference.
		/// </summary>
		public Player Player;

		/// <summary>
		/// The queue of the prepared gems.
		/// </summary>
		public Queue<MetaGem> prepareQueue = new Queue<MetaGem>();

		/// <summary>
		/// The UI layout parent.
		/// </summary>
		public Transform gemPrepareLayoutParent;

		/// <summary>
		/// The focused next gem parent.
		/// </summary>
		public Transform nextGemParent;

		/// <summary>
		/// The stored swap gem parent.
		/// </summary>
		public Transform swapGemParent;

		/// <summary>
		/// The prepared object prefab.
		/// </summary>
		public GameObject preparedObjectPrefab;

		/// <summary>
		/// The next object prefab (bigger than the prepared objects).
		/// </summary>
		public GameObject nextObjectPrefab;

		/// <summary>
		/// The last prepared gem.
		/// </summary>
		private string _lastGem = "";

		private string _swapGem = "";
		private string _currGem = "";
		

		// ********************************** //
		#endregion

		#region Initialization
		// ********************************** //

		public void GenerateNewGemQueue()
		{
			for (int i = 0; i < 12; i++)
			{
				MetaGem newGem = World.GetRandomNonRepeatingGem(_lastGem);
				prepareQueue.Enqueue(newGem);
				_lastGem = newGem.gemName;
			}
		}

		/// <summary>
		/// Prepares the queue with random non repeating gems and creates the appropriate UI objects.
		/// </summary>
		public void InitialPreparation()
		{
			bool first = false;
			foreach (MetaGem g in prepareQueue)
			{
				GameObject newGem;
				if (!first)
				{		
					if (nextGemParent.childCount > 0)
						Destroy(nextGemParent.GetChild(0).gameObject);

					newGem = Instantiate(nextObjectPrefab, nextGemParent);
					newGem.GetComponent<Image>().sprite = g.ui_sprite;
					Player.SetAimerGem(g);
					first = true;
					continue;
				}

				newGem = Instantiate(preparedObjectPrefab, gemPrepareLayoutParent);
				newGem.GetComponent<Image>().sprite = g.ui_sprite;
			}
		}

		// ********************************** //
		#endregion

		#region Standard Methods
		// ********************************** //

		public void ClearQueue()
		{
			prepareQueue.Clear();
			if (nextGemParent.childCount > 0)
				Destroy(nextGemParent.GetChild(0).gameObject);
			for(int i = 0; i < gemPrepareLayoutParent.childCount; i++)
			{
				Destroy(gemPrepareLayoutParent.GetChild(i).gameObject);
			}
		}

		public void ClearSwapGem()
		{
			if(swapGemParent.childCount > 0)
				Destroy(swapGemParent.GetChild(0).gameObject);
			_swapGem = "";
		}

		public void SwapGem()
		{
			if(nextGemParent.childCount > 0)
			{
				Transform gemToSwap = nextGemParent.GetChild(0);

				if (swapGemParent.childCount == 0)
				{
					gemToSwap.SetParent(swapGemParent);
					gemToSwap.localPosition = Vector3.zero;
					_swapGem = PopGemFromQueue().gemName;
					return;
				}

				Destroy(swapGemParent.GetChild(0).gameObject);

				Queue<MetaGem> newQueue = new Queue<MetaGem>();
				bool first = false;
				string firstGem = "";
				foreach (MetaGem g in prepareQueue)
				{
					if (!first)
					{
						newQueue.Enqueue(World.GetMetaGem(_swapGem));
						firstGem = g.gemName;
						first = true;
						continue;
					}
					newQueue.Enqueue(g);
				}

				gemToSwap = nextGemParent.GetChild(0);
				gemToSwap.SetParent(swapGemParent);
				gemToSwap.localPosition = Vector3.zero;
				_swapGem = firstGem;

				ClearQueue();
				prepareQueue = newQueue;
				InitialPreparation();
			}
		}

		/// <summary>
		/// Pops a gem from the prepared queue and updates the UI.
		/// </summary>
		/// <returns></returns>
		public MetaGem PopGemFromQueue(bool first = false)
		{
			// destroy the next gem object
			if (nextGemParent.childCount > 0)
				Destroy(nextGemParent.GetChild(0).gameObject);

			// get the old gem from queue
			MetaGem oldGem = prepareQueue.Dequeue();
			// get a new random gem 
			MetaGem newGem = World.GetRandomNonRepeatingGem(_lastGem);

			// get the bottom prepared gem object
			GameObject oldGemPrepared = gemPrepareLayoutParent.GetChild(0).gameObject;

			// create a new next object to represent the prepared one into the next parent
			GameObject nextGemObject = Instantiate(nextObjectPrefab, nextGemParent);
			nextGemObject.GetComponent<Image>().sprite = oldGemPrepared.GetComponent<Image>().sprite;
			Destroy(oldGemPrepared);
			Player.SetAimerGem(prepareQueue.Peek());

			// add a new prepare object to represent the newly generated gem at top of the prepare list
			GameObject prepareGemObject = Instantiate(preparedObjectPrefab, gemPrepareLayoutParent);
			prepareGemObject.transform.SetAsLastSibling();
			prepareGemObject.GetComponent<Image>().sprite = newGem.ui_sprite;

			// put the new gem in the queue
			_lastGem = newGem.gemName;
			prepareQueue.Enqueue(newGem);
			return oldGem;
		}

		// ********************************** //
		#endregion
	}
}

