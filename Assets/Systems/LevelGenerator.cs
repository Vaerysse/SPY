using UnityEngine;
using FYFY;
using System.Collections.Generic;
using System.Xml;
using UnityEngine.UI;
using TMPro;
using FYFY_plugins.PointerManager;
using System.IO;

/// <summary>
/// Read XML file and load level
/// </summary>
public class LevelGenerator : FSystem {

	private Family levelGO = FamilyManager.getFamily(new AnyOfComponents(typeof(Position), typeof(CurrentAction)));
	private Family enemyScript = FamilyManager.getFamily(new AllOfComponents(typeof(HorizontalLayoutGroup), typeof(CanvasRenderer)), new NoneOfComponents(typeof(Image)));
	private Family editableScriptContainer = FamilyManager.getFamily(new AllOfComponents(typeof(UITypeContainer), typeof(VerticalLayoutGroup), typeof(CanvasRenderer), typeof(PointerSensitive)));
	private Family infoLevelGen_f = FamilyManager.getFamily(new AnyOfComponents(typeof(infoLevelGenerator))); // Charge la famille d'information sur le niveau créer procedurallement
	private Family modelLearner_f = FamilyManager.getFamily(new AnyOfComponents(typeof(UserModel)));
	private List<List<int>> map;
	private GameData gameData;
	private GameObject scriptContainer;
	private GameObject model; // modélisation de l'utilisateur
	GameObject infoLevelGen; // info sur la création procedural du level 

	//List de dictionaire qui continient le niveau généré procéduralement. Chaque Dictionaire est un noeud qui contient une liste de int (cordonées de la case + nom noeud parent) et a comme clef son nom
	private List<Case> pathLevel = new List<Case>();
	//On doit aussi noter les murs pour la création des murs
	private List<Case> wallLevel = new List<Case>();
	//On doit aussi noter les murs pour la création des murs
	private List<Case> gateAndTermLevel = new List<Case>();
	// Nombre de corridor créer
	int nbCorridor = 0;
	// Nombre de case seul créer
	int nbCaseLonely = 0;
	// Nombre de room créer
	int nbRoom = 0;
	// Nombre de porte créer
	int nbGate = 0;
	// Difficulté du niveau
	int hardlevel = 0;
	// direction du premier bloc
	int direct = 1;
	// Dico des action et du nombre à créer ensuite
	public Dictionary<string, int> actionCreation = new Dictionary<string, int>();


	//Classe Case 
	//Elle permet d'avoir toutes les informations d'une case lors de la création de niveau procédural
	//Fonctionne comme un systéme de noeud
	//Chaque case à un parent qui est la case créer juste avant elle à lequelle elle est rataché
	//Un seul parent mais plusieurs enfants possible
	//Le parent est à zéro pour tous objet qui n'est pas une case ou la case start
	private class Case
    {
		string name; //Nom de la case, par défaut son numéro hiérarchique mais peux changer de nom si besoin (comme par exemple la case de départ appeller start)
		int nbStruct; // A quel structure la case appartient
		int pathPosition; //Le numéro hiérarchique dans le chemin le plus cours
		List<int> pos; //Coordonnée X et Y de la case

		public Case(string n, int nb, int p, List<int> coord)
        {
			this.name = n;
			this.nbStruct = nb;
			this.pathPosition = p;
			this.pos = coord;
		}

		//vérifi si lees ccoordonée sont les même on non
		//retourn False si ce n'est pas le cas
		public bool sameCoord(List<int> coord)
        {
			if (coord[0] == this.pos[0] && coord[1] == this.pos[1])
            {
				return true;
            }
            else{
				return false;
			}
        }

		//renvoie le nom de la case (nom de la struc à l'aquel elle appartient)
		public string getName()
        {
			return this.name;
        }

		//renvoie les coordonnées de la case
		public List<int> getCoord()
        {
			return this.pos;
        }

		//renvoie a quel numéro de struct elle apartientt
		public int getnbStruct()
        {
			return this.nbStruct;
        }

		//renvoi le numéro de position dans le cghemin le plus court
		public int getPathPosition()
        {
			return this.pathPosition;
        }

		// Permet de changer la position de la case dans le chemin opti
		public void setPathPosition(int nb)
        {
			this.pathPosition = nb;

		}

		// Permet de modifier le nom de la case
		public void setName(string newName)
        {
			this.name = newName;
        }
	}


	public LevelGenerator()
	{
		if (Application.isPlaying)
		{
			GameObject gameDataGO = GameObject.Find("GameData");
			if (gameDataGO == null)
				GameObjectManager.loadScene("TitleScreen");

			gameData = gameDataGO.GetComponent<GameData>();
			gameData.Level = GameObject.Find("Level");
			scriptContainer = enemyScript.First();
			infoLevelGen = infoLevelGen_f.First();
			Debug.Log("nb model : " + modelLearner_f.Count);
			model = modelLearner_f.First();

			//Création du dictionaire pour la création procédurale des actions
			actionCreation.Add("Forward", 0);
			actionCreation.Add("TurnLeft", 0);
			actionCreation.Add("TurnRight", 0);
			actionCreation.Add("Wait", 0);
			actionCreation.Add("Activate", 0);
			actionCreation.Add("TurnBack", 0);
			actionCreation.Add("If", 0);
			actionCreation.Add("For", 0);


			if (gameData.levelToLoad.Item1 != "generique")
			{
				XmlToLevel(gameData.levelList[gameData.levelToLoad.Item1][gameData.levelToLoad.Item2]);
				GameObject.Find("LevelName").GetComponent<TMP_Text>().text = Path.GetFileNameWithoutExtension(gameData.levelList[gameData.levelToLoad.Item1][gameData.levelToLoad.Item2]);
			}
			else
			{
				CreateLvlAuto();
			}
		}
	}

	private void generateMap(){
		for(int i = 0; i< map.Count; i++){
			for(int j = 0; j < map[i].Count; j++){
				switch (map[i][j]){
					case 0: // Path
						createCell(i,j);
						break;
					case 1: // Wall
						createCell(i,j);
						createWall(i,j);
						break;
					case 2: // Spawn
						createCell(i,j);
						createSpawnExit(i,j,true);
						break;
					case 3: // Exit
						createCell(i,j);
						createSpawnExit(i,j,false);
						break;
				}
			}
		}
	}

	private GameObject createEntity(int i, int j, Direction.Dir direction, string type, List<GameObject> script = null){
		GameObject entity = null;
		Sprite agentSpriteIcon = null;
		switch(type){
			case "player": // Robot
				entity = Object.Instantiate<GameObject>(Resources.Load ("Prefabs/Robot Kyle") as GameObject, gameData.Level.transform.position + new Vector3(i*3,1.5f,j*3), Quaternion.Euler(0,0,0), gameData.Level.transform);
				agentSpriteIcon =  Resources.Load("UI Images/robotIcon", typeof(Sprite)) as Sprite;
				break;
			case "enemy": // Enemy
				entity = Object.Instantiate<GameObject>(Resources.Load ("Prefabs/Drone") as GameObject, gameData.Level.transform.position + new Vector3(i*3,5f,j*3), Quaternion.Euler(0,0,0), gameData.Level.transform);
				agentSpriteIcon =  Resources.Load("UI Images/droneIcon", typeof(Sprite)) as Sprite;
				break;
		}
		entity.GetComponent<Position>().x = i;
		entity.GetComponent<Position>().z = j;
		entity.GetComponent<Direction>().direction = direction;
		
		//add new container to entity
		ScriptRef scriptref = entity.GetComponent<ScriptRef>();
		GameObject containerParent = Object.Instantiate<GameObject>(Resources.Load ("Prefabs/Container") as GameObject);
		scriptref.uiContainer = containerParent;
		scriptref.scriptContainer = containerParent.transform.Find("Container").Find("Viewport").Find("ScriptContainer").gameObject;
		containerParent.transform.SetParent(scriptContainer.gameObject.transform);
		containerParent.transform.Find("Header").Find("agent").GetComponent<Image>().sprite = agentSpriteIcon;

		AgentColor ac = MainLoop.instance.GetComponent<AgentColor>();
		scriptref.uiContainer.transform.Find("Container").GetComponent<Image>().color = (type == "player" ? ac.playerBackground : ac.droneBackground);

		if(script != null){
			if (type == "player" && editableScriptContainer.First().transform.childCount == 1){ //player & empty script (1 child for position bar)
				GameObject editableCanvas = editableScriptContainer.First();
				for(int k = 0 ; k < script.Count ; k++){
					script[k].transform.SetParent(editableCanvas.transform); //add actions to editable container
					GameObjectManager.bind(script[k]);
					GameObjectManager.refresh(editableCanvas);
				}
				foreach(BaseElement act in editableCanvas.GetComponentsInChildren<BaseElement>()){
					GameObjectManager.addComponent<Dropped>(act.gameObject);
				}
				LayoutRebuilder.ForceRebuildLayoutImmediate(editableCanvas.GetComponent<RectTransform>());
			}

			else if(type == "enemy"){
				foreach(GameObject go in script){
					go.transform.SetParent(scriptref.scriptContainer.transform); //add actions to container
					List<GameObject> basicActionGO = getBasicActionGO(go);
					if(basicActionGO.Count != 0)
						foreach(GameObject baGO in basicActionGO)
							baGO.GetComponent<Image>().color = MainLoop.instance.GetComponent<AgentColor>().droneAction;
				}
				computeNext(scriptref.scriptContainer);				
			}			

		}
		GameObjectManager.bind(containerParent);
		GameObjectManager.bind(entity);
		return entity;
	}

	private List<GameObject> getBasicActionGO(GameObject go){
		List<GameObject> res = new List<GameObject>();
		if(go.GetComponent<BasicAction>())
			res.Add(go);
		foreach(Transform child in go.transform){
			if(child.GetComponent<BasicAction>())
				res.Add(child.gameObject);
			else if(child.GetComponent<UITypeContainer>() && child.GetComponent<BaseElement>()){
				List<GameObject> childGO = getBasicActionGO(child.gameObject); 
				foreach(GameObject cgo in childGO){
					res.Add(cgo);
				}
			}		
		}
		return res;
	}

	private void createDoor(int i, int j, Direction.Dir orientation, int slotID){
		GameObject door = Object.Instantiate<GameObject>(Resources.Load ("Prefabs/Door") as GameObject, gameData.Level.transform.position + new Vector3(i*3,3,j*3), Quaternion.Euler(0,0,0), gameData.Level.transform);

		door.GetComponent<ActivationSlot>().slotID = slotID;
		door.GetComponent<Position>().x = i;
		door.GetComponent<Position>().z = j;
		door.GetComponent<Direction>().direction = orientation;
		GameObjectManager.bind(door);
	}

	private void createActivable(int i, int j, List<int> slotIDs, Direction.Dir orientation)
	{
		GameObject activable = Object.Instantiate<GameObject>(Resources.Load("Prefabs/ActivableConsole") as GameObject, gameData.Level.transform.position + new Vector3(i * 3, 3, j * 3), Quaternion.Euler(0, 0, 0), gameData.Level.transform);

		activable.GetComponent<Activable>().slotID = slotIDs;
		activable.GetComponent<Position>().x = i;
		activable.GetComponent<Position>().z = j;
		activable.GetComponent<Direction>().direction = orientation;
		GameObjectManager.bind(activable);
	}

	private void createSpawnExit(int i, int j, bool type){
		GameObject spawnExit;
		if(type)
			spawnExit = Object.Instantiate<GameObject>(Resources.Load ("Prefabs/TeleporterSpawn") as GameObject, gameData.Level.transform.position + new Vector3(i*3,1.5f,j*3), Quaternion.Euler(-90,0,0), gameData.Level.transform);
		else
			spawnExit = Object.Instantiate<GameObject>(Resources.Load ("Prefabs/TeleporterExit") as GameObject, gameData.Level.transform.position + new Vector3(i*3,1.5f,j*3), Quaternion.Euler(-90,0,0), gameData.Level.transform);

		spawnExit.GetComponent<Position>().x = i;
		spawnExit.GetComponent<Position>().z = j;
		GameObjectManager.bind(spawnExit);
	}

	private void createCoin(int i, int j){
		GameObject coin = Object.Instantiate<GameObject>(Resources.Load ("Prefabs/Coin") as GameObject, gameData.Level.transform.position + new Vector3(i*3,3,j*3), Quaternion.Euler(90,0,0), gameData.Level.transform);
		coin.GetComponent<Position>().x = i;
		coin.GetComponent<Position>().z = j;
		GameObjectManager.bind(coin);
	}

	private void createCell(int i, int j){
		GameObject cell = Object.Instantiate<GameObject>(Resources.Load ("Prefabs/Cell") as GameObject, gameData.Level.transform.position + new Vector3(i*3,0,j*3), Quaternion.Euler(0,0,0), gameData.Level.transform);
		GameObjectManager.bind(cell);
	}

	private void createWall(int i, int j){
		GameObject wall = Object.Instantiate<GameObject>(Resources.Load ("Prefabs/Wall") as GameObject, gameData.Level.transform.position + new Vector3(i*3,3,j*3), Quaternion.Euler(0,0,0), gameData.Level.transform);
		wall.GetComponent<Position>().x = i;
		wall.GetComponent<Position>().z = j;
		GameObjectManager.bind(wall);
	}

	private void eraseMap(){
		foreach( GameObject go in levelGO){
			GameObjectManager.unbind(go.gameObject);
			Object.Destroy(go.gameObject);
		}
	}

	public void XmlToLevel(string fileName){
		Debug.Log("path name for xml : " + fileName);
		gameData.totalActionBloc = 0;
		gameData.totalStep = 0;
		gameData.totalExecute = 0;
		gameData.totalCoin = 0;
		gameData.levelToLoadScore = null;
		gameData.dialogMessage = new List<(string, string)>();
		gameData.actionBlocLimit = new Dictionary<string, int>();
		map = new List<List<int>>();

		XmlDocument doc = new XmlDocument();
		doc.Load(fileName);

		XmlNode root = doc.ChildNodes[1];
		foreach(XmlNode child in root.ChildNodes){
			switch(child.Name){
				case "map":
					readXMLMap(child);
					break;
				case "dialogs":
					string src = null;
					//optional xml attribute
					if(child.Attributes["img"] !=null)
						src = child.Attributes.GetNamedItem("img").Value;
					gameData.dialogMessage.Add((child.Attributes.GetNamedItem("dialog").Value, src));
					break;
				case "actionBlocLimit":
					readXMLLimits(child);
					break;
				case "coin":
					createCoin(int.Parse(child.Attributes.GetNamedItem("posX").Value), int.Parse(child.Attributes.GetNamedItem("posZ").Value));
					break;
				case "activable":
					readXMLActivable(child);
					break;
				case "door":
					createDoor(int.Parse(child.Attributes.GetNamedItem("posX").Value), int.Parse(child.Attributes.GetNamedItem("posZ").Value),
					(Direction.Dir)int.Parse(child.Attributes.GetNamedItem("direction").Value), int.Parse(child.Attributes.GetNamedItem("slot").Value));
					break;
				
				case "player":
					createEntity(int.Parse(child.Attributes.GetNamedItem("posX").Value), int.Parse(child.Attributes.GetNamedItem("posZ").Value),
					(Direction.Dir)int.Parse(child.Attributes.GetNamedItem("direction").Value),"player", readXMLScript(child.ChildNodes[0], true));
					break;
				
				case "enemy":
					GameObject enemy = createEntity(int.Parse(child.Attributes.GetNamedItem("posX").Value), int.Parse(child.Attributes.GetNamedItem("posZ").Value),
					(Direction.Dir)int.Parse(child.Attributes.GetNamedItem("direction").Value),"enemy", readXMLScript(child.ChildNodes[0]));
					enemy.GetComponent<DetectRange>().range = int.Parse(child.Attributes.GetNamedItem("range").Value);
					enemy.GetComponent<DetectRange>().selfRange = bool.Parse(child.Attributes.GetNamedItem("selfRange").Value);
					enemy.GetComponent<DetectRange>().type = (DetectRange.Type)int.Parse(child.Attributes.GetNamedItem("typeRange").Value);
					break;
				
				case "score":
					gameData.levelToLoadScore = new int[2];
					gameData.levelToLoadScore[0] = int.Parse(child.Attributes.GetNamedItem("threeStars").Value);
					gameData.levelToLoadScore[1] = int.Parse(child.Attributes.GetNamedItem("twoStars").Value);
					break;
			}
		}

		eraseMap();
		generateMap();
		GameObjectManager.addComponent<GameLoaded>(MainLoop.instance.gameObject);
	}

	private void readXMLMap(XmlNode mapNode){
		foreach(XmlNode lineNode in mapNode.ChildNodes){
			List<int> line = new List<int>();
			foreach(XmlNode rowNode in lineNode.ChildNodes){
				line.Add(int.Parse(rowNode.Attributes.GetNamedItem("value").Value));
			}
			map.Add(line);
		}
	}

	private void readXMLLimits(XmlNode limitsNode){
		string actionName = null;
		foreach(XmlNode limitNode in limitsNode.ChildNodes){
			//gameData.actionBlocLimit.Add(int.Parse(limitNode.Attributes.GetNamedItem("limit").Value));
			actionName = limitNode.Attributes.GetNamedItem("actionType").Value;
			if (!gameData.actionBlocLimit.ContainsKey(actionName)){
				Debug.Log(actionName);
				gameData.actionBlocLimit[actionName] = int.Parse(limitNode.Attributes.GetNamedItem("limit").Value);
			}
		}
	}

	private void readXMLActivable(XmlNode activableNode){
		List<int> slotsID = new List<int>();

		foreach(XmlNode child in activableNode.ChildNodes){
			slotsID.Add(int.Parse(child.Attributes.GetNamedItem("slot").Value));
		}

		createActivable(int.Parse(activableNode.Attributes.GetNamedItem("posX").Value), int.Parse(activableNode.Attributes.GetNamedItem("posZ").Value),
		 slotsID, (Direction.Dir)int.Parse(activableNode.Attributes.GetNamedItem("direction").Value));
	}

	private List<GameObject> readXMLScript(XmlNode scriptNode, bool editable = false){
		if(scriptNode != null){
			List<GameObject> script = new List<GameObject>();
			foreach(XmlNode actionNode in scriptNode.ChildNodes){
				script.Add(readXMLAction(actionNode, editable));
				Debug.Log("script : " + readXMLAction(actionNode, editable));
			}

			return script;			
		}
		return null;
	}

	private GameObject readXMLAction(XmlNode actionNode, bool editable = false){
		GameObject obj = null;
		BaseElement action = null;
		GameObject prefab = null;
		bool firstchild;

		string actionKey = actionNode.Attributes.GetNamedItem("actionType").Value;
		switch(actionKey){
			case "If" :
				prefab = Resources.Load ("Prefabs/IfDetectBloc") as GameObject;
				obj = Object.Instantiate (prefab);
				obj.GetComponent<UIActionType>().linkedTo = GameObject.Find("If");
				action = obj.GetComponent<IfAction>();
				//read xml
				((IfAction)action).ifDirection = int.Parse(actionNode.Attributes.GetNamedItem("ifDirection").Value);
				((IfAction)action).ifEntityType = int.Parse(actionNode.Attributes.GetNamedItem("ifEntityType").Value);
				((IfAction)action).range = int.Parse(actionNode.Attributes.GetNamedItem("range").Value);
				((IfAction)action).ifNot = bool.Parse(actionNode.Attributes.GetNamedItem("ifNot").Value);

				//add to gameobject
				obj.transform.GetChild(0).Find("DropdownEntityType").GetComponent<TMP_Dropdown>().value = ((IfAction)action).ifEntityType;
				obj.transform.GetChild(0).Find("DropdownDirection").GetComponent<TMP_Dropdown>().value = ((IfAction)action).ifDirection;
				obj.transform.GetChild(0).Find("InputFieldRange").GetComponent<TMP_InputField>().text = ((IfAction)action).range.ToString();
				
				if(!((IfAction)action).ifNot)
					obj.transform.GetChild(0).Find("DropdownIsOrIsNot").GetComponent<TMP_Dropdown>().value = 0;
				else
					obj.transform.GetChild(0).Find("DropdownIsOrIsNot").GetComponent<TMP_Dropdown>().value = 1;

				//not interactable actions
				obj.transform.GetChild(0).Find("DropdownEntityType").GetComponent<TMP_Dropdown>().interactable = editable;
				obj.transform.GetChild(0).Find("DropdownDirection").GetComponent<TMP_Dropdown>().interactable = editable;
				obj.transform.GetChild(0).Find("InputFieldRange").GetComponent<TMP_InputField>().interactable = editable;
				obj.transform.GetChild(0).Find("DropdownIsOrIsNot").GetComponent<TMP_Dropdown>().interactable = editable;
				
				Object.Destroy(obj.GetComponent<UITypeContainer>());

				//add children
				firstchild = false;
				if(actionNode.HasChildNodes){
					foreach(XmlNode actNode in actionNode.ChildNodes){
						GameObject child = (readXMLAction(actNode, editable));
						child.transform.SetParent(obj.transform);
						if(!firstchild){
							firstchild = true;
							((IfAction)action).firstChild = child;
						}
					}
				}
				break;
			
			case "For":
				prefab = Resources.Load ("Prefabs/ForBloc") as GameObject;
				obj = Object.Instantiate (prefab);
				obj.GetComponent<UIActionType>().linkedTo = GameObject.Find("For");
				action = obj.GetComponent<ForAction>();

				//read xml
				((ForAction)action).nbFor = int.Parse(actionNode.Attributes.GetNamedItem("nbFor").Value);
				
				//add to gameobject
				if(editable){
					obj.transform.GetChild(0).GetChild(1).GetComponent<TMP_InputField>().text = ((ForAction)action).nbFor.ToString();
				}
				else{
					obj.transform.GetChild(0).GetChild(1).GetComponent<TMP_InputField>().text = (((ForAction)action).currentFor).ToString() + " / " + ((ForAction)action).nbFor.ToString();
					Object.Destroy(obj.GetComponent<UITypeContainer>());
				}
				obj.transform.GetChild(0).GetChild(1).GetComponent<TMP_InputField>().interactable = editable;

				//add children
				firstchild = false;
				if(actionNode.HasChildNodes){
					foreach(XmlNode actNode in actionNode.ChildNodes){
						GameObject child = (readXMLAction(actNode, editable));
						child.transform.SetParent(action.transform);
						if(!firstchild){
							firstchild = true;
							((ForAction)action).firstChild = child;
						}
					}	
				}
				break;

			case "Forever":
				prefab = Resources.Load ("Prefabs/InfiniteLoop") as GameObject;
				obj = Object.Instantiate (prefab);
				obj.GetComponent<UIActionType>().linkedTo = null;
				action = obj.GetComponent<ForeverAction>();
				
				if(!editable)
					//add to gameobject
					Object.Destroy(obj.GetComponent<UITypeContainer>());

				//add children
				firstchild = false;
				if(actionNode.HasChildNodes){
					foreach(XmlNode actNode in actionNode.ChildNodes){
						GameObject child = (readXMLAction(actNode, editable));
						child.transform.SetParent(action.transform);
						if(!firstchild){
							firstchild = true;
							((ForeverAction)action).firstChild = child;
						}
					}	
				}
				break;			
			
			default:

				prefab = Resources.Load ("Prefabs/"+actionKey+"ActionBloc") as GameObject;
				obj = Object.Instantiate (prefab);
				obj.GetComponent<UIActionType>().linkedTo = GameObject.Find(actionKey);
				action = obj.GetComponent<BasicAction>();		

				break;
		}
		obj.GetComponent<UIActionType>().prefab = prefab;
		action.target = obj;
		if(!editable)
			Object.Destroy(obj.GetComponent<PointerSensitive>());
		return obj;
	}

	// link actions together => define next property
	public static void computeNext(GameObject container){
		for(int i = 0 ; i < container.transform.childCount ; i++){
			Transform child = container.transform.GetChild(i);
			if(i < container.transform.childCount-1 && child.GetComponent<BaseElement>()){
				child.GetComponent<BaseElement>().next = container.transform.GetChild(i+1).gameObject;
			}
			else if(i == container.transform.childCount-1 && child.GetComponent<BaseElement>() && container.GetComponent<BaseElement>()){
				if(container.GetComponent<ForAction>() || container.GetComponent<ForeverAction>())
					child.GetComponent<BaseElement>().next = container;
				else if(container.GetComponent<IfAction>())
					child.GetComponent<BaseElement>().next = container.GetComponent<BaseElement>().next;
			}
			//if or for action
			if(child.GetComponent<IfAction>() || child.GetComponent<ForAction>() || child.GetComponent<ForeverAction>())
				computeNext(child.gameObject);
		}
	}

    // Regarde le status de l'apprenant et défini les paramétres pour la création du niveau générer procéduralement
    public void choiceParameterLevel()
    {
		List<bool> stepLearning = model.GetComponent<UserModel>().stepLearning;
		List<int> levelHardProposition = model.GetComponent<UserModel>().levelHardProposition;

		// On choisis au hasard de travailler sur une nouvelle compentence ou bien un mélange de ceux déjà connue
		bool learnNewComp = false;
		bool paraOk = false; // pour savoir si on trouve des parametre durant la recherche

		//On commence par regarder si il reste des nouvelles compétences à apprendre
		if (stepLearning.Contains(false)){
			float rndChoice = Random.Range(0.0f, 1.0f);
            // 0.5 pourcent de chance d'apprendre une nouvelle compétence ou bien si l'apprenant n'a pas encore validé de compétence
            if(rndChoice < 0.5f || !stepLearning.Contains(true))
			{
				learnNewComp = true;
			}
		}

        if (learnNewComp)
        {
			Debug.Log("On travail de nouvelle compétence");
			bool compFind = false;
			// Si on travail sur un nouvelle compétence, on regarde sur laquel on peux travaillé
			for (int i = 0; i < stepLearning.Count; i++)
			{
				// Si la compétence n'est pas encore apprise
                if (!stepLearning[i])
                {
					bool preroqui = true;
					// Je vérifie si les prequis sont remplie
					for(int j = 0; j < model.GetComponent<UserModel>().followStateLearn[i].Count; j++)
                    {
                        if (!stepLearning[model.GetComponent<UserModel>().followStateLearn[i][j]])
                        {
							preroqui = false;
						}

                        // Si les prérequi sont remplis on arréte la boucle
                        if (preroqui)
                        {
							break;
                        }
                    }

					// Si prérequi ok on initialise les paramétres
					if (preroqui)
                    {
						compFind = true;
						infoLevelGen.GetComponent<infoLevelGenerator>().hardLevel = 1;
						infoLevelGen.GetComponent<infoLevelGenerator>().nbActionMin = 0;
						List<bool> vectorComp = new List<bool>();
						for(int k = 0; k < stepLearning.Count; k++)
                        {
							if(k != i)
                            {
								vectorComp.Add(false);
							}
                            else
                            {
								vectorComp.Add(true);
							}
                        }
						infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence = vectorComp;
						paraOk = true;
					} 
                }

                if (compFind)
                {
					break;
                }
			}

			// Si au final pas de nouvelle compétence trouvé, on va finalement générer un niveau baser sur plusieurs compétence
            if (!compFind)
            {
				learnNewComp = false;
			}
		}

		// Si on ne travail pas sur une nouvelle compétence
		if (!learnNewComp)
		{
			Debug.Log("On ne travail pas de nouvelle compétence, mais on renforce les anciennes");
			// On commence par en choisir une au hasard
			bool compFind = false;
			int indiceComp = 0;
			while (!compFind)
			{   // On tire un nombre au hasard parmis les compétences
				int compRand = Random.Range(0, stepLearning.Count);
				// Si cette compétence est aquise
				if (stepLearning[compRand])
				{
					compFind = true;
					indiceComp = compRand;
				}
			}

			// Ensuite on regarde le niveau de difficulté associé à cette compétence
			hardlevel = model.GetComponent<UserModel>().levelHardProposition[indiceComp];
			int compLearn = 0;
			foreach (bool c in stepLearning)
			{
				if (c)
				{
					compLearn += 1;

				}
			}

			// on définit quel compétence seront travailler selon la difficulté
			// 1 -> Une seul compétence de travaillé
			// 2 -> Deux ou trois compétences travaillé
			// 3 -> Plus de trois compétences travaillé
			int compteurAntiBoucleInfini = 20;
			Dictionary<List<bool>, bool> learningState = model.GetComponent<UserModel>().learningState;
			while (compteurAntiBoucleInfini > 0 && !paraOk)
			{
				Debug.Log("Recherche para");
				Debug.Log("hardlevel : " + hardlevel);
				Debug.Log("compLearn : " + compLearn);
				if (hardlevel == 1)
				{
					Debug.Log("On para le niveau 1");
					infoLevelGen.GetComponent<infoLevelGenerator>().hardLevel = 1;
					infoLevelGen.GetComponent<infoLevelGenerator>().nbActionMin = 0;
					List<bool> vectorComp = new List<bool>();
					for (int k = 0; k < stepLearning.Count; k++)
					{
						if (k != indiceComp)
						{
							vectorComp.Add(false);
						}
						else
						{
							vectorComp.Add(true);
						}
					}
					// On vérifie que le vecteur trouvé n'ai pas déjà connue
					if (learningState.ContainsKey(vectorComp))
					{
						if (!learningState[vectorComp])
						{
							Debug.Log("ok pour vecteur level 1");
							infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence = vectorComp;
							paraOk = true;
						}
					}
                    else
                    {
						Debug.Log("ok pour vecteur level 1");
						infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence = vectorComp;
						paraOk = true;
					}
				}
				else if (hardlevel == 2 && compLearn <= 2)
				{
					Debug.Log("On para le niveau 2");
					infoLevelGen.GetComponent<infoLevelGenerator>().hardLevel = 2;
					infoLevelGen.GetComponent<infoLevelGenerator>().nbActionMin = 0;
					List<int> compPlus = new List<int>(); // On va choisir les compétences à associer
					int nbComPlus = Random.Range(1, compLearn); // on veux entre 1 à 2 compétence en plus
					while (compPlus.Count < nbComPlus) // Tant qu'on a pas les compétences
					{
						int c = Random.Range(0, 5);
						if (c != indiceComp && stepLearning[c])
						{
							compPlus.Add(c);
						}
					}
					List<bool> vectorComp = new List<bool>();
					for (int k = 0; k < stepLearning.Count; k++)
					{
						if (k != indiceComp && !compPlus.Contains(k))
						{
							vectorComp.Add(false);
						}
						else
						{
							vectorComp.Add(true);
						}
					}
					if (learningState.ContainsKey(vectorComp))
					{
						if (!learningState[vectorComp])
						{
							Debug.Log("ok pour vecteur level 1");
							infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence = vectorComp;
							paraOk = true;
						}
					}
                    else
                    {
						Debug.Log("ok pour vecteur level 2");
						infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence = vectorComp;
						paraOk = true;
					}
				}
				else if (hardlevel == 3 && compLearn > 3)
				{
					Debug.Log("On para le niveau 3");
					infoLevelGen.GetComponent<infoLevelGenerator>().hardLevel = 3;
					infoLevelGen.GetComponent<infoLevelGenerator>().nbActionMin = 0;
					List<int> compPlus = new List<int>(); // On va choisir les compétences à associer
					int nbComPlus = Random.Range(3, compLearn); // on veux entre 1 à 2 compétence en plus
					while (compPlus.Count < nbComPlus) // Tant qu'on a pas les compétences
					{
						int c = Random.Range(0, 5);
						if (c != indiceComp && stepLearning[c])
						{
							compPlus.Add(c);
						}
					}
					List<bool> vectorComp = new List<bool>();
					for (int k = 0; k < stepLearning.Count; k++)
					{
						if (k != indiceComp && !compPlus.Contains(k))
						{
							vectorComp.Add(false);
						}
						else
						{
							vectorComp.Add(true);
						}
					}
					if (learningState.ContainsKey(vectorComp))
					{
						if (!learningState[vectorComp])
						{
							Debug.Log("ok pour vecteur level 1");
							infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence = vectorComp;
							paraOk = true;
						}
					}
                    else
                    {
						Debug.Log("ok pour vecteur level 3");
						infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence = vectorComp;
						paraOk = true;
					}
				}
				compteurAntiBoucleInfini = compteurAntiBoucleInfini - 1;
			}
		}

        // Si on a pas de parametre ok on recharge la scene d'accueil
        if (!paraOk)
        {
			Debug.Log("Probléme lors de la recherche des paramétres de niveau pour la création de niveau procédural");
			GameObjectManager.loadScene("TitleScreen");
		}
	}


	//Création de niveau auto
	public void CreateLvlAuto() {
		Debug.Log("Création niveau auto");

		infoLevelGen.GetComponent<infoLevelGenerator>().nbActionMin = 0;

        // Définie la difficulté du niveau si pas d'option
        if (!infoLevelGen.GetComponent<infoLevelGenerator>().optionOk)
        {
			choiceParameterLevel();
		}

		//Variable pour la création
		gameData.totalActionBloc = 0;
		gameData.totalStep = 0;
		gameData.totalExecute = 0;
		gameData.totalCoin = 0;
		gameData.levelToLoadScore = null;
		gameData.dialogMessage = new List<(string, string)>();
		gameData.actionBlocLimit = new Dictionary<string, int>();
		map = new List<List<int>>();
		bool gateCreate = false;
		bool robotCreate = false;

		//Création du chemin
		PathCreation();
		//Ajout du player et de la platforme de teleportation du début
		StartCreation();

		// Ajoute des porte et terminal de contrôle
		int rNbGate = 0;
		int rNbRobot = 0;
		if (infoLevelGen.GetComponent<infoLevelGenerator>().hardLevel == 1)
        {
			rNbGate = 1;
			rNbRobot = 1;
		}
		if (infoLevelGen.GetComponent<infoLevelGenerator>().hardLevel == 2)
		{
			rNbGate = Random.Range(1, nbCorridor);
			robotCreate = RobotCreation();
		}
		for (int i = 1; i <= rNbGate; i++)
		{
			gateCreate = GateCreation();
			if(infoLevelGen.GetComponent<infoLevelGenerator>().hardLevel > 1)
            {
				actionCreation["Activate"] += 1;
				//si while et if
				if(infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence[1] && infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence[2])
                {
					actionCreation["If"] += 1;
				}
			}
            else
            {
				actionCreation["Activate"] = -1;
				//si while et if
				if (infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence[1] && infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence[2])
				{
					actionCreation["If"] = -1;
				}
			}
		}
		//ajout des robots
		for (int i = 1; i <= rNbRobot; i++)
		{
			robotCreate = RobotCreation();
			if (infoLevelGen.GetComponent<infoLevelGenerator>().hardLevel > 1)
			{
				actionCreation["Wait"] += 1;
			}
			else
			{
				actionCreation["Wait"] = -1;
			}
		}
		//Ajout des murs 
		WallCreation();
		//Ajout du spawn de fin
		EndCreation();

		//pour les vérifictions
		vericationPath();

        //Ajout ou non de la console
        if (!infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence[4])
        {
			GameObject[] console = GameObject.FindGameObjectsWithTag("Console");
			console[0].SetActive(false);
		}
		/*
		if (infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence[4] && infoLevelGen.GetComponent<infoLevelGenerator>().hardLevel == 3)
        {
			Debug.Log("On vire les actions");
			// On enléve toutes les actions pour forcer le joueur à utiliser la console
			actionCreation["Forward"] = 0;
			actionCreation["TurnLeft"] = 0;
			actionCreation["TurnRight"] = 0;
			actionCreation["Wait"] = 0;
			actionCreation["Activate"] = 0;
			actionCreation["TurnBack"] = 0;
			actionCreation["If"] = 0;
			actionCreation["For"] = 0;
		}
		*/
		// On initialise la liste d'action
		foreach (KeyValuePair<string, int> actionl in actionCreation)
		{
			gameData.actionBlocLimit[actionl.Key] = actionl.Value;
		}

		// Déclaration variable pour envoie des parametre du niveau par la trace
		infoLevelGen.GetComponent<infoLevelGenerator>().sendPara = true;
		GameObjectManager.addComponent<GameLoaded>(MainLoop.instance.gameObject);
	}

	//Création d'un chemin automatique selon les différentes variables de difficulté
	private void PathCreation()
    {
		Debug.Log("Génération automatique du chemin");

		// Variable de création
		int x = 1;
		int y = 1;
		int pathPos = 1;
		int nbElement = 0;
		int nbElementMax = 2;
		bool stopCreation = false;

		// On créer et enregistre les informations de la case de départ
		List<int> startPos = new List<int> { x, y };
		Case start = new Case("start", 1, pathPos, startPos);
		pathLevel.Add(start);
		createCell(x, y);

		while (!stopCreation && nbElement < nbElementMax)
        {
			int randElement = Random.Range(0, 2); // a modifier selon le nombre d'ellement

			// Si on fait 0 on créer un couloir
			if (randElement == 0)
			{
				pathPos = CorridorCreation(x, y, pathPos, 6);
			}// Sinon à 1 on fait une piéce
			else if (randElement == 1)
			{
				pathPos = RoomCreation(x, y, pathPos, 6);
			}

			// Permet de noter présisément qu'elle est la dernier case du chemin creer pour éviter d'éventuelle erreur
			foreach (Case item in pathLevel)
			{
				if (item.getPathPosition() == pathPos)
				{
					x = item.getCoord()[0];
					y = item.getCoord()[1];
				}
			}
			nbElement += 1;
		}

		// Si on demande la compétence while
        if (infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence[1])
        {
			pathPos =  whileCreation(x, y, pathPos);
		}

		// Une fois la trajet fini, on note la derniére case comme la case de fin
		foreach (Case item in pathLevel)
		{
			if (item.getPathPosition() == pathPos)
			{
				item.setName("end");
			}
		}
	}

	//Reboucle sur le début du niveau pour recréer un couloir chemin identique
	private int whileCreation(int x, int y, int pathPos)
    {
		// on parcourt case par case le chemin effectué depuis le début et on le recréer en modifiant la position par rapport à la position de racordement 
		int i = 0;
		List<Case> pathlevelTemp = new List<Case>();
		int pathposition = 0;
		foreach (Case c in pathLevel)
        {
			int newX = c.getCoord()[0] + x;
			int newY = c.getCoord()[1] + y;
			if (voidCase(new List<int> { newX, newY }))
            {
				// si la case est la case du chemin on le renseihne dant la nouvelle
				if(c.getPathPosition() != -1)
                {
					Case newCase = new Case("loop", 1, (pathPos + c.getPathPosition()), new List<int> { newX, newY });
					pathlevelTemp.Add(newCase);
					createCell(newX, newY);
					i++;
				}
                else
                {
					Case newCase = new Case("loop", 1, -1, new List<int> { newX, newY });
					pathlevelTemp.Add(newCase);
					createCell(newX, newY);
				}
			}
		}

		//on ajoute les nouvelle case au pathlevel
		// Et on cherche la dernier case
		foreach (Case c in pathlevelTemp)
		{
			pathLevel.Add(c);
			if(c.getPathPosition() > pathposition)
            {
				pathposition = c.getPathPosition();
			}
		}

		actionCreation["For"] = -1;
		infoLevelGen.GetComponent<infoLevelGenerator>().nbActionMin += 1;

		return pathposition;
	}

	// Parcourt le list de pathLevel afin de définir si il éxiste une case sur les mêmes coordonées int x, int y
	private bool voidCase(List <int> coor) {

		foreach(Case item in pathLevel)
        {
			// Si les coordonnées sont déjà occupé par une case
			if (item.sameCoord(coor)){
				return false;
			}
        }
		// Sinon aucunee case n'occupe ses coordonnées donc ok pour nouvelle case
		return true;
	}

	//Place le player et le platforme de téléportation sur la case de départ
	private void StartCreation()
    {
		List<int> start = new List<int>();
		bool creation = false;

		//On cherche la case de départ
		foreach (Case item in pathLevel)
		{
			// Si les coordonnées sont déjà occupé par une case
			if (item.getName() == "start")
			{
				start = item.getCoord();
				creation = true;
				break;
			}
		}

        //Si on a bien trouvé la case start
        if (creation)
        {
			// Création du player
			createEntity(start[0], start[1], (Direction.Dir) direct, "player", null);
			// Création de la platform de téléportation bleu
			createSpawnExit(start[0], start[1], true);
		}
        else
        {
			Debug.Log("Case start introuvable");
        }

	}

	//Crée les murs qui entours le niveau
	private void WallCreation()
    {
		// Regarde les 8 positions autours de chaque case du niveau générer
		// Si la ccase testé autour est vide, crée un mur sinon ne fait rien
		foreach (Case item in pathLevel)
		{
			List<int> coord = item.getCoord();
			bool nord = true;
			bool nordEst = true;
			bool est = true;
			bool sudEst = true;
			bool sud = true;
			bool sudOuest = true;
			bool ouest = true;
			bool nordOuest = true;


			//On test si une case est présente sur l'une des coordonées autour
			foreach (Case item2 in pathLevel)
			{
				// Nord
				if (item2.sameCoord(new List<int> { coord[0], coord[1] - 1 }))
				{
					nord = false;
				}
				//Nord Est
				if (item2.sameCoord(new List<int> { coord[0] + 1, coord[1] - 1 }))
				{
					nordEst = false;
				}
				//Est
				if (item2.sameCoord(new List<int> { coord[0] + 1, coord[1] }))
				{
					est = false;
				}
				//Sud Est
				if (item2.sameCoord(new List<int> { coord[0] + 1, coord[1] + 1 }))
				{
					sudEst = false;
				}
				//Sud
				if (item2.sameCoord(new List<int> { coord[0], coord[1] + 1 }))
				{
					sud = false;
				}
				//Sud Ouest
				if (item2.sameCoord(new List<int> { coord[0] - 1, coord[1] + 1 }))
				{
					sudOuest = false;
				}
				//Ouest
				if (item2.sameCoord(new List<int> { coord[0] - 1, coord[1]}))
				{
					ouest = false;
				}
				//Nord Ouest
				if (item2.sameCoord(new List<int> { coord[0] - 1, coord[1] - 1 }))
				{
					nordOuest = false;
				}
			}
			//on fait pareil pour les murs
			foreach (Case item2 in wallLevel)
			{
				// Nord
				if (item2.sameCoord(new List<int> { coord[0], coord[1] - 1 }))
				{
					nord = false;
				}
				//Nord Est
				if (item2.sameCoord(new List<int> { coord[0] + 1, coord[1] - 1 }))
				{
					nordEst = false;
				}
				//Est
				if (item2.sameCoord(new List<int> { coord[0] + 1, coord[1] }))
				{
					est = false;
				}
				//Sud Est
				if (item2.sameCoord(new List<int> { coord[0] + 1, coord[1] + 1 }))
				{
					sudEst = false;
				}
				//Sud
				if (item2.sameCoord(new List<int> { coord[0], coord[1] + 1 }))
				{
					sud = false;
				}
				//Sud Ouest
				if (item2.sameCoord(new List<int> { coord[0] - 1, coord[1] + 1 }))
				{
					sudOuest = false;
				}
				//Ouest
				if (item2.sameCoord(new List<int> { coord[0] - 1, coord[1] }))
				{
					ouest = false;
				}
				//Nord Ouest
				if (item2.sameCoord(new List<int> { coord[0] - 1, coord[1] - 1 }))
				{
					nordOuest = false;
				}
			}

			//Maintenant on créer les mu ou il faut et les ajoutes a la liste de case pour ne pas en créer plusieurs au même endroit
			// Nord
			if (nord)
			{
				Case newCase = new Case("wall", 0, 0, new List<int> { coord[0], coord[1] - 1 });
				wallLevel.Add(newCase);
				createWall(coord[0], coord[1] - 1);
			}
			//Nord Est
			if (nordEst)
			{
				Case newCase = new Case("wall", 0, 0, new List<int> { coord[0] + 1, coord[1] - 1 });
				wallLevel.Add(newCase);
				createWall(coord[0] + 1, coord[1] - 1);
			}
			//Est
			if (est)
			{
				Case newCase = new Case("wall", 0, 0, new List<int> { coord[0] + 1, coord[1] });
				wallLevel.Add(newCase);
				createWall(coord[0] + 1, coord[1]);
			}
			//Sud Est
			if (sudEst)
			{
				Case newCase = new Case("wall", 0, 0, new List<int> { coord[0] + 1, coord[1] + 1 });
				wallLevel.Add(newCase);
				createWall(coord[0] + 1, coord[1] + 1);
			}
			//Sud
			if (sud)
			{
				Case newCase = new Case("wall", 0, 0, new List<int> { coord[0], coord[1] + 1 });
				wallLevel.Add(newCase);
				createWall(coord[0], coord[1] + 1);
			}
			//Sud Ouest
			if (sudOuest)
			{
				Case newCase = new Case("wall", 0, 0, new List<int> { coord[0] - 1, coord[1] + 1 });
				wallLevel.Add(newCase);
				createWall(coord[0] - 1, coord[1] + 1);
			}
			//Ouest
			if (ouest)
			{
				Case newCase = new Case("wall", 0, 0, new List<int> { coord[0] -1 , coord[1]});
				wallLevel.Add(newCase);
				createWall(coord[0] - 1, coord[1]);
			}
			//Nord Ouest
			if (nordOuest)
			{
				Case newCase = new Case("wall", 0, 0, new List<int> { coord[0] - 1, coord[1] - 1 });
				wallLevel.Add(newCase);
				createWall(coord[0] - 1, coord[1] - 1);
			}

			//On réinitialise les variables
			nord = true;
			nordEst = true;
			est = true;
			sudEst = true;
			sud = true;
			sudOuest = true;
			ouest = true;
			nordOuest = true;
		}
	}

	//Créer la platform de fin à la fin du niveau
	private void EndCreation()
    {
		bool endCase = false;
		List<int> coord = new List<int>();
		
		// On parcourt l'ensemblee des cases
		foreach (Case item in pathLevel)
        {
			// si la case est marquer comme case final
			if(item.getName() == "end")
            {
				// alors on enregistre les coordonées
				coord = item.getCoord();
				endCase = true;
				break;
			}
        }

        // Si on a bien enregistré les coodronées de la case final
        if (endCase)
        {
			// on créer le spawn final
			createSpawnExit(coord[0], coord[1], false);
		}
        else // sinon il y a un probléme car pas de case final
        {
			Debug.Log("Case final introuvable!");
        }
	}

	//Création couloir
	// Retourne le numéro de path de la derniére case créer
	// avant de créer le couloir, on ve regarder si toutes les cases sont libres, sinon deux cas de figure
	// si le couloir ne fait pas au moins 2 cases de distance, alors nepas le créer et renvoyé le numéro de path de la case en cours
	// si le couloir ne peux pas antienrément être créeer, s'arétté avant
	private int CorridorCreation(int coordx, int coordy, int pathPos, int resteNbCase)
    {
		Debug.Log("Création d'un couloir");

		int x = coordx;
		int y = coordy;
		bool sizeMax = true;
		string orientation = "";
		int sizeCorridorTemps = 0;
		int r = Random.Range(0, 4);
		if(resteNbCase > 5)
        {
			sizeCorridorTemps = Random.Range(2, 6);
		}
        else
        {
			sizeCorridorTemps = Random.Range(2, resteNbCase);

		}
		int sizeCorridor = 0;

		if (r == 0)
        {
			orientation = "nord";
			//test de toutes la longeur du couloir
			for (int i = 1; i <= sizeCorridorTemps + 1; i++)
			{
				for(int j = -1; j <= 1; j++){
					//si une case n'est pas libre sur la trajectoir de construction
					if (!voidCase(new List<int> { x + j, y - i }))
					{
						//si le couloir fait moins de 2 cases, on annule la construction (on controle jusqu'é la case 3 pour éviter une fusion avec un salle annexe)
						if (i <= 3)
						{
							return pathPos;
						}
						else
						{
							sizeCorridor = i - 2;
							sizeMax = false;
						}
					}
				}
			}
		}
		else if(r == 1)
        {
			orientation = "est";
			//test de toutes la longeur du couloir
			for (int i = 1; i <= sizeCorridorTemps + 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					//si une case n'est pas libre sur la trajectoir de onstruction
					if (!voidCase(new List<int> { x + i, y + j }))
					{
						//si le couloir fait moins dde 2 cases, on annule la construction (on controle jusqu'é la case 3 pour éviter une fusion avec un salle annexe)
						if (i <= 3)
						{
							return pathPos;
						}
						else
						{
							sizeCorridor = i - 2;
							sizeMax = false;
						}
					}
				}
			}
		}
		else if (r == 2)
        {
			orientation = "sud";
			//test de toutes la longeur du couloir
			for (int i = 1; i <= sizeCorridorTemps + 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					//si une case n'est pas libre sur la trajectoir de onstruction
					if (!voidCase(new List<int> { x + j, y + i }))
					{
						//si le couloir fait moins dde 2 cases, on annule la construction (on controle jusqu'é la case 3 pour éviter une fusion avec un salle annexe)
						if (i <= 3)
						{
							return pathPos;
						}
						else
						{
							sizeCorridor = i - 2;
							sizeMax = false;
						}
					}
				}
			}
		}
        else
        {
			orientation = "ouest";
			//test de toutes la longeur du couloir
			for (int i = 1; i <= sizeCorridorTemps + 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					//si une case n'est pas libre sur la trajectoir de onstruction
					if (!voidCase(new List<int> { x - i, y + j}))
					{
						//si le couloir fait moins de 2 cases, on annule la construction (on controle jusqu'é la case 3 pour éviter une fusion avec un salle annexe)
						if (i <= 3)
						{
							return pathPos;
						}
						else
						{
							sizeCorridor = i - 2;
							sizeMax = false;
						}
					}
				}
			}
		}

		//Si Pas de soucis sur les tests de postions du couloir, alors construire le couloir de taille max
        if (sizeMax)
        {
			sizeCorridor = sizeCorridorTemps;

		}

		if(orientation != "")
        {
			nbCorridor += 1;
		}
        else
        {
			return pathPos;
		}

		int lastPosition = pathPos;
		if (hardlevel > 1)
        {
			actionCreation["TurnLeft"] += 1;
			actionCreation["TurnRight"] += 1;
			actionCreation["TurnBack"] += 1;
		}
        else
        {
			actionCreation["TurnLeft"] = -1;
			actionCreation["TurnRight"] = -1;
			actionCreation["TurnBack"] += -1;
		}
		infoLevelGen.GetComponent<infoLevelGenerator>().nbActionMin += 1;
		for (int i = 1; i <= sizeCorridor; i++)
        {
            if (orientation == "nord")
            {
				Case newCase = new Case("corridor" + nbCorridor, nbCorridor, (pathPos + i), new List<int> { x, y - i });
				pathLevel.Add(newCase);
				createCell(x, y - i);
				direct = 1;
			}
			else if(orientation == "est")
            {
				Case newCase = new Case("corridor" + nbCorridor, nbCorridor, (pathPos + i), new List<int> { x + i, y });
				pathLevel.Add(newCase);
				createCell(x + i, y );
				direct = 2;
			}
			else if(orientation == "sud")
            {
				Case newCase = new Case("corridor" + nbCorridor, nbCorridor, (pathPos + i), new List<int> { x, y + i });
				pathLevel.Add(newCase);
				createCell(x, y + i);
				direct = 4;
			}
			else if(orientation == "ouest")
            {
				Case newCase = new Case("corridor" + nbCorridor, nbCorridor, (pathPos + i), new List<int> { x - i, y });
				pathLevel.Add(newCase);
				createCell(x - i, y);
				direct = 3;
			}
            else
            {
				Debug.Log("Erreure lors du choix d'orintation du couloir!");
				return lastPosition;
			}
			lastPosition = (pathPos + i);
			if(hardlevel > 1)
            {
				actionCreation["Forward"] += 1;
			}
            else
            {
				actionCreation["Forward"] = -1;
			}
			infoLevelGen.GetComponent<infoLevelGenerator>().nbActionMin += 1;
		}
		return lastPosition;
	}

	// Création d'une piece
	// Retourne le numéro de path de la la case de sortie de la salle
	// Avant de créer la salle, on tcheck si on pet la metre à l'endroit désiré
	// Si on ne peux pas, on relance une boucle pour tirer au hasard une nouvelle position
	// si au bout d'un certain nombre d'essais on ne peut toujours pas placer de salle, alors on abandonne l'idée dans faire une une
	private int RoomCreation(int coordx, int coordy, int pathPos, int resteNbCase)
    {
		Debug.Log("Création d'une piéce");

		// Variable pour la construction de la piéce
		int largeur = 0;
		int longeur = 0;
		bool positionnementOK = false;
		string orientation = "";
		int newPathPos = pathPos;
		int rEnter = 0;
		int rExit = 0;
		List<int> exitPoint = new List<int>();

		int cpt = 0;
		//On cherche une place ou créer la salle
        while(!positionnementOK && cpt < 100)
		{
			largeur = Random.Range(3, 5);
			longeur = Random.Range(3, 5); 
			int r = Random.Range(0, 4);

			// Nord
			if(r == 0)
            {
				rEnter = Random.Range(1, largeur);
				bool isOk = true;
				/// On parcours l'espace que peux occupe la salle pour voir si l'espace est libre
				for (int newx = coordx - rEnter; newx <= coordx + (largeur - rEnter) + 1; newx++)
				{
					for (int newy = coordy - longeur - 1; newy <= coordy; newy++)
					{
						// Si il n'y a rien sur la case et que les coordonées tester ne sont pas ceux de la case de départ
						if(!voidCase(new List<int> { newx, newy }) && isOk && !EqualCoordinate(coordx, coordy, newx, newy))
                        {
							isOk = false;
						}
					}
				}

                if (isOk)
                {
					positionnementOK = true;
					orientation = "nord";
				}
			}
			else if(r == 1) // Est
            {
				rEnter = Random.Range(1, longeur);
				bool isOk = true;
				// On parcours l'espace que peux occupe la salle pour voir si l'espace est libre
				for (int newx = coordx; newx <= coordx + largeur + 1; newx++)
				{
					for (int newy = coordy - rEnter; newy <= coordy + (longeur - rEnter) + 1; newy++)
					{
						// Si il n'y a rien sur la case et que lees coordonée tester ne sont pas ceux de la case de départ
						if (!voidCase(new List<int> { newx, newy }) && isOk && !EqualCoordinate(coordx, coordy, newx, newy))
						{
							isOk = false;
						}
					}
				}

				if (isOk)
				{
					positionnementOK = true;
					orientation = "est";
				}			
			}
			else if(r == 2) // Sud
            {
				rEnter = Random.Range(1, largeur);
				bool isOk = true;
				// On parcours l'espace que peux occupe la salle pour voir si l'espace est libre
				for (int newx = coordx - rEnter; newx <= coordx + (largeur - rEnter) + 1; newx++)
				{
					for (int newy = coordy; newy <= coordy + longeur + 1; newy++)
					{
						// Si il n'y a rien sur la case et que les coordonées tester ne sont pas ceux de la case de départ
						if (!voidCase(new List<int> { newx, newy }) && isOk && !EqualCoordinate(coordx, coordy, newx, newy))
						{
							isOk = false;
						}
					}
				}
				if (isOk)
				{
					positionnementOK = true;
					orientation = "sud";
				}				
			}
			else if(r == 3) // Ouest
            {
				rEnter = Random.Range(1, longeur);
				bool isOk = true;
				// On parcours l'espace que peux occupe la salle pour voir si l'espace est libre
				for (int newx = coordx - largeur - 1; newx <= coordx; newx++)
				{
					for (int newy = coordy - rEnter; newy <= coordy + (longeur - rEnter) + 1; newy++)
					{
						// Si il n'y a rien sur la case et que lees coordonée tester ne sont pas ceux de la case de départ
						if (!voidCase(new List<int> { newx, newy }) && isOk && !EqualCoordinate(coordx, coordy, newx, newy))
						{
							isOk = false;
						}
					}
				}

				if (isOk)
				{
					positionnementOK = true;
					orientation = "ouest";
				}				
			}
            else
            {
				Debug.Log("Probléme recherche orientation création salle r = " + r);
				return pathPos;
			}
			cpt += 1;
		}

		if (orientation != "")
		{
			nbRoom += 1;
		}
        else
        {
			return pathPos;
        }

		//Si ok, on créer la salle à la bonne position
		if (positionnementOK)
        {
			if (hardlevel > 1)
			{
				actionCreation["TurnLeft"] += 1;
				actionCreation["TurnRight"] += 1;
				actionCreation["TurnBack"] += 1;
			}
			else
			{
				actionCreation["TurnLeft"] = -1;
				actionCreation["TurnRight"] = -1;
				actionCreation["TurnBack"] += -1;
			}
			infoLevelGen.GetComponent<infoLevelGenerator>().nbActionMin += 1;
			//si au nord
			if (orientation == "nord")
			{
				direct = 1;
				for (int newx = coordx - rEnter + 1; newx <= coordx + (largeur - rEnter); newx++)
				{
					for (int newy = coordy - longeur; newy <= coordy - 1; newy++)
					{
						Case newCase = new Case("room", nbRoom, -1, new List<int> { newx, newy });
						pathLevel.Add(newCase);
						createCell(newx, newy);
					}
				}

				//définir case de sortie
				rExit = Random.Range(1, largeur);
				exitPoint.Add(coordx - (rEnter - rExit));
				exitPoint.Add(coordy - longeur);
			}
			else if (orientation == "est")
			{
				direct = 2;
				for (int newx = coordx + 1; newx <= coordx + largeur; newx++)
				{
					for (int newy = coordy - rEnter; newy <= coordy + (longeur - rEnter); newy++)
					{
						Case newCase = new Case("room", nbRoom, -1, new List<int> { newx, newy });
						pathLevel.Add(newCase);
						createCell(newx, newy);
					}
				}

				//définir case de sortie
				rExit = Random.Range(1, longeur);
				exitPoint.Add(coordx + largeur);
				exitPoint.Add(coordy - (rEnter + 1) + rExit);
			}
			else if (orientation == "sud")
			{
				direct = 4;
				for (int newx = coordx - rEnter + 1; newx <= coordx + (largeur - rEnter); newx++)
				{
					for (int newy = coordy + 1; newy <= coordy + longeur; newy++)
					{
						Case newCase = new Case("room", nbRoom, -1, new List<int> { newx, newy });
						pathLevel.Add(newCase);
						createCell(newx, newy);
					}
				}

				//définir case de sortie
				rExit = Random.Range(1, longeur);
				exitPoint.Add(coordx - (rEnter - rExit));
				exitPoint.Add(coordy + longeur);
			}
			else if (orientation == "ouest")
			{
				direct = 3;
				for (int newx = coordx - largeur; newx <= coordx - 1; newx++)
				{
					for (int newy = coordy - rEnter; newy <= coordy + (longeur - rEnter); newy++)
					{
						Case newCase = new Case("room", nbRoom, -1, new List<int> { newx, newy });
						pathLevel.Add(newCase);
						createCell(newx, newy);
					}
				}

				//définir case de sortie
				rExit = Random.Range(1, longeur);
				exitPoint.Add(coordx - largeur);
				exitPoint.Add(coordy - (rEnter + 1) + rExit);
			}
			else
			{
				Debug.Log("Probléme orientation à la création de la salle");
			}

			//on met a jour les info des cases du chemin
			int x = coordx;
			int y = coordy;
			while (!EqualCoordinate(x, y, exitPoint[0], exitPoint[1]))
			{
				// On note les coordonées de la case suivante pour le path en commancant par les x
				if (x < exitPoint[0] && !voidCase(new List<int> { x + 1, y }))
				{
					x += 1;
				}
				else if (x > exitPoint[0] && !voidCase(new List<int> { x - 1, y }))
                {
					x -= 1;
                }
				else if (y < exitPoint[1] && !voidCase(new List<int> { x, y + 1 }))
                {
					y += 1;
                }
				else if(y > exitPoint[1] && !voidCase(new List<int> { x, y - 1 }))
                {
					y -= 1;
				}
				// on met à jour le path position de la case concerné
				foreach (Case item in pathLevel)
				{
					//On a trouver la case de sortie
					if (item.getCoord()[0] == x && item.getCoord()[1] == y)
					{
						newPathPos = newPathPos + 1;
						item.setPathPosition(newPathPos);
					}
				}
				if (hardlevel > 1)
				{
					actionCreation["Forward"] += 1;
				}
				else
				{
					actionCreation["Forward"] = -1;
				}
				infoLevelGen.GetComponent<infoLevelGenerator>().nbActionMin += 1;
			}
		}
		return newPathPos;
	}

	// Compare deux coordonées
	private bool EqualCoordinate(int x1, int y1, int x2, int y2) 
	{ 
		if(x1 == x2 && y1 == y2)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	// Met une porte et un terminal dans le niveau
	// choisis un couloir au hasard
	// détermine ou placer une porte pour bloquer le chemin
	// détermine si on peux mettre un terminal avant
	// Si oui place les deux éléments et renvoie true
	// sinon renvoie false
	private bool GateCreation()
    {
		Debug.Log("Création d'une porte et d'un terminal");

		//Si pas de couloir encore construit, alors pas de porte à créer
		if(nbCorridor == 0)
        {
			Debug.Log("Pas de couloir créer pour le moment");
			return false;
        }
		// On tire un numéro de couloir au hasard
		int r = Random.Range(1, nbCorridor + 1);

		// On tire la position de la porte dans se couloir au hasard
		// Pour commencer on détermine quel séquence de position détermine les cases du couloir
		int numCaseStart = 1000;
		int numCaseEnd = 0;
		foreach(Case item in pathLevel)
        {
			if(item.getName() == ("corridor" + r) && item.getPathPosition() < numCaseStart)
            {
				numCaseStart = item.getPathPosition();

			}
			else if(item.getName() == ("corridor" + r) && item.getPathPosition() > numCaseEnd)
            {
				numCaseEnd = item.getPathPosition();
			}
        }
		//On tire ensuite au hasard une position dans le couloir (au moins position 2 pour avoir la place de placer le terminal dans la case d'avant)
		int rGatePos = Random.Range(numCaseStart + 1, numCaseEnd); // On ne prend pas la derniére case pour ne pas se retrouvé dans un angle
		//On determine ensuite la position du terminal
		int rTermPos = 1;
		if (rGatePos > 5)
		{
			rTermPos = Random.Range(rGatePos - 5, rGatePos);
		}
        else
        {
			rTermPos = Random.Range(1, rGatePos);
		}
		if (rGatePos <= rTermPos)
        {
			Debug.Log("Couloir trop petit");
			return false;
        }

		// On place ensuite les deux éléments
		nbGate += 1;
		List<int> coordGate = new List<int>();
		List<int> coordNextCaseGate = new List<int>();
		List<int> coordTerminal = new List<int>();
		//On cherche les coordonées corespondant au pathPosition
		foreach (Case item in pathLevel)
        {
			if(item.getPathPosition() == rGatePos)
            {
				coordGate.Add(item.getCoord()[0]);
				coordGate.Add(item.getCoord()[1]);
			}

            if (item.getPathPosition() == rTermPos)
            {
				coordTerminal.Add(item.getCoord()[0]);
				coordTerminal.Add(item.getCoord()[1]);
			}

            if (item.getPathPosition() == rGatePos - 1)
            {
				coordNextCaseGate.Add(item.getCoord()[0]);
				coordNextCaseGate.Add(item.getCoord()[1]);
			}
        }

		// On verifie avant qu'il n'y a pas déjà une gate ou un terminal à cette endroit
		foreach(Case item in gateAndTermLevel)
        {
            if (EqualCoordinate(item.getCoord()[0], item.getCoord()[1], coordGate[0], coordGate[1]) || EqualCoordinate(item.getCoord()[0], item.getCoord()[1], coordTerminal[0], coordTerminal[1]))
            {
				return false;
            }
        }

		createDoor(coordGate[0], coordGate[1], (Direction.Dir)int.Parse(corridorOrientationGate(coordNextCaseGate[0], coordGate[0])), nbGate);
		createActivable(coordTerminal[0], coordTerminal[1], new List<int> { nbGate }, (Direction.Dir)int.Parse(corridorOrientationTerminal(coordTerminal[0], coordTerminal[1], rTermPos)));

		// On ajoute les deux éléments à la liste des gateAndTerminal
		Case newElement = new Case("gate", nbGate, 0, coordGate);
		gateAndTermLevel.Add(newElement);
		newElement = new Case("terminal", nbGate, 0, coordTerminal);
		gateAndTermLevel.Add(newElement);

		return true;
    }

	// Determine l'orientation que doit prendre une gate
	private string corridorOrientationGate(int x1, int x2)
    {
		if(x1 != x2)
        {
			return "2";
        }
        else
        {
			return "1";
		}
    }

	// Determine l'orientation que doit prendre un terminal
	private string corridorOrientationTerminal(int x, int y, int pathPos)
    {
		bool orientationOk = false;
		List<int> previousCase = new List<int>();
		List<int> nextCase = new List<int>();
		if (voidCase(new List<int> { x - 1, y }))
        {
			return "3";
        }
		else if (voidCase(new List<int> { x, y - 1}))
        {
			return "1";
		}
		else if(voidCase(new List<int> { x + 1, y }))
		{
			return "2";
		}
		else if(voidCase(new List<int> { x, y + 1}))
		{
			return "4";
		}

		if (!orientationOk)
		{
			//determiner sens du chemin
			foreach (Case item in pathLevel)
			{
				if (item.getPathPosition() == pathPos + 1)
				{
					nextCase.Add(item.getCoord()[0]);
					nextCase.Add(item.getCoord()[1]);
				}
				else if (item.getPathPosition() == pathPos - 1)
				{
					previousCase.Add(item.getCoord()[0]);
					previousCase.Add(item.getCoord()[1]);
				}
			}

			if (nextCase[0] != previousCase[0])
			{
				return "1";
			}
			else
			{
				return "2";
			}
		}

		return "1";
    }

	// Met en place un robot
	// Pour le moment le robot sera placer non loin du chemin dans des rooms et fera des tours sur lui même
	private bool RobotCreation()
    {
		Debug.Log("Création Robot");

		//Variable pour la creation d'un robot
		bool creationOk = false;
		string orientation = "";
		List<int> coordRobot = new List<int>();

		//Si pas de salle de créer, pas le peine d'essayer de placer un robot
		if(nbRoom == 0)
        {
			Debug.Log("Pas de salle créer donc pas de robot à placer");
			return false;
        }

		//On selectionne une salle au hasard
		int rRoom = Random.Range(1, nbRoom + 1);
		//On determine quel sont les cases du chemin
		List<Case> casePath = new List<Case>();
        foreach(Case item in pathLevel)
        {
			if(item.getName() == "room" && item.getnbStruct() == rRoom && item.getPathPosition() > 0)
            {
				casePath.Add(item);
			}
        }
		//On selectionne une case au hasard et on regarde si on peux placer le robot à une case à coté (mais pas sur une autre case du chemin)
		int rCase = 0;
		int nbTentative = 0;
		while(!creationOk && nbTentative < 10)
        {
			rCase = Random.Range(1, casePath.Count);
			nbTentative += 1;
			if (!voidCase(new List<int> { casePath[rCase].getCoord()[0] + 1, casePath[rCase].getCoord()[1] }) && !EqualePath(new List<int> { casePath[rCase].getCoord()[0] + 1, casePath[rCase].getCoord()[1] }))
            {
				creationOk = true;
				coordRobot.Add(casePath[rCase].getCoord()[0] + 1);
				coordRobot.Add(casePath[rCase].getCoord()[1]);
				orientation = "2"; //Doit regarder à l'Ouest
			}
			else if (!voidCase(new List<int> { casePath[rCase].getCoord()[0], casePath[rCase].getCoord()[1] + 1 }) && !EqualePath(new List<int> { casePath[rCase].getCoord()[0], casePath[rCase].getCoord()[1] + 1 }))
			{
				creationOk = true;
				coordRobot.Add(casePath[rCase].getCoord()[0]);
				coordRobot.Add(casePath[rCase].getCoord()[1] + 1);
				orientation = "4"; //Doit regarder au Nord
			}
			else if (!voidCase(new List<int> { casePath[rCase].getCoord()[0] - 1, casePath[rCase].getCoord()[1] }) && !EqualePath(new List<int> { casePath[rCase].getCoord()[0] - 1, casePath[rCase].getCoord()[1] }))
			{
				creationOk = true;
				coordRobot.Add(casePath[rCase].getCoord()[0] - 1);
				coordRobot.Add(casePath[rCase].getCoord()[1]);
				orientation = "3"; //Doit regarder à l'Est
			}
			else if (!voidCase(new List<int> { casePath[rCase].getCoord()[0], casePath[rCase].getCoord()[1] - 1 }) && !EqualePath(new List<int> { casePath[rCase].getCoord()[0], casePath[rCase].getCoord()[1] - 1 }))
			{
				creationOk = true;
				coordRobot.Add(casePath[rCase].getCoord()[0]);
				coordRobot.Add(casePath[rCase].getCoord()[1] - 1);
				orientation = "1"; //Doit regarder au Sud
			}
		}

        // Si pas de placage trouvé
        if (!creationOk)
        {
			return false;
        }
        else // Sinon on creer l'ennemie
        {
			//Debug.Log("item1 : " + gameData.levelToLoad.Item1);
			//Debug.Log("item2 : " + gameData.levelToLoad.Item2);
			//Debug.Log("level list : " + gameData.levelList);
			XmlDocument doc = new XmlDocument();
			//doc.Load("E:/Unity_project/Assets/StreamingAssets/Levels/generique/DroneRotation.xml");
			//doc.Load("Assets / StreamingAssets / Levels / Campagne / Niveau 3.xml");
			//Debug.Log("Script robot chargé");
			//XmlNode root = doc.ChildNodes[1];

			//createEntity(coordRobot[0], coordRobot[1], (Direction.Dir)int.Parse(orientation), "enemy", readXMLScript(root.ChildNodes[0]));
			createEntity(coordRobot[0], coordRobot[1], (Direction.Dir)int.Parse(orientation), "enemy");
			return true;	
		}
    }

	// Renvoir true si la case est une case du path
	private bool EqualePath(List<int> coor)
    {
		foreach (Case item in pathLevel)
		{
			// Si les coordonnées sont déjà occupé par une case et que c'est une case du path
			if (item.sameCoord(coor) && item.getPathPosition() > 0)
			{
				return true;
			}
		}
		// Sinon pas de case du path sur ces coordonées
		return false;
	}

	// Pour les test, permet de vérifier le chemin
	private void vericationPath()
    {
		foreach(Case item in pathLevel)
        {
			if(item.getPathPosition() > 0)
            {
				createCell(item.getCoord()[0], item.getCoord()[1]);
            }
        }
    }

}
