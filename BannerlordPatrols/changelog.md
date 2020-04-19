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