# NemesesGame
A turn-based Telegram game

BUGS
* Max Level (solution: return GetLangString("MaxLevel"), and return to main menu (AskAction))
* PayCost(): currentResources doesn't cut
* Multiple BroadcastCityStatus(output)

TODO 
* Add exception catcher on PayCost (cost - 0)
* choosename limit length

* Avoid truncating (limit Button string to 25-30 chars)
* choosename cannot be null
* City.cs --> erase 'city' on cityResources & cityArmytel

MVP Target:

1. Player's resources & army
2. Player can upgrade resources
3. Player can atk & raise army

Additional Features:

1. Merchant
2. Tech
3. Ult Tech
4. Settings (need sql)
5. Skills (for each city)

Idea:
1. Ult Tech effect --> become fucking imba (He must beat all opponents in X turns to win, unless then, the other win)