# Carpentry Catalogue and Blacksmith Bulletin

[Carpentry Catalogue and Blacksmith Bulletin](https://www.nexusmods.com/stardewvalley/mods/32864) (aka
Custom Builders) is a Stardew Valley mod that
* Extends the game's building data to allow for more carpenter-like NPCs, assign buildings to
multiple builders, and more.
* Allows blacksmith-inspired shops (buy/upgrade an item, not just tools, get it some days later)

This document is mainly intended for modders. For mod users, install the mod
from the link above.

- [Custom Carpenters](#custom-carpenters)
  + [Custom building menu](#custom-building-menu)
  + [Additional eligible builders for a building](#additional-eligible-builders-for-a-building)
  + [Allow upgrades to be directly built with custom costs](#allow-upgrades-to-be-directly-built-with-custom-costs)
- [Custom Blacksmiths](#custom-blacksmiths)

## Custom Carpenters

### Custom building menu

This mod adds the following new tile action that opens the construction menu associated with a new NPC:

`selph.CustomBuilders_ShowConstruct <NpcName> [from direction] [open time] [close time] [owner tile area]`

Fill in `<NpcName>` with the name of the NPC that owns the menu. This will add
any buildings associated with this NPC via the `Builder` field in building
data, as well as extra buildings detailed in the section below. All other
fields are optional, and act identically to the fields in `OpenShop` (see [wiki
for reference](https://stardewvalleywiki.com/Modding:Maps#Action)).

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

* They can only build one building at a time.
* If there's a building in progress, they will teleport to your farm and play
  an animation next to/inside it like Robin.
* They do not work on festival days.

#### Custom building animations

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

### Additional eligible builders for a building

By default, a building can only be associated with one builder. To allow for
multiple builders for one building (e.g. allow your NPC to also construct Sheds
even though Shed is associated with Robin), there are two methods:

#### Add an extra building for an NPC
Add the following key to the building's `CustomFields` field:

`selph.CustomBuilders_ExtraBuilder_<Put Anything Here As Long As It's Unique, Ideally Containing Your Mod ID>`

The value will be the NPC's internal name. You can specify multiple eligible builders by adding multiple entries.

#### Inherit all buildings from another NPC
Add the following key to the custom builder NPC's `CustomFields` field:

`selph.CustomBuilders_InheritBuilder_<Put Anything Here As Long As It's Unique, Ideally Containing Your Mod ID>`

The value is the other builder to inherit from (e.g. `Robin` or `Wizard`).

Additionally, the mod adds the following GSQ that checks the current builder menu:

`selph.CustomBuilders_IS_BUILDER <NpcName>`

This GSQ only works in Data/Buildings, and its main use case is limiting a
specific building skin to a builder NPC.

#### Override the building cost for a builder

Set the following fields in the building's CustomFields, replacing `<BuilderName>` as needed:

| Key                         |  Description             |
| ------------------------------ | ------------------------ |
| `selph.CustomBuilders_BuildCostFor_<BuilderName>` | The build cost that should be used for this building instead of the base cost for the specified builder. |
| `selph.CustomBuilders_BuildDaysFor_<BuilderName>` | Similar, but for build days.|
| `selph.CustomBuilders_BuildMaterialsFor_<BuilderName>` | Similar, but for build materials. This is a space delimited string of item ids followed by quantity (e.g. `"388 100 390 50"` for 100 Wood and 50 Stone).|

To override skins, append `_<SkinName>` to the above keys.

To set the cost for an upgrade that's being directly built from scratch, append `_ForDirectBuild` to the above keys (after the skins part if needed).

#### Allow upgrades to be directly built with custom costs

While not directly related to custom carpenters, this mod allows specifying upgrades to be built
directly from scratch, bypassing the base building with a custom cost. To do this, add
`selph.CustomBuilders_CanBeDirectBuild` to `CustomFields`. Once done, a new button will appear in
the build menu that allows toggling between upgrade mode and direct build mode for a specific
building recipe.

To specify a different cost for direct build as compared to upgrades, set the following keys:

| Key                         |  Description             |
| ------------------------------ | ------------------------ |
| `selph.CustomBuilders_BuildCostForDirectBuild` | The build cost that should be used for building this building instead of the base cost. |
| `selph.CustomBuilders_BuildDaysForDirectBuild` | Similar, but for build days.|
| `selph.CustomBuilders_BuildMaterialsForDirectBuild` | Similar, but for build materials. This is a space delimited string of item ids followed by quantity (e.g. `"388 100 390 50"` for 100 Wood and 50 Stone).|

To override skins, append `_<SkinName>` to the above keys. To override these values for a custom builder, see above.

## Custom Blacksmiths

(NOTE: Despite the name, this feature can also be used for generic time-delayed shops where you
order stuff that's ready a couple days later)

First, adds the following new tile action that opens a special shop menu with special handling:

`selph.CustomBuilders_OpenBlacksmithShop <ShopId> [from direction] [open time] [close time] [owner tile area]`

Next, define a shop with the specified `ShopId` in `Data/Shops`, with
`"selph.CustomBuilders_IsCustomToolShop": "true"` in its `CustomFields` dictionary.

The items being sold in the shop should have the following fields in the `ModData` dictionary:

| Key                         |  Description             |
| ------------------------------ | ------------------------ |
| `selph.CustomBuilders_ReadyDay` | The days it takes to process this item before it's ready. Vanilla tools need 2.|
| `selph.CustomBuilders_RequireToolId` | *(Optional)* If this entry is a tool/weapon, the tool/weapon to upgrade it from instead of buying it blank. When this item is bought, the existing tool/weapon in the player inventory is consumed alongside money and the trade item (e.g. 5 Iridium Bars), and any enchantments/attachments are transferred over.<br>Optionally, you can also set the `Condition` field to be `PLAYER_HAS_ITEM Current ToolIdHere` so the entry only shows up when the player has the base tool/weapon in their inventory.|

Once set up correctly, the shop should have the following changes:
* Once you bought any item, the shop will become unavailable as the blacksmith processes it. Interacting with them (if the owner is present) shows a dialogue line defined below.
* When the item is ready, you'll get the item when interacting with the tile. You'll also get a daily notification

Next, edit `Character/<NpcName>/Dialogue` to add the following dialogue keys:

| Dialogue Key                         |  Description              |
| ---------------------------------- | ------------------------ |
| `selph.CustomBuilders_Blacksmith_Bought` | You just ordered the item. |
| `selph.CustomBuilders_Blacksmith_Bought_OneDay` | You just bought the item, and it's ready the next day. |
| `selph.CustomBuilders_Blacksmith_Busy` | They're still processing the tool. |
| `selph.CustomBuilders_Blacksmith_Busy_OneDay` | They're still processing the tool (one day left). |

All of the above dialogue lines can have the following strings that will automatically be substituted:
| Macros                         |  Description             |
| ------------------------------ | ------------------------ |
| `{0}` |  The item's display name. |
| `{1}` |  The number of days remaining. |

See the below for an example of a shop that allows upgrading a regular pickaxe to an iridium axe,
or a galaxy sword to an infinity blade, or 20 preserves jar, each requiring 2 prismatic shards:

<details>

<summary>Content Patcher definition</summary>

```
{
  "LogName": "Custom Crafting Shop",
  "Action": "EditData",
  "Target": "Data/Shops",
  "Entries": {
  "TestBlacksmithShoppe": {
    "Items": [
      {
        "Id": "IridiumPickaxe",
        "ItemId": "(T)IridiumPickaxe",
        "Price": 50000,
        "TradeItemId": "(O)74",
        "TradeItemAmount": 2,
        "ModData": {
          "selph.CustomBuilders_ReadyDay": "2",
          "selph.CustomBuilders_RequireToolId": "(T)Pickaxe",
        },
      },
      {
        "Id": "InfinitySword",
        "ItemId": "(W)62",
        "Price": 50000,
        "TradeItemId": "(O)74",
        "TradeItemAmount": 2,
        "ModData": {
          "selph.CustomBuilders_ReadyDay": "2",
          "selph.CustomBuilders_RequireToolId": "(W)4",
        },
      },
      {
        "Id": "Jars",
        "ItemId": "(BC)15",
        "MinStack": 20,
        "Price": 20000,
        "ModData": {
          "selph.CustomBuilders_ReadyDay": "2",
        },
      },
    ],
    "CustomFields": {
      "selph.CustomBuilders_IsCustomToolShop": "true",
    },
  },
  },
},
```
</details>
