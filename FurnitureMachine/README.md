# Furniture Machine

Furniture Machine is a
Stardew Valley mod that allows furnitures to function as machines (in the
vanilla game only `"DayUpdate"` rules work, and it is clearly an unintended
interaction). It is essentially a reimagining of Bigger Craftables, albeit with
easier usage instructions, compat for 1.6's new machine features.

[Download Link](https://github.com/zombifier/My_Stardew_Mods/releases)

This document is for modders looking to incorporate this mod into their own
content packs. For users, install the mod as usual from the link above.

## Instructions

First, add your [furniture item](https://stardewvalleywiki.com/Modding:Items#Furniture):
* For best results, use the `other` type. Other types of interactable furniture
  are not tested, and likely will not work.
* Explicitly set tilesheet size and bounding box size. The mod cannot infer those.
* Set rotations to 1. The mod currently does not work with rotateable furniture.
* Add `furniture_machine` to the furniture's context tags. This enables machine
  interactions with the furniture item; namely item input, automatic produce if
  defined, breakable with a tool if processing, increasing stack count to 999,
  and more.
* Optionally add `dont_draw_held_object_while_processing` to not draw the
  currently processed item on its surface (like items placed on tables).
* Optionally add `dont_draw_held_object_when_ready` to not draw a ready item.
* Optionally add `dont_draw_ready_bubble` to not draw the ready item bubble.

Next, define your [machine rules](https://stardewvalleywiki.com/Modding:Machines) as usual:
* The key would be the qualified item ID, so something like `(F)YourFurnitureIdDefinedAbove`.
* Wobble effects currently does not work. Use load/working effects to indicate that your machine is working.
* For load/working frames, since furniture will be larger than 16x16 (or 1x1
  tiles) the sprite index's frames will be a few tiles away from the main
  sprite and not immediately 1 tile after it. Keep this in mind when setting them.
* Consequently, `ShowNextIndexWhileWorking` and `ShowNextIndexWhenReady` won't
  quite work properly, because the next index will usually still be inside the
  furniture sprite. To properly specify what index should be used, add the
  field `"selph.FurnitureMachine.NextIndexToShow"` to the machine data's
  `CustomFields`, with the value being the absolute sprite index to use for
  those fields. For example, if you add a 2x2 item at sprite index 20, then
  you'd usually want to set that to 22, unless the next sprite is in a new row.

Crafting Addendum
* Stardew Valley 1.6.9 and newer supports
  [crafting](https://stardewvalleywiki.com/Modding:Recipe_data) furniture as
  well as using furniture in crafting; simply provide the full qualified IDs
  (with `(F)` in it) your recipe's ingredient list and craft result. Set "is
  big craftable" to false.
* Unfortunately the crafting page will look weird as the big furniture item
  will overlap with other craftables; this is a bug in the vanilla game that
  can be fixed by using [Better
  Crafting](https://www.nexusmods.com/stardewvalley/mods/11115).

### Mod compat notes:
* Support for Furniture Framework is untested, and likely will not work. FF's
  features are tailored toward better decorative furniture, so it is unlikely
  there will be overlap with this mod's feature set.
* Automate is somewhat supported, with two caveats:
   * Only the top left tile will be checked. Use path connectors underneath the
     furniture, and press `U` to see the overlay and see if you did it
     correctly.
   * Placing a furniture machine will not update Automate's cache. Place a
     regular machine or a path connector next to the chest to force it to
     update its cache.
