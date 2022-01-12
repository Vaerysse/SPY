using UnityEngine;
using FYFY;
using System.Collections.Generic;

public class UserModelSystem : FSystem {

	public static UserModelSystem instance;

	// Load family
	private Family learnerModel = FamilyManager.getFamily(new AllOfComponents(typeof(UserModel)));
	private Family editableScriptContainer_f = FamilyManager.getFamily(new AllOfComponents(typeof(UITypeContainer)), new AnyOfTags("ScriptConstructor"));


	// Learner
	private GameObject currentLearner;

	// Other
	bool debugSystem = true;
	bool testSolution = false;
	private GameObject editableContainer; // L'objet qui continent la liste d'instruction créer par l'utilisateur, contient un enfant des le début (la barre rouge)
	GameObject infoLevelGen;

	public UserModelSystem()
	{
		if(Application.isPlaying)
		{
			currentLearner = GameObject.Find("Learner");
			currentLearner.GetComponent<UserModel>().endLevel = false; // Le niveau n'est pas terminé
			stratTentative();
			editableContainer = editableScriptContainer_f.First(); // On récupére le container d'action éditable
			infoLevelGen = GameObject.Find("infoLevelGen"); // On récupére le gameobject contenant les infos du niveau
			Debug.Log("nb enfant au début : " + editableContainer.transform.childCount);
		}
		instance = this;
	}

	// Use this to update member variables when system pause. 
	// Advice: avoid to update your families inside this function.
	protected override void onPause(int currentFrame) {
	}

	// Use this to update member variables when system resume.
	// Advice: avoid to update your families inside this function.
	protected override void onResume(int currentFrame){
	}

	// Use to process your families.
	protected override void onProcess(int familiesUpdateCount) {
        //testModelPresence();
        if (!testSolution) {
			timerResolution();
		}

		// Si le niveau est terminé
		if (currentLearner.GetComponent<UserModel>().endLevel)
        {
			// On lance la maj du model
			endLevelMajModel();
		}
	}

	public void testModelPresence()
    {
		Debug.Log("testModelPresence taille : " + learnerModel.Count);
		foreach(GameObject model in learnerModel)
        {
			Debug.Log(model.GetComponent<UserModel>().learnerName);
		}

    }

	// Lorsque dans un niveau l'utilisateur appuie sur play, on calcule le temps mis pour construire la réponse de la tentative
	// on l'ajoute ensuite au totalLevelTime
	public void playLevelActivated()
	{
		if (debugSystem)
        {
			Debug.Log("play level activated : UserModelSysteme");
		}
		// Permet d'arréter de compter le temp passer à créer sa liste d'action
		testSolution = true;
		timerResolution();
		// Incrémente une tentative
		addAttempt();

		currentLearner.GetComponent<UserModel>().totalLevelTime = currentLearner.GetComponent<UserModel>().timeStart;
		Debug.Log(currentLearner.GetComponent<UserModel>().totalLevelTime);


	}

	// Lorsqu'un niveau est chargé, on enregistre l'heure de début afin de calculer par la suite le temps de construction de la réponse au niveau
	private void stratTentative()
    {
		currentLearner.GetComponent<UserModel>().timeStart = 0;
	} 

	// Sert de timer lors de la résulition avant de lancer une tentative
	private void timerResolution()
    {
		currentLearner.GetComponent<UserModel>().timeStart = currentLearner.GetComponent<UserModel>().timeStart + Time.deltaTime;
	}

	// Imcrémente de 1 le nombre de tentive de résoudre le 
	private void addAttempt()
    {
		currentLearner.GetComponent<UserModel>().nbTry += 1;

	}

	// Effectue les mise à jour pour la modélisation de l'utilisation
	private void endLevelMajModel()
    {
		Debug.Log("end level");
		Debug.Log("nb enfant a la fin : " + editableContainer.transform.childCount);

		// On repasse tous de suite à la variable à false pour éviter des doubles calcules
		currentLearner.GetComponent<UserModel>().endLevel = false;
		// calcule le temps moyen pour avoir réussit le niveau
		currentLearner.GetComponent<UserModel>().meanLevelTime = currentLearner.GetComponent<UserModel>().totalLevelTime / currentLearner.GetComponent<UserModel>().nbTry;
		// On regarde la différence du nombre d'action entre ce que à fait l'utilisateur et le minimum calculé par le systéme
		currentLearner.GetComponent<UserModel>().difNbAction = (editableContainer.transform.childCount - 1) - infoLevelGen.GetComponent<infoLevelGenerator>().nbActionMin;
		// On calcule si on considére que l'on peux augmenter ou non (et de combien) la balanceWinFail de l'uilisateur
		float point = 0;
		//Si 1 tentative = 2 points, 2 ou 3 = 1 point si plus de 10 tentative = -1 point, sinon 0 point
		if(currentLearner.GetComponent<UserModel>().nbTry == 1)
        {
			point = 2;
        }
		else if (currentLearner.GetComponent<UserModel>().nbTry == 2 || currentLearner.GetComponent<UserModel>().nbTry == 2)
        {
			point = 1;
		}
		else if (currentLearner.GetComponent<UserModel>().nbTry >= 10)
        {
			point = -1;
		}
        // Si le nombre d'écrat d'action et au moins de +20% alors -0.5 point
        if (currentLearner.GetComponent<UserModel>().difNbAction >= (infoLevelGen.GetComponent<infoLevelGenerator>().nbActionMin / 5))
        {
			point = point - 0.5f;
        }
		//si le temps est beaucoup trop long (20s par block minim) alors -0.5 point
		if (currentLearner.GetComponent<UserModel>().meanLevelTime > (20 * infoLevelGen.GetComponent<infoLevelGenerator>().nbActionMin)){
			point = point - 0.5f;
		}

		// on met à jour la balance pour la/les compétences testées
		// Si le vecteur compétence n'est pas encore présent on l'ajoutedans le suivis de la balance ET dans le dictionnaire de compétence
		if (!currentLearner.GetComponent<UserModel>().balanceFailWin.ContainsKey(infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence))
		{
			Debug.Log("Ajout vector");
			if(point < 0)
            {
				point = 0;
            }
			currentLearner.GetComponent<UserModel>().balanceFailWin.Add(infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence, point);
			currentLearner.GetComponent<UserModel>().learningState.Add(infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence, false); // la compétence n'est pas aprice donc false
		}
		else // sinon on met juste à jour la valeur en faisant attention de ne pas avoir de valeur négative
		{
			if(currentLearner.GetComponent<UserModel>().balanceFailWin[infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence] + point < 0)
            {
				currentLearner.GetComponent<UserModel>().balanceFailWin[infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence] = 0;
			}
            else
            {
				currentLearner.GetComponent<UserModel>().balanceFailWin[infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence] += point;
			}
		}
		currentLearner.GetComponent<UserModel>().balanceFailWin[infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence] += point;
		// On valide une compétence (ou un ensemble de compétence) lorsque le résultat de la balance est à au moins 4
		if (currentLearner.GetComponent<UserModel>().balanceFailWin[infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence] >= 4)
        {
			currentLearner.GetComponent<UserModel>().learningState.Add(infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence, true);
		}

		// Envoie trace fin de niveau

		/////   A FAIRE    ////
	}
}