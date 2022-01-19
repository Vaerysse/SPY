using UnityEngine;
using FYFY;
using System.Collections.Generic;

public class UserModelSystem : FSystem {

	public static UserModelSystem instance;

	// Load family
	private Family learnerModel = FamilyManager.getFamily(new AllOfComponents(typeof(UserModel)));
	private Family editableScriptContainer_f = FamilyManager.getFamily(new AllOfComponents(typeof(UITypeContainer)), new AnyOfTags("ScriptConstructor"));
	private Family infoLevel_F = FamilyManager.getFamily(new AnyOfComponents(typeof(infoLevelGenerator)));


	// Learner
	private GameObject currentLearner;

	// Other
	bool testSolution = false;
	private GameObject editableContainer; // L'objet qui continent la liste d'instruction créer par l'utilisateur, contient un enfant des le début (la barre rouge)
	GameObject infoLevelGen;

	public UserModelSystem()
	{
		if(Application.isPlaying)
		{
			currentLearner = learnerModel.First();
			if(editableScriptContainer_f.Count  > 0)
            {
				editableContainer = editableScriptContainer_f.First(); // On récupére le container d'action éditable
			}
			infoLevelGen = infoLevel_F.First(); // On récupére le gameobject contenant les infos du niveau

			initModelLearner();
			stratTentative();
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
			// Puis on réinitialise le model
			infoLevelGen.GetComponent<infoLevelGenerator>().newLevelGen = true;
		}
	}

	// Initialise le model de l'apprenant
	public void initModelLearner()
    {
		// On réinitialise le level
		if (infoLevelGen.GetComponent<infoLevelGenerator>().newLevelGen)
		{
			infoLevelGen.GetComponent<infoLevelGenerator>().newLevelGen = false; // pour éviter d'initialiser à chaque fois qu'on recommence le niveau en cours

			currentLearner.GetComponent<UserModel>().meanLevelTime = 0.0f;
			currentLearner.GetComponent<UserModel>().timeStart = 0.0f;
			currentLearner.GetComponent<UserModel>().totalLevelTime = 0.0f;
			currentLearner.GetComponent<UserModel>().difNbAction = 0;
			currentLearner.GetComponent<UserModel>().nbTry = 0;
			currentLearner.GetComponent<UserModel>().endLevel = false;
			currentLearner.GetComponent<UserModel>().newCompetenceValide = false;
			currentLearner.GetComponent<UserModel>().newCompetenceValideVector = new List<bool>();
		}
	}

	// Lorsque dans un niveau l'utilisateur appuie sur play, on calcule le temps mis pour construire la réponse de la tentative
	// on l'ajoute ensuite au totalLevelTime
	public void playLevelActivated()
	{
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
		else if (currentLearner.GetComponent<UserModel>().nbTry == 2 || currentLearner.GetComponent<UserModel>().nbTry == 3)
        {
			point = 1;
		}
		else if (currentLearner.GetComponent<UserModel>().nbTry >= 5)
        {
			point = -1;
		}
		// Si le nombre d'écrat d'action et au moins de +20% alors -0.5 point
		if (currentLearner.GetComponent<UserModel>().difNbAction >= ((float)infoLevelGen.GetComponent<infoLevelGenerator>().nbActionMin / 5))
        {
			point = point - 0.5f;
        }
		//si le temps est beaucoup trop long (20s par block minim) alors -0.5 point
		if (currentLearner.GetComponent<UserModel>().meanLevelTime > (20 * infoLevelGen.GetComponent<infoLevelGenerator>().nbActionMin)){
			point = point - 0.5f;
		}

		Debug.Log("Point obtenue : " + point);

		// Pb avec les clef du dico il faut noter la sequence à la main...
		bool vecPresent = false;
		List<bool> vecComp = new List<bool>();
		int cpt = 0;
		foreach (KeyValuePair<List<bool>, float> vec in currentLearner.GetComponent<UserModel>().balanceFailWin)
		{
			bool res = true;
			for(int i = 0; i < vec.Key.Count; i++)
            {
				if(vec.Key[i] != infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence[i])
                {
					res = false;

				}
            }

			if (res)
			{
				vecPresent = true;
				vecComp = vec.Key;

			}
			cpt++;
		}
		if (!vecPresent)
		{
			vecComp = infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence;
		}

        // on met à jour la balance pour la/les compétences testées
        // Si le vecteur compétence n'est pas encore présent on l'ajoutedans le suivis de la balance ET dans le dictionnaire de compétence
        if (!vecPresent)
		{
			if (point < 0)
            {
				point = 0;
            }
			currentLearner.GetComponent<UserModel>().balanceFailWin.Add(vecComp, point);
			currentLearner.GetComponent<UserModel>().learningState.Add(vecComp, false); // la compétence n'est pas aprice donc false
		}
		else // sinon on met juste à jour la valeur en faisant attention de ne pas avoir de valeur négative
		{
			if (currentLearner.GetComponent<UserModel>().balanceFailWin[vecComp] + point < 0)
			{
				currentLearner.GetComponent<UserModel>().balanceFailWin[vecComp] = 0;
			}
            else
            {
				currentLearner.GetComponent<UserModel>().balanceFailWin[vecComp] += point;
			}
		}


		// Si on a travailler qu'une seul compétence, on ne touche pas au niveau de difficulté
		int nbTrainCompetence = 0;
		for (int i = 0; i < infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence.Count; i++)
        {
            if (infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence[i])
            {
				nbTrainCompetence += 1;
			}
        }

		// Sinon
		// Si obtenue 2 points, on monte la difficulté
		// Si obtenue plus de 0 mais moins de 2 on ne change pas
		// Si obtenue 0 ou moins on baisse la difficulté
		if (nbTrainCompetence > 1)
		{
			if (point >= 2)
			{
				// On parcours la liste du vector compétence travaillé et on augmente le niveau de difficulté de toutes les compétences présentes
				for (int i = 0; i < infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence.Count; i++)
				{
					if (infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence[i])
					{
						currentLearner.GetComponent<UserModel>().levelHardProposition[i] = infoLevelGen.GetComponent<infoLevelGenerator>().hardLevel + 1;
					}
				}
			}
			else if (point <= 0)
			{
				for (int i = 0; i < infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence.Count; i++)
				{
					if (infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence[i])
					{
						// La compétence ne doit pas être en dessous du niveau 1
						if (currentLearner.GetComponent<UserModel>().levelHardProposition[i] > 1)
                        {
							currentLearner.GetComponent<UserModel>().levelHardProposition[i] = infoLevelGen.GetComponent<infoLevelGenerator>().hardLevel - 1;
						}
					}
				}
			}
            else
            {
				for (int i = 0; i < infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence.Count; i++)
				{
					if (infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence[i])
					{
						Debug.Log("update dificulté");
						currentLearner.GetComponent<UserModel>().levelHardProposition[i] = infoLevelGen.GetComponent<infoLevelGenerator>().hardLevel;
					}
				}
			}
		}

		// On valide une compétence (ou un ensemble de compétence) lorsque le résultat de la balance est à au moins 5
		if (currentLearner.GetComponent<UserModel>().balanceFailWin[vecComp] >= 5)
		{
			currentLearner.GetComponent<UserModel>().learningState[vecComp] = true;
			currentLearner.GetComponent<UserModel>().newCompetenceValideVector = infoLevelGen.GetComponent<infoLevelGenerator>().vectorCompetence;
			currentLearner.GetComponent<UserModel>().newCompetenceValide = true;

			// Si une seul compétence travaillé, alor on valide cettec ompétence ausssi dans le suivit de l'etat de l'apprenant
			int nbCompVal = 0;
			int indice = -1;
			int i = 0;
            foreach (bool b in vecComp)
            {
                if (b)
                {
					nbCompVal += 1;
					indice = i;
				}
				i++;
            }
			if(nbCompVal == 1)
            {
				currentLearner.GetComponent<UserModel>().stepLearning[indice] = true;
			}
		}
	}
}