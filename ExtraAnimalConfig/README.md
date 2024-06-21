# Extra Animal Configuration Framework

[Extra Animal Config](https://www.nexusmods.com/stardewvalley/mods/25328)
is a Stardew Valley mod that adds extra functionalities to Content Patcher
farm animals recipe definitions.

This document is for modders looking to incorporate this mod into their own
content packs. For users, install the mod as usual from the link above.

## Table of Contents
- [Extra Animal Configuration Framework](#extra-animal-configuration-framework)
   * [Table of Contents](#table-of-contents)
   * [Animal data asset](#animal-data-asset)
      + [Setting up animals that eat alternate feed](#setting-up-animals-that-eat-alternate-feed)
   * [Multiple possible animals from one egg](#multiple-possible-animals-from-one-egg)
   * [Examples](#examples)
   * [Known Issues/Future Content Roadmap](#known-issuesfuture-content-roadmap)

## Animal data asset

This mod adds a new asset `selph.ExtraAnimalConfig/AnimalExtensionData`, which
is a dictionary where the key is the animal ID similar to the key of
[Data/FarmAnimals](https://stardewvalleywiki.com/Modding:Animal_data), and the
value a model with the following fields:

| Field Name                         | Type             | Description              |
| ---------------------------------- | ---------------- | ------------------------ |
| MalePercentage          | `float`            | The percentage of this species that will be male. Currently this only affects Marnie's message after buying the animal. |
| FeedItemId              | `string`           | The *qualified* item ID of this animal's food item. If set, the animal will need to eat this item in addition to/in place of grass and hay. See below for a full guide. |
| AnimalProduceExtensionData| `Dictionary<string, AnimalProduceExtensionData>` | A map of *qualified* item IDs corresponding to an (unqualified) entry in `(Deluxe)ProduceItemIds` to its extra settings. This is used to store extra settings associated with an animal produce; see below for more info.|
| AnimalSpawnList          | `List<AnimalSpawnData>`   | A list of animal spawn data objects to determine which new animal to add when this animal gets pregnant and gives birth. The list will be evaluated in order.|

`AnimalSpawnData` is a model with the following fields:

| Field Name                         | Type             | Description              |
| ---------------------------------- | ---------------- | ------------------------ |
| Id          | `string`            | The unique Id for this model within the current list.|
| AnimalId    | `string`            | The animal to spawn.<br>KNOWN ISSUE: The overnight pop up (X has given birth to a Y) will still show the parent species in Y. This is very hardcoded and difficult to untangle, so don't expect this to be fixed soon unfortunately.|
| Condition   | `string`            | A [game state query](https://stardewvalleywiki.com/Modding:Game_state_queries) determining whether this animal should be spawned. |

`AnimalProduceExtensionData` is a model with the following fields:

| Field Name                         | Type             | Description              |
| ---------------------------------- | ---------------- | ------------------------ |
| ItemQuery          | [`GenericSpawnItemData`](https://stardewvalleywiki.com/Modding:Item_queries)  | The override item query to use to actually generate this produce. Most item query fields will work, aside from quality (which is determined by friendship) and stack size (which will always be 1, or 2 for animals that were fed Golden Crackers).<br>KNOWN ISSUE: This does not work for `DropOvernight` items yet. This will be fixed in the next update.|
| HarvestTool        | `string`  | For animals whose produce is tool-gathered, the harvest tool to use for this produce. Supports `Milk Pail` and `Shears`. |
| ProduceTexture     | `string`  | The animal's texture asset to override the default texture if it currently has this produce. |
| SkinProduceTexture | `Dictionary<string, string>` | Same as the above, but for non-default textures (which is the key, with the asset being the value).|

### Setting up animals that eat alternate feed

1. Optional: In base animal data, set `GrassEatAmount` to `0` so the animal
   doesn't eat grass.
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
     held feed into storage, or show the current feed capacity.
   * `selph.ExtraAnimalConfig.CustomFeedHopper (O)ItemId1`: Put into, or take
     the specified feed out of storage. Usable only inside an animal building.\
   Then, set the following field on your building's `CustomFields` map to
   specify the feed capacity provided by this building:
   * key : `selph.ExtraAnimalConfig.SiloCapacity.(O)ItemId1`
   * value: an integer (as a string). For example, `"100"` for 100 items.

Important notes/current limitations:
* Feed is shared globally between all locations. This is actually how vanilla
  hay works, but doesn't come into play unless modded farm locations are used.
* Animal that can also eat grass will still prefer fresh grass, and won't
  get full happiness from eating their modded feed. If they are not a grass
  eater, they will get full happiness from their modded feed.
* Even with `GrassEatAmount` set to 0, animals with modded food can *still* eat
  hay! This functionality is not as straightforward to disable, and as a
  result, your custom buildings should not have hay troughs if you don't want
  them to eat hay. If you need non-hay-eating animal functionality for some
  reason (e.g. mixed farm buildings), please leave a feature request.

## Multiple possible animals from one egg

You can define multiple possible hatches from an egg with the asset
`selph.ExtraAnimalConfig/EggExtensionData`, which is a map of qualified
item IDs to a model that currently only has the following field:

| Field Name                         | Type             | Description              |
| ---------------------------------- | ---------------- | ------------------------ |
| AnimalSpawnList | `List<AnimalSpawnData>`  | A list of animal spawn data to use instead, which will be evaluated in order. See above for details on the `AnimalSpawnData` field.<br>Note that you still need to set this egg item as the valid hatch item for at least one animal, and that any conditions will be evaluated when the animal becomes ready for hatching.|

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
            "Entries": {
                "Pig": {
                    "AnimalProduceExtensionData": {
                        "(O)430": {
                            "ItemQuery": {
                                "ItemId": "RANDOM_ITEMS (O)",
                                "PerItemCondition": "ITEM_CATEGORY Target -12",
                            },
                        }
                    }
                },
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
        "Entries": {
            "Sheep": {
                "AnimalProduceExtensionData": {
                    "(O)186": {
                        "HarvestTool": "Milk Pail",
                        // This makes the sheep have the default sprite if it has milk
                        "ProduceTexture": "Animals\\ShearedSheep",
                    },
                },
            },
        },
    },
    ]
}
```
</details>

## Known Issues/Future Content Roadmap

Note that this is not a guarantee these features will be added.

*  Animals that can have drop overnight, tool-collected produce, and dig-up produce. They can do only one of these things per day however, and this limitation won't change (both from a balance and a code difficulty reason).
*  Disallow hay as food.
*  Custom grass-like outdoor food.
