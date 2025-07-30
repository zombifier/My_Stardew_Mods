# Extra Machine Configuration Framework

[Extra Machine Config](https://www.nexusmods.com/stardewvalley/mods/22256)
is a Stardew Valley mod that adds extra functionalities to Content Patcher
machine recipe definitions, and allows modders to define recipes that can do
things beyond what's possible in the base game (e.g per-recipe fuel).

As of 1.5.0, this mod also adds a bunch of miscellaneous features not strictly
related to machines.

This document is for modders looking to incorporate this mod into their own
content packs. For users, install the mod as usual from the link above.

## Table of Contents
- [Extra Machine Configuration Framework](#extra-machine-configuration-framework)
   * [Table of Contents](#table-of-contents)
   * [Item Features](#item-features)
      + [Icons and descriptions for the new buff attributes](#icons-and-descriptions-for-the-new-buff-attributes)
      + [Draw smoke particles around item](#draw-smoke-particles-around-item)
      + [Draw an item's preserve item's sprite instead of its base sprite](#draw-an-items-preserve-items-sprite-instead-of-its-base-sprite)
      + [Define extra loved items for Junimos](#define-extra-loved-items-for-junimos)
      + [Append extra context tags to shop and machine item queries](#append-extra-context-tags-to-shop-and-machine-item-queries)
      + [Items with multiple flavors and colors](#items-with-multiple-flavors-and-colors)
      + [Items that can be used as slingshot ammo](#items-that-can-be-used-as-slingshot-ammo)
      + [Use item queries in add item trigger action](#use-item-queries-in-add-item-trigger-action)
      + [On buff removed trigger](#on-buff-removed-trigger)
      + [Custom slime eggs (including prismatic slime eggs)](#custom-slime-eggs-including-prismatic-slime-eggs)
   * [Machine Features](#machine-features)
      + [Passive features/fixes](#passive-features-fixes)
      + [Adding additional fuel for a specific recipe](#adding-additional-fuel-for-a-specific-recipe)
      + [Output inherit the flavor of input items](#output-inherit-the-flavor-of-input-items)
      + [Output inherit the dye color of input items](#output-inherit-the-dye-color-of-input-items)
      + [Specify range of input count and scale output count with the input amount consumed](#specify-range-of-input-count-and-scale-output-count-with-the-input-amount-consumed)
      + [Adding extra byproducts for machine recipes](#adding-extra-byproducts-for-machine-recipes)
      + [Allow non-Object outputs from machines](#allow-non-object-outputs-from-machines)
      + [Generate nearby flower-flavored modded items (or, generate flavored items outside of machines)](#generate-nearby-flower-flavored-modded-items-or-generate-flavored-items-outside-of-machines)
      + [Override display name if the output item is unflavored](#override-display-name-if-the-output-item-is-unflavored)
      + [Generate an input item for recipes that don't have any, and use 'nearby flower' as a possible query](#generate-an-input-item-for-recipes-that-dont-have-any-and-use-nearby-flower-as-a-possible-query)
      + [Automatic machines that stop producing after X times](#automatic-machines-that-stop-producing-after-X-times)
      + [Run trigger action on machine ready](#run-trigger-action-on-machine-ready)
      + [Custom casks](#custom-casks)
      + [Custom slime incubators](#custom-slime-incubators)
      + [Machines that spit out the input item when removed](#machines-that-spit-out-the-input-item-when-removed)
      + [On machine ready effects](#on-machine-ready-effects)
      + [Edibility based on input](#edibility-based-on-input)
      + [Colored draw layers based on output item](#colored-draw-layers-based-on-output-item)
   * [Crafting/Cooking Features](#craftingcooking-features)
      + [Use some machine-like features in crafting and cooking (namely copy flavor and color)](#use-some-machine-like-features-in-crafting-and-cooking-namely-copy-flavor-and-color)

## Item Features

### Icons and descriptions for the new buff attributes

The new buff attributes added in 1.6.9 (CombatLevel, AttackMultiplier,
CriticalChanceMultiplier, CriticalPowerMultiplier, Immunity,
KnockbackMultiplier, WeaponPrecisionMultiplier, and WeaponSpeedMultiplier) now
have a buff icon and show up in item description where they previously were
hidden. This is a passive change.

### Draw smoke particles around item

Items with the context tag `smoked_item` will have its sprite darkened and
have smoke particles drawn around it like smoked fish.

### Draw an item's preserve item's sprite instead of its base sprite

Items with the context tag `draw_preserve_sprite` will have its sprite be
the sprite of its `preservedParentSheetIndex` item instead (if set).

With `smoked_item` and `draw_preserve_sprite` combined, you can implement
custom smoked item similar to smoked fish without having to output a
`(O)SmokedFish` item, such as a smoked egg item that uses the sprite of the egg
used to make it albeit dark and smoking.

More item effects aside from smoke might come in the future.

### Define extra loved items for Junimos

Items with the context tag `junimo_loved_item` can be fed to junimos to improve
their harvest rate just like raisins.

### Append extra context tags to shop and machine item queries

Set the following field in the [item query's `ModData`
field](https://stardewvalleywiki.com/Modding:Item_queries#Item_spawn_fields).

| Field Name                         | Description              |
| ---------------------------------- | ------------------------ |
| `selph.ExtraMachineConfig.ExtraContextTags` | A comma-separated list of extra context tags for the item spawned by this item query.|

Important notes:

* This feature can be used anywhere item queries are used, such as machines or shops.
* If you're using this field, it's highly recommended you also set the
  `ObjectInternalName` field (and optionally the display name) so the spawned
  items do not stack with other items of the same ID that may not have this
  field, causing the context tags to be lost.

For an example, scroll down to the example for additional fuels for machine recipes.

### Items with multiple flavors and colors

Set the following fields in the item query's `ModData` field. Note that extra
colors only work if the base item is also colored, which (until 1.6.9 is
released) is only available in machine rules, or if you use the modded
`FLAVORED_ITEM` query added by this mod as detailed below.

| Field Name                         | Description              |
| ---------------------------------- | ------------------------ |
| `selph.ExtraMachineConfig.ExtraPreserveId.1` | The unqualified ID of the extra flavor, aside from the primary one. To add more flavors, add another field with the number incremented. With these flavors, the item's display name can contain the `%EXTRA_PRESERVED_DISPLAY_NAME_1` macro (and `_2`, and `_3`, etc.), which will be replaced with the flavor's display name.|
| `selph.ExtraMachineConfig.ExtraColor.1` | The extra color, formatted as three numbers separated by commas corresponding to the RGB values (e.g. `"255,120,20"`). The sprite two spaces (or three, or four for any additional colors) from the item's primary sprite will be used as the color mask, just like how the immediate next sprite is used for the main color. To add more colors, add another field with the number incremented.|

See below for how to copy the fuel's colors into the output for machine rules.

Important notes:

* Just like with custom context tags, if you're using these field, it's highly
  recommended you also set the `ObjectInternalName` field so the spawned items
  do not stack with other items of the same ID that may not have these fields.
* Extra flavors can be retrieved with the context tags `extra_preserve_sheet_index_1_flavorid`. Increment the number for additional flavors.

----

### Items that can be used as slingshot ammo

Set the following fields in the object definition's `CustomFields` dict:

| Field Name                         | Description              |
| ---------------------------------- | ------------------------ |
| `selph.ExtraMachineConfig.SlingshotDamage` | The impact damage dealt by this item when used as ammo for a slingshot. If set, it will be usable as ammo. Keep in mind the master slingshot doubles this value.|
| `selph.ExtraMachineConfig.SlingshotExplosiveRadius` | The explosion radius of this ammo. If not set, it will not explode.|
| `selph.ExtraMachineConfig.SlingshotExplosiveDamage` | The damage dealt by this item's explosion. If not set (even if only to `"0"`), it will not explode.|

----

### Use item queries in add item trigger action

First, write your item queries to a new asset named
`selph.ExtraMachineConfig/ExtraOutputs`. This asset is a map of unique IDs to
[item query](https://stardewvalleywiki.com/Modding:Item_queries) objects.

Then use the trigger action like below:

`selph.ExtraMachineConfig_AddItemQuery UniqueIdAddedAbove AnotherUniqueIdIfYouWant OrAnotherIdHere`

If multiple IDs are provided, the action will evaluate them left to right,
stopping at the first item query that gives an item.

This action can be used to add flavored items, add items only if a GSQ
satisfies, or add a random item from a list.

----

### On buff removed trigger

You can listen for the `selph.ExtraMachineConfig_BuffRemoved` trigger in
[trigger actions](https://stardewvalleywiki.com/Modding:Trigger_actions). The following
game state queries can be used in the trigger block's `Condition` field:

* `selph.ExtraMachineConfig_BUFF_NAME buffName`: Whether this buff has a
  specific name. For food/drink buffs, this is the food/drink item's `Name` in
  its object data. For other buff sources, the below GSQ should be used.
* `selph.ExtraMachineConfig_BUFF_ID buffId`: Whether this buff has a specific ID. In vanilla, this can be:
  *  the key in `Data/Buffs`
  * `iridiumspur`: Iridium Spur buff
  * `CalicoStatueSpeed`: the speed boost from Calico Statues during the Desert Festival
  * `DesertFestival`: the buff from eating the Desert Festival food
  * `food`: a food buff
  * `drink`: a drink buff

NOTE:
* Beware that this can fire multiple times when a food with multiple buffs expires.
* For C# authors, the trigger passes a Weeds item as the target item, of which
  the GSQs checks for the `modData` fields `selph.ExtraMachineConfig_BuffName`
  and `selph.ExtraMachineConfig_BuffId`.

----

### Custom slime eggs (including prismatic slime eggs) 

You can define custom slime eggs by adding the field
`selph.ExtraMachineConfig.SlimeColorToHatch` field to the object data's
`CustomFields` dictionary. The value is either three numbers corresponding to
RGB values separated by commas (e.g. `"155,50,20"`), or `"Prismatic"` to make
it a prismatic slime.

Don't forget to also add the `slime_egg_item` context tag!

NOTE: slain Prismatic Slimes *will* drop Prismatic Jelly. If you're
going to make prismatic jellies a common item in your mod, it's highly
recommended you do the following:
* Make prismatic jellies trashable (and perhaps sellable) in `Data/Objects`.
* Make the Wizard's special order not remove prismatic jellies when it ends in
  `Data/SpecialOrders`.

You can also control the chance of hatched/spawned (as in, not randomly spawned
by the Wizard special order) prismatic slimes dropping prismatic jellies on
killed by editing `Data/Machines`, targeting the slime incubator's entry
(`"(BC)156"`), and add `selph.ExtraMachineConfig.HatchedPrismaticJellyChance`
to the `CustomFields` dictionary. The value is the chance (a number between
0.0, never, and 1.0, always). You can use CP When conditions to control this
number.

NOTE: Despite being a field on `Data/Machines`, it will globally affect every
prismatic slime, regardless of from which incubator they spawn. The reason it's
like that is because I'm too lazy to make an entirely new asset for it.

Finally, this mod includes a passive feature that allows prismatic slimes to
breed with other slimes, making more prismatic slimes (50% chance if only one
of the parent is prismatic). Feel free to use it for nefarious purposes.

These works with [custom slime incubators](#custom-slime-incubators)!

---

## Machine Features
Unless otherwise specified, this mod reads extra data defined the [`CustomData`
field in
`OutputItem`](https://stardewvalleywiki.com/Modding:Machines#Item_processing_rules),
which is a map of arbitrary string keys to string values intended for mod use.
Since `CustomData` is per-output, it's possible to specify different settings
for each recipe, or even each output in the case of multiple possible outputs.

### Passive features/fixes
* Non-objects (eg hats, rings, clothing) can now be used as machine input.
* Item queries `ObjectColor` and machine rules' `CopyColor` now works to dye shirts.

----

### Adding additional fuel for a specific recipe

| Field Name                         | Description              |
| ---------------------------------- | ------------------------ |
| `selph.ExtraMachineConfig.RequirementId.1`<br>`selph.ExtraMachineConfig.RequirementTags.1` | The additional fuel that should be consumed by this recipe in addition to the ones specified in the machine's `AdditionalConsumedItems` field.<br> You can specify multiple fuels by adding another field with the same name, but with the number at the end incremented (eg. `ExtraMachineConfig.RequirementId.2`).<br> `RequirementId` allows specifying by qualified ID for a specific item, or a category ID for only categories (eg. `-2` will consume any gemstones as fuel), while `RequirementTags` allow specifying a comma-separated list of tags that must all match.<br>**CURRENT LIMITATION**: Both `RequirementId` and `RequirementTags` currently cannot be used for the same fuel number. If you need to specify both, add the item ID to the tag list (e.g. `"id_(o)itemid"`).|
| `selph.ExtraMachineConfig.RequirementCount.1` | The count of the additional fuel specified in the field above. Defaults to 1 if not specified. |
| `selph.ExtraMachineConfig.RequirementAddPriceMultiplier.1` | If specified, the fuel's price will be multiplied by the specified number, and added to the output item's final price (after `PriceModifiers`).|
| `selph.ExtraMachineConfig.RequirementNoDuplicate.1` | If specified, the fuel cannot have be the same item as the input item or any other consumed fuels (**NOTE**: Will *not* check `AdditionalConsumedItems`). Use this to make rules like "take any 3 fruit, but must be different fruits". Same item is defined as "have the same item ID or internal name" (so no mixing silver and gold quality apples!).|
| `selph.ExtraMachineConfig.RequirementInvalidMsg` | The message to show to players if all the requirements are not satisfied. Note that if there are multiple output rules with this field for the same input item, only the first one will be shown.|

KNOWN ISSUE: Recipes using `RequirementNoDuplicate` are not compatible with
Junimatic yet. I'll submit a PR to Junimatic to fix this.

#### Example

The example below adds a new recipe to furnaces that accepts 1 diamond, any 5
ores, and any milk item (in addition to the 1 coal required by the base
machine) and returns 4 iridium-quality diamonds if iridium ore was used, 3
gold-quality diamonds if gold ore was used, 2 silver-quality for iron, and 1
normal for copper.

<details>

<summary>Content Patcher definition</summary>

```
{
  "Changes": [
    {
      "LogName": "Add Diamond Milk Polishing (what) to Furnace Rules",
      "Action": "EditData",
      "Target": "Data/Machines",
      "TargetField": ["(BC)13", "OutputRules"],
      "Entries": {
        "PurifyDiamond": {
          "Id": "PurifyDiamond",
          "Triggers": [
            {
              "Id": "ItemPlacedInMachine",
              "Trigger": "ItemPlacedInMachine",
              "RequiredItemId": "(O)72",
              "RequiredCount": 1,
            }
          ],
          "UseFirstValidOutput": true,
          "OutputItem": [
            {
              "CustomData": {
                "selph.ExtraMachineConfig.RequirementId.1": "(O)386",
                "selph.ExtraMachineConfig.RequirementCount.1": "5",
                "selph.ExtraMachineConfig.RequirementInvalidMsg": "Need 5 ores and milk",
                "selph.ExtraMachineConfig.RequirementId.2": "-6",
              },
              "ItemId": "(O)72",
              "MinStack": 4,
              "Quality": 3,
              "ModData": {
                "selph.ExtraMachineConfig.ExtraContextTags": "milk_polished,milk_polished_iridium"
              },
            },
            {
              "CustomData": {
                "selph.ExtraMachineConfig.RequirementId.1": "(O)384",
                "selph.ExtraMachineConfig.RequirementCount.1": "5",
                "selph.ExtraMachineConfig.RequirementId.2": "-6",
              },
              "ItemId": "(O)72",
              "MinStack": 3,
              "Quality": 2,
              "ModData": {
                "selph.ExtraMachineConfig.ExtraContextTags": "milk_polished,milk_polished_gold"
              },
            },
            {
              "CustomData": {
                "selph.ExtraMachineConfig.RequirementId.1": "(O)380",
                "selph.ExtraMachineConfig.RequirementCount.1": "5",
                "selph.ExtraMachineConfig.RequirementId.2": "-6",
              },
              "ItemId": "(O)72",
              "MinStack": 2,
              "Quality": 1,
              "ModData": {
                "selph.ExtraMachineConfig.ExtraContextTags": "milk_polished,milk_polished_iron"
              },
            },
            {
              "CustomData": {
                "selph.ExtraMachineConfig.RequirementId.1": "(O)378",
                "selph.ExtraMachineConfig.RequirementCount.1": "5",
                "selph.ExtraMachineConfig.RequirementId.2": "-6",
              },
              "ItemId": "(O)72",
              "MinStack": 1,
              "Quality": 0,
              "ModData": {
                "selph.ExtraMachineConfig.ExtraContextTags": "milk_polished,milk_polished_copper"
              },
            },
          ],
          "MinutesUntilReady": 10,
        },
      },
    },
  ]
}
```
</details>

#### Inheriting the used fuels' flavor and color (in addition to the primary input item as detailed below)

**NOTE**: This feature is in beta. Please report any issues you find.

This feature integrates with the "Additional flavor and color" feature. The
`PreserveId` field and `ModData` values documented above in the item query can
be set to the following values, which will be replaced when the item is
created:

| Value                         | Description              |
| ---------------------------------- | ------------------------ |
| `DROP_IN_ID_1` | Will be replaced with the item ID/color of the fuel with the number `1`.|
| `DROP_IN_PRESERVE_1` | Will be replaced with the item ID/color of the *flavor* of the fuel with the number `1`.|
| `INPUT_EXTRA_ID_1` | Will be replaced with the input item's additional flavor/color with the number `1`.|

To avoid stacking, the machine output's internal name can contain the macro `PRESERVE_ID_1`, which will be replaced with the values resolved above.

This example creates a 'peanut butter and jelly' item that are flavored both after the input nut butter and the jelly used as additional fuel (required Cornucopia):

<details>

<summary>Content Patcher definition</summary>

```

{
  "LogName": "Add PBJ Rule",
  "Action": "EditData",
  "Target": "Data/Machines",
  "TargetField": ["(BC)15", "OutputRules"],
  "Entries": {
    "PeanutButter": {
      "Id": "PeanutButter",
      "Triggers": [
        {
          "Id": "ItemPlacedInMachine",
          "Trigger": "ItemPlacedInMachine",
          "RequiredItemId": "(O)Cornucopia_PeanutButter",
          "RequiredCount": 1,
        }
      ],
      "OutputItem": [
        {
          "CustomData": {
            "selph.ExtraMachineConfig.CopyColor": "true",
            "selph.ExtraMachineConfig.RequirementId.1": "(O)344",
            "selph.ExtraMachineConfig.RequirementAddPriceMultiplier.1": "1.5",
            "selph.ExtraMachineConfig.RequirementId.2": "(O)216",
            "selph.ExtraMachineConfig.RequirementInvalidMsg": "{{i18n:PBJSandwich.machineMissingFuel}}",
          },
          "ItemId": "{{ModId}}.PBJSandwich",
          "PreserveId": "Cornucopia_Peanut",
          "ObjectDisplayName": "{{i18n:PBJSandwich.name}}",
          "ObjectInternalName": "{0} Butter and PRESERVE_ID_1 Jelly Sandwich",
          "CopyPrice": true,
          "CopyColor": true,
          "PriceModifiers": 
          [
            // PB is classic, so increment the price by a little bit more (:
            {
              "Modification": "Multiply",
              "Amount": 2,
            },
            {
              "Modification": "Add",
              "Amount": 100,
            },
          ],
          "ModData": {
            "selph.ExtraMachineConfig.ExtraPreserveId.1": "DROP_IN_PRESERVE_1",
            "selph.ExtraMachineConfig.ExtraColor.1": "DROP_IN_ID_1",
          },
        },
      ],
      "MinutesUntilReady": 10,
    },
    "GenericButter": {
      "Id": "GenericButter",
      "Triggers": [
        {
          "Id": "ItemPlacedInMachine",
          "Trigger": "ItemPlacedInMachine",
          "RequiredItemId": "(O)Cornucopia_NutButter",
          "RequiredCount": 1,
        }
      ],
      "OutputItem": [
        {
          "CustomData": {
            "selph.ExtraMachineConfig.InheritPreserveId": "true",
            "selph.ExtraMachineConfig.CopyColor": "true",
            "selph.ExtraMachineConfig.RequirementId.1": "(O)344",
            "selph.ExtraMachineConfig.RequirementAddPriceMultiplier.1": "1.5",
            "selph.ExtraMachineConfig.RequirementId.2": "(O)216",
            "selph.ExtraMachineConfig.RequirementInvalidMsg": "{{i18n:PBJSandwich.machineMissingFuel}}",
          },
          "ItemId": "{{ModId}}.PBJSandwich",
          "ObjectDisplayName": "{{i18n:PBJSandwich.name}}",
          "ObjectInternalName": "{0} Butter and PRESERVE_ID_1 Jelly Sandwich",
          "CopyPrice": true,
          "CopyColor": true,
          "PriceModifiers": 
          [
            {
              "Modification": "Multiply",
              "Amount": 1.5,
            },
            {
              "Modification": "Add",
              "Amount": 100,
            },
          ],
          "ModData": {
            "selph.ExtraMachineConfig.ExtraPreserveId.1": "DROP_IN_PRESERVE_1",
            "selph.ExtraMachineConfig.ExtraColor.1": "DROP_IN_ID_1",
          },
        },
      ],
      "MinutesUntilReady": 10,
    },
  },
},


```
</details>

This second example adds a new recipe to the Furnace (yeah) which requires 2 of
any 4 fruits, and produces a "Fruit A, Fruit B, Fruit C and Fruit D" Jelly
item.

<details>

<summary>Content Patcher definition</summary>

```
{
  "LogName": "Modify Furnace Rules",
  "Action": "EditData",
  "Target": "Data/Machines",
  "TargetField": ["(BC)13", "OutputRules"],
  "Entries": {
    "FruitSalad": {
      "Id": "FruitSalad",
      "Triggers": [
        {
          "Id": "ItemPlacedInMachine",
          "Trigger": "ItemPlacedInMachine",
          "RequiredTags": ["category_fruits"],
          "RequiredCount": 2,
        }
      ],
      "UseFirstValidOutput": true,
      "OutputItem": [
        {
          "CustomData": {
            "selph.ExtraMachineConfig.RequirementTags.1": "category_fruits",
            "selph.ExtraMachineConfig.RequirementCount.1": "2",
            "selph.ExtraMachineConfig.RequirementAddPriceMultiplier.1": "2",
            "selph.ExtraMachineConfig.RequirementNoDuplicate.1": "true",
            "selph.ExtraMachineConfig.RequirementTags.2": "category_fruits",
            "selph.ExtraMachineConfig.RequirementCount.2": "2",
            "selph.ExtraMachineConfig.RequirementAddPriceMultiplier.2": "2",
            "selph.ExtraMachineConfig.RequirementNoDuplicate.2": "true",
            "selph.ExtraMachineConfig.RequirementTags.3": "category_fruits",
            "selph.ExtraMachineConfig.RequirementCount.3": "2",
            "selph.ExtraMachineConfig.RequirementAddPriceMultiplier.3": "2",
            "selph.ExtraMachineConfig.RequirementNoDuplicate.3": "true",
            "selph.ExtraMachineConfig.RequirementInvalidMsg": "Requires 2 of any 4 fruits",
            "selph.ExtraMachineConfig.CopyColor": "true",
          },
          "ItemId": "(O)344",
          "PreserveId": "DROP_IN",
          "ObjectInternalName": "Jelly PRESERVE_ID PRESERVE_ID_1 PRESERVE_ID_2 PRESERVE_ID_3",
          "ObjectDisplayName": "%PRESERVED_DISPLAY_NAME, %EXTRA_PRESERVED_DISPLAY_NAME_1, %EXTRA_PRESERVED_DISPLAY_NAME_2 and %EXTRA_PRESERVED_DISPLAY_NAME_3 Jelly",
          "ModData": {
            "selph.ExtraMachineConfig.ExtraPreserveId.1": "DROP_IN_ID_1",
            "selph.ExtraMachineConfig.ExtraPreserveId.2": "DROP_IN_ID_2",
            "selph.ExtraMachineConfig.ExtraPreserveId.3": "DROP_IN_ID_3",
          },
        },
      ],
      "MinutesUntilReady": 10,
    },
  },
},


```
</details>

----

### Output inherit the flavor of input items

**NOTE**: This feature is deprecated in the upcoming Stardew Valley 1.6.9, which supports this feature natively.

| Field Name                         | Description              |
| ---------------------------------- | ------------------------ |
| `selph.ExtraMachineConfig.InheritPreserveId` | When set to any value, copies the input item's flavor (e.g. the "Blueberry" part of "Blueberry Wine") into the output item.|

NOTE: Version 1.0.0 also supports this functionality via setting the
`PreserveId` field to `"INHERIT"`. This is deprecated and might no longer work in later versions.

#### Example

The example below modifies the base game's honey to mead recipe to also copy the
honey's flower flavor to the mead, and increment its price accordingly.

<details>

<summary>Content Patcher definition</summary>

```
{
  "Changes": [
    {
      "LogName": "Modify Mead Rules",
      "Action": "EditData",
      "Target": "Data/Machines",
      "TargetField": ["(BC)12", "OutputRules", "Default_Honey", "OutputItem", "(O)459"],
      "Entries": {
        "CustomData": {
          "selph.ExtraMachineConfig.InheritPreserveId": "true",
          // See below for that this does
          "selph.ExtraMachineConfig.UnflavoredDisplayNameOverride": "{{i18n: selph.FlavoredMead.WildMead.name}}"
        },
        "CopyPrice": true,
        "ObjectInternalName": "{0} Mead",
        // See https://stardewvalleywiki.com/Modding:Item_queries#Item_spawn_fields
        "ObjectDisplayName": "[LocalizedText Strings\\Objects:selph.FlavoredMead.name %PRESERVED_DISPLAY_NAME]",
        "PriceModifiers": 
        [
          {
            "Modification": "Add",
            "Amount": 100
          },
          {
            "Modification": "Multiply",
            "Amount": 2
          }
        ],
      },
    },
  ]
}
```

</details>

----

### Output inherit the dye color of input items

**NOTE**: This feature is deprecated in the upcoming Stardew Valley 1.6.9, which supports this feature natively.

| Field Name                         | Description              |
| ---------------------------------- | ------------------------ |
| `selph.ExtraMachineConfig.CopyColor` | When set to any value, copies the input item's color into the output item.<br>The difference between this settings and the base game's `CopyColor` is that the latter only supports copying the color of colored items (eg. flowers), while the former will copy the dye color if the input is not a colored object.<br>Make sure the output item's next sprite is a monochrome color sprite, otherwise the coloring might look weird.|

#### Example

The example below adds a new recipe to preserves jar that turns a gemstone into
5 fairy roses of the gem's color. Without this mod enabled, the roses will all
be of the base color, even with `CopyColor` set.

<details>

<summary>Content Patcher definition</summary>

```
{
  "Changes": [
    {
      "LogName": "Add Gemstone To Rose Rule",
      "Action": "EditData",
      "Target": "Data/Machines",
      "TargetField": ["(BC)15", "OutputRules"],
      "Entries": {
        "RoseMaker": {
          "Id": "RoseMaker",
          "Triggers": [
            {
              "Id": "ItemPlacedInMachine",
              "Trigger": "ItemPlacedInMachine",
              "RequiredTags": ["category_gem"],
              "RequiredCount": 1,
            }
          ],
          "OutputItem": [
            {
              "CustomData": {
                "selph.ExtraMachineConfig.CopyColor": "true",
              },
              "ItemId": "(O)595",
              "MinStack": 5,
              // This does nothing
              "CopyColor": true,
            },
          ],
          "MinutesUntilReady": 10,
        },
      },
    },
  ]
}
```

</details>

----

### Specify range of input count and scale output count with the input amount consumed

| Field Name                         | Description              |
| ---------------------------------- | ------------------------ |
| `selph.ExtraMachineConfig.RequiredCountMax` | When set to an int (as a string), the primary input item's count can be between the min value of the trigger rule's `RequiredCount`, or the max value as specified by this field.<br>The output item's stack count will be set to equal the amount of input item consumed, and `MinStack` and `MaxStack` will be ignored. To modify the stack count, use `StackModifiers` and `StackModifiersMode`.<br>The required fuels (either via `AdditionalConsumedItems` or this mod's per-recipe fuels) will remain the same regardless of how many input items are consumed. For the fuel added by this mod, if you want the amount consumed to depend on the amount of input items consumed, make multiple output rules conditioned on the input item's stack size.|
| `selph.ExtraMachineConfig.RequiredCountDivisibleBy` | When set to an int (as a string), the input count will be rounded down to the nearest multiple of this number. For example, specify 5 to make the machine take 5, 10, 15, etc.|

Note that this functionality is completely achievable with vanilla machine
rules, using `RequiredCount` and output rules condition. This macro simply
reduces repetition, and is not as customizable as actual machine rules.

#### Example

The example below modifies the vanilla game's wine recipe to be able to process up to 10 fruits at a time, and produce a wine for every fruit used. If less than 10 fruits are used, only that amount of fruit will be processed.

<details>

<summary>Content Patcher definition</summary>

```
{
  "Changes": [
    {
      "LogName": "Modify Wine Rules",
      "Action": "EditData",
      "Target": "Data/Machines",
      "TargetField": ["(BC)12", "OutputRules", "Default_Wine", "OutputItem", "Default"],
      "Fields": {
        "CustomData": {
          "selph.ExtraMachineConfig.RequiredCountMax": "10",
        },
      },
    },
  ]
}
```

</details>

----

### Adding extra byproducts for machine recipes

First, write your extra output item queries to a new asset named
`selph.ExtraMachineConfig/ExtraOutputs`. This asset is a map of unique IDs to
item queries similar to the ones used in the recipe's `OutputItem` field. Most
machine-related features (e.g. `CopyColor`, `CopyQuality`, this mod's
`CustomData` fields, etc.) will be supported in these item queries.

Then, set this field in the actual machine output's `CustomData` dict as usual:

| Field Name                         | Description              |
| ---------------------------------- | ------------------------ |
| `selph.ExtraMachineConfig.ExtraOutputIds` | A comma-separated list of item query IDs written to the asset above to also spawn with this output item.|

NOTE: You can also set this field on the `Data/Machines`  data's `CustomFields` dict to add a byproduct to *every* recipe associated with this machine!

#### Example

This example modifies the vanilla fruit to wine keg recipe to also spawn fruit-flavored jelly and mead alongside the wine item.

<details>

<summary>Content Patcher definition</summary>

```
{
  "Changes": [
    {
      "LogName": "Add Jelly&Mead Extra Outputs",
      "Action": "EditData",
      "Target": "selph.ExtraMachineConfig/ExtraOutputs",
      "Entries": {
        "JellyExtra": {
          "Id": "JellyExtra",
          "ItemId": "FLAVORED_ITEM Jelly DROP_IN_ID",
        },
        "MeadExtra": {
          "Id": "MeadExtra",
          "ItemId": "(O)459",
          "PreserveId": "DROP_IN",
          "ObjectInternalName": "{0} Mead",
          "ObjectDisplayName": "%PRESERVED_DISPLAY_NAME Mead",
          "PriceModifiers": 
          [
            {
              "Modification": "Multiply",
              "Amount": 2
            },
            {
              "Modification": "Add",
              "Amount": 100
            }
          ],
        },
      },
    },
    {
      "LogName": "Add Jelly and Mead to Wine Rule",
      "Action": "EditData",
      "Target": "Data/Machines",
      "TargetField": ["(BC)12", "OutputRules", "Default_Wine", "OutputItem", "Default", "CustomData"],
      "Entries": {
        "selph.ExtraMachineConfig.ExtraOutputIds": "JellyExtra,MeadExtra",
      },
    },
  ]
}
```

</details>

---

### Allow non-Object outputs from machines

SDV machines currently have a limitation where they're unable to output
weapons, hats, and other item types that do not inherit from the `Object` type.
You can get around this by leveraging the 'multiple outputs' feature
and setting the primary input as an item the qualified ID
`(O)selph.ExtraMachineConfig.Holder`, and the secondary input as the actual
non-`Object` output. The holder item's only purpose is to wrap the secondary
output in a way that the machine can hold it, and it will immediately disappear
if it enters the player's inventory or a chest.

IMPORTANT NOTE: Also set either `ObjectColor`, or if you're using a custom output method, output a
`ColoredObject` on the holder item's output entry. This makes zero mechanical
difference, but it makes the game properly draw the holder item as its content
properly (colored objects force the game to use the full draw function instead
of directly grabbing the item sprite from data).

#### Example

This example allows mystery boxes to be crushable by the Geode crusher;
previously this was impossible because mystery boxes may contain weapons or
hats, which cannot be outputted by a machine.

(TODO: This example isn't perfect - needs limiting the trigger to only geodes/mystery boxes. Vanilla relies on the C# output method to filter the input, which doesn't happen here since we moved it to the byproduce. If you're looking to actually make mystery boxes crushable, I recommend adding a separate rule for them next to the vanilla crusher rule.)

<details>

<summary>Content Patcher definition</summary>

```
{
  "Changes": [
    {
      "LogName": "Change geode crusher output to holder item as the primary input",
      "Action": "EditData",
      "Target": "Data/Machines",
      "TargetField": ["(BC)182", "OutputRules", "Default", "OutputItem", "Default"],
      "Entries": {
        "CustomData": {
          "selph.ExtraMachineConfig.ExtraOutputIds": "ActualGeodeCrusherOutput",
        },
        "OutputMethod": null,
        "ItemId": "selph.ExtraMachineConfig.Holder",
        "CopyColor": true,
      },
    },
    {
      "LogName": "Add Geode Crusher actual output",
      "Action": "EditData",
      "Target": "selph.ExtraMachineConfig/ExtraOutputs",
      "Entries": {
        "ActualGeodeCrusherOutput": {
          "Id": "ActualGeodeCrusherOutput",
          "OutputMethod": "StardewValley.Object, Stardew Valley: OutputGeodeCrusher",
        },
      },
    },
    {
      "LogName": "Unban mystery boxes from geode crushers",
      "Action": "EditData",
      "Target": "Data/Objects",
      "TargetField": ["MysteryBox", "ContextTags"],
      "Entries": {
        "geode_crusher_ignored": null,
      },
    },
  ]
}
```

</details>

---

### Generate nearby flower-flavored modded items (or, generate flavored items outside of machines)

This mod implements a new item query, `selph.ExtraMachineConfig_FLAVORED_ITEM`
that acts as the generic version of the base game's
[`FLAVORED_ITEM`](https://stardewvalleywiki.com/Modding:Item_queries#Available_queries),
but usable for any modded items. The query takes the following arguments:

`selph.ExtraMachineConfig_FLAVORED_ITEM <output item ID> <flavor item ID> [optional override price]`

Replace <output item ID> with your modded artisan item ID, and flavor item ID
with your desired flavor, including
[`NEARBY_FLOWER_ID`](https://stardewvalleywiki.com/Modding:Machines) to take
the ID of a nearby flower if any. Make both of them unqualified (ie. without
the `(O)` part), or you may get harmless errors in the console.

For example, the following creates nearby flower-flavored mead:

`"ItemId": "selph.ExtraMachineConfig_FLAVORED_ITEM 459 NEARBY_FLOWER_ID"`

The flavored output item spawned by this query will:

* Have its flavor set to the flavor item ID.
  * Note that like the vanilla `FLAVORED_ITEM` rule, if the flavor is `-1` (due
    to the `NEARBY_FLOWER_ID` macro) it will be kept as-is and mess up the
    display name if you use `%PRESERVED_DISPLAY_NAME`! Stardew Valley 1.6.9
    will fix this as part of the new built-in flavor inherit feature, but in
    the mean time use the field `UnflavoredDisplayNameOverride` (as detailed
    below) to get around this.
* Inherit the color of the flavor item, if any. If you don't want this, simply
  put an empty sprite next to the item's sprite on the sprite sheet.
* Have its price set to the first matching entry of the below list:
  * The optional third parameter, if specified
  * The flavor item's price, if applicable
  * The item's base price otherwise. It's recommended that the base price be
    lower than the potential price of the flavor ingredient item to avoid the
    unflavored item being more expensive than flavored ones.
* If you want to scale the price further, use the machine rules' `PriceModifiers`.

Everything else (e.g. display name, etc.) will have to be set manually by the rest of the item/machine query.

Note that this item query technically can be used outside of machine rules.

---

### Override display name if the output item is unflavored

| Field Name                         | Description              |
| ---------------------------------- | ------------------------ |
| `selph.ExtraMachineConfig.UnflavoredDisplayNameOverride` | The display name to use for this machine rule's output item if the output happens to be unflavored (due to `InheritPreserveId` copying from an unflavored item, or if `NEARBY_FLOWER_ID` cannot find a nearby flower). |

---

### Generate an input item for recipes that don't have any, and use 'nearby flower' as a possible query

NOTE: This functionality is rather specialized and should not be used unless
you know what you're doing. I'll likely tweak this further; please give
feedback/feature requests if you're interested.

| Field Name                         | Description              |
| ---------------------------------- | ------------------------ |
| `selph.ExtraMachineConfig.OverrideInputItemId` | The item query to use as the actual "input", regardless of what/whether you used an input item. Supports an item query, or if set to `NEARBY_FLOWER_QUALIFIED_ID`, get a nearby flower, or null if no nearby flowers.<br>With this set, you can make use of features that requires a valid input item (`CopyColor`, `CopyPrice`, `PreserveId`, etc.) for non-input trigger rules like `DayUpdate`.|

#### Example

This example modifies the vanilla beehouse recipe to generate flower-flavored
mead instead of honey. It also colors the mead using
`selph.ExtraMachineConfig.CopyColor` (though the sprites will look weird since
the default mead sprite doesn't have a mask).

<details>

<summary>Content Patcher definition</summary>

```
{
  "Changes": [
    {
      "LogName": "Change Beehouse To Mead",
      "Action": "EditData",
      "Target": "Data/Machines",
      "TargetField": ["(BC)10", "OutputRules", "Default", "OutputItem", "Default"],
      "Entries": {
        "ItemId": "(O)459",
        "ObjectInternalName": "{0} Mead",
        "ObjectDisplayName": "%PRESERVED_DISPLAY_NAME Mead",
        "PriceModifiers": [
          {
            "Id": "FlowerBase",
            "Modification": "Multiply",
            "Amount": 4.0,
            // This condition will be false if input item is null, and true otherwise.
            // It is to ensure we only apply the price change if there's an actual nearby flower.
            // Otherwise, if no flowers are found, it will apply the price change on top of the base mead item!
            "Condition": "ITEM_PRICE Input 0"
          },
          {
            "Id": "HoneyBase",
            "Modification": "Add",
            "Amount": 300,
            "Condition": "ITEM_PRICE Input 0"
          },
        ],
        // All fields below this will only apply if there is an actual nearby flower.
        "CopyPrice": true,
        "PreserveId": "DROP_IN",
        "CustomData": {
          "selph.ExtraMachineConfig.CopyColor": "true",
          "selph.ExtraMachineConfig.OverrideInputItemId": "NEARBY_FLOWER_QUALIFIED_ID",
        },
      },
    },
  ]
}
```

</details>

----

### Automatic machines that stop producing after X times

| Field Name                         | Description              |
| ---------------------------------- | ------------------------ |
| `selph.ExtraMachineConfig.AutomaticProduceCount` | For machines that use `OutputCollected` and `ItemPlacedInMachine` (ie a Crystalarium clone), the number of times this output rule can automatically produce (including from `OutputCollected` reloading) before it must be reloaded with another input item via `ItemPlacedInMachine`. The count is global and will apply to every recipe by this machine, and each output item can (and must!) specify its own count.<br>Will ignore `DayUpdate` and `MachinePutDown`, since it doesn't make sense to apply to these rules.|

#### Example

This example changes the coffee maker to require reloading with 5 coffee beans every 5 coffees collected.

<details>

<summary>Content Patcher definition</summary>

```
{
  "Changes": [
    {
      "LogName": "Coffee Maker must be reloaded every 5 coffees",
      "Action": "EditData",
      "Target": "Data/Machines",
      "TargetField": ["(BC)246", "OutputRules", "Default"],
      "Entries": {
        "Triggers": [
          {
            "Id": "OutputCollected",
            "Trigger": "OutputCollected",
          },
          {
            "Id": "ItemPlacedInMachine",
            "Trigger": "ItemPlacedInMachine",
            "RequiredItemId": "(O)433",
            "RequiredCount": 5,
          },
        ],
        "OutputItem": [
          {
            "Id": "(O)395",
            "ItemId": "(O)395",
            "CustomData": {
              "selph.ExtraMachineConfig.AutomaticProduceCount": "5",
            },
          }
        ],
        "MinutesUntilReady": -1,
        "DaysUntilReady": 1,
      },
    },
  ]
}
```

</details>


---

### Run trigger action on machine ready

NOTE: This can also be added to the machine entry in `Data/Machines`'s `CustomFields` dictionary to make it run for every rule.

| Field Name                         | Description              |
| ---------------------------------- | ------------------------ |
| `selph.ExtraMachineConfig.TriggerActionToRunWhenReady` | Run this trigger action string when this machine rule is ready for harvest.|

----

### Custom casks

NOTE: Add these to the entry in `Data/Machines`'s `CustomFields` dictionary.

| Field Name                         | Description              |
| ---------------------------------- | ------------------------ |
| `selph.ExtraMachineConfig.IsCustomCask` | Whether this machine is a cask, and should subject to cask-like behavior (ie. can use cask rules, drop item when smacked, has quality star, only usable in a cellar unless specified below)|
| `selph.ExtraMachineConfig.CaskWorksAnywhere` | If set, this cask can be placed anywhere.|
| `selph.ExtraMachineConfig.AllowMoreThanOneQualityIncrement` | Casks by default can only increment quality by one per day. If set, this limitation is removed.|
| `selph.ExtraMachineConfig.CaskStarLocationX` | Add this, and the Y one too, to be able to set the position of the star location on the cask. For the value put the amount of pixels on the X axis (Horizontal). |
| `selph.ExtraMachineConfig.CaskStarLocationY` | Put the amount of pixels on the Y axis (Vertical).  |

-----

### Custom slime incubators 

Add these to the entry in `Data/Machines`'s `CustomFields` dictionary.

| Field Name                         | Description              |
| ---------------------------------- | ------------------------ |
| `selph.ExtraMachineConfig.IsCustomSlimeIncubator` | Whether this machine is a slime incubator and should create slime monsters when the machine is ready. Its machine rules should be the same as vanilla (ie. only accepts the 4 slime eggs), but you can specify any ready time you want.|

-----

### Machines that spit out the input item when removed

Add these to the entry in `Data/Machines`'s `CustomFields` dictionary.

| Field Name                         | Description              |
| ---------------------------------- | ------------------------ |
| `selph.ExtraMachineConfig.ReturnInput` | Whether this machine should return the *output* item (despite the name) as debris when removed, like the crystalarium. Only works for `ItemPlacedInMachine` and `OutputCollected` rules. This should only be used for crystalarium-like machines where the input is the same as the output. Let me know if you want this to also work for machines where the input is different from the output.|
| `selph.ExtraMachineConfig.ReturnActualInput` | Whether this machine should return the actual input item as debris when removed. Only works for `ItemPlacedInMachine` and `OutputCollected` rules.|

-----

### On machine ready effects

This requires writing to a new asset named
`selph.ExtraMachineConfig/ExtraMachineData`, where the key is the qualified
item ID of the machine (like `Data/Machines`), and the value being a model with
the following field:

| Field Name                         | Description              |
| ---------------------------------- | ------------------------ |
| `ReadyEffects` | The effects to play once when this machine is ready for harvest. It is identical to the vanilla `LoadEffects`/`WorkingEffects` fields (including support for GSQs), with the addition of one extra field:<br><br> `IncrementMachineParentSheetIndex`: the amount to increment the machine's sprite sheet index while it is holding this output. This can be used to have your machine's appearance differ depending on what produced.|

#### Example

This example makes the keg play the wood chop sound, a brief funky animation, and turns into a furnace when ready:

<details>

<summary>Content Patcher definition</summary>

```
{
  "Changes": [
    {
      "LogName": "Add ready effect to keg",
      "Action": "EditData",
      "Target": "selph.ExtraMachineConfig/ExtraMachineData",
      "Entries": {
        "(BC)12": {
          "ReadyEffects": [
            {
              "Id": "Animation",
              "Sounds": [
                {
                  "Id": "axchop"
                }
              ],
              "ShakeDuration": 1000,
              "Frames": [0, 1, 2],
              "Interval": 100,
              "IncrementMachineParentSheetIndex": 1,
            }
          ]
        }
      },
    },
  ]
}
```

</details>

-----

### Edibility based on input 

In machine output's `CustomData`:

| Field Name                         | Description              |
| ---------------------------------- | ------------------------ |
| `selph.ExtraMachineConfig.InputEdibilityMultiplier` | If set, the output's edibility will be a multiplier of the primary input's edibility. For reference, the vanilla Jelly is 2. This will apply even if the input item's edibility is negative.|

-----

### Colored draw layers based on output item

Set these fields on the machine's top level `CustomFields` field in `Data/Machines`.

| Field Name                         | Description              |
| ---------------------------------- | ------------------------ |
| `selph.ExtraMachineConfig.DrawLayerTexture` | The texture asset to use for the extra draw layer on top of the machine. This will be drawn if the machine has an output item (whether ready or processing). Arrange it similarly to the big craftables sheet (ie. 1x2). |
| `selph.ExtraMachineConfig.DrawLayerTextureIndex` | The starting index inside the texture. NOTE: If the machine itself has a work/loading effect that changes it frame, the offset will also be applied to the draw layer's value. This is to also allow the layer to animate.|

-----

## Crafting/Cooking Features

### Use some machine-like features in crafting and cooking (namely copy flavor and color)

This feature utilizes a new CP asset to write to named
`selph.ExtraMachineConfig/ExtraCraftingConfig`. This asset is a map where the
key is the name of a crafting recipe defined in `Data/CraftingRecipes` or
`Data/CookingRecipes`, and the value is a model object containing extra configs
to apply to the recipe. The model contains the following fields, all of which are optional:

| Field Name                         | Type              | Description              |
| ---------------------------------- | ------------------------ | ------------------------ |
| `ObjectDisplayName` | `string` | The override display name to use for the item made by this recipe.|
| `ObjectInternalName` | `string` | The override internal name to use for the item made by this recipe, to avoid stacking in the case of flavored items. The internal name can contain the strings `PRESERVE_ID_0`, `PRESERVE_ID_1`, etc., which will be replaced with the item's flavor IDs (the primary one for `_0`, and the secondary ones added by this mod for `_1` and onward).|
| `IngredientConfigs` | `List<IngredientConfig>` | A list of options to apply for each of the ingredient consumed by this recipe.|

`IngredientConfig` is an object with the following fields, all of which are optional aside from `Id` and one of `ItemId` or `ContextTags`:

| Field Name                         | Type              | Description              |
| ---------------------------------- | ------------------------ | ------------------------ |
| `Id` | `string` | The unique ID of this entry. Must be unique within the same list, but not used outside of CP patching.|
| `ItemId` | `string` | The item ID of the ingredient this config should apply to. Can be the item ID, or category number.|
| `ContextTags` | `string` | The context tags of the ingredient this config should apply to. Useful for SpaceCore's context tags-based ingredients feature, or simply for further filtering.|
| `InputPreserveId` | `string` | The flavor ID to pass in the output item. Use any item IDs here, or `DROP_IN_ID` for the ingredient's ID, or `DROP_IN_PRESERVE` for its flavor.|
| `OutputPreserveId` | `int` | The output item's flavor ID to override with the input flavor specified above. 0 for the primary flavor, 1 and above for secondary.|
| `OutputColor` | `int` | The output item's color to override with the input item's color. 0 for the primary color, 1 and above for secondary.|
| `OutputPriceMultiplier` | `float` | If specified, the ingredient's price will be multiplied by this number and added to the output item's price.|

Multiple configs can be used for one ingredient if more fine grained control is needed, and each ingredient will match with at most one config.

Known issues:

* This feature is compatible with vanilla, SpaceCore, and Better Crafting.
* With Better Crafting, bulk crafting recipes modified by this mod does not
  currently work, and you need to craft one at a time.
* Partially compatible with Yet Another Cooking Skill (requires Better Crafting).
* Not currently compatible with Love of Cooking, unless you use Better Crafting.

#### Example

This example modifies the base game's sashimi recipe to make it flavored after
the input fish, setting the output display name (e.g. Tuna Sashimi) and price
(add the input fish's price times 0.5, so not that profitable depending on the
fish used) to match:

<details>

<summary>Content Patcher definition</summary>

```
{
  "LogName": "Add flavored sashimi cooking rules",
  "Action": "EditData",
  "Target": "selph.ExtraMachineConfig/ExtraCraftingConfig",
  "Entries": {
    "Sashimi": {
      "Id": "Sashimi",
      "IngredientConfigs": [
        {
          "ItemId": "-4",
          "InputPreserveId": "DROP_IN_ID",
          "OutputPreserveId": 0,
          "OutputPriceMultiplier": 0.5,
        },
      ],
      // This should be in i18n obviously
      "ObjectDisplayName": "%PRESERVED_DISPLAY_NAME Sashimi",
      "ObjectInternalName": "PRESERVE_ID_0 Sashimi",
    },
  },
},
```
</details>
