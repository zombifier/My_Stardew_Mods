# Fresh Farm Produce

[Fresh Farm Produce](https://www.nexusmods.com/stardewvalley/mods/22256)
is a Stardew Valley mod that incentivizes shipping a wide variety of raw, fresh produce

This document is mainly intended for modders. For mod users, install the mod
from the link above.

- [Farm Fresh Produce](#farm-fresh-produce)
  + [Freshness Features](#freshness-features)
  + [Modify the Competition](#modify-the-competition)
  + [Console Commands](#console-commands)

## Freshness Features

* If you want an item that isn't usually part of a 'fresh' category benefit
  from 'freshness', add the `spoilable_item` context tag to the item data. This
  mod does this to Coffee Beans.
* Conversely, add the `non_spoilable_item` tag to make an item *not* eligible
  for freshness.
* In-game, items marked Fresh will have the "fresh_item" context tags.
* Item queries (shops, machines, etc.) will spawn Fresh items. To make them not fresh, add
  `"selph.FreshFarmProduce.NotFresh": "true"` to the item query's ModData field.

## Modify the Competition

You can specify your own farm competition categories, or even replace this
mod's own! See the mod's included content pack for an example.

The mod reads competition data from the asset `selph.FreshFarmProduce/CompetitionData`, which has two fields:

| Field Name                         | Type             | Description              |
| ---------------------------------- | ---------------- | ------------------------ |
| `ActiveCategoryIds`         | `List<string>`            | A list of category IDs that should be active. Categories are defined below. Note that changing this will not change an active competition.|
| `Categories`              | `Dictionary<string, CategoryData>`           | A map of unique category IDs to their data. It is highly recommended that you don't delete the base mod's categories, but add your own and use the above field to control which category is active.|

`CategoryData` is a model with the following fields:

| Field Name                         | Type             | Description              |
| ---------------------------------- | ---------------- | ------------------------ |
| `Name`              | `string`           | The category display name. |
| `Description`              | `string`           | The category description. This should indicate what the user should ship here for informational purposes. |
| `Texture`              | `string`           | The spritesheet to use for this category's 16x16 icon. Defaults to `"Maps/springobjects"`.|
| `SpriteIndex`              | `int`           | The sprite index to use for this category's 16x16 icon.|
| `TotalPoints`              | `int`           | The total amount of points this category needs. |
| `MaxIndividualPoints`              | `int`           | Optional. The max amount of points one specific item can fully contribute to the category.|
| `UseSalePrice`              | `bool`           | Optional. If `true`, use the item's sell price instead of its Stardew Valley Fair points.|
| `ItemCriterias`              | `List<ItemCriteria>`           | Optional. A list of criterias controlling whether a shipped item is eligible for this category. An item matches if any criteria matches.|

`ItemCriteria` is a model with the following fields:

| Field Name                         | Type             | Description              |
| ---------------------------------- | ---------------- | ------------------------ |
| `Id`              | `string`           | The unique ID for this entry. |
| `ItemIds`              | `List<string>`           | Optional, if set the item's *qualified* ID must be in this list.|
| `ContextTags`              | `List<string>`           | Optional, if set the item must match *all* context tags in this list.|
| `Condition`              | `string`           | Optional, if set the item must match the [Game State Query](https://stardewvalleywiki.com/Modding:Game_state_queries#For_items_only) specified in this field. Use on `Target`.|

## Mail/Flags/Rewards
This mod's competition start logic and rewards are almost entirely handled in
the CP component, so feel free to edit them to change the timing/rewards/etc.
to your liking. The only things that are handled in C# are adding the following
mail flags to every player when the player finishes the competition or when it expires:

| Flag Name                         | Description              |
| --------------------------------- | ------------------------ |
| `selph.FreshFarmProduce.Finished` | The contest is finished, and the farmer will not win any medals.|
| `selph.FreshFarmProduce.FinishedBronze` | The contest is finished, and the farmer will win a Bronze medal.|
| `selph.FreshFarmProduce.FinishedSilver` | The contest is finished, and the farmer will win a Silver medal.|
| `selph.FreshFarmProduce.FinishedGold` | The contest is finished, and the farmer will win a Gold medal.|
| `selph.FreshFarmProduce.FinishedIridium` | The contest is finished, and the farmer will win an Iridium medal.|

The global friendship increase are handled via a custom trigger action `selph.FreshFarmProduce_AddGlobalFriendshipPoints <value>`.

## Console Commands

| Command Name                         | Description              |
| ---------------------------------- |  ------------------------ |
| `selph.FreshFarmProduce_AddSpecialOrder` | Add the competition special order.|
| `selph.FreshFarmProduce_RemoveSpecialOrder` | Remove the competition special order.|
| `selph.FreshFarmProduce_ResetSpecialOrder` | Remove and then readd the competition special order.|
| `selph.FreshFarmProduce_PrintDiagnostics` | Print detailed info about the competition (including items shipped, points, etc.)|
| `selph.FreshFarmProduce_AddWinningItems` | Add stacks of items that can be shipped to win the competition.|
