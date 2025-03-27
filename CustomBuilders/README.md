# Custom Builders

[Custom Builders](https://www.nexusmods.com/stardewvalley/mods/29299) is a
Stardew Valley mod that extends the game's building data to allow for more
carpenter-like NPCs, assign buildings to multiple builders, and more.

This document is mainly intended for modders. For mod users, install the mod
from the link above.

- [Custom Builders](#custom-builders)
  + [Custom building menu](#custom-building-menu)
  + [Additional eligible builders for a building](#additional-eligible-builders-for-a-building)

## Custom building menu

This mod adds the following new tile action that opens the construction menu associated with a new NPC:

`selph.CustomBuilders_ShowConstruct <NpcName>`

Fill in `<NpcName>` with the name of the NPC that owns the menu. This will add
any buildings associated with this NPC via the `Builder` field in building
data, as well as extra buildings detailed in the section below.

Next, edit `Character/<NpcName>/Dialogue` to add the following dialogue keys:

| Dialogue Key                         |  Description              |
| ---------------------------------- | ------------------------ |
| `selph.CustomBuilders_UpgradeConstruction_Festival` | A building is upgraded, but the next day is a festival. |
| `selph.CustomBuilders_UpgradeConstruction` | A building is upgraded. |
| `selph.CustomBuilders_NewConstruction_Festival` | A new building is constructed, but the next day is a festival. |
| `selph.CustomBuilders_NewConstruction` | A new building is constructed. |
| `selph.CustomBuilders_Instant` | An instant build building is constructed. |
| `selph.CustomBuilders_Busy` | The NPC is currently building a building. |

All of the above dialogue lines can have the following strings that will automatically be substituted:
| Macros                         |  Description             |
| ------------------------------ | ------------------------ |
| `{0}` |  The building's `Name` in lowercase. |
| `{1}` |  The building's `NameForGeneralType` in lowercase. |
| `{2}` |  The building's `Name`. |
| `{3}` |  The building's `NameForGeneralType`. |

The NPC builders will have the following notes/restrictions:

* They can only build one building at a time. (TODO: Add ability to make them
  build more?)
* If there's a building in progress, they will teleport to your farm and play
  an animation next to/inside it like Robin.
* They do not work on festival days.

### Custom building animations

By default, the NPC uses frames 23 to 27 for their hammer smashing animation
(see Robin's sprite sheet). It's recommended to go with the default, but if you
want to make them use different sprites, set the following field in their NPC
data's `CustomFields` dict:

| Key                         |  Description             |
| ------------------------------ | ------------------------ |
| `selph.CustomBuilders_ConstructAnimationIdleIndex1` | The index of the first idle sprite to use. |
| `selph.CustomBuilders_ConstructAnimationIdleIndex2` | The index of the second idle sprite to use. |
| `selph.CustomBuilders_ConstructAnimationHammerIndex` | The index of the first hammer animation sprite. Three sprites will be used.|

All three must either be set, or not be set.

## Additional eligible builders for a building

By default, a building can only be associated with one builder. To allow for
multiple builders for one building (e.g. allow your NPC to also construct Sheds
even though Shed is associated with Robin), add the following key to the
building's `CustomFields` field:

`selph.CustomBuilders_ExtraBuilder_<Put Anything Here As Long As It's Unique, Ideally Containing Your Mod ID>`

The value will be the NPC's internal name. You can specify multiple eligible builders by adding multiple entries.

Additionally, the mod adds the following GSQ that checks the current builder menu:

`selph.CustomBuilders_IS_BUILDER <NpcName>`

This GSQ only works in Data/Buildings, and its main use case is limiting a
specific building skin to a builder NPC.

### Override the building cost for a builder

Set the following fields in the building's CustomFields, replacing `<BuilderName>` as needed:

| Key                         |  Description             |
| ------------------------------ | ------------------------ |
| `selph.CustomBuilders_BuildCostFor_<BuilderName>` | The build cost that should be used for this building instead of the base cost for the specified builder. |
| `selph.CustomBuilders_BuildDaysFor_<BuilderName>` | Similar, but for build days.|
| `selph.CustomBuilders_BuildMaterialsFor_<BuilderName>` | Similar, but for build materials. This is a space delimited string of item ids followed by quantity (e.g. `"388 100 390 50"` for 100 Wood and 50 Stone).|
To override skins, append `_<SkinName>` to the above keys.
