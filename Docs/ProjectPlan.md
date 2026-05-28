# Green Prince — Project Plan

## Vision

Form a band to explore the land around your camp as you chart a safe passage to new worlds. You and your camp must build up knowledge of your surroundings until you can safely migrate your camp to another site.

This increased knowledge is represented by limited persistent elements in the map, persistent changes to map tiles, and upgrades to your characters that will modify future challenges

## Controls and core loop

Players start at their camp and select their party. They choose who will go on the adventure and who will stay at camp making sure to rotate who they take so the camp resources stay balanced. They walk through the wilderness choosing between adjacent tiles on the map which are drawn from the adventure deck. They also collect camp resources, overcome peristent challenges, and finding persistent landmarks.

After calling an end to the run or losing all of their food they trudge back to base to regroup and start out on a new adventure with a new set of adventurers.

After overcoming persistent challenges and identifying a new landmark, they must pack up camp travel across the map in a high risk caravan of all their supplies to progress permenantly.

## Resources

Adventurers start their journey with food, force, and tools as core resources and will collect camp resources:
technology: aligned with force and tools, improves camp capabilities
experience: aligned with food and force, improves adventurers
lore: aligned with food and tools, improves nature tiles

---

v0.1 - explore tiles filled by cards
- [x] Deck filled with green, red, and blue cards with numbers
- [x] Tiled map 7 high and 20 wide full of dark squares initially
- [x] Adventurers start at camp in the middle left tile and move in cardinal directions
- [x] Fill in empty squares adjacent to adventurers from the deck

v0.2 - resources
- [x] Add food, force, and tools as resources starting at 10, 5, 4
- [x] Cooresponding tiles cost those resources based on their value
- [x] Every n tiles consume one food (n=5 for base party)
- [x] Create resource UI
- [x] Add failure state when a resource runs out

v0.3 - persistence
- [ ] Add an underlaying terrain board that is populated on game start
- [ ] Implement camp resources, for now these only show up when you click the camp in a pop up
- [ ] Add 2 landmarks somewhere 6-9 and 12-15 squares out from camp
- [ ] Separate handling of revealing the environment and challenges, previously explored tiles should have a lighter fog that shows the underlying terrain or landmark but show clearly that you haven't adventured there yet
- [ ] Add a quit option in the pause menu to fully restart the game resetting persist elements

v0.4 - party
- [ ] Select party member screen before adventure
- [ ] 4 party members with abilities that affect drawn tiles (reduce damage by one, find food in green tiles)
- [ ] Placed tiles get modified by party before showing up on map

v0.5 - camp
- [ ] Adventurers left at camp modify resource gain at camp 
- [ ] Collect camp resources as you explore
- [ ] Camp resources improve adventurers and modify map tiles
