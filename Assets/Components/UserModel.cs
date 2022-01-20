using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class UserModel : MonoBehaviour {
	// Advice: FYFY component aims to contain only public members (according to Entity-Component-System paradigm).

	// Matrice permettant de connaittre l'etat d'un apprenant, permet de voir aussi son état d'apprentissage lors que l'on mélange plusieurs compétences ensemble
	// Le premier niveau du dictionnaire concerne la compétence que dont on souhaite obtenir les informations
	// Le dictionnaire du deuxiéme niveau contient en clef la liste des compétence mse ensemble lors d'une génération de niveau
	// Actuellement on représente la listes de compétence comme ceci : [Séquence, Boucle, If...Then, Négation, Console]
	// Attention à bien respecter l'ordre de création de la séquence dans l'odre
	// Exemple d'utilisation -> Séquence + Négation + Console = [1, 0, 0, 1, 1]
	public Dictionary<List<bool>, bool> learningState;
	// Même utilisation que learningState pour connaitre le nombre de fois ou l'apprenant à réussit
	// On incrémente ou décrémente selon les critére de réussite d'un niveau
	// Arriver à un certain nombre à définir, on valide la (ou le mélange) de compétence dans learningState
	public Dictionary<List<bool>, float> balanceFailWin;
	// Nom de l'apprenant
	public string learnerName;
	// Permet de connaitre ou en est l'aprenant dans son apprentissage : [Séquence, Boucle, If...Then, Négation, Console]
	public List<bool> stepLearning = new List<bool>();
	// Permet de savoir quel compétence dépend de quel compétence au préalable
	// Atention les indice donnée sont ceux du vecteur : [Séquence, Boucle, If...Then, Négation, Console]
	// dict format [Compétence : liste compétence dont on a besoin]
	public Dictionary<int, List<int>> followStateLearn;

	// Variable pour l'initialisation du model
	// Est ce que l'initialisation à déjà été faite
	public bool initOk = false;
	// Chargement du model lors du début du jeu
	public bool loadModel = false;


	//Les variables suivante servent lors du calcule de connaissance de la compéence en fin de niveau

	// Temps (moyen) que l'apprenant à passé a construire son chemin avant de lancer la tentative de résolution
	public float meanLevelTime;
	// Début timer pour calculer le temps de la résolution du niveau
	public float timeStart;
	// Enregistre le temps total passer à construire la réponse au niveau (toutes les tentative sont aditionnées)
	public float totalLevelTime;
	// Dif entre le minimum d'action à faire glisser et le nombre d'action utiliser par l'apprenant
	public int difNbAction;
	// Nombre de tentative fait pour finir le niveau
	public int nbTry;
	// Défini si le joueur à atteind la fin du niveau
	public bool endLevel;
	// Défini si le joueur a validé une nouvelle compétence (ou compétence croisé) dans le niveau
	public bool newCompetenceValide;
	// Le vector de compétence que viens de valider le joueur
	public List<bool> newCompetenceValideVector = new List<bool>();
	// La difficulté pour le jours pour le prochain niveau
	public List<int> levelHardProposition = new List<int>();

}