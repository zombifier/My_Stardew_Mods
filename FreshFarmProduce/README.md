# Farm Fresh Produce

[Farm Fresh Produce](https://www.nexusmods.com/stardewvalley/mods/22256)
is a Stardew Valley mod that incentivizes shipping a wide variety of raw, fresh produce

This document is mainly intended for modders. For mod users, install the mod
from the link above.


- [Farm Fresh Produce](#farm-fresh-produce)
   * [Description (THIS SECTION TO BE MOVED TO NEXUS)](#description-this-section-to-be-moved-to-nexus)
      + [Mini-Desc](#mini-desc)
      + [Intro](#intro)
      + [Fresh Farm Produce](#fresh-farm-produce)
      + [Farm of the Season](#farm-of-the-season)
      + [Installation Instruction](#installation-instruction)
   * [For Mod Authors](#for-mod-authors)
      + [Freshness Features](#freshness-features)
      + [Modify the Competition](#modify-the-competition)
      + [Console Commands](#console-commands)

## Description (THIS SECTION TO BE MOVED TO NEXUS)

### Mini-Desc

Be rewarded for selling fresh, high quality produce. Increase sell price of
high quality crops and other organic items, and add a season-long competition
to crown the farm with the highest variety of fresh produce in all of the
Ferngill Republic!

### Intro

Are you bothered that the late game optimal money making strategy is to
sell bottles and bottles of starfruit/ancient fruit wine and nothing else?
Where are the fresh veggies, fresh fruits, even fresh eggs that a typical real
world farm is known for?

While there are mods that attempt to solve these problems by either introducing
a fluctuating supply/demand system, or straight up spoiling items to discourage
hoarding everything for kegs/jars, in my opinion they're a little too exploitable,
punishing and game-y for my taste. Instead, I want to simply reward players for
selling a variety of raw produce, and make converting things into artisan goods not
always the best option available; specifically, farmers are now encouraged to
sell gold and iridium quality crops, and process the low quality remainders
into juice/pickles/what-have-yous, just like a real farm!

This is accomplished by two features that work together in harmony:

### Fresh Farm Produce
Freshly harvested organic goods (fruits, veggies, fish, milk and egg, etc.) now
have their prices increased. The increase is lesser for lower quality, but
greatly improves as quality also rises:

* Regular (aka Stale) price modifiers:
   * Base: 1x
   * Silver: 1.25x
   * Gold: 1.5x
   * Iridium: 2x
* Fresh price modifiers:
   * Base: 1.25x (25% more)
   * Silver: 1.75x (40% more)
   * Gold: 2.5x (66% more)
   * Iridium: 4x (100% more)

This has a few balance implications:
* Fresh gold items are now more profitable than many vanilla artisan goods, and
  iridium items more profitable than all but modded ones. While the (very
  overpowered) Artisan profession still makes processing all but iridium items
  into artisan goods the most profitable route, the opportunity costs and
  diminishing returns are now worth considering for all but the latest,
  largest, [Shed](https://stardewvalleywiki.com/Shed)-iest of farms.
* [Deluxe Fertilizer](https://stardewvalleywiki.com/Deluxe_Fertilizer), an
  otherwise completely useless item in a late game farm, is now seriously worth
  considering over [Hyper
  Speed-Gro](https://stardewvalleywiki.com/Hyper_Speed-Gro) on a per-crop
  basis. A fresh Iridium Starfruit sells for 3300g - that's a lot of money!
* Artisan, as strong as it is, now have serious competition from its peer
  orofession Agriculturalist for those favoring a crop-based playthrough.
  Rancher is now also worth considering for an animal-based playthrough,
  because getting fresh iridium produce from animals are trivial at max friendship

For the number crunchers, see the collapsible below on analysis on specific
artisan goods:

<details>

* Fruit Wine (which doesn't inherit quality) sells for 3x the base fruit price
  (4.2x with Artisan) while Iridium Fresh Fruit now sells for 4x (4.4x with
  Tiller, which is required for Artisan)! Sure, it still beats Fresh Gold Fruit
  (3.3x with tiller), but it's not quite a productive use of 7 days.
* Gold Fresh Large Goat Milk (858g) now beats
  Gold Artisan-boosted Goat Cheese (840g)! Aging the Goat Cheese to
  Iridium (1120g) still beats it though, however it does *not* beat
  Iridium Fresh Large Goat Milk (1380g, or 1656g if Rancher is taken!).
* Iridium Smoked Fish (which do inherit quality) sells for 4x the base fish
  price, which makes them exactly on par with Iridium Fresh Fish also selling
  for 4x. With Artisan, Iridium Smoked Fish wins by 40%, but now
  you can weigh your cost - is it worth using one precious coal to increase
  the value just by 40%, compared to what would have been a 180% increase from
  non-fresh fish?

</details>

Be warned however - if you don't immediately sell fresh items the day of
harvest, they will go stale (marked as such in their display name) after you go
to bed and revert to boring vanilla prices, even inside fridges! Thankfully,
they don't spoil or change further, so feel free to turn that years-old apple
in the back of your fridge into a bottle of refreshing apple wine. Items inside
Junimo Chests also don't spoil, which gives you an incentive to use these
otherwise underwhelming items.

Here is a list of item categories affected by freshness:
<details>

* -4 (Fish)
* -5 (Eggs)
* -6 (Milk)
* -14 (Meat, with meat mods like Animal Husbandry)
* -75 (Vegetables)
* -79 (Fruit)
* -80 (Flowers)
* -81 (Forage)
* Special exception added for Coffee Beans, which can be "fresh". Other seeds do not qualify.

Notably, freshness does not apply to these categories:
* -7 (Cooking)
* -26 (Artisan Goods).
* -27 (Tapper output)
* -17 (Sweet Gem Berries and Truffles. Let's be honest, they're valuable enough already).
* -18 (Duck Feathers, Wools, and other non-perishable animal produce. This does
  make ducks, sheep and rabbits weaker, but I have a WIP mod that can hopefully
  remedy that).
* Any other category not listed here.
</details>

### Farm of the Season

So now farmers are encouraged to process low quality produce while directly
selling gold and iridium stuff, but what about variety over a field of just
Starfruits? That's where the Farm Of The Season competition comes in!

Once your farm gets featured in the Stardew Valley Tribunal (ie. earns a total
of 27000g), on the second day of every season you will receive a letter that says
you are entered into the Farm of the Season, a month-long competition to find
the best farm in all of Ferngill, with great prizes and amazing perks for those
who achieve a good score. Then, you can open the competition tracker window by
clicking on the Farm of the Season quest in your quest tracker.

Competition Details:

* Shipping fresh (unless not applicable) items will contribute points equal to
  what they'd earn at the Stardew Valley Fair to a single category. A category
  is won once its overall points threshold is reached. For example, for the
  "Vegetable Farm" category, shipping 10 Gold-Quality Parsnips will contribute
  40 points.
* Each unique item type can only contribute a certain amount to the overall
  category. Past that amount and they'll contribute only 25%, so you can spam a
  crop if you wish, but it alone likely won't carry you through the finish
  line!
* Flavored items have special handling - all items of the same base type are
  considered the same, but every unique flavor shipped extends the individual
  threshold by 5%. As an example, if the threshold is 100 points and you ship
  pickled parsnips, pickled kale and pickled cauliflower, pickles can
  contribute 115 points before the slowdown. So you're still rewarded for
  selling multiple types of pickles, but customers still don't like it if you
  sell only pickles and nothing else!
* You will get half credits for completing at least 50% of a category.

The following is a list of categories in the competition, and the amount of points needed with only vanilla:

* Vegetable Farm (FRESH veggies and Coffee Beans, 1000 points, max 200 points per item)
* Fruit Orchard (FRESH fruits and sweet gem berries, 1000 points, 200 points)
* Flower Garden (FRESH flowers and honey, 400 points, 200 points) (Disable if Inflorescence installed?)
* Forage Fields (FRESH forage, tapper produce, other misc items like truffles, 800 points, 200 points)
* Dairy Farm (FRESH milk, 400 points, 200 points)
* Egg Ranch (FRESH egg, 400 points, 200 points)
* Butchery (FRESH meat, alongside wool, duck feather, and other animal produce not including truffles, 200 points, no threshold)
* Fishery (FRESH fish, smoked fish and (aged) roe, 400 points, 40 points)
* Artisan House (artisan goods excluding honey, smoked fish and aged roe, 1000 points, 100 points)
* Fine Diners (cooked food, 200 points, 20 points)
* Jewellers (metals, gemstones, and minerals, 500 points, 100 points)
* Bountiful Farm (1M gold in shipment by season's end)
* Greenhouse Farm (only during the winter, replaces Vegetable Farm, Fruit Orchard and Flower Garden. Fresh crops, 1500 points, 200 points)

When the next season begins, the competition will end and you'll receive
rewards through mail depending on how many categories you managed to achieve:

* Bronze Medal (25% of categories):
  * 20,000 gold
  * Nothing else, better luck next time :(
* Silver Medal (50% of categories):
  * 50,000 gold
  * +0.2 heart with every villager
  * A voucher that when used grants a month-long subscription to JojaDash(tm)
    Daily Delivery, allowing you to use the phone to order one free food
    item of your choice from JojaDash. You can only order from JojaDash(tm) once per
    day, and the subscription expires at the end of the season.
  * NOTE: If you go the Joja Route, you'll also receive lifetime free JojaDash!
    Feel free to toss these vouchers in that case, they have no use.
* Gold Medal (75% of categories):
  * 100,000 gold
  * +0.5 heart with every villager,
  * JojaDash subscription
  * Iridium Swag Bag: contains a random assortment of items, including a Tea
    Set, Magic Bait, Qi Seasoning, and 5 random items that you have yet to
    ship/donate/cook/fish for Perfection (cooking and fishing will count even
    if you haven't done the work yourself). Courtesy of Mr Qi, who snuck this
    in your mailbox as a reward for a job well done.
* Iridium Medal (every category, wow!):
  * 200,000 gold
  * +1 heart with every villager
  * JojaDash
  * Iridium Swag Bag
  * Pride of the Valley: A buff that lasts the entire month. Adds +2 to all skills and attack/defense, +0.5 move speed and +1 Luck

### Installation Instruction
* Download and install SMAPI, Content Patcher, [SpaceCore](https://www.nexusmods.com/stardewvalley/mods/1348) and [StardewUI](https://www.nexusmods.com/stardewvalley/mods/28870).
* Then unzip this mod into the Mods folder as usual.
* This mod is safe to install mid-save! If installed mid-save:
  * Every existing item will be fresh, only becoming stale on sleeping the next
    day. You can exploit this by immediately dumping your fridges and make big
    bucks, so I'll gently request that you don't ;)
  * The competition immediately start if your farm meets the
    requirements, which does make completing it very hard if you receive it
    near the middle or end of season. Future competitions should properly start
    on a season's second day.
* OPTIONAL: If starting a new save, also highly recommend setting profit margin
  to a lower value, since this mod does increase the amount of money you can
  make.
* This mod is safe to uninstall; all fresh and stale items will revert to being
  regular items and have regular prices, and other items added by this mod
  will become error items that can be trashed. The farm competition order might
  still linger harmlessly though, and will do nothing once expiring at the end
  of the month.

MOD COMPATIBILITY: This mod should be fully compatible with other mods, with special interactions noted below:
* Cornucopia: This mod's competition categories are adjusted depending on what's installed:
  * If More Crops is installed, the veggies and fruits category will have their
    max individual threshold lowered to 100. Gotta make use of all those added
    crops!
  * If More Flowers is installed, the flowers category will have their
    threshold lowered to 100, and the required points bumped up to 800. Veggies
    and fruits will also have their max points lowered to 800 to compensate.
* Wildflour Atelier Goods: This mod will make the
  Artisan category slightly harder with WAG installed! Better make use of all the artisan goods!
* Inflorescence: This mod will disable the Flowers category since Inflorescence
  has its own flower competition.
* Ferngill Simple Economy: Works well together, the two mod's sell price modifier will stack.
* Spoilage: Works fine, spoilage should take over spoiling non-fresh items.
* Machines Copy Quality and other similar "copy quality mods": Fully
  compatible, however NOT recommended. This mod's balance is deliberately
  dependent on artisan goods not copying quality from the input.
* Any other custom crops/animals/trees/artisan goods mods: Fully compatible, as
  long as they set item categories correctly. Mod authors may also choose to
  add more explicit compatibility, as instructed below.

## For Mod Authors
This mod is designed to be extensible; you can detect fresh items, specify
other "fresh-able" items, add your own categories for the competition, or even
replace this mod's own with your own categories! See below for guide and
technical details.

### Freshness Features

* If you want an item that isn't usually part of a 'fresh' category benefit
  from 'freshness', add the `spoilable_item` context tag to the item data. This
  mod does this to Coffee Beans.
* Conversely, add the `non_spoilable_item` tag to make an item *not* eligible
  for freshness.
* In-game, items marked Fresh will have the "fresh_item" context tags.
* Item queries (shops, machines, etc.) will spawn Fresh items. To make them not fresh, add
  `"selph.FreshFarmProduce.NotFresh": "true"` to the item query's ModData field.

### Modify the Competition

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

### Mail/Flags/Rewards
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

### Console Commands

| Command Name                         | Description              |
| ---------------------------------- |  ------------------------ |
| `selph.FreshFarmProduce_AddSpecialOrder` | Add the competition special order.|
| `selph.FreshFarmProduce_RemoveSpecialOrder` | Remove the competition special order.|
| `selph.FreshFarmProduce_ResetSpecialOrder` | Remove and then readd the competition special order.|
| `selph.FreshFarmProduce_PrintDiagnostics` | Print detailed info about the competition (including items shipped, points, etc.)|
| `selph.FreshFarmProduce_AddWinningItems` | Add stacks of items that can be shipped to win the competition.|
