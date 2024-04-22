# Custom Tappers Framework

[Custom Tapper Framework](https://www.nexusmods.com/stardewvalley/mods/22975)
is a Stardew Valley mod that adds a framework to define custom tappers and
tapper-like big craftables to the game, and allows them to be placeable on fruit trees
and giant crops (and potentially other types of terrain features in the
future).

This document is for modders looking to incorporate this mod into their own
content packs. For users, install the mod as usual from the link above.

## Use

First, if you're adding a new tapper big craftable, add it to the game via
Content Patcher. Make sure to set `"tapper_item"` is set in the custom tags;
otherwise it will be treated as a regular machine placeable on the ground.

Then, add the mod data to `selph.CustomTapperFramework/Data`. The asset
takes the form of a map, with the key being the qualified item ID of the tapper
and the value being a model with the following fields:

| Field Name | Type | Description |
| ---------- | ---- | ----------- |
| `AlsoUseBaseGameRules` | `bool` | Whether this tapper can also be used like the base game tapper (ie. place on a wild tree to get their tap produce). Defaults to false.<br> This will also be true for tapper item that isn't defined in the mod data.|
| `TreeOutputRules` | `List<ExtendedTapItemData>` | A list of output rules to apply when this tapper is placed on a wild tree. If null, will not be placeable on trees (unless `AlsoUseBaseGameRules` is true)|
| `FruitTreeOutputRules` | `List<ExtendedTapItemData>` | A list of output rules to apply this tapper is placed on a fruit tree. If null, will not be placeable on fruit trees.|
| `GiantCropTreeOutputRules` | `List<ExtendedTapItemData>` | A list of output rules to apply this tapper is placed on a giant crop. If null, will not be placeable on giant crops.|

`ExtendedTapItemData` is an item query object that defines the items produced
by the tree. The type extends from the entries in a [wild tree's `TapItem`
data](https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.6#Custom_wild_trees),
so visit the wiki page for reference on its base fields.

Additionally, `ExtendedTapItemData` supports the following additional fields:

| Field Name | Type | Description |
| ---------- | ---- | ----------- |
| `SourceId` | `string` | If set, only apply this rule if the tapped tree/fruit tree/giant crop is of this ID. |
| `RecalculateOnCollect` | `bool` | Whether to recalculate the output upon collection. This is really only useful for flower honey, to readjust the honey flavor. |

`ExtendedTapItemData`'s game state and item queries support supplying the input item aside from the target:

* For trees, the input is their seed object
* For fruit trees, the input is their first defined fruit
* For giant crop, the input is their first defined drop

This can be used for defining dynamic output e.g. flavored juice, when combined with the macros below.

The output item query supports the following macros:

| Name | Description |
| ---------- | ----------- |
| `DROP_IN_ID` | The qualified item ID of the "input" item as defined above. |
| `NEARBY_FLOWER_ID` | The qualified item ID of a nearby flower. Only useful for honey rules. |

## Example

See below for an example from the [Additional Tree Equipments](https://google.com)
mod, which adds a tree bee house that can be placed on any tree, and
produces honey every 4 days except in winter:

<details>

<summary>Content Patcher definition</summary>

```
{
  "Changes": [
    {
      "LogName": "Add Custom Tapper Framework Data",
      "Action": "EditData",
      "Target": "selph.CustomTapperFramework/Data",
      "Entries": {
        "(BC)selph.ExtraTappers.BeeHouse": {
          "AlsoUseBaseGameRules": false,
          "FruitTreeOutputRules": [
            {
              "Id": "Honey",
              "ItemId": "FLAVORED_ITEM Honey NEARBY_FLOWER_ID",
              "DaysUntilReady": 4,
              "Condition": "!LOCATION_SEASON Target Winter",
              // SourceId = null allows all fruit trees. You can set this field if you want to limit it to only certain types of trees.
              "SourceId": null,
              "RecalculateOnCollect": true,
            },
          ],
          "TreeOutputRules": [
            {
              "Id": "Honey",
              "ItemId": "FLAVORED_ITEM Honey NEARBY_FLOWER_ID",
              "DaysUntilReady": 4,
              "Condition": "!LOCATION_SEASON Target Winter",
              "RecalculateOnCollect": true,
            },
          ],
        },
      }
    },
  ]
}
```
</details>
