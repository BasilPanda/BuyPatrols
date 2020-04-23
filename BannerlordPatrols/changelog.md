Version 1.2.4:
- Added translation template for mod options.

Version 1.2.3:
- Added languages folder for translations.

**Version 1.2.2**																																											
- Due to duplication bug, patrols no longer store prisoners in settlements and instead sell them off automatically when they go to a settlement and take the money themselves. (This should fix duplicating lords)
- Duplication removal now runs daily and should keep the original lord alive to prevent crashing.

**Version 1.2.1**
- Possible fix to duplicating lords (Needs more testing)
- Added option to delete duplicate lords on load (Needs more testing. From personal testing, if I do it hourly/daily it will crash the game 50% of the time. Lords will not be deleted if they're fighting)

**Version 1.2.0**
- Fixed player patrol count not changing properly when giving fiefs/receiving fiefs with patrols (Patrols banners currently stay the to whatever faction owner's banner is. I'll look into this further next patch.)
- Fixed player patrol count not increasing after using remove all patrols option
- Fixed player patrol count not increasing after personally disbanding a patrol
- Unknown behavior patrols should now start patrolling after a day (This should fix most of the freezing patrols. This doesn't fix patrols that were unknown and disbanded via disband all)
- Added warning notification when you go over total patrols when capturing a settlement that has patrols 
- Added militia option for patrols to be made up of militia instead of normal troops (only affects new patrols)

**Version 1.1.10**
- Added castles and towns to AI generation plus related config
- Fixed menu options not showing up for attacking/being attacked by enemy patrols

**Version 1.1.9**
- Imprisoned lords should no longer duplicate.

**Version 1.1.8**
- Updated MBOptionScreen to 1.1.14 so mod options in game should work again.
- Added town patrols plus related config. For towns: Go to the keep -> Manage patrols
- Added a total patrol cap for players. Default = 12
- Added basic dialog for enemy and neutral patrols
- No longer need to restart game if you hit done and it doesn't tell you to restart. Settings are loaded upon save load. They will not update if you are currently in the ingame map.
- Fixed issue where menus failed to generate
- Fixed crash issue involving targeted parties
- Fixed castle cost issue when prosperity was 0
- Fixed patrols attacking caravans and villagers despite the option being off
- Patrols should not continue attacking lords/players after they switch allegiance
- Patrols should not attack parties with 0 healthy troops 
- Player will now lose relations and go to war if they attack neutral patrols. -5 relation only at the moment.
- Notification when a patrol is lost at the bottom left. Has a toggle option. Default = true
- Lowered the min to 0 for max patrol caps.


KNOWN ISSUE:
- If you're playing with an old save, the total patrol cap does not count patrols hired before this version. I'm not going to fix this and old patrols should eventually die out and be replaced by new ones.