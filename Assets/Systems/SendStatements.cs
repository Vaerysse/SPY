﻿using UnityEngine;
using FYFY;
using DIG.GBLXAPI;
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public class SendStatements : FSystem {

    private Family f_actionForLRS = FamilyManager.getFamily(new AllOfComponents(typeof(ActionPerformedForLRS)));
    private Family learnerModel = FamilyManager.getFamily(new AllOfComponents(typeof(UserModel))); // Charge les familles model

    public static SendStatements instance;
    public GameObject currentLearner;
    GameObject infoLevelGen; // info sur la création procedural du level 


    private bool activeTrace = false;

    public SendStatements()
    {
        if (Application.isPlaying)
        {
            initGBLXAPI();
            currentLearner = GameObject.Find("Learner");
            infoLevelGen = GameObject.Find("infoLevelGen");
        }
        instance = this;
    }

    public void initGBLXAPI()
    {
        if (!GBLXAPI.IsInit)
            GBLXAPI.Init(GBL_Interface.lrsAddresses);

        GBLXAPI.debugMode = false;

        string sessionID = Environment.MachineName + "-" + DateTime.Now.ToString("yyyy.MM.dd.hh.mm.ss");
        //Generate player name unique to each playing session (computer name + date + hour)
        GBL_Interface.playerName = String.Format("{0:X}", sessionID.GetHashCode());
        //Generate a UUID from the player name
        GBL_Interface.userUUID = GBLUtils.GenerateActorUUID(GBL_Interface.playerName);
    }

	// Use to process your families.
	protected override void onProcess(int familiesUpdateCount) {
        // Do not use callbacks because in case in the same frame actions are removed on a GO and another component is added in another system, family will not trigger again callback because component will not be processed
        foreach (GameObject go in f_actionForLRS)
        {
            ActionPerformedForLRS[] listAP = go.GetComponents<ActionPerformedForLRS>();
            int nb = listAP.Length;
            ActionPerformedForLRS ap;
            if (!this.Pause)
            {
                for (int i = 0; i < nb; i++)
                {
                    ap = listAP[i];
                    //If no result info filled
                    if (!ap.result)
                    {
                        GBL_Interface.SendStatement(ap.verb, ap.objectType, ap.objectName, ap.activityExtensions);
                    }
                    else
                    {
                        bool? completed = null, success = null;

                        if (ap.completed > 0)
                            completed = true;
                        else if (ap.completed < 0)
                            completed = false;

                        if (ap.success > 0)
                            success = true;
                        else if (ap.success < 0)
                            success = false;

                        GBL_Interface.SendStatementWithResult(ap.verb, ap.objectType, ap.objectName, ap.activityExtensions, ap.resultExtensions, completed, success, ap.response, ap.score, ap.duration);
                    }
                }
            }
            for (int i = nb - 1; i > -1; i--)
            {
                GameObjectManager.removeComponent(listAP[i]);
            }
        }

        // Si le niveau est terminé on apelle la trace pour la fin de niveau
        if (currentLearner.GetComponent<UserModel>().endLevel)
        {
            endLevel();
        }

        // Si le joueur à acquis une nouvelle compétence ou compétence croisé
        if (currentLearner.GetComponent<UserModel>().newCompetenceValide)
        {

        }

    }

    public void testSendStatement()
    {
        Debug.Log(GBL_Interface.playerName + " asks to send statement...");
        GameObjectManager.addComponent<ActionPerformedForLRS>(MainLoop.instance.gameObject, new
        {
            verb = "interacted",
            objectType = "menu",
            objectName = "myButton"
        });
    }

    // Lorsque le joueur appuie sur le bouton play
    // On envoie le temps écoulé pour avoir créer la sequence d'action + la liste des actions
    public void playLevelActivated()
    {
        if (activeTrace)
        {
            Debug.Log("Play level activated : SendStatements");
            GameObjectManager.addComponent<ActionPerformedForLRS>(MainLoop.instance.gameObject, new
            {
                verb = "interacted",
                objectType = "menu",
                objectName = "ExectuteActionList"
                // ajouter data
            });
        }
    }

    // Losrque l'on reset un niveau
    // On envoie juste l'action, on pourra avec la trace voir si quelqu'un à un pb pour compléter un niveau
    public void resetLevelActiveted()
    {
        if (activeTrace)
        {
            Debug.Log("Reset level : SendStatements");
            GameObjectManager.addComponent<ActionPerformedForLRS>(MainLoop.instance.gameObject, new
            {
                verb = "interacted",
                objectType = "menu",
                objectName = "reset level"
            });
        }
    }

    // Lorsque le joueur arrive à faire sortir le robot du niveau
    // Envoie les données de l'apprenant pour le niveau
    // meanLevelTime, totalLevelTime, nbTry
    public void endLevel()
    {
        if (activeTrace)
        {
            Debug.Log("End level : SendStatements");
            GameObjectManager.addComponent<ActionPerformedForLRS>(MainLoop.instance.gameObject, new
            {
                verb = "interacted", // à changer
                objectType = "menu", // à changer
                objectName = "Data End Level" 
                // ajouter donnée
            });
        }
    }

    // Lorsque le joueur valide une compétence (ou un ensemble de complétence croisé)
    // On envoie le vecteur validé
    public void newCompetenceValide()
    {
        if (activeTrace)
        {
            Debug.Log("New competence valide : SendStatements");
            GameObjectManager.addComponent<ActionPerformedForLRS>(MainLoop.instance.gameObject, new
            {
                verb = "interacted", // à changer
                objectType = "menu", // à changer
                objectName = "New competence valide"
                // ajouter donnée newCompetenceValideVector
            });
        }

        // Une fois la trace envoyé (ou non selon option) on remet la variable de validation à false pour éviter des envoies succéssif
        currentLearner.GetComponent<UserModel>().newCompetenceValide = false;
    }

    // Envoie les données de parametre du niveau créer procéduralement
    // vectorCompetence, hardLevel
    public void paraLevelProcessCreation()
    {
        if (activeTrace)
        {
            Debug.Log("Parameter Level Process Creation : SendStatements");
            GameObjectManager.addComponent<ActionPerformedForLRS>(MainLoop.instance.gameObject, new
            {
                verb = "interacted", // à changer
                objectType = "menu", // à changer
                objectName = "Parameter level creation process"
                // ajouter donnée 
            });
        }

        // Une fois la trace envoyé (ou non selon option) on remet la variable à false pour éviter des envoies succéssif
        infoLevelGen.GetComponent<infoLevelGenerator>().sendPara = false;
    }

}