# Machine Terrain Expansion Framework (Formerly Custom Tappers Framework)

[Machine Terrain Expansion Framework](https://www.nexusmods.com/stardewvalley/mods/22975)
is a Stardew Valley mod that adds the following features:

* Define machines that are placeable on terrain features (currently wild trees,
  fruit trees, giant crops).
* Define machines that are placeable on water like crab pots.
* Define aquatic crops that are (only or also) plantable on water, and two new pot-like
  items to plant them with.

This document is for modders looking to incorporate this mod into their own
content packs. For users, install the mod as usual from the link above.

## Table of Contents
* [Terrain-Based Machine Feature](#terrain-based-machine-feature)
   + [Machine API](#machine-api)
      - [Extra GSQs](#extra-gsqs)
      - [Extra item queries](#extra-item-queries)
      - [Get the tapper output in machine rules](#get-the-tapper-output-in-machine-rules)
   + [Tapper API](#tapper-api)
   + [Example](#example)
* [Aquatic Crops Feature](#aquatic-crops-feature)
   + [Retexture the water planters](#retexture-the-water-planters)
* [Custom Planting Pots](#custom-planting-pots)
* [Custom Lightning Rods](#custom-lightning-rods)
* [Crop Behavior Expansion](#crop-behavior-expansion)

## Terrain-Based Machine Feature

There are two APIs available:

* The Machine API using context tags and machine rules, which is more powerful
  and supports water placement.
* The Tapper API using a custom asset to define automatic produce
  overtime. This API doesn't support water-placeable buildings.

The Tapper API is deprecated (the Machine API should support everything it does), but it will
continue to work into the future. For now use only one for your machine, not both.

---

### Machine API
First, set the appropriate context tags for your big craftables:

* If a tapper-like tree/giant crop machine, add `"tapper_item"` (same as vanilla)
  * If you don't want the vanilla tree tapper output (e.g. Maple Syrup, Oak Resin) to apply, also add `"custom_wild_tree_tapper_item"`
  * All tappers are placeable on trees by default. To disallow this, add `"disallow_wild_tree_placement"`
  * To make this building placeable on fruit trees, add `"custom_fruit_tree_tapper_item"`
  * To make this building placeable on giant crops, add `"custom_giant_crop_tapper_item"`
* If a crab pot-like water building, add `"custom_crab_pot_item"`
  * If you want to draw the "submerged" water overlay like regular crab pots,
    add `"draw_water_overlay"`.
  * To make a machine unremovable when processing an output, add
    `"prevent_remove_when_processing"`. Outside of this case, all water
    machines can be picked up if they're not processing, or if they don't have
    ready output.
  * NOTE: Currently there's a bug where the machine hand removal logic triggers
    a bit too judiciously. Until I can track this down it's recommended
    `"prevent_remove_when_processing"` be set for all your water machines,
    *especially* if it accepts input.

Then define your machine behavior in
[`Data/Machines`](https://stardewvalleywiki.com/Modding:Machines) as usual.

---

#### Extra GSQs

For tapper-like machines placed on terrain features, this mod allows defining the following new
field in the machine output rule's `CustomData` field:

| Field Name |  Description |
| ---------- |  ----------- |
| `selph.CustomTapperFramework.TerrainCondition` | Similar to the output rule's `Condition` field, but with the following crucial differences:<br><br>* The `Target` item will be the terrain feature's primary produce: Seed object for wild trees, first defined fruit for fruit trees, and first defined cut-down drop for giant crops.<br><br>* The `Input` item will be the machine itself.<br><br>* The GSQs below are usable in (and *only* in) this field (as well as any GSQ context that passes a tile, ie. SpaceCore crops).|
| `selph.CustomTapperFramework.ReplaceInputWithTerrainItem` | If set, when calculating the output item, the input item will be replaced with the terrain feature's primary produce. As a result you can use machine fields that depend on the input like `CopyPrice`, `CopyColor`, etc.|

The `selph.CustomTapperFramework_MACHINE_TILE_HAS_TERRAIN_FEATURE` GSQ takes the following format:

| Game State Queries |  Description |
| ---------- |  ----------- |
| `selph.CustomTapperFramework_MACHINE_TILE_HAS_TERRAIN_FEATURE <feature type> [optional feature ID]` | Whether machine tile has a terrain feature where feature type can be one of `Tree`, `FruitTree` or `GiantCrop`. A feature ID can optionally be specified, to limit the condition to certain types of wild trees/fruit trees/giant crops. |
| `selph.CustomTapperFramework_MACHINE_TILE_HAS_FRUIT_TREE_IN_SEASON` | Whether machine tile has a fruit tree that is in season.|

Additional GSQs that can be used anywhere:

| Game State Queries |  Description |
| ---------- |  ----------- |
| `selph.CustomTapperFramework_IS_VALID_FISH_FOR_POND <target>` | Whether the target item (`Target` or `Input`) can be put in a fish pond building.|
---

#### Extra item queries

This mod adds the following item queries, the first two usable only in machine
output item rules (as well as any mod that pass a `Tile` parameter into the
item query context's custom fields), while the last one is usable anywhere:

| Item query |  Description |
| ---------- |  ----------- |
| `selph.CustomTapperFramework_MACHINE_CRAB_POT_OUTPUT <ignoreLocationJunkChance> <usingGoodBait> <isMariner> <baitTargetFish>` | Get a crab pot fish, or a junk item, from the tile the machine is placed on. This GSQ attempts to simulate vanilla crab pot logic as close as humanly possible, including the percentage chance each fish can be caught, and thus accepts four optional parameters to control its behavior:<br><br>`ignoreLocationJunkChance`: if `true`, ignore the location's crab pot junk chance as defined in [`CrabPotJunkChance`](https://stardewvalleywiki.com/Modding:Location_data).<br><br>`usingGoodBait`: Whether to cut the aformentioned junk chance in half (e.g. due to good bait being used).<br><br>`isMariner`: Whether to simulate the farmer having the [Mariner profession](https://stardewvalleywiki.com/Fishing#Fishing_Skill), which does three things: remove junk from crab pots entirely, make crab pots ignore fish-specific bait, and make all crab pot fish equally likely to be picked.<br><br>`baitTargetFish`: The ID of a specific fish to prioritize, to simulate the effect of targeted bait. If your machine rules accept a targeted bait item, you can put `DROP_IN_PRESERVE` into this field.|
| `selph.CustomTapperFramework_MACHINE_FISH_LOCATION <getAllFish> <alsoCatchBossFish> <usingMagicBait> <allowNonObject>`| Identical to the [`FISH_LOCATION`](https://stardewvalleywiki.com/Modding:Item_queries#Specialized) GSQ, but with location, bobber tile and bobber depth already populated with the machine's current location. Accepts the following params:<br><br>*`getAllFish`: if `true`, get all possible fish that can be caught from this body of water, ignoring catch chance, player position requirement, bobber/depth requirement, and time requirement. Setting this to `true` will also activate the parameters after this one, settings to `false` will just have the item query call `FISH_LOCATION` directly. Highly recommended this is set to true.<br><br>`alsoCatchBossFish`: If set to `true`, also allow legendary boss fish to be caught.<br><br>`usingMagicBait`: If set to `true`, ignore season requirement, and allows catching fish that is marked to only be catchable with magic bait.<br><br>`allowNonObject` If set to `true`, also allow non-objects to be selected (ie. the Iridium Krobus statue). Keep this false/unset for machine rules to avoid machines blowing up, unless you're using Extra Machine Config's "non-object output" feature.|
| `selph.CustomTapperFramework_FISH_POND_DROP <fishId> <fallbackId>`| Get the drop of a hypothetical fish pond containing the specified fishId. The pond will be treated as having full capacity of the specified fish, and ignores the base population-based chance. Note that since ponds don't always produce, a fallback item (defaults to green algae) should be provided so your machine doesn't randomly stop working.|

If you want the fish caught by these item queries to count for perfection, add
`"selph.CustomTapperFramework.CountForPerfection": "true"` to the item query's `ModData`
field. If not, leave it out. This field currently only works for machine processing.

For the crab pot item query it's recommended you use
`selph.CustomTapperFramework_MACHINE_CRAB_POT_OUTPUT true true true` by default
for best results. This gets all crab pot catchable fish from the machine's
location, without junk, special logic or any bias to any one fish.

Similarly, for the fish location query it's *highly* recommended you use
`selph.CustomTapperFramework_MACHINE_FISH_LOCATION true false` for best results,
with similar effects. Use `selph.CustomTapperFramework.MACHINE_FISH_LOCATION
true false true` for magic bait effect. If you also want Legendary Fishes (but
why lol), change the `false` to `true`.

#### Get the tapper output in machine rules
For machines placed on trees, you can get the vanilla tapper output by setting the following field in the item query:

```
"OutputMethod": "Selph.StardewMods.MachineTerrainFramework.Utils, CustomTapperFramework: OutputTapper",
```

Make sure to also set `"custom_wild_tree_tapper_item"` context tag (alongside
`tapper_item`) so the base game handling doesn't overwrite it.

This will set the output to be the tree tapper produce, with ready time, stack
count, and other fields already populated from the wild tree data. IMPORTANT:
If you want to modify ready time, quality and stack size further, use
`ReadyTimeModifiers`, `QualityModifiers` and `StackModifiers`. For example, to
have your custom tapper produce 2 days earlier, add a ["minus
2"](https://stardewvalleywiki.com/Modding:Common_data_field_types#Quantity_modifiers)
to the `ReadyTimeModifiers` field.

The example below adds a tapper that produces 2x slower than regular tappers:
<details>

```json
// In Data/BigCraftables
"SlowTapper": {
  "Name": "SlowTapper",
  "DisplayName": "Slow Tapper",
  "Description": "This tapper is slow!",
  "CanBePlacedOutdoors": true,
  "CanBePlacedIndoors": true,
  "ContextTags": [
    "tapper_item",
    "custom_wild_tree_tapper_item",
  ],
},

// In Data/Machines
"(BC)SlowTapper": {
  "OutputRules": [
    {
      "Id": "Default",
      "Triggers": [
        {
          "Id": "Default",
          "Trigger": "DayUpdate,MachinePutDown,OutputCollected",
        }
      ],
      "OutputItem": [
        {
          "OutputMethod": "Selph.StardewMods.MachineTerrainFramework.Utils, CustomTapperFramework: OutputTapper",
        }
      ],
    }
  ],
  "ReadyTimeModifiers": [
    {
      "Modification": "Multiply",
      "Amount": 2.0
    }
  ],
  "ReadyTimeModifierMode": "Stack",
},
```
</details>


---

### Tapper API

NOTE: This API is mostly deprecated, and should only be used for when you want
the "input item is the terrain feature's usual produce" feature, or if you're
modifying the base game tappers' output.

First, add `"tapper_item"` context tag to your big craftables.

Then, write the mod data to the `selph.CustomTapperFramework/Data` asset, unless
you're modifying the base game tappers' data, in which case their data is
populated and you should instead edit/add to them. The asset takes the form of
a map, with the key being the qualified item ID of the tapper and the value
being a model with the following fields:

| Field Name | Type | Description |
| ---------- | ---- | ----------- |
| `AlsoUseBaseGameRules` | `bool` | Whether this tapper can also be used like the base game tapper (ie. place on a wild tree to get their tap produce). Defaults to false, except for base game tappers, where this value will always be true.<br><br> This will also be true for tapper item that isn't defined in the mod data.<br><br>Set this or `TreeOutputRules`, not both.|
| `TreeOutputRules` | `List<ExtendedTapItemData>` | A list of output rules to apply when this tapper is placed on a wild tree. If null, will not be placeable on trees (unless `AlsoUseBaseGameRules` is true).|
| `FruitTreeOutputRules` | `List<ExtendedTapItemData>` | A list of output rules to apply when this tapper is placed on a fruit tree. If null, will not be placeable on fruit trees.|
| `GiantCropOutputRules` | `List<ExtendedTapItemData>` | A list of output rules to apply when this tapper is placed on a giant crop. If null, will not be placeable on giant crops.|

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

---

### Example

See below for an example from the [Additional Tree Equipments](https://www.nexusmods.com/stardewvalley/mods/22991)
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

If you want to instead add to the base game tapper's outputs, instead do something like below, which makes tapped fruit trees produce sap:

<details>

<summary>Content Patcher Definition</summary>

```
{
  "Changes": [
    {
      "LogName": "Modify base heavy tapper rules",
      "Action": "EditData",
      "Target": "selph.CustomTapperFramework/Data",
      "TargetField": ["(BC)105", "FruitTreeOutputRules"],
      "Priority": "Late",
      "Entries": {
        "selph.ExtraTappers.Sap": {
          "DaysUntilReady": 1,
          "Chance": 1.0,
          "Id": "selph.ExtraTappers.Sap",
          "ItemId": "(O)92",
          "MinStack": 3,
          "MaxStack": 8,
        },
      },
    },
}

```
</details>

---

## Aquatic Crops Feature

This mod adds two new items:

* A Water Planter, placeable on water tiles to create a steady planting spot
  for water crops. Craftable with 20 wood.
* A Water Pot, to allow water crops to be plantable on land. Requires a Garden
  Pot to craft.

By default, these items' crafting recipes are disabled unless
aquatic/semiaquatic crops are added by content packs, in which
case they're automatically enabled (after Garden Pots are available for Water Pots).

To define crops that are plantable on water, add the following new keys to the
crop definition's `CustomFields` dict (their values can be anything as long as they're set):

* `selph.CustomTapperFramework.IsAquaticCrop` for crops that are only plantable in the water planter or water pot.
* `selph.CustomTapperFramework.IsSemiAquaticCrop` for crops that are plantable on both land and water.

Additionally, it's recommended that the crop be set to be paddy crops. Paddy
crops will get the growth speed bonus when placed inside water planters,
but *not* water pots. Gotta get that natural water!

Crops in water planters/pots are automatically considered watered every day,
for obvious reasons.

Bushes (specifically [Custom Bushes](https://www.nexusmods.com/stardewvalley/mods/20619)) currently are not supported.

For an example content pack, see the optional file on [this page](https://www.nexusmods.com/stardewvalley/mods/22975?tab=files).

### Retexture the water planters

You can patch the `Mods/selph.CustomTapperFramework/WaterPlanterTexture` asset
to change the texture of the water planters and water pots. The files
themselves should be in the mod's `assets` folder for easy reference.

### For C# mods

You can set a `HoeDirt` to only aquatic/amphibious crops by adding
`selph.CustomTapperFramework.IsWater` key to its `modData` dictionary. Add
`selph.CustomTapperFramework.IsAmphibious` to allow both aquatic and regular
crops to grow in it.

## Custom Planting Pots

Additionally, you can add custom planting pots, and specify crops that can be
planted in them. To specify custom pots, add the following fields to the
`CustomFields` dictionary in the
[`Data/BigCraftables`](https://stardewvalleywiki.com/Modding:Big_craftables)
entry:

* `selph.CustomTapperFramework.IsCustomPot`: if set, this BC will act like a
  garden pot when placed down.
* `selph.CustomTapperFramework.BansRegularCrops`: if set, this garden pot will only accept a crop
  specifically set to be plantable in it (see below).
* `selph.CustomTapperFramework.CropYOffset`: Optional, the y pixel offset to
  draw the crop at relative to the garden pot's usual crop draw position.
  Negative values push the crop upward, while positive values push them down.
  Use this for pots that are visibly taller/shorter than vanilla garden pots.
* `selph.CustomTapperFramework.CropTintColor`: Optional, the color to tint the
  drawn crop with. This can be used to achieve a "behind glass" effect for
  example. Note that this may look weird if the crop is taller than the big craftable bounds.

To define crops that can be planted in these pots, add the following keys to
the crop definition's `CustomFields` dict in
[`Data/Crops`](https://stardewvalleywiki.com/Modding:Crop_data):

* `selph.CustomTapperFramework.CustomPots`: A list of *qualified* item IDs of
  pot items that this crop can grow in, as a comma-separated string. If set,
  this crop will not be plantable in vanilla garden pots (unless that's also in
  the list)

Note that these fields cannot override `PlantableLocationRules` if it
determined that a crop cannot be planted in the first place.

Water planter-like floating pots are currently not implemented (because I'm
lazy, and because I'm not sure what use cases there would be for custom water
planters). Let me know if you have ideas for custom water pots, and I can get
to implementing them.

## Custom Lightning Rods

You can define custom lightning rods, each with their own produce upon getting
hit with lightning. Follow the below instructions:
* First, add the `custom_lightning_rod` context tag to your big craftable. This
  makes your custom lightning rods able to get hit by lightning during
  thunderstorms.
* Without further changes, they will produce battery packs after 1 day like
  vanilla lightning rods. To define custom produce, add a new machine rule to
  `Data/Machines` for the machine with the following properties:
  + Trigger rule set to `None`. This makes this rule never triggers
    regularly.
  + A field `"selph.CustomTapperFramework.LightningRodOutput"` in the output
    item's `CustomData` field. This will cause this rule to be checked by the
    custom lightning rod handler. Set it for every entry in an output rule.
  + Don't forget to set processing time! This does mean you can have lightning
    rods that process faster or slower than vanilla.
* You're free to use any other machine features (e.g. conditions, stack count,
  ExtraMachineConfig byproduce, make it floating, or even mix in regular
  machine rules with the lightning rule).
* Also note that lightning only strikes on the farm. Lightning rod placed
  outside of the farm will not get hit. If you're a modder and thinking of
  adding lightning strikes outside of the farm, please reach out to me for compat.

## Crop Behavior Expansion

This mod expands crop behavior with various modifiers and triggers, allowing crops to buff each
other or even spread themselves depending on a variety of conditions.

This is achieved via a new asset, `selph.CustomTapperFramework/CropExtensionData`, which is a
dictionary of key values, the key being either:
* the crop's key (aka the seed's unqualified item ID) in `Data/Crops`
* `Default` for behavior that applies to every crop that don't have their own fields set
* `Empty` for behavior that applies to empty hoe dirt. Only `DayStartTriggers` work here, and only the `PlantCrop` action. This can be used to make crops that sprout that spontaneously sprout in empty dirt.

Whether to use `Default` fields is checked on a per-field basis. For example, if
`GrowSpeedModifiers` is both set on entry `499` (Ancient Seeds) and `Default`, only the former will
be used for ancient seeds, while the latter is used for every crop. If `RegrowSpeedModifiers` is
only set in `Default` both Ancient Seeds and other seeds will use that.

This model is automatically populated with every crop in game, so you can use `TargetField`
and avoid overwriting another mod's data.

The value being the following data model:


| Field Name | Type | Description |
| ---------- | ---- | ----------- |
| `GrowSpeedModifiers`<br>`GrowSpeedModifierMode` | [Quantity modifiers and modifier mode](https://stardewvalleywiki.com/Modding:Common_data_field_types#Quantity_modifiers) | A list of quantity modifiers to apply to this crop's grow speed. The base value being modified is 0, so if you want the crop to grow 10% faster you want to `Add` 0.1. Conversely, negative values can slow down the crop's grow speed.<br>IMPORTANT NOTES:<br>* The game rounds up the days remaining for crop growth, including for negative values, so small negative modifiers may not take effect for crops that are already growing very fast!<br>* This will only be checked when the seed is planted, or when fertilizer is applied. It won't be checked retroactively if the surrounding condition changes later! For example, if you have corn grow 10% faster if pumpkin is nearby, planting corn and then pumpkin won't update the corn's grow speed!<br>To get around this, set pumpkins to run the `ResetGrowDays` trigger when planted and destroyed (see below).|
| `RegrowSpeedModifiers`<br>`RegrowSpeedModifierMode` | [Quantity modifiers and modifier mode](https://stardewvalleywiki.com/Modding:Common_data_field_types#Quantity_modifiers) | Similar to the above, but for regrow speed (ie the times between harvest for multiple harvest crops). This is only checked on harvest, and `ResetGrowDays` do not affect it.|
| `CropQuantityModifiers`<br>`CropQuantityModifierMode` | [Quantity modifiers and modifier mode](https://stardewvalleywiki.com/Modding:Common_data_field_types#Quantity_modifiers) | A list of quantity modifiers to apply to the harvest quantity. The base value is the original count.|
| `CropQualityModifiers`<br>`CropQualityModifierMode` | [Quantity modifiers and modifier mode](https://stardewvalleywiki.com/Modding:Common_data_field_types#Quality_modifiers) | A list of quantity modifiers to apply to the harvest's quality. Only the main drop is affected, like vanilla.|
| `MainDropOverride` | List of [item queries](https://stardewvalleywiki.com/Modding:Item_queries#Item_spawn_fields) | A list of item queries that when evaluated yields an item to override the harvest.<br>IMPORTANT NOTES:<br>* This cannot be used to make the crop drop non-`Object`s, like weapons or rings. Big craftables and furniture is fair game though.<br>* If there are multiple items in the list, the first query whose condition qualifies is chosen.<br>* The item query accepts all the regular fields, with a fewe new additions:<br>  - `CopyColor`: whether to inherit the color of the original produce (e.g. colored flowers). Defaults false.<br>  - `OverrideQuality`: whether to override the quality of the original produce in favor of the `Quality` field. Defaults false.<br>  - `OverrideStack`: whether to override the harvest count of the original produce. Defaults false.<br> * Quality only applies to the first drop for multidrop crops. This is vanilla behavior.|
| `ExtraDrops` | List of [item queries](https://stardewvalleywiki.com/Modding:Item_queries#Item_spawn_fields) | A list of item queries that when evaluated yields an item to add alongide the main harvest.<br>IMPORTANT NOTES:<br>* If there are multiple items in the list, *every* query that qualifies will be chosen to drop.<br>* Unlike the main drop override, quality and stack is respected.|
| `PlantTriggers` | List of [trigger action action strings](https://stardewvalleywiki.com/Modding:Trigger_actions#Actions) | A list of trigger actions that should be run when this crop is planted. This accepts the regular actions, plus some special actions that are only usable in this field listed below.|
| `DestroyedTriggers` | List of [trigger action action strings](https://stardewvalleywiki.com/Modding:Trigger_actions#Actions) | A list of trigger actions that should be run when this crop is removed (via tools or etc.). This accepts the regular actions, plus some special actions that are only usable in this field listed below.|
| `DayStartTriggers` | List of [trigger action action strings](https://stardewvalleywiki.com/Modding:Trigger_actions#Actions) | A list of trigger actions that should be run on day start for this empty dirt/crop, after the daily logic (growth, harvest, etc.). This accepts the regular actions, plus some special actions that are only usable in this field listed below.<br>IMPORTANT NOTE: This will only run for crops that have grown at least one day (ie. it will not run for seeds spread by the `PlantCrop` trigger).|

### Trigger actions
The following special actions can only be used in `PlantTriggers` and `DayStartTriggers`. Their main
purpose is to affect all crops in a radius surrounding the primary crop.

Unless otherwise stated, they have the following common fields:
* radius: The radius around the crop to affect. This can be 0 to only affect the crop itself.
* exclude main crop: Optional, whether to exclude the crop being checked from the action itself. Defaults to true.
* GSQ: Optional, the GSQ that a crop must satisfy before it's acted on. Make sure to wrap it in (escaped) quotes.

| Trigger Action Action | Description |
| --------------------- | ----------- |
| `selph.CustomTapperFramework_ResetGrowDays <radius> [exclude main crop] [GSQ]` | Reset the grow days of surrounding crops in a radius. For why this is necessary, read the entry for `GrowSpeedModifiers` above.|
| `selph.CustomTapperFramework_KillCrop <radius> [exclude main crop] [GSQ]` | Kill crops in a radius, like when a crop goes out of season.|
| `selph.CustomTapperFramework_DestroyCrop <radius> [exclude main crop] [GSQ]` | Destroy crops in a radius, removing them entirely.|
| `selph.CustomTapperFramework_TransformCrop <cropId> <radius> [exclude main crop] [GSQ]` | Transform crops in a radius to the specified `cropId`.|
| `selph.CustomTapperFramework_PlantCrop <cropId> <maxCount> <radius> [chance] [maxCount]` | Plants the specified crop/seed ID in empty tilled dirt (not pots) in a radius. Chance is optional, a number between 0 and 1 for a chance the crop is planted.|
| `selph.CustomTapperFramework_IfCrop <query> ## <action if true> ## <action if false>` | A special version of the `If` action that works in crop trigger actions.|

Need even moar power? You can use Cloudy Skies's brilliant [trigger
actions](https://github.com/KhloeLeclair/StardewMods/blob/main/CloudySkies/author-guide.md#trigger-actions)
as well! Note that unlike this mod's actions, CS requires you to provide an area to target; you can
use the following macros in the action string, which will be replaced accordingly:

| Macros | Description |
| --------------------- | ----------- |
| TILE_X | The X coordinate of the main crop.|
| TILE_Y | The Y coordinate of the main crop.|
| LOCATION_NAME | The name of the crop's location.|

For example, you could use `leclair.cloudyskies_KillCrops Tile Location LOCATION_NAME TILE_X TILE_Y 3` to kill crops in a 2 tile radius around the main crop (CS uses `1` as affecting only 1 tile).

### Game State Queries
Every `Condition` field in the custom asset has the `Target` item set as the target crop's main
produce, and the `Input` item set as the seed item. You can thus condition your modifiers/trigger
actions/etc to act only on a specific crop.

NOTE: These conditions cannot be used in `If` actions! Use `selph.CustomTapperFramework_IfCrop` instead.

Additionally, the following GSQs can be used in (and only in) the asset above:

| Game State Query | Description |
| ---------------- | ----------- |
| `selph.CustomTapperFramework_NEARBY_CROPS <radius> [sub GSQ] [count] [fullGrownOnly] [acceptsPots]` | Whether there are crops of a certain count near the crop being checked.<br>Fields:<br>* radius: The tile radius to check.<br>* sub GSQ: The game state query to check for the crop in the radius (defaults to accepting any crop). Make sure to wrap it in (escaped) quotes.<br>* count: The total count (defaults to one).<br>* fullGrownOnly: Whether to only consider fully grown crops (default false).<br>* acceptsPots: Whether to also count crops in indoor pots (default false. Water planters are considered "natural" soil).|
| `selph.CustomTapperFramework_IS_IN_POT` | Whether the crop is in an indoor pot. Water planters are not considered pots.|
| `selph.CustomTapperFramework_IS_FULLY_GROWN` | Whether the crop is fully grown.|

### Example

The below example makes ancient fruits:
* Cause nearby crops to grow 50% slower
* Have a chance to spread to one nearby tile every day
* Have a 50% chance to drop iridium bar instead of ancient fruit
* Have a 50% chance to also drop iridium ore alongside the main drop
* Increase the main drop's stack count by 4 if it's near 2 other ancient fruits
* Set the main drop's quality to iridium if it's near 2 other ancient fruits
* Have a 50% chance to transform nearby non-ancient fruit crops into ancient fruits

<details>

```json
{
  "Changes": [
    {
      "LogName": "Crop stuff",
      "Action": "EditData",
      "Target": "selph.CustomTapperFramework/CropExtensionData",
      "Entries": {
        "499": {
          "PlantTriggers": [
            "selph.CustomTapperFramework_ResetGrowDays 2 true",
          ],
          "DestroyedTriggers": [
            "selph.CustomTapperFramework_ResetGrowDays 2 true",
          ],
          "DayStartTriggers": [
            "selph.CustomTapperFramework_PlantCrop 499 2 1 1",
          ],
          "MainDropOverride": [
            {
              "Id": "Iridium Bar",
              "ItemId": "337",
              "Condition": "RANDOM 0.5",
            }
          ],
          "ExtraDrops": [
            {
              "Id": "Iridium Ore",
              "ItemId": "386",
              "MinStack": 2,
              "MaxStack": 10,
              "Condition": "RANDOM 0.5",
            }
          ],
          "CropQuantityModifiers": [
            {
              "Id": "MoreMainDrop",
              "Modification": "Add",
              "Amount": 4,
              "Condition": "selph.CustomTapperFramework_NEARBY_CROPS 2 \"ITEM_ID Input 499\" 2"
            }
          ],
          "CropQualityModifiers": [
            {
              "Id": "Iridium",
              "Modification": "Set",
              "Amount": 4,
              "Condition": "selph.CustomTapperFramework_NEARBY_CROPS 2 \"ITEM_ID Input 499\" 2"
            }
          ],
        },
        "Default": {
          "DayStartTriggers": [
            "selph.CustomTapperFramework_TransformCrop 499 0 false \"RANDOM 0.5\"",
          ],
          "GrowSpeedModifiers": [
            {
              "Id": "Slow",
              "Modification": "Subtract",
              "Amount": 0.5,
              "Condition": "!ITEM_ID Input 499, selph.CustomTapperFramework_NEARBY_CROPS 2 \"ITEM_ID Input 499\""
            }
          ],
          "RegrowSpeedModifiers": [
            {
              "Id": "Slow",
              "Modification": "Subtract",
              "Amount": 0.5,
              "Condition": "!ITEM_ID Input 499, selph.CustomTapperFramework_NEARBY_CROPS 2 \"ITEM_ID Input 499\""
            }
          ],
        }
      }
    }
  ]
}
```
</details>
