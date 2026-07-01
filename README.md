# DCCG

DCCG was developed as a project for the Web, Mobile and Cloud Software Project subject at Universidade da Beira Interior (UBI).<br>
This project provides a collectible card game where the player has two factions (Enchanter and Monster Slayer), four decks, being two of them for each faction and each faction has access to one unique avatar that has an avatar power and a card ability.

### Core Features

This application has some core features that are important to mention since they are decisive in the area o DCCGs.

#### Cards

- Essence Cost
- Effect that the system resolves
- Status for attack and vigor that are updated based on the situation
- Name
- Art
- Border
- Rarity
- Faction

#### Turn System and Essence

- Each turn a player gets an additional one point of essence until each player reaches ten points.
- Whenever a player plays a card it reduces the actual essence points.
- Validates if a card can be played based on its essence cost.
- Operates end of turn triggers in certain cards

#### Avatar

- Updates the life of a player being thirty health points the maximum amount a player can reach.
- Avatar Power using two essence Points
- Uses seven will points to activate the avatar reincarnation, summoning the card version of the players' avatar to the field.

#### Will

- A player gains points whenever that player destroys an opponent's creature during their turn or whenever they deal damage to the opponent




