using UnityEngine;
using FYFY;
using FYFY_plugins.PointerManager;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System;
using System.IO;
using System.Collections.Generic;
/// <summary>
/// Implement Drag&Drop interaction and dubleclick
/// </summary>
public class DragDropSystem : FSystem
{
	private Family draggableElement = FamilyManager.getFamily(new AllOfComponents(typeof(ElementToDrag)));
	private Family libraryElementPointed_f = FamilyManager.getFamily(new AllOfComponents(typeof(PointerOver), typeof(ElementToDrag), typeof(Image)));
	private Family containerPointed_f = FamilyManager.getFamily(new AllOfComponents(typeof(PointerOver), typeof(UITypeContainer)));
	private Family actionPointed_f = FamilyManager.getFamily(new AllOfComponents(typeof(PointerOver), typeof(UIActionType), typeof(Image)));
	private Family inputUIOver_f = FamilyManager.getFamily(new AllOfComponents(typeof(PointerOver)), new AnyOfComponents(typeof(TMP_InputField), typeof(TMP_Dropdown)));
	private Family editableScriptContainer_f = FamilyManager.getFamily(new AllOfComponents(typeof(UITypeContainer)), new AnyOfTags("ScriptConstructor"));
	private Family editableScriptPointed_f = FamilyManager.getFamily(new AllOfComponents(typeof(UITypeContainer), typeof(PointerOver)), new AnyOfTags("ScriptConstructor"));
	private Family libraryPanel = FamilyManager.getFamily(new AllOfComponents(typeof(GridLayoutGroup)));
	private Family editableScriptContainer = FamilyManager.getFamily(new AllOfComponents(typeof(UITypeContainer), typeof(VerticalLayoutGroup), typeof(CanvasRenderer), typeof(PointerSensitive)));
	private Family playerGO = FamilyManager.getFamily(new AllOfComponents(typeof(ScriptRef), typeof(Position)), new AnyOfTags("Player"));
	private GameObject mainCanvas;
	private GameObject itemDragged;
	private GameObject go;
	private GameObject positionBar;
	private GameObject editableContainer;
	private GameData gameData;
	private GameObject buttonStop;
	private GameObject executionCanvas;
	private GameObject lastEditedScript;
	private Family agents = FamilyManager.getFamily(new AllOfComponents(typeof(ScriptRef)));
	private TMP_Text text;
	private GameObject textDisplay;
	private GameObject con;
	private TMP_InputField input;



	//console
	private string consoleText;
	private string oldText = "";
	private int linenb;
	private int oldnb;
	private bool helped=false;
	private string written;
	private bool stackchanged = false;
	private bool infor = false;
	private bool inif = false;
	private List<GameObject> looplist = new List<GameObject>();
	private GameObject target;

	//double click
	private float lastClickTime;
	private float catchTime;

	private GameObject buttonPlay;

	public DragDropSystem()
	{
		if (Application.isPlaying)
		{
			executionCanvas = GameObject.Find("ExecutionCanvas");
			gameData = GameObject.Find("GameData").GetComponent<GameData>();
			buttonStop = executionCanvas.transform.Find("StopButton").gameObject;
			catchTime = 0.25f;
			mainCanvas = GameObject.Find("Canvas");
			editableContainer = editableScriptContainer_f.First();
			positionBar = editableContainer.transform.Find("PositionBar").gameObject;
			buttonPlay = GameObject.Find("ExecuteButton");
		}
	}

	// Use to process your families.
	protected override void onProcess(int familiesUpdateCount)
	{

		if (Input.GetKeyDown(KeyCode.Return))
		{
			if (helped == true)
            {
				input.text = written;
				helped = false;
			}
			helped = false;
			foreach (Transform t in mainCanvas.GetComponentInChildren<Transform>())
			{
				if (t.name == "EditableCanvas")
				{
					GameObject ec = t.gameObject;
					foreach (Transform v in ec.GetComponentInChildren<Transform>())
					{
						if (v.name == "Console")
						{
							con = v.gameObject;
							input = con.GetComponent<TMP_InputField>();
							text = input.textComponent;
						}
					}
				}
			}
			if (consoleText != oldText)
			{
				for (int i = 0; i < editableContainer.transform.childCount; i++)
				{
					if (editableContainer.transform.GetChild(i).GetComponent<BaseElement>())
					{
						destroyScript(editableContainer.transform.GetChild(i).gameObject, false);
					}

				}
				refreshUI();
				oldText = consoleText;
				using (var reader = new StringReader(consoleText))
				{
					for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
					{
						if (looplist.Count == 0)
						{
							target = editableContainer;
						}
						else
						{
							if (line.Length >= looplist.Count)
                            {
								int nbtab = 0;
								foreach(char c in line)
                                {
									if (c == '\t')
                                    {
										nbtab += 1;
                                    }
                                }
								if (nbtab == looplist.Count)
                                {
									target = looplist[nbtab];
                                }
                                else
                                {
									if (nbtab< looplist.Count)
                                    {
										int difftab = looplist.Count - nbtab;
										for (int f=0; f< difftab; f++)
                                        {
											GameObject looptoput;
											GameObject looptoclose = looplist[looplist.Count - 1];
											if (looplist.Count > 1)
                                            {
												 looptoput = looplist[looplist.Count - 2];
											}
                                            else
                                            {
												looptoput = editableContainer;
											}
											looptoclose.transform.SetParent(looptoput.transform);

										}
                                    }
                                }
                            }
			
			}


						line=line.Replace("\t", "");
						//clear instruction
						if (line.Length >= 5)
						{
							if (line.Substring(0, 5) == "clear")
							{
								input.text = "";
								helped = false;
							}
						}

						//help instructions
						if (line.Length >= 4)
						{
							if (line.Substring(0, 4) == "help")
							{

								written = "";
								string[] test = consoleText.Split('\n');
								for (int u=0; u < test.Length - 2; u++)
                                {
									written = written + test[u]+ '\n';
									Debug.Log(test[u]);
                                }

								helped = true;
								//general help
								if (line.Length == 4)
								{
									string te = "available fonctions :\nMoveForward \nTurnLeft\nTurnRight\nWait\nActivate\nclear\nhelp\nFor specific help, type\n\"help function_name\"\n";
									input.text = te;
								}

								//functions help
								//help MoveForward
								if (line.Length == 16)
								{
									if (line.Substring(0, 16) == "help MoveForward")
									{
										string te = "Moves the robot to the cell in front of him\nusage :\n \"MoveForward(n)\"\nwith n the number of cells you want to move forward\n";
										input.text = te;

									}
								}
								//help help
								if (line.Length == 9)
								{
									if (line.Substring(0, 9) == "help help")
									{
										string te = "Get the help for this console\nType \"help\" for general help or\n\"help function_name\" for help on a specific function\n";
										input.text = te;
									}
								}
								//help TurnLeft
								if (line.Length == 13)
								{
									if (line.Substring(0, 13) == "help TurnLeft")
									{
										string te = "Turn the robot at 90 degrees to his left\nusage :\n\"TurnLeft(n)\"\nwith n the number of times you want to turn\n";
										input.text = te;
									}
								}
								// help TurnRight
								if (line.Length == 14)
								{
									if (line.Substring(0, 14) == "help TurnRight")
									{
										string te = "Turn the robot at 90 degrees to his right\nusage :\n\"TurnRight(n)\"\nwith n the number of times you want to turn\n";
										input.text = te;
									}
								}
								//help Wait
								if (line.Length == 9)
                                {
									if (line.Substring(0,9)=="help Wait")
                                    {
										string te = "Makes the robot wait without moving for 1 period of time\nusage :\n\"Wait(n)\"\nwith n the number of periods you want to wait\n ";
										input.text = te;
                                    }
                                }
								//help Activate
								if (line.Length == 13)
                                {
									if (line.Substring(0,13)=="help Activate")
                                    {
										string te = "Activates a switch on the current cell\nusage :\n\"Activate\"\n";
										input.text = te;
                                    }
                                }



								input.text = input.text + "\nPress Enter to close help\n";
								input.MoveTextEnd(false);

								//end of help instructions
							}
						}

						//MoveForward instruction
						if (line.Length > 11)
						{
							if (line.Substring(0, 12) == "MoveForward(")
							{
								int nb = Int32.Parse(line.Substring(12, line.Length - 13));
								for (int k = 0; k < nb; k++)
								{
									GameObject go = draggableElement.getAt(0);
									GameObject prefab = go.GetComponent<ElementToDrag>().actionPrefab;
									GameObject item = UnityEngine.Object.Instantiate<GameObject>(prefab);
									BaseElement action = item.GetComponent<BaseElement>();
									item.transform.SetParent(target.transform);
									foreach (BaseElement actChild in item.GetComponentsInChildren<BaseElement>())
										GameObjectManager.addComponent<Dropped>(actChild.gameObject);

									action.target = item;
									GameObjectManager.bind(item);
									GameObjectManager.addComponent<Dragged>(item);

									if (item.GetComponent<UITypeContainer>())
										item.GetComponent<Image>().raycastTarget = true;
									target.transform.parent.parent.GetComponent<AudioSource>().Play();
									refreshUI();
								}
							}

						}

						//TurnLeft instruction
						if (line.Length > 8)
						{
							if (line.Substring(0, 9) == "TurnLeft(")
							{
								int nb = Int32.Parse(line.Substring(9, line.Length - 10));
								for (int k = 0; k < nb; k++)
								{
									GameObject go = draggableElement.getAt(1);
									GameObject prefab = go.GetComponent<ElementToDrag>().actionPrefab;
									GameObject item = UnityEngine.Object.Instantiate<GameObject>(prefab);
									item.transform.SetParent(target.transform);
									foreach (BaseElement actChild in item.GetComponentsInChildren<BaseElement>())
										GameObjectManager.addComponent<Dropped>(actChild.gameObject);

									BaseElement action = item.GetComponent<BaseElement>();
									action.target = item;
									GameObjectManager.bind(item);
									GameObjectManager.addComponent<Dragged>(item);
									if (item.GetComponent<UITypeContainer>())
										item.GetComponent<Image>().raycastTarget = true;
									target.transform.parent.parent.GetComponent<AudioSource>().Play();
									refreshUI();
								}
							}
						}

						//TurnRight instruction
						if (line.Length > 9)
						{
							if (line.Substring(0, 10) == "TurnRight(")
							{
								int nb = Int32.Parse(line.Substring(10, line.Length - 11));
								for (int k = 0; k < nb; k++)
								{
									GameObject go = draggableElement.getAt(2);
									GameObject prefab = go.GetComponent<ElementToDrag>().actionPrefab;
									GameObject item = UnityEngine.Object.Instantiate<GameObject>(prefab);
									item.transform.SetParent(target.transform);
									foreach (BaseElement actChild in item.GetComponentsInChildren<BaseElement>())
										GameObjectManager.addComponent<Dropped>(actChild.gameObject);

									BaseElement action = item.GetComponent<BaseElement>();
									action.target = item;
									GameObjectManager.bind(item);
									GameObjectManager.addComponent<Dragged>(item);

									if (item.GetComponent<UITypeContainer>())
										item.GetComponent<Image>().raycastTarget = true;
									target.transform.parent.parent.GetComponent<AudioSource>().Play();
									refreshUI();
								}
							}
						}

						//Activate instruction
						if (line.Length> 8)
                        {
							if (line.Substring(0, 9) == "Activate(")
							{
								GameObject go = draggableElement.getAt(4);
								GameObject prefab = go.GetComponent<ElementToDrag>().actionPrefab;
								GameObject item = UnityEngine.Object.Instantiate<GameObject>(prefab);

								item.transform.SetParent(target.transform);
								foreach (BaseElement actChild in item.GetComponentsInChildren<BaseElement>())
									GameObjectManager.addComponent<Dropped>(actChild.gameObject);

								BaseElement action = item.GetComponent<BaseElement>();
								action.target = item;
								GameObjectManager.bind(item);
								GameObjectManager.addComponent<Dragged>(item);

								if (item.GetComponent<UITypeContainer>())
									item.GetComponent<Image>().raycastTarget = true;
								target.transform.parent.parent.GetComponent<AudioSource>().Play();
								refreshUI();


							}
                        }

						//wait instruction
						if (line.Length > 4)
						{
							if (line.Substring(0, 5) == "Wait(")
							{
								int nb = Int32.Parse(line.Substring(5, line.Length - 6));
								for (int k = 0; k < nb; k++)
								{
									GameObject go = draggableElement.getAt(3);
									GameObject prefab = go.GetComponent<ElementToDrag>().actionPrefab;
									GameObject item = UnityEngine.Object.Instantiate<GameObject>(prefab);
									item.transform.SetParent(target.transform);
									foreach (BaseElement actChild in item.GetComponentsInChildren<BaseElement>())
										GameObjectManager.addComponent<Dropped>(actChild.gameObject);

									BaseElement action = item.GetComponent<BaseElement>();
									action.target = item;
									GameObjectManager.bind(item);
									GameObjectManager.addComponent<Dragged>(item);

									if (item.GetComponent<UITypeContainer>())
										item.GetComponent<Image>().raycastTarget = true;
									target.transform.parent.parent.GetComponent<AudioSource>().Play();
									refreshUI();
								}
							}
						}

						//for instruction
						if (line.Length > 18)
                        {
							if (line.Substring(0, 3)=="for")
                            {
								infor = true;
								string gofor = line.Trim();
								string fistnb="";
								string lastnb="";
								bool fgo = false;
								bool fgo2 = false;
								for (int k=0; k < gofor.Length; k++)
                                {
									if (fgo2 == true)
                                    {
										if (gofor.Substring(k, 1) == ")")
                                        {
											fgo2 = false;
                                        }
                                        else
                                        {
											lastnb += gofor.Substring(k, 1);
										}

									}
									if (fgo == true)
                                    {
										if (gofor.Substring(k, 1) == ",")
                                        {
											fgo = false;
											fgo2 = true;
                                        }
										else
											fistnb += gofor.Substring(k, 1);

									}
									if (gofor.Substring(k, 1) == "(")
                                    {
										fgo = true;
                                    }
                                }
								int diff = Int32.Parse(lastnb) - Int32.Parse(fistnb);
								GameObject go = draggableElement.getAt(7);
								GameObject prefab = go.GetComponent<ElementToDrag>().actionPrefab;
								GameObject item = UnityEngine.Object.Instantiate<GameObject>(prefab);
								BaseElement action = item.GetComponent<BaseElement>();
								
								foreach (BaseElement actChild in item.GetComponentsInChildren<BaseElement>())
									GameObjectManager.addComponent<Dropped>(actChild.gameObject);

								action.target = item;
								GameObjectManager.bind(item);
								GameObjectManager.addComponent<Dragged>(item);

								if (item.GetComponent<UITypeContainer>())
									item.GetComponent<Image>().raycastTarget = true;
								target.transform.parent.parent.GetComponent<AudioSource>().Play();
								refreshUI();
							}
							
                        }

						//Run instruction (deos not work)
						if (line.Length == 5)
						{
							if (line.Substring(0, 5) == "Run()")
							{

							}
						}
					}
				}
				MainLoop.instance.StartCoroutine(updatePlayButton());
			}
		}

		//Mouse down
		// On fait glisser une action selectionner tant que le bouton gauche de la souris est enfoncé
		if (Input.GetMouseButtonDown(0) && !Input.GetMouseButtonUp(0))
		{ //focus in play mode (unity editor) could be up and down !!! (bug unity)
		  //manage click on library
			if (libraryElementPointed_f.Count > 0)
			{
				go = libraryElementPointed_f.First();
				GameObject prefab = go.GetComponent<ElementToDrag>().actionPrefab;
				// Create a dragged GameObject
				itemDragged = UnityEngine.Object.Instantiate<GameObject>(prefab, go.transform);
				BaseElement action = itemDragged.GetComponent<BaseElement>();
				Debug.Log(action.GetType().ToString().Equals("ForAction"));
				if (action.GetType().ToString().Equals("ForAction"))
				{
					TMP_InputField input = itemDragged.GetComponentInChildren<TMP_InputField>();
					input.onEndEdit.AddListener(delegate { onlyPositiveInteger(input); });
				}
				itemDragged.GetComponent<UIActionType>().prefab = prefab;
				itemDragged.GetComponent<UIActionType>().linkedTo = go;
				action.target = itemDragged;
				GameObjectManager.bind(itemDragged);
				GameObjectManager.addComponent<Dragged>(itemDragged);
				// exclude this GameObject from the EventSystem
				itemDragged.GetComponent<Image>().raycastTarget = false;
				if (itemDragged.GetComponent<BasicAction>())
					foreach (Image child in itemDragged.GetComponentsInChildren<Image>())
						child.raycastTarget = false;
			}

			// drag in editable script
			if (actionPointed_f.Count > 0 && inputUIOver_f.Count == 0 && editableScriptPointed_f.Count > 0) // cannot drag if inputfield or dropdown pointed
			{
				itemDragged = actionPointed_f.getAt(actionPointed_f.Count - 1); // get the last one <=> deeper child PointerOver
																				// make this Action draggable
				GameObjectManager.setGameObjectParent(itemDragged, mainCanvas, true);
				stackchanged = true;
				itemDragged.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
				GameObjectManager.addComponent<Dragged>(itemDragged);
				// exclude this GameObject from the EventSystem
				itemDragged.GetComponent<Image>().raycastTarget = false;
				if (itemDragged.GetComponent<BasicAction>())
					foreach (Image child in itemDragged.GetComponentsInChildren<Image>())
						child.raycastTarget = false;
				// Restore action and subactions to inventory
				foreach (BaseElement actChild in itemDragged.GetComponentsInChildren<BaseElement>())
					GameObjectManager.addComponent<AddOne>(actChild.gameObject);
				editableContainer.transform.parent.GetComponentInParent<ScrollRect>().enabled = false;
			}

			MainLoop.instance.StartCoroutine(updatePlayButton());
		}

		//Find the deeper container pointed
		GameObject targetContainer = null;
		if (containerPointed_f.Count > 0)
			targetContainer = containerPointed_f.getAt(containerPointed_f.Count - 1);

		if (itemDragged != null)
		{
			itemDragged.transform.position = Input.mousePosition;

			//PositionBar positioning
			if (targetContainer)
			{
				// default put position Bar last
				positionBar.transform.SetParent(targetContainer.transform);
				positionBar.transform.SetSiblingIndex(targetContainer.transform.childCount + 1);
				if (actionPointed_f.Count > 0)
				{
					// get focused item and adjust position bar depending on mouse position
					GameObject focusedItemTarget = actionPointed_f.getAt(actionPointed_f.Count - 1);
					if (focusedItemTarget == targetContainer && Input.mousePosition.y > focusedItemTarget.transform.position.y - 30)
					{
						targetContainer = targetContainer.transform.parent.gameObject;
						positionBar.transform.SetParent(targetContainer.transform);
					}
					if ((focusedItemTarget.GetComponent<UITypeContainer>() == null && Input.mousePosition.y > focusedItemTarget.transform.position.y) ||
					 (focusedItemTarget.GetComponent<UITypeContainer>() != null && Input.mousePosition.y > focusedItemTarget.transform.position.y - 30))
					{
						positionBar.transform.SetSiblingIndex(focusedItemTarget.transform.GetSiblingIndex());
					}

					else if (focusedItemTarget.GetComponent<UITypeContainer>())
					{
						positionBar.transform.SetSiblingIndex(focusedItemTarget.transform.GetSiblingIndex() + focusedItemTarget.transform.childCount);
					}
					else
					{
						positionBar.transform.SetSiblingIndex(focusedItemTarget.transform.GetSiblingIndex() + 1);
					}
				}
			}
		}
		else
		{
			positionBar.transform.SetParent(editableContainer.transform);
			positionBar.transform.SetSiblingIndex(editableContainer.transform.childCount + 1);
		}


		// Delete with right click
		if (itemDragged == null && Input.GetMouseButtonUp(1) && actionPointed_f.Count > 0)
		{
			GameObjectManager.addComponent<ResetBlocLimit>(actionPointed_f.getAt(actionPointed_f.Count - 1));
			MainLoop.instance.StartCoroutine(updatePlayButton());
			stackchanged = true;
			updateConsole();
		}

		// Mouse Up
		if (Input.GetMouseButtonUp(0))
		{
			bool doubleclick = false;
			//check double click
			if (Time.time - lastClickTime < catchTime)
				doubleclick = true;

			//Drop in script
			if (itemDragged != null && (targetContainer != null || doubleclick))
			{
				if (doubleclick)
				{

					itemDragged.transform.SetParent(editableContainer.transform);
					stackchanged = true;
				}
				else
				{
					itemDragged.transform.SetParent(targetContainer.transform);
					stackchanged = true;
				}
				itemDragged.transform.SetSiblingIndex(positionBar.transform.GetSiblingIndex());
				itemDragged.transform.localScale = new Vector3(1, 1, 1);
				itemDragged.GetComponent<Image>().raycastTarget = true;
				if (itemDragged.GetComponent<BasicAction>())
					foreach (Image child in itemDragged.GetComponentsInChildren<Image>())
						child.raycastTarget = true;

				// update limit bloc
				foreach (BaseElement actChild in itemDragged.GetComponentsInChildren<BaseElement>())
					GameObjectManager.addComponent<Dropped>(actChild.gameObject);

				GameObjectManager.removeComponent<Dragged>(itemDragged);

				if (itemDragged.GetComponent<UITypeContainer>())
					itemDragged.GetComponent<Image>().raycastTarget = true;
				editableContainer.transform.parent.parent.GetComponent<AudioSource>().Play();
				refreshUI();
			}
			// priority == null, means drop item outside editablePanel
			else if (!doubleclick && itemDragged != null)
			{
				// remove item and all its children
				for (int i = 0; i < itemDragged.transform.childCount; i++)
					UnityEngine.Object.Destroy(itemDragged.transform.GetChild(i).gameObject);
				itemDragged.transform.DetachChildren();
				GameObjectManager.unbind(itemDragged);
				UnityEngine.Object.Destroy(itemDragged);
				stackchanged = true;
			}
			itemDragged = null;
			editableContainer.transform.parent.parent.GetComponent<ScrollRect>().enabled = true;

			lastClickTime = Time.time;
			MainLoop.instance.StartCoroutine(updatePlayButton());
		}
		if (stackchanged == true)
        {
			updateConsole();
			stackchanged = false;
        }

	}

	private IEnumerator updatePlayButton()
	{
		yield return null;
		buttonPlay.GetComponent<Button>().interactable = !(editableScriptContainer_f.First().transform.childCount < 2);
	}

	public void onlyPositiveInteger(TMP_InputField input)
	{
		int res;
		bool success = Int32.TryParse(input.text, out res);
		if (!success || (success && Int32.Parse(input.text) < 0))
		{
			input.text = "0";
		}
	}

	//Refresh Containers size
	private void refreshUI()
	{
		foreach (GameObject go in editableScriptContainer_f)
			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)go.transform);
	}


	public void readConsole(string s)
	{
		consoleText = s;
		//Debug.Log(consoleText);
	}


	private void destroyScript(GameObject go, bool refund = false)
	{
		if (go.GetComponent<UIActionType>() != null)
		{
			if (!refund)
				gameData.totalActionBloc++;
			else
				GameObjectManager.addComponent<AddOne>(go.GetComponent<UIActionType>().linkedTo);
		}

		if (go.GetComponent<UITypeContainer>() != null)
		{
			foreach (Transform child in go.transform)
			{
				if (child.GetComponent<BaseElement>())
				{
					destroyScript(child.gameObject, refund);
				}
			}
		}
		go.transform.DetachChildren();
		GameObjectManager.unbind(go);
		UnityEngine.Object.Destroy(go);
	}

	public void updateConsole()
    {
		foreach (Transform t in mainCanvas.GetComponentInChildren<Transform>())
		{
			if (t.name == "EditableCanvas")
			{
				GameObject ec = t.gameObject;
				foreach (Transform v in ec.GetComponentInChildren<Transform>())
				{
					if (v.name == "Console")
					{
						con = v.gameObject;
						input = con.GetComponent<TMP_InputField>();
						text = input.textComponent;
					}
				}
			}
		}
		input.text = "";
		string stack = "";
		int nbrecur = 0;
		for (int i = 0; i < editableContainer.transform.childCount; i++)
		{
			if (stack == editableContainer.transform.GetChild(i).name)
            {
				nbrecur += 1;
            }
            else
            {
				if (stack == "ForwardActionBloc(Clone)")
				{
					input.text = input.text + "MoveForward("+ nbrecur +")\n";
					input.MoveTextEnd(false);
				}
				if (stack == "TurnLeftActionBloc(Clone)")
				{
					input.text = input.text + "TurnLeft(" + nbrecur + ")\n";
					input.MoveTextEnd(false);
				}
				if (stack == "TurnRightActionBloc(Clone)")
				{
					input.text = input.text + "TurnRight(" + nbrecur + ")\n";
					input.MoveTextEnd(false);
				}
				if (stack == "WaitActionBloc(Clone)")
				{
					input.text = input.text + "Wait(" + nbrecur + ")\n";
					input.MoveTextEnd(false);
				}
				if (stack == "ActivateActionBloc(Clone)")
				{
					for (int j = 0; j < nbrecur; j++)
					{
						input.text = input.text + "Activate()\n";
						input.MoveTextEnd(false);
					}
				}
				nbrecur = 1;
				stack = editableContainer.transform.GetChild(i).name;
			}

		}
	}

	public void consolemanager()
	{
		if (Input.GetKey(KeyCode.KeypadEnter))
		{
			Debug.Log(consoleText);
		}


	}

}