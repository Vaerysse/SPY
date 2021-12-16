using UnityEngine;
using FYFY;

[ExecuteInEditMode]
public class DragDropSystem_wrapper : MonoBehaviour
{
	private void Start()
	{
		this.hideFlags = HideFlags.HideInInspector; // Hide this component in Inspector
	}

	public void onlyPositiveInteger(TMPro.TMP_InputField input)
	{
		MainLoop.callAppropriateSystemMethod ("DragDropSystem", "onlyPositiveInteger", input);
	}

	public void readConsole(System.String s)
	{
		MainLoop.callAppropriateSystemMethod ("DragDropSystem", "readConsole", s);
	}

	public void updateConsole()
	{
		MainLoop.callAppropriateSystemMethod ("DragDropSystem", "updateConsole", null);
	}

	public void consolemanager()
	{
		MainLoop.callAppropriateSystemMethod ("DragDropSystem", "consolemanager", null);
	}

}
