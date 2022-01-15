using UnityEngine;
using FYFY;

[ExecuteInEditMode]
public class LevelGenerator_wrapper : MonoBehaviour
{
	private void Start()
	{
		this.hideFlags = HideFlags.HideInInspector; // Hide this component in Inspector
	}

	public void XmlToLevel(System.String fileName)
	{
		MainLoop.callAppropriateSystemMethod ("LevelGenerator", "XmlToLevel", fileName);
	}

	public void computeNext(UnityEngine.GameObject container)
	{
		MainLoop.callAppropriateSystemMethod ("LevelGenerator", "computeNext", container);
	}

	public void choiceParameterLevel()
	{
		MainLoop.callAppropriateSystemMethod ("LevelGenerator", "choiceParameterLevel", null);
	}

	public void CreateLvlAuto()
	{
		MainLoop.callAppropriateSystemMethod ("LevelGenerator", "CreateLvlAuto", null);
	}

}
