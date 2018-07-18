# SweetHome

**SweetHome** est un simple utilitaire permettant de gagner du temps lors de sa recherche de location d'appartements.

## Principe

Le principe est de :
- Récupérer les dernières offres d'appartement correspondant à vos critères sur les différents sites disponibles
	> De base, SweetHome supporte **Le bon coin**, **Foncia**, **Seloger .com**, **ParuVendu** et **LogicImmo**

- Vérifier chaque offre en décidant si vous êtes intéressés ou non, puis cliquer sur le bouton "Done" une fois que vous avez traité l'offre. 
- Chaque offre validée sera alors sauvegardée et **n'apparaitra plus** lors des prochaines recherches.
	> Ainsi, il vous suffit de lancer l'application tous les jours et de regarder les offres proposées. Vous aurez l'assurance que celles-ci soient de **nouvelles offres que vous n'avez pas encore regardé**, ce qui vous permet de gagner du temps sans avoir à rechercher/vous souvenir quelles offres vous avez déjà vu ou non.

## Installation et configuration

Rendez-vous sur le site de votre choix et effectuez votre recherche avec les critères voulus. Copiez l'adresse URL de votre navigateur.
> Exemple : pour le bon coin, mon adresse était du type https://www.leboncoin.fr/recherche/?category=10&region=22&cities=Villeurbanne_69100,Lyon_69003&furnished=1&real_estate_type=2,1&price=300-800&rooms=2-max&square=25-max

Allez dans le dossier bin/Debug de SweetHome, ouvrez le fichier "conf.ini" avec un éditeur de texte (bloc-notes) et collez cette adresse à côté du signe "=" correspondant au site sur lequel vous êtes.
> Exemple pour le bon coin, mon fichier conf.ini contiendrait :
> 
    [Links]
    SeLoger=
    LogicImmo=
    Foncia=
    ParuVendu=
    LeBonCoin=https://www.leboncoin.fr/recherche/?category=10&region=22&cities=Villeurbanne_69100,Lyon_69003&furnished=1&real_estate_type=2,1&price=300-800&rooms=2-max&square=25-max

Lancez SweetHome.exe

## Utilisation

Cliquez sur la loupe pour lancer la recherche. Des offres d'appartement devraient apparaître.
Vous pouvez alors cliquez sur le symbole loupe à côté de chacune des offres pour les ouvrir dans votre navigateur (ou en faisant un copier/coller du lien). Une fois que vous avez traité l'offre selon vos envies, cliquez sur le bouton "Done".
Celle-ci n'apparaitra plus lors de vos prochaines recherches, vous évitant ainsi de perdre du temps à retrouver uniquement les nouvelles annonces.
> Lors de la première recherche, vous aurez bien évidemment beaucoup d'annonces à traiter. Par la suite, seules les nouvelles annonces apparaitront et vous n'aurez plus qu'à regarder une dizaine d'annonces par jour !
