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
	private List<List<int>> map;
	private GameData gameData;
	private GameObject scriptContainer;

	//List de dictionaire qui continient le niveau généré procéduralement. Chaque Dictionaire est un noeud qui contient une liste de int (cordonées de la case + nom noeud parent) et a comme clef son nom
	private List<Case> pathLevel = new List<Case>();
	//On doit aussi noter les murs pour la création des murs
	private List<Case> wallLevel = new List<Case>();

	//Classe Case 
	//Elle permet d'avoir toutes les informations d'une case lors de la création de niveau procédural
	//Fonctionne comme un systéme de noeud
	//Chaque case à un parent qui est la case créer juste avant elle à lequelle elle est rataché
	//Un seul parent mais plusieurs enfants possible
	//Le parent est à zéro pour tous objet qui n'est pas une case ou la case start
	private class Case
    {
		string name; //Nom de la case, par défaut son numéro hiérarchique mais peux changer de nom si besoin (comme par exemple la case de départ appeller start)
		int hierarchyPosition; //Ca position dans la hiérarchie
		int parent; //Le numéro hiérarchique du parent
		List<int> pos; //Coordonnée X et Y de la case

		public Case(string n, int i, int p, List<int> coord)
        {
			this.name = n;
			this.hierarchyPosition = i;
			this.parent = p;
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

		//renvoie le nom de la case
		public string getName()
        {
			return this.name;
        }

		//renvoie les coordonnées de la case
		public List<int> getCoord()
        {
			return this.pos;
        }

		//renvoie la position hiérarchique de la case
		public int getHierachyPosittion()
        {
			return this.hierarchyPosition;
        }

		//renvoi le numéro de parent de la case
		public int getParent()
        {
			return this.parent;
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
			Debug.Log(gameData.levelToLoad.Item1);
			Debug.Log(gameData.levelToLoad.Item2);
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


	//Création de niveau auto
	public void CreateLvlAuto() {
		Debug.Log("Création niveau auto");
		//Création du chemin
		PathCreation();
		//Ajout du player et de la platforme de teleportation du début
		StartCreation();
		//Ajout des murs 
		WallCreation();
		//Ajout du spawn de fin
		EndCreation();
	}

	//Création d'un chemin automatique selon les différentes variables de difficulté
	private void PathCreation()
	{
		Debug.Log("Génération automatique du chemin");

		// Paramétre de création
		int lanePourcent = 49; // (à metre entre 99 et 50) 99 si l'on veux de niveaux type couloir, 50 si l'on veux un niveau entiérement en platforme

		// Variable de création
		int x = 1;
		int y = 1;
		bool noX = false; // Permet de refaire une boucle et modifier diretement le Y
		bool noY = false; // Permet de refaire une boucle et modifier diretement le Y
		bool createCase = true; // autorise la création d'une case
		int taillePath = Random.Range(20, 31); // A remplacer par tailleMinPath et tailleMaxPath + 1 (car ne prend pas la derniére valeur A VERIFIER) lorsque que j'ajouterais la gestion de la difficulté
		Debug.Log(taillePath);

		// On créer et enregistre les informations de la case de départ
		List<int> startPos = new List<int> { x, y };
		Case start = new Case("start", 1, 0, startPos);
		pathLevel.Add(start);
		createCell(x, y);

		for (int i = 2; i <= taillePath; i++)
		{
			// Choisis si la prochaine case sera un déplacement vers l'axe x ou vers l'axe Y
			int r = Random.Range(0, 2);
			int r2 = Random.Range(0, 100);
			//si r = 0 on crer la nouvelle case en changeeant le x
			if (r == 0)
			{
				// si r2 <= lanePourcent et qu'il n'existe pas de case en modifiant les coordonnée avec x+1 et qu'on l'on peux modifier les coordonnées X alors on fait x+1
				if (r2 <= lanePourcent && voidCase(new List<int> {x + 1, y}) && !noX)
                {
					x += 1;
				}
                else if(voidCase(new List<int> {x - 1, y}) && !noX) // sinon si il n'existe pas de case en modifiant les coordonnée avec x-1 et qu'on l'on peux modifier les coordonnées X alors on fait x-1
				{
					x -= 1;
				}
                else // on refait un tour pour passer en y
                {
					noX = true;
					i -= 1;
					createCase = false;
				}
			}
			else if (!noY) // sinon on change le y si l'on à le droit de modifier le Y
			{
				// si r2 <= lanePourcent et qu'il n'existe pas de case en modifiant les coordonnée avec y+1 et qu'on l'on peux modifier les coordonnées Y alors on fait y+1
				if (r2 <= lanePourcent && voidCase(new List<int> {x, y + 1}) && !noY)
				{
					y += 1;
				}
				else if (voidCase(new List<int> {x, y - 1}) && !noY) // sinon si il n'existe pas de case en modifiant les coordonnée avec y-1 et qu'on l'on peux modifier les coordonnées Y alors on fait y-1
				{
					y -= 1;
				}
				else // on refait un tour pour passer en y
				{
					noY = true;
					i -= 1;
					createCase = false;
				}
			}

			// Si les deux coordonées ne peuvent pas être modifiés, il y a un pb A GERER PLUS TARD
            if (noX && noY)
            {
				Debug.Log("Probléme lors de la création procédural");
			}
            else if(i == taillePath && createCase) // on créer et enregistre les informations de la derniére case
			{
				List<int> casePos = new List<int> { x, y };
				Case lastCase = new Case("end", i, i - 1, casePos);
				pathLevel.Add(lastCase);
				createCell(x, y);
			}
			else if (createCase) // sinon on créer et enregistre les informations de la nouvelle case
			{
				List<int> casePos = new List<int> { x, y };
				Case newCase = new Case("case", i, i - 1, casePos);
				pathLevel.Add(newCase);
				createCell(x, y);

				// On réinitialise les varibale de test de position avant la prochaine boucle
				noX = false;
				noY = false;
			}
            else
            {
				// On réinitialise les varibale de test de position avant la prochaine boucle
				noX = false;
				noY = false;
				createCase = true;
			}

		}
		Debug.Log(pathLevel);

	}

	// Parcourt le list de pathLevel afin dd définir si il éxiste une casee sur les mêmes coordonées int x, int y
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
			createEntity(start[0], start[1], (Direction.Dir)1, "player", null);
			// Création de la platform de téléportation bleu
			createSpawnExit(start[0], start[1], true);
		}
        else
        {
			Debug.Log("Case start introuvable");
        }

	}

	//Crée les murs qui entours le niveau
	public void WallCreation()
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
	public void EndCreation()
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
}
