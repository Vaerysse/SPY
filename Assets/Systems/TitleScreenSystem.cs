using UnityEngine;
using FYFY;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;
using TMPro;
using System.Xml;

/// <summary>
/// Manage main menu to launch a specific mission
/// </summary>
public class TitleScreenSystem : FSystem {
	private GameData gameData;
	private GameObject campagneMenu;
	private GameObject playButton;
	private GameObject quitButton;
	private GameObject backButton;
	private GameObject buttonLvlGenerator;
	private GameObject cList;
	private Dictionary<GameObject, List<GameObject>> levelButtons; //key = directory button,  value = list of level buttons

	private GameObject infoLevelGen;
	private GameObject model;

	//Chargement des famille
	private Family modelLearner = FamilyManager.getFamily(new AnyOfComponents(typeof(UserModel)));
	private Family infoLevel_F = FamilyManager.getFamily(new AnyOfComponents(typeof(infoLevelGenerator)));
	private Family proceduralOptionMenu_f = FamilyManager.getFamily(new AnyOfComponents(typeof(ProceduralOptionMenu))); // récupére le menu d'option pour la génération procédural
	private Family proceduralOptionCheckBox_f = FamilyManager.getFamily(new AnyOfComponents(typeof(ProceduralOptionCheckbox))); // récupére les checkBox des otpion pour la génération procédural

	public TitleScreenSystem(){
		if (Application.isPlaying)
		{
			gameData = GameObject.Find("GameData").GetComponent<GameData>();
			gameData.levelList = new Dictionary<string, List<string>>();
			campagneMenu = GameObject.Find("CampagneMenu");
			playButton = GameObject.Find("Jouer");
			quitButton = GameObject.Find("Quitter");
			backButton = GameObject.Find("Retour");
			buttonLvlGenerator = GameObject.Find("LvlGenerator");
			GameObjectManager.dontDestroyOnLoadAndRebind(GameObject.Find("GameData"));
			GameObjectManager.dontDestroyOnLoadAndRebind(GameObject.Find("Learner")); // Permet de garder l'objet contenant la modélisation de l'apprenant entre chaque scéne
			GameObjectManager.dontDestroyOnLoadAndRebind(GameObject.Find("infoLevelGen")); // Permet de garder l'objet contenant les information des niveau scréer procéduralement entre chaque scéne

			Debug.Log("nombre de learn : " + modelLearner.Count);

			//Initialisation des infomations de suivis de niveau
			infoLevelGen = infoLevel_F.First();
			model = modelLearner.First();
			initInfoLevelGen();
			initModelLearn();

			cList = GameObject.Find("CampagneList");
			levelButtons = new Dictionary<GameObject, List<GameObject>>();

			GameObjectManager.setGameObjectState(campagneMenu, false);
			GameObjectManager.setGameObjectState(backButton, false);
			string levelsPath = Application.streamingAssetsPath + Path.DirectorySeparatorChar + "Levels";
			List<string> levels;
			foreach (string directory in Directory.GetDirectories(levelsPath))
			{
				levels = readScenario(directory);
				if (levels != null)
				{
					gameData.levelList[Path.GetFileName(directory)] = levels; //key = directory name
				}
			}

			//create level directory buttons
			foreach (string key in gameData.levelList.Keys)
			{
				GameObject directoryButton = Object.Instantiate<GameObject>(Resources.Load("Prefabs/Button") as GameObject, cList.transform);
				directoryButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = key;
				levelButtons[directoryButton] = new List<GameObject>();
				GameObjectManager.bind(directoryButton);
				// add on click
				directoryButton.GetComponent<Button>().onClick.AddListener(delegate { showLevels(directoryButton); });
				// create level buttons
				for (int i = 0; i < gameData.levelList[key].Count; i++)
				{
					GameObject button = Object.Instantiate<GameObject>(Resources.Load("Prefabs/LevelButton") as GameObject, cList.transform);
					button.transform.Find("Button").GetChild(0).GetComponent<TextMeshProUGUI>().text = Path.GetFileNameWithoutExtension(gameData.levelList[key][i]);
					int indice = i;
					button.transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate { launchLevel(key, indice); });
					levelButtons[directoryButton].Add(button);
					GameObjectManager.bind(button);
					GameObjectManager.setGameObjectState(button, true);
				}
			}
		}
	}

	//Load Scenario list in XML (into the special folder)
	private List<string> readScenario(string repositoryPath){
		if(File.Exists(repositoryPath+Path.DirectorySeparatorChar+"Scenario.xml")){
			List<string> levelList = new List<string>();
			XmlDocument doc = new XmlDocument();
			doc.Load(repositoryPath+Path.DirectorySeparatorChar+"Scenario.xml");
			XmlNode root = doc.ChildNodes[1]; //root = <scenario/>
			foreach(XmlNode child in root.ChildNodes){
				if (child.Name.Equals("level")){
					levelList.Add(repositoryPath + Path.DirectorySeparatorChar + (child.Attributes.GetNamedItem("name").Value));
				}
			}
			return levelList;			
		}
		return null;
	}

	//Quit the application
	protected override void onProcess(int familiesUpdateCount) {
		if(Input.GetButtonDown("Cancel")){
			Application.Quit();
		}
	}

	// See Jouer button in editor
	public void showCampagneMenu(){
		GameObjectManager.setGameObjectState(campagneMenu, true);
		GameObjectManager.setGameObjectState(buttonLvlGenerator, false);// fait disparaitre le bouton
		foreach (GameObject directory in levelButtons.Keys){
			//show directory buttons
			GameObjectManager.setGameObjectState(directory, true);
			//hide level buttons
			foreach(GameObject level in levelButtons[directory]){
				GameObjectManager.setGameObjectState(level, false);
			}
		}
		GameObjectManager.setGameObjectState(playButton, false);
		GameObjectManager.setGameObjectState(quitButton, false);
		GameObjectManager.setGameObjectState(backButton, true);
	}

	private void showLevels(GameObject levelDirectory){
		//show/hide levels
		foreach(GameObject directory in levelButtons.Keys){
			//hide level directories
			GameObjectManager.setGameObjectState(directory, false);
			//show levels
			if(directory.Equals(levelDirectory)){
				//foreach(GameObject go in levelButtons[directory]){
				for(int i = 0 ; i < levelButtons[directory].Count ; i ++){
					GameObjectManager.setGameObjectState(levelButtons[directory][i], true);

					string directoryName = levelDirectory.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text;
					//locked levels
					if(i > PlayerPrefs.GetInt(directoryName, 0)) //by default first level of directory is the only unlocked level of directory
						levelButtons[directory][i].transform.Find("Button").GetComponent<Button>().interactable = true;
					//unlocked levels
					else{
						levelButtons[directory][i].transform.Find("Button").GetComponent<Button>().interactable = true;
						//scores
						int scoredStars = PlayerPrefs.GetInt(directoryName + Path.DirectorySeparatorChar + i + gameData.scoreKey, 0); //0 star by default
						Transform scoreCanvas = levelButtons[directory][i].transform.Find("ScoreCanvas");
						for (int nbStar = 0 ; nbStar < 4 ; nbStar++){
							if(nbStar == scoredStars)
								GameObjectManager.setGameObjectState(scoreCanvas.GetChild(nbStar).gameObject, true);
							else
								GameObjectManager.setGameObjectState(scoreCanvas.GetChild(nbStar).gameObject, false);
						}
					}
				}
			}
			//hide other levels
			else{
				foreach(GameObject go in levelButtons[directory]){
					GameObjectManager.setGameObjectState(go, false);
				}
			}
		}
	}

	//Load (after click to level select) the Main Scene for start a level
	public void launchLevel(string levelDirectory, int level){
		gameData.levelToLoad = (levelDirectory,level);
		GameObjectManager.loadScene("MainScene");
	}

	// Fait apparaitre le panel des options pour la génération de niveau
	// Les compétences acquise ou en cours d'acquisistion sont présent
	public void genLvlProcedural()
    {
		// On active le menu d'option  pour la génération procédural
		foreach(GameObject menu in proceduralOptionMenu_f)
        {
			menu.SetActive(true);
        }

		// On parcourt les chekbox, on active la premiére (le jeu choisis) et désactive les autres
		foreach (GameObject checkbox in proceduralOptionCheckBox_f)
		{
			if(checkbox.name == "aleaToggle")
            {
				checkbox.GetComponent<Toggle>().isOn = true;
			}
            else
            {
				// On ne laisse aparaitre que les compétences possible

				//si le nom = vector comp + comp possible
				checkbox.GetComponent<Toggle>().isOn = false;
			}

            if (checkbox.name == "sequenceToggle" || checkbox.name == "aleaToggle")
            {
				checkbox.SetActive(true);
			}
			else if(checkbox.name == "loopToggle" && model.GetComponent<UserModel>().stepLearning[0])
            {
				checkbox.SetActive(true);
			}
			else if (checkbox.name == "conditionToggle" && model.GetComponent<UserModel>().stepLearning[0])
			{
				checkbox.SetActive(true);
			}
			else if (checkbox.name == "negationToggle" && model.GetComponent<UserModel>().stepLearning[1])
			{
				checkbox.SetActive(true);
			}
			else if (checkbox.name == "consoleToggle" && model.GetComponent<UserModel>().stepLearning[0])
			{
				checkbox.SetActive(true);
			}
            else
            {
				checkbox.SetActive(false);
			}
		}

	}

	// Lance la génération aléatoire selon les options choisis
	public void loadSceneForProceduralGeneration()
    {
		int nbOption = 0;

		// récuperer les options selectionné
		foreach (GameObject checkbox in proceduralOptionCheckBox_f)
		{

			if (checkbox.name == "sequenceToggle" && checkbox.GetComponent<Toggle>().isOn)
			{
				foreach (GameObject go in infoLevel_F)
				{
					go.GetComponent<infoLevelGenerator>().vectorCompetence[0] = true;
				}
				nbOption++;
			}
			else if (checkbox.name == "loopToggle" && checkbox.GetComponent<Toggle>().isOn)
			{
				foreach (GameObject go in infoLevel_F)
				{
					go.GetComponent<infoLevelGenerator>().vectorCompetence[1] = true;
				}
				nbOption++;
			}
			else if (checkbox.name == "conditionToggle" && checkbox.GetComponent<Toggle>().isOn)
			{
				foreach (GameObject go in infoLevel_F)
				{
					go.GetComponent<infoLevelGenerator>().vectorCompetence[2] = true;
				}
				nbOption++;
			}
			else if (checkbox.name == "negationToggle" && checkbox.GetComponent<Toggle>().isOn)
			{
				foreach (GameObject go in infoLevel_F)
				{
					go.GetComponent<infoLevelGenerator>().vectorCompetence[3] = true;
				}
				nbOption++;
			}
			else if (checkbox.name == "consoleToggle" && checkbox.GetComponent<Toggle>().isOn)
			{
				foreach (GameObject go in infoLevel_F)
				{
					go.GetComponent<infoLevelGenerator>().vectorCompetence[4] = true;
				}
				nbOption++;
			}

			int hardLevel = 1;
			// On attribut le niveau de difficulté selon le nombre d'option prise
			if(nbOption == 2 || nbOption == 3)
            {
				hardLevel = 2;
			}
			if (nbOption > 3)
			{
				hardLevel = 3;
			}

			foreach (GameObject go in infoLevel_F)
			{
				go.GetComponent<infoLevelGenerator>().newLevelGen = true;
				if(nbOption > 0)
                {
					go.GetComponent<infoLevelGenerator>().hardLevel = hardLevel;
					go.GetComponent<infoLevelGenerator>().optionOk = true;
				}
                else
                {
					go.GetComponent<infoLevelGenerator>().optionOk = false;
				}
			}
			gameData.levelToLoad = ("generique", 1);
			GameObjectManager.loadScene("MainScene");
		}

		gameData.levelToLoad = ("generique", 1);
		Debug.Log("Generation procedural");
		// On informe dans level info que l'on créer un nouveau niveau proceduralement
		foreach (GameObject go in infoLevel_F)
		{
			go.GetComponent<infoLevelGenerator>().newLevelGen = true;
		}
		GameObjectManager.loadScene("MainScene");
	}

	// See Retour button in editor
	public void backFromCampagneMenu(){
		foreach(GameObject directory in levelButtons.Keys){
			if(directory.activeSelf){
				//main menu
				GameObjectManager.setGameObjectState(campagneMenu, false);
				GameObjectManager.setGameObjectState(playButton, true);
				GameObjectManager.setGameObjectState(quitButton, true);
				GameObjectManager.setGameObjectState(buttonLvlGenerator, true);
				GameObjectManager.setGameObjectState(backButton, false);
				break;
			}
			else{
				//show directory buttons
				GameObjectManager.setGameObjectState(directory, true);
				//hide level buttons
				foreach(GameObject go in levelButtons[directory]){
					GameObjectManager.setGameObjectState(go, false);
				}
			}
		}
	}

	// Initialise le model learner
	private void initModelLearn()
    {
		Debug.Log("Initialisation du model");
		// Si initOk = false, alors c'est le premier chargement du model il faut initialiser les variable concernant l'apprenant en plus de ceux du suivant de l'apprantissage du niveau
		if (!model.GetComponent<UserModel>().initOk)
		{
			Debug.Log("Initialisation  Général du model");
			model.GetComponent<UserModel>().learnerName = "Test learner ok";

			List<bool> listLearn = new List<bool>();
			listLearn.Add(false); // Sequence
			listLearn.Add(false); // While
			listLearn.Add(false); // If...Then
			listLearn.Add(false); // Negation
			listLearn.Add(false); // Console
			model.GetComponent<UserModel>().stepLearning = listLearn;

			Dictionary<int, List<int>> followStateLearn = new Dictionary<int, List<int>>();
			followStateLearn.Add(0, new List<int>()); // Sequence
			List<int> parent0 = new List<int>();
			parent0.Add(0);
			List<int> parent2 = new List<int>();
			parent2.Add(2);
			followStateLearn.Add(1, parent0); // While
			followStateLearn.Add(2, parent0); // If...Then
			followStateLearn.Add(3, parent2); // Negation
			followStateLearn.Add(4, parent0); // Console
			model.GetComponent<UserModel>().followStateLearn = followStateLearn;
			model.GetComponent<UserModel>().initOk = true;
			List<int> hardList = new List<int>();
			hardList.Add(1);
			hardList.Add(1);
			hardList.Add(1);
			hardList.Add(1);
			hardList.Add(1);
			model.GetComponent<UserModel>().levelHardProposition = hardList;
			model.GetComponent<UserModel>().learningState = new Dictionary<List<bool>, bool>();
			model.GetComponent<UserModel>().balanceFailWin = new Dictionary<List<bool>, float>();
		}

		// On réinitialise le level
		if (infoLevelGen.GetComponent<infoLevelGenerator>().newLevelGen)
		{
			Debug.Log("Initialisation  du level du model");

			model.GetComponent<UserModel>().meanLevelTime = 0.0f;
			model.GetComponent<UserModel>().timeStart = 0.0f;
			model.GetComponent<UserModel>().totalLevelTime = 0.0f;
			model.GetComponent<UserModel>().difNbAction = 0;
			model.GetComponent<UserModel>().nbTry = 0;
			model.GetComponent<UserModel>().endLevel = false;
			model.GetComponent<UserModel>().newCompetenceValide = false;
			model.GetComponent<UserModel>().newCompetenceValideVector = new List<bool>();
		}
	}

	// Initialise les infos de level gen
	private void initInfoLevelGen()
    {
		foreach (GameObject go in infoLevel_F)
		{
			go.GetComponent<infoLevelGenerator>().nbActionMin = 0;
			go.GetComponent<infoLevelGenerator>().vectorCompetence = new List<bool> { false, false, false, false, false };
			go.GetComponent<infoLevelGenerator>().hardLevel = 1; //  Niveau 1 par defaut
			go.GetComponent<infoLevelGenerator>().sendPara = false;
			go.GetComponent<infoLevelGenerator>().newLevelGen = true;
		}
	}

	// See Quitter button in editor
	public void quitGame(){
		Application.Quit();
	}
}