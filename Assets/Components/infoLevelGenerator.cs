using UnityEngine;
using System.Collections.Generic;

public class infoLevelGenerator : MonoBehaviour {
	// Advice: FYFY component aims to contain only public members (according to Entity-Component-System paradigm).

	// Nombre d'action minimum pour finir le niveau
	public int nbActionMin;
	// Les compétence en test
	public List<bool> vectorCompetence = new List<bool>();
}