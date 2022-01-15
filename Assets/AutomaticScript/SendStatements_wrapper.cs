using UnityEngine;
using FYFY;

[ExecuteInEditMode]
public class SendStatements_wrapper : MonoBehaviour
{
	private void Start()
	{
		this.hideFlags = HideFlags.HideInInspector; // Hide this component in Inspector
	}

	public void initGBLXAPI()
	{
		MainLoop.callAppropriateSystemMethod ("SendStatements", "initGBLXAPI", null);
	}

	public void testSendStatement()
	{
		MainLoop.callAppropriateSystemMethod ("SendStatements", "testSendStatement", null);
	}

	public void playLevelActivated()
	{
		MainLoop.callAppropriateSystemMethod ("SendStatements", "playLevelActivated", null);
	}

	public void resetLevelActiveted()
	{
		MainLoop.callAppropriateSystemMethod ("SendStatements", "resetLevelActiveted", null);
	}

	public void endLevel()
	{
		MainLoop.callAppropriateSystemMethod ("SendStatements", "endLevel", null);
	}

	public void newCompetenceValide()
	{
		MainLoop.callAppropriateSystemMethod ("SendStatements", "newCompetenceValide", null);
	}

	public void paraLevelProcessCreation()
	{
		MainLoop.callAppropriateSystemMethod ("SendStatements", "paraLevelProcessCreation", null);
	}

}
