# Model Trains and Locomotives

[Model Trains and Locomotives](https://www.nexusmods.com/stardewvalley/mods/43156)
adds moving model trains to decorate the farm with.

This document is mainly intended for modders. For mod users, install the mod
from the link above.

## New tracks
To add new tracks, add a new object to `Data/Objects` with the context tag `selph.ModelTrains_track`
and associated track data in `Data/FloorsAndPaths`.

## New cars
To add new cars, first add them as a new object in `Data/Objects` with the context tag
`selph.ModelTrains_locomotive` for engines, or `selph.ModelTrains_wagon` for wagons.

Next, you need to load their world sprites into `Characters/<the unqualified ID of the base item
here>`. The spritesheet's sprite size is 32x32 and is laid out similarly to an NPC's spritesheet;
you want four rows for down/right/up/left idling and moving sprites.

## New bridges and tunnels
To add new bridges and tunnels, first add them as big craftables in `Data/BigCraftables` with one of the following tags:
* `selph.ModelTrains_bridge_n` for northern entrances
* `selph.ModelTrains_bridge_s` for southern entrances
* `selph.ModelTrains_bridge_w` for western entrances
* `selph.ModelTrains_bridge_e` for eastern entrances

Optionally, add the tag `selph.ModelTrains_tunnel` to make them tunnels; this makes trains going
across them disappear aside from their shadows.

Next, set the `selph.ModelTrains_BasePath` field in the BC data's `CustomFields` dict. The value is
the path in `Data/FloorsAndPaths` to use as their base path.

Finally, you need to draw the tunnels' world sprites, which will be much larger than the BC item
sprite. There are two spritesheets you need to load:
* `<the BC item's spritesheet name>_Front` for the front layer (goes above the train)
* `<the BC item's spritesheet name>_Back` for the back layer (goes behind the train)

The sheets' tile size is 3x2 (ie. 48x32 pixels), and the sprite index is the same as the BC item's original sprite
index in its base sprite sheet. For example, if the bridge item's sprite index is 1 (ie. the second
sprite), the Front and Back layer will also use the 2nd 3x2 rectangle to draw the front and back
layer.
