using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class ZenerCardsScript : MonoBehaviour {

	public KMBombModule _module;
	public KMBombInfo _bomb;
	public KMAudio _audio;

	public KMSelectable[] _cardButtons;
	public KMSelectable _moduleSelectable;

	public Sprite[] _cards;
	public SpriteRenderer[] _cardRenderers;
	public SpriteRenderer[] _cardSelectRenderers;

	// Specifically for Logging
	static int _modIDCount = 1;
	int _modID;
	private bool _modSolved;

	int _chosenCard;
	int _waitTillFlip;
	int _chooseTime;
	int _startingTime;

	bool _choosing = false;
	bool _hasShownCard = false;


	void Awake() {
		_modID = _modIDCount++;
		_startingTime = (int)_bomb.GetTime();
		int tries = 0;
        _waitTillFlip = rnd.Range(180, _startingTime > 300 ? 300 : _startingTime);
        while ((_startingTime - _waitTillFlip) < 180)
        {
            if (tries == 10) { _waitTillFlip = 30; break; }
            _waitTillFlip = rnd.Range(180, _startingTime > 300 ? 300 : _startingTime);
            tries++;
        }
        foreach (KMSelectable km in _cardButtons) 
		{
			km.OnInteract = delegate () { if (_modSolved) return false; PressButton(km); return false; };
		}
	}	

	void Start() {
		Debug.LogFormat("[Zener Cards #{0}]: The card has decided to flip at {1}:{2}.", _modID, (_startingTime - _waitTillFlip) / 60, 
			((_startingTime - _waitTillFlip) % 60).ToString().Length == 1 ? "0" + ((_startingTime - _waitTillFlip) % 60).ToString() : ((_startingTime - _waitTillFlip) % 60).ToString());
		
		_chosenCard = rnd.Range(0, _cards.Length);
		Debug.LogFormat("[Zener Cards #{0}]: The card that has been chosen to be seen is {1}.", _modID, _cards[_chosenCard].name);
		_cardRenderers[0].sprite = _cards[_chosenCard];
		StartCoroutine(FlipCard(_waitTillFlip));
	}

	void FixedUpdate() 
	{
        if (_modSolved) return;
        if ((int)_bomb.GetTime() <= _chooseTime && !_choosing && _hasShownCard)
        {
            _choosing = true;
            StartCoroutine(ChooseCard());
        }
    }

	void PressButton(KMSelectable km) 
	{
		int index = Array.IndexOf(_cardButtons, km);
		if (index != _chosenCard)
		{
			_module.HandleStrike();
			Debug.LogFormat("[Zener Cards #{0}]: Incorrect card chosen. Expected {1} but was given {2}.", _modID, _cards[_chosenCard].name, _cards[index].name);
			return;
		}
		else
		{
			Debug.LogFormat("[Zener Cards #{0}]: Correct card chosen. Mod Solved.", _modID);
			_module.HandlePass();
			_modSolved = true;
			return;
		}
	}

	IEnumerator FlipCard(float initialDelay) 
	{
		yield return new WaitForSeconds(initialDelay);
		_cardRenderers[0].enabled = true;
		_cardRenderers[1].enabled = false;
		yield return new WaitForSeconds(20.0f);
		_cardRenderers[0].enabled = false;
		_cardRenderers[1].enabled = true;
		_hasShownCard = true;
		int currentTime = (int)_bomb.GetTime();
        _chooseTime = rnd.Range(180, currentTime > 300 ? 300 : currentTime);
        while (currentTime - _chooseTime < 120)
        {
            if (_chooseTime == 0) break;
            _chooseTime--;
        }
        Debug.LogFormat("[Zener Cards #{0}]: The chosen time to be able to choose a card will be at {1}:{2}.", _modID, (currentTime - _chooseTime) / 60, (currentTime - _chooseTime) % 60);
		_chooseTime = currentTime - _chooseTime;
		yield break;
	}

	IEnumerator ChooseCard() 
	{
		_cardRenderers[0].enabled = false;
		_cardRenderers[1].enabled = false;
		foreach (SpriteRenderer sr in _cardSelectRenderers) 
		{
			sr.enabled = true;
			yield return new WaitForSeconds(0.4f);
		}
		_moduleSelectable.Children = new KMSelectable[5];
		for (int i = 0; i < 5; i++) 
		{
			_moduleSelectable.Children[i] = _cardButtons[i];
		}
		_moduleSelectable.UpdateChildren();
		yield break;
	}


#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} <symbol> [Presses the named card, possible shapes are 'Star', 'Circle', 'Cross', 'Waves' and 'Square']";
#pragma warning restore 414

	IEnumerator ProcessTwitchCommand(string command) 
	{
		string[] args = command.ToLower().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
		if (!_choosing) 
		{
			yield return "sendtochaterror The module has not wanted you to choose a card yet. Try again when it wants an answer.";
			yield break;
		}
		if (args.Length > 1) 
		{
			yield return "sendtochaterror Incorrect command with that many arguments, please try again.";
			yield break;
		}
		string[] possibleShapes = new string[] { "star", "circle", "cross", "waves", "square" };
		if (!possibleShapes.Any(x => x == args[0])) 
		{
			yield return "sendtochaterror Incorrect shape name. Possible shapes are " + possibleShapes.Join(", ") + ". Please try again.";
			yield break;
		}
		_cardButtons[Array.IndexOf(possibleShapes, args[0])].OnInteract();
		yield return "solve";
		yield break;
	}

}
