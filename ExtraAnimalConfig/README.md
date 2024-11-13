# Extra Animal Configuration Framework

[Extra Animal Config](https://www.nexusmods.com/stardewvalley/mods/25328)
is a Stardew Valley mod that adds extra functionalities to Content Patcher
farm animals recipe definitions.

This document is for modders looking to incorporate this mod into their own
content packs. For users, install the mod as usual from the link above.

## Table of Contents
* [Table of Contents](#table-of-contents)
* [Animal data asset](#animal-data-asset)
   + [Setting up animals that eat alternate feed](#setting-up-animals-that-eat-alternate-feed)
   + [Extra drops from scything grass](#extra-drops-from-scything-grass)
* [Multiple possible animals from one egg](#multiple-possible-animals-from-one-egg)
* [Game State Queries](#game-state-queries)
* [Building CustomFields](#building-customfields)
* [Examples](#examples)
* [Known Issues/Future Content Roadmap](#known-issuesfuture-content-roadmap)

## Animal data asset

This mod adds a new asset `selph.ExtraAnimalConfig/AnimalExtensionData`, which
is a dictionary where the key is the animal ID similar to the key of
[Data/FarmAnimals](https://stardewvalleywiki.com/Modding:Animal_data), and the
value a model with the fields detailed below.

IMPORTANT: This asset will be pre-populated with every animal in game, with
every values set to default. To ensure compatibility with other mods do not add
new entries to the asset (ie. use Content Patcher's `Entries` without
`TargetField`); instead, always use either `Fields`, or `Entries` with
[`TargetField`](https://github.com/Pathoschild/StardewMods/blob/stable/ContentPatcher/docs/author-guide/action-editdata.md#target-field)
to edit it.

| Field Name                         | Type             | Description              |
| ---------------------------------- | ---------------- | ------------------------ |
| MalePercentage          | `float`            | The percentage of this species that will be male. Currently this only affects Marnie's message after buying the animal.|
| FeedItemId              | `string`           | The *qualified* item ID of this animal's food item instead of hay. If set, the animal will need to eat this item in addition to grass and hay if `GrassEatAmount` > 0, or in place of grass and hay if `GrassEatAmount` is 0. If this field is not set and `GrassEatAmount` is 0, then the animal doesn't need to eat.<br> See below for a full guide.|
| AnimalProduceExtensionData| `Dictionary<string, AnimalProduceExtensionData>` | A map of *qualified* item IDs corresponding to an (unqualified) entry in `(Deluxe)ProduceItemIds` to its extra settings. This is used to store extra settings associated with an animal produce; see below for more info.|
| AnimalSpawnList          | `List<AnimalSpawnData>`   | A list of animal spawn data objects to determine which new animal to add when this animal gets pregnant and gives birth.<br>**NOTE**: The list will be evaluated in order, from top to bottom. Make sure the last entry is always true/has no condition. Also because of this, probability won't work as you initially expect. If you want 3 animals each with the same chance for example, the first one needs a 0.333 probability, the second one needs 0.5, and the third one 1.|
| OutsideForager          | `bool` | Whether to make this animal an outside forager: if let outside they can forage from dirt and not consume any grass, though they'd still get a happiness bonus from eating inside a tuft of (blue) grass.<br>NOTES:<br>* If their `GrassEatAmount` is larger than 0, they would eat hay when inside. They still won't consume grass when outside though.<br>* This settings' main purpose is to emulate how chickens work in Harvest Moon.|
| ExtraProduceSpawnList          | `List<ExtraProduceSpawnData>`   | A list of extra produce slots to determine which additional produce aside from the primary produce this animal can make.|
| ExtraHouses           | `List<string>` | A list of extra houses that can house this animal aside from the primary one defined in its animal data.|
| IgnoreRain | `bool` | Whether this animal can go out in rain. You can also override this on a per-building basis (see below).|
| IgnoreWinter | `bool` | Whether this animal can go out in winter. You can also override this on a per-building basis (see below).|
| GlowColor | `string` | If set to a [color](https://stardewvalleywiki.com/Modding:Common_data_field_types#Color), this animal will glow with said color like a light source. KNOWN ISSUE: `patch reload`-ing your mod will only update lights when the animal switches location (ie. when it goes out to eat or goes back in).|
| GlowRadius | `float` | The radius of the light glow, if color is set above. 10 = 1 tile.|
| SpeedOverride | `int` | This animal's speed, instead of the default 2.|
| IsAttackAnimal | `bool` | Whether this animal will attempt to attack the farmer by chasing them and dealing damage on contact.|
| AttackDamage | `int` | The amount of damage dealt (default 1).|
| AttackIntervalMs | `int` | The minimum interval between attacks in milliseconds (default 5000 aka 5 seconds).|
| AttackRange | `int` | The minimum range chasing range (default 10 tiles).|
| AttackMaxChaseTimeMs | `int` | How long to chase before the animal gets bored (default 10000 aka 10 seconds).|


`AnimalSpawnData` is a model with the following fields:

| Field Name                         | Type             | Description              |
| ---------------------------------- | ---------------- | ------------------------ |
| Id          | `string`            | The unique Id for this model within the current list.|
| AnimalId    | `string`            | The animal to spawn.<br>KNOWN ISSUE: The overnight pop up (X has given birth to a Y) will still show the parent species in Y. This is very hardcoded and difficult to untangle, so don't expect this to be fixed soon unfortunately.|
| Condition   | `string`            | A [game state query](https://stardewvalleywiki.com/Modding:Game_state_queries) determining whether this animal should be spawned. The `Target` location will refer to the animal house that belongs to the animal/incubator being checked.|

`AnimalProduceExtensionData` is a model with the following fields:

| Field Name                         | Type             | Description              |
| ---------------------------------- | ---------------- | ------------------------ |
| ItemQuery          | [`GenericSpawnItemData`](https://stardewvalleywiki.com/Modding:Item_queries)  | The override item query to use to actually generate this produce. Most item query fields will work, aside from quality (which is determined by friendship) and stack size (which will always be 1, or 2 for animals that were fed Golden Crackers).|
| HarvestTool        | `string`  | The harvest tool/method to use for this produce instead of its default method. Supports `DropOvernight`, `Milk Pail`, `Shears`, `DigUp` and a new value `Debris`, which makes the produce drop overnight but as a debris item.<br>**IMPORTANT NOTES**:<br>* Debris produce do not grant experience when collected, and may disappear the next day if not collected (manually or with an autograbber). <br>* During testing, if you change an animal produce's harvest method to `DropOvernight` mid-save, any produce lodged inside it will not drop on its own. The mod includes a fallback for this, allowing the produce to be removed by milk pail or shears. Afterwards, it should not happen again.|
| ProduceTexture     | `string`  | The animal's texture asset to override the default texture if it currently has this produce. |
| SkinProduceTexture | `Dictionary<string, string>` | Same as the above, but for non-default textures (which is the key, with the asset being the value).|

`ExtraProduceSpawnData` is a model with the following fields:

| Field Name                         | Type             | Description              |
| ---------------------------------- | ---------------- | ------------------------ |
| Id          | `string`| The ID of this entry. Must be unique.|
| ProduceItemIds        | `List<ProduceData>`  | A list of possible produces that can fill this 'slot'. The first eligible item in the list will be picked.|
| DaysToProduce          | `int`| How many days until this slot is filled. Defaults to 1, which makes it produce every day. |
| SyncWithMainProduce          | `bool`| Whether to only produce in this slot if the animal's scheduled to make its main produce that day. Defaults to false. Set this for produce that is made at the same speed, or 2x/3x slower than the main produce to sync its schedule and reduce confusion.|

`ProduceData` is a model with the following fields:

| Field Name                         | Type             | Description              |
| ---------------------------------- | ---------------- | ------------------------ |
| Id          | `string`| The ID of this entry. Must be unique.|
| ItemId          | `string`| The *unqualified* item ID of the produce item. This field can integrate with the parent animal's `AnimalProduceExtensionData` field.|
| Condition          | `string`| If set, the condition for this entry.|
| MinimumFriendship          | `int`| If set, the minimum amount of friendship needed before the animal will start making this produce. |

NOTE: To define the secondary produce's harvest method, set them in `AnimalProduceExtensionData` using their qualified item ID as the key, just like how you'd do it with the primary produce.

### Setting up animals that eat alternate feed

1. Optional: In base animal data, set `GrassEatAmount` to `0` so the animal
   doesn't eat grass and hay. And/or set `OutsideForager` to `true` if you want the
   animal to forage in dirt when outside.
2. In the mod asset, set `FeedItemId` as specified above.
3. In the map for the building that will house this animal, add custom feeding
   trough tiles in the `Back` layer with the
   `selph.ExtraAnimalConfig.CustomTrough` property set to the qualified item ID
   of the feed item to be placeable/autoplaceable with an autofeeder (compared to
   vanilla hay trough which has the `Trough` property).
4. To add silo/hopper functionality for your new feed, add the
   following [custom
   actions](https://stardewvalleywiki.com/Modding:Maps#Custom_Actions) to your
   desired tiles on the building exterior or interior (replace `(O)ItemId1` with
   the actual qualified ID of your item):
   * `selph.ExtraAnimalConfig.CustomFeedSilo (O)ItemId1`: Put the currently
     held feed into storage, or show the current feed capacity. The item ID is
     *optional*; if not set it will accept all feed item that can be stored in
     the building (including vanilla hay).
   * If you're adding modded feed capacity to the vanilla silo (see below),
     make sure to also convert its default building action to using this action 
   * `selph.ExtraAnimalConfig.CustomFeedHopper (O)ItemId1`: Put into, or take
     the specified feed out of storage. Usable only inside an animal building.
     As of 1.3.0 this tile action is optional - the default hay hopper will
     also work with modded feed.\
   Then, set the following field on your building's `CustomFields` map to
   specify the feed capacity provided by this building:
   * key : `selph.ExtraAnimalConfig.SiloCapacity.(O)ItemId1`
   * value: an integer (as a string). For example, `"100"` for 100 items.

Important notes/current limitations:
* Autofeeder will draw from silos in every location, but the feed hopper will
  only draw from the current location. This is actually how vanilla hay
  works, but doesn't come into play unless modded farm locations are used.
* Animal that can also eat grass will still prefer fresh grass, and won't get
  full happiness from eating their modded feed. If they are not a grass eater,
  they will get full happiness from their modded feed.

### Extra drops from scything grass

You can define extra drops from scything grass by writing to the
asset `selph.ExtraAnimalConfig/GrassDropExtensionData`, which is a map of
*qualified* item IDs to a model that has the following fields:

| Field Name                         | Type             | Description              |
| ---------------------------------- | ---------------- | ------------------------ |
| BaseChance | `float` | The base chance to yield this item when scything a tuft of grass. If there are silos to add this item to, it will automatically be added. For reference, hay's drop chance is 0.5.<br>Similar to hay, this is multiplied by 1.5 if using the Golden Scythe, and 2 if using the Iridium Scythe. During winter, this is multiplied by 0.33. |
| EnterInventoryIfSilosFull | `bool` | If there are no silos to add this item to, whether to try adding to the farmer inventory. If you want this for hay, there are mods on Nexus for that functionality. |

NOTE: This is compatible with [Scythe Tool Enchantments](https://www.nexusmods.com/stardewvalley/mods/26217)!

## Multiple possible animals from one egg

You can define multiple possible hatches from an egg with the asset
`selph.ExtraAnimalConfig/EggExtensionData`, which is a map of qualified
item IDs to a model that currently only has the following field:

| Field Name                         | Type             | Description              |
| ---------------------------------- | ---------------- | ------------------------ |
| AnimalSpawnList | `List<AnimalSpawnData>`  | A list of animal spawn data to use instead, which will be evaluated in order. See above for details on the `AnimalSpawnData` field.<br>**NOTE**:<br>* The list will be evaluated in order, from top to bottom. Make sure the last entry is always true/has no condition. Also because of this, probability won't work as you initially expect. If you want 3 animals each with the same chance for example, the first one needs a 0.333 probability, the second one needs 0.5, and the third one 1.<br>* You still need to set this egg item as the valid hatch item for at least one animal.<br>* Any conditions will be evaluated when the animal becomes ready for hatching, not when the egg is placed into the incubator. This is only really relevant for time-based conditions.|

## Game state queries
Version 1.2.0 introduces the following Game State Queries:

| GSQ                          |  Description              |
| ---------------------------  | ------------------------ |
| `selph.ExtraAnimalConfig_ANIMAL_HOUSE_COUNT <location> <animal type> <min friendship> [min count] [max count]` | Whether the specified location (in practice only `Here` or `Target` works reliably) is an animal house and is the home of the specified animal type (or any animal if set to `ANY`) with a count between min (0 if not specified) and max (no limit if not specified), with friendship above the specified amount (or 0 if not specified).|
| `selph.ExtraAnimalConfig_ANIMAL_COUNT <animal type> <min friendship> [min count] [max count]` | Same as above, but checks every owned animals globally.|

Additionally, this mod enhances the `Condition` field in the animal data's
`(Deluxe)ProduceItemIds` field by passing a golden animal cracker item into the
`Input` argument if the animal has consumed a golden animal cracker. This
allows specifying produce that the animal will only make if it has been fed a
golden animal cracker. This also works for the condition in the `ProduceData`
object above.

## Building CustomFields

You can set the following fields on a building data's `CustomFields` field:

| Field Name                          |  Description              |
| ---------------------------  | ------------------------ |
| `selph.ExtraAnimalConfig.BuildingFeedOverrideId` | If set to a qualified item ID, the building's hay troughs/hoppers will be modified to accept that item instead of hay, and any animals living inside that building that usually eat hay will instead eat that item.<br>Use this field *only* if you want to override a vanilla building, and building animals, to eat a certain feed type and don't want to add compat to every other animal and animal house interior mods. Otherwise, follow the guide above for custom feed.|
| `selph.ExtraAnimalConfig.InhabitantsIgnoreRain` | If set to any value, this building type's inhabitants can go out in rainy days.|
| `selph.ExtraAnimalConfig.InhabitantsIgnoreWinter` | If set to any value, this building type's inhabitants can go out in winter.|

## Examples

<details>

<summary>Blue chicken have a 25% chance to hatch from large white eggs</summary>

```
{
    "Changes": [
    {
        "LogName": "Modify Egg Hatch",
            "Action": "EditData",
            "Target": "selph.ExtraAnimalConfig/EggExtensionData",
            "Entries": {
                "(O)174": {
                    "AnimalSpawnList": [
                    {
                        "Id": "Blue",
                        "AnimalId": "Blue Chicken",
                        "Condition": "RANDOM 0.25",
                    },
                    {
                        "Id": "White",
                        "AnimalId": "White Chicken",
                    },
                    ],
                },
            },
    }
    ]
}
```
</details>

<details>

<summary>Pigs dig up random minerals instead of truffles</summary>

```
{
    "Changes": [
    {
        "LogName": "Modify Truffles",
            "Action": "EditData",
            "Target": "selph.ExtraAnimalConfig/AnimalExtensionData",
            "TargetField": ["Pig", "AnimalProduceExtensionData"],
            "Entries": {
                "(O)430": {
                    "ItemQuery": {
                        "ItemId": "RANDOM_ITEMS (O)",
                        "PerItemCondition": "ITEM_CATEGORY Target -12",
                    },
                }
            },
    },
    ]
}
```
</details>

<details>

<summary>Sheep can produce either milk or wool</summary>

```
{
    "Changes": [
    {
        "LogName": "Modify Sheep Produce List",
            "Action": "EditData",
            "Target": "Data/FarmAnimals",
            "Fields": {
                "Sheep": {
                    "DaysToProduce": 1,
                    "ProduceItemIds": [
                    {
                        "Id": "Default",
                        "Condition": null,
                        "MinimumFriendship": 0,
                        "ItemId": "440",
                    },
                    {
                        "Id": "Milk",
                        "Condition": null,
                        "MinimumFriendship": 0,
                        "ItemId": "186",
                    },
                    ],
                },
            },
    },
    {
        "LogName": "Make Sheep Milk Harvested by Milk Pail instead of Shears",
        "Action": "EditData",
        "Target": "selph.ExtraAnimalConfig/AnimalExtensionData",
        "TargetField": ["Sheep", "AnimalProduceExtensionData"],
        "Entries": {
            "(O)186": {
                "HarvestTool": "Milk Pail",
                // This makes the sheep have the default sprite if it has milk
                "ProduceTexture": "Animals\\ShearedSheep",
            },
        },
    },
    ]
}
```
</details>

## Possible Future Content Roadmap?

*  Custom grass-like outdoor food. Disclaimer, it will be quite some time before I implement this, if I ever decide to.
