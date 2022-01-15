using UnityEngine;
using FYFY;

[ExecuteInEditMode]
public class UserModelSystem_wrapper : MonoBehaviour
{
	private void Start()
	{
		this.hideFlags = HideFlags.HideInInspector; // Hide this component in Inspector
	}

	public void initModelLearner()
	{
		MainLoop.callAppropriateSystemMethod ("UserModelSystem", "initModelLearner", null);
	}

	public void playLevelActivated()
	{
		MainLoop.callAppropriateSystemMethod ("UserModelSystem", "playLevelActivated", null);
	}

}