TODO Version 1.3.7:
- Add option for AI lords to buy patrols at a certain player clan tier.
- 

Version 1.3.6:
- Fixed patrols not engaging parties. 

Version 1.3.5:
- Added option for patrols to be made of mercenaries.
- Added text to patrol menu if player has no money to buy any patrols.
- Changed options relating to patrol troop type to a drop down.
- Changed remove dupe lord behavior. No longer removes on load. Only removes on daily tick.
- Reverted to old way of 'Disband All' due to change in removal options. Deletes them immediately to prevent abuse of stockpiling garrisons.
- Fixed mod option ordering.
- Removal options now end battles before deleting to prevent crashing. This should remove all patrols/duplicates at once now.
- Removed some unnecessary game restarts for mod options.

Version 1.3.4:
- Updated to Mod Configuration Menu v2.0.10
- Added options for AI to toggle hiring for village and walled settlements.

Version 1.3.3:
- Updated to Mod Configuration Menu v2.0.8 (formally MBOptionScreen)

Version 1.3.2:
- Updated to MBOptionScreen v2.0.6

Version 1.3.1:
- Updated to MBOptionScreen v2.0.4
- AI is now also limited to hiring patrols based on their clan tier.
- Added AI additional patrol limit config setting.

Version 1.3.0:
- Updated for beta branch e1.3.0
- Tested on beta branch e1.3.0 (I do not know if it is compatible with stable branch e1.2.0 since they changed things internally. Previous mod versions should be fine with e1.2.0)

Version 1.2.8:
- Fixed patrol tether range

Version 1.2.7:
- Fixed patrol wages not showing up in hint box (this won't work if using a different language at the moment)

Version 1.2.6:
- Fixed translation ids.

Version 1.2.5:
- Fixed stuttering issue.

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