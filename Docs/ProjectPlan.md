# Green Prince — Project Plan

## Vision

Form a band to explore the land around your camp as you chart a safe passage to new worlds. You and your camp must build up knowledge of your surroundings until you can safely migrate your camp to another site.

This increased knowledge is represented by limited persistent elements in the map, persistent changes to map tiles, and upgrades to your characters that will modify future challenges

## Controls and core loop

Players start at their camp and select their party. They choose who will go on the adventure and who will stay at camp making sure to rotate who they take so the camp resources stay balanced. They walk through the wilderness choosing between adjacent tiles on the map which are drawn from the adventure deck. They also collect camp resources, overcome peristent challenges, and finding persistent landmarks.

After calling an end to the run or losing all of their food they trudge back to base to regroup and start out on a new adventure with a new set of adventurers.

After overcoming persistent challenges and identifying a new landmark, they must pack up camp travel across the map in a high risk caravan of all their supplies to progress permenantly.

---

v0.1 - explore tiles filled by cards
- [ ] Deck filled with empty, green, red, and blue cards with numbers
- [ ] Tiled map 7 high and 20 wide full of dark squares initially
- [ ] Adventurers start at camp in the middle left tile and move in cardinal directions
- [ ] Fill in empty squares adjacent to adventurers from the deck

v0.2 - resources
- [ ] Add food, force, and tools as resources starting at 10, 5, 4
- [ ] Cooresponding tiles cost those resources based on their value
- [ ] Every n tiles consume one food (n=5 for base party)
- [ ] Create resource UI
- [ ] Add failure state when a resource runs out

v0.3 - party
- [ ] Select party member screen before adventure
- [ ] 4 party members with abilities that affect drawn tiles (reduce damage by one, find food in green tiles)
- [ ] Placed tiles get modified by party before showing up on map

v0.4 - camp
- [ ] Adventurers left at camp modify resource gain at camp 
- [ ] Collect camp resources as you explore
- [ ] Camp resources improve adventurers and modify map tiles
