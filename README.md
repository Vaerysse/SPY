# SPY

Ce projet et un projet universitaire dans le cadre de l’UE d’ISG (Ingénierie des Serious Games) effectué en Master 2 Informatique parcours ANDROIDE. Ce projet à été fait un binôme.

Le but de l’exercice était d’améliorer un jeu sérieux déjà existant (développer par les étudiants de l’année précédente) : SPY ![SPY original](https://github.com/Mocahteam/SPY)

La jeu sérieux SPY a pour objectif l’aide au développement de la pensée informatique. Pour cela il faut construire une séquence d’actions via les block proposés, afin de faire sortir son robot d’un labyrinthe tous en surmontant les obstacles.

Il est développer sous Unity en ECS (Entity Component System) via le plugin ![Fyfy](https://github.com/Mocahteam/FYFY).

L’ensemble des améliorations apportées se trouve dans la section de jeu « Génération niveau » (PHOTO)

## Axe d’amélioration : 

### Ajout d’un mode console :

Afin de donner un aperçue d’un langage de programmation au apprenant, nous avons intégrés une console qui permet à la fois de lire en langage python la forme des instructions pour chaque block de la séquence d’actions, mais aussi d’interagir avec elle (écrire directement l’action ou les actions dans la console).
(PHOTO)

### Création d’une modélisation de l’apprenant :

Nous avons définie via ![CbKST](http://leas-box.cognitive-science.at/cbkstfca.html) les différentes compétences du domaine présent dans le jeu (séquence, boucle, condition, négation, langage de programmation). Nous avons ensuite créer 2 vecteurs permettant de modéliser l’état de l’apprenant à la fin de chaque niveau. Une premier vecteur indique si oui ou non l’apprenant maîtrise une conséquence spécifique, le deuxième vecteur permet de déterminer qu’elle est le niveau d’apprentissage d’une compétence. 

La modélisation de l’apprenant ce trouve dans le fichier « UserModel » dans le dossier « Components ».

### Génération procédural de niveau :

Grâce à l’identification des compétences nous avons pue y associer un (ou plusieurs) éléments de gameplay. Toujours via CbKST nous avons aussi pue établir la structure de compétence et définir qu’elle était les compétences en prérequis de certaines autres.

Cela nous a permit de mettre en place des procédures de créations de niveau basées sur la modélisation de l’apprenant. Les niveaux sont générés soit pour l’apprenant travail une compétence non acquis, soit pour travailler un ensembles de compétences acquise mais mise ensemble (utilisation de boucle et de condition par exemple).
A la sélection de la génération de niveau procédural, l’apprenant peux, si il le souhaite, définir lui même les compétences qu’il veux travailler. Seul les compétences maîtrisées ainsi que les compétences à apprendre, dont le prérequis est comblé, sont disponible.

(PHOTO option + niveau aléatoire)
