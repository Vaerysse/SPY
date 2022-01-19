using UnityEngine;
using System.Collections.Generic;

public class infoLevelGenerator : MonoBehaviour {
	// Advice: FYFY component aims to contain only public members (according to Entity-Component-System paradigm).

	// Nombre d'action minimum pour finir le niveau
	public int nbActionMin;
	// Les compétence en test
	public List<bool> vectorCompetence = new List<bool>();
	// Difficulté du niveau
	public int hardLevel;
	// Défini si le niveau est fini d'être créer pour l'envoie des traces
	public bool sendPara;
	// Défini si un nouveau level à été lancé
	public bool newLevelGen;
	// Précise si des options on été choisis
	public bool optionOk;
}