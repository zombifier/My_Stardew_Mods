#!/usr/bin/python3

import json5

objects_data = dict(json5.load(open("Data_Objects.json", "r")))
recipe_data = dict(json5.load(open("Data_CookingRecipes.json", "r")))

def IsDuplicatePantry(base_item_name):
    return base_item_name in ["butter", "olive_oil", "cream", "coconut_milk"]

def IsUniqueCrop(base_item_name):
    return base_item_name in ["wild_strawberry", "gentian_root", "cherry_blossoms"]

with open("output.json", 'w') as output:
    output.write("{\n")
    for rule in recipe_data:
        should_write = False
        split = recipe_data[rule].split("/")
        if "Wildflour" not in split[0]:# and "HXW.Mixology" not in split[0]:
            continue
        rawingredients = split[0].split(" ")
        ingredients = [[rawingredients[i],rawingredients[i+1]] for i in range(0, len(rawingredients)) if i % 2 == 0 ]
        product = split[2].split(" ")
        if len(product) == 1:
          product.append("1")
        recipestr = ""
        recipestr += (f'  "{rule}": {{\n')
        recipestr += (f'    "ProductQualifiedId": "(O){product[0]}",\n')
        recipestr += (f'    "ProductAmount": {product[1]},\n')
        recipestr += (f'    "Ingredients": [\n')
        for ingredient in ingredients:
            recipestr += (f'      {{\n')
            obj_data = objects_data.get(ingredient[0], None)
            base_item_name = ingredient[0][len("Wildflour.AtelierGoods_"):].lower()
            if base_item_name == "wild_raspberry":
                base_item_name = "raspberry"
            if base_item_name == "cacao_pod":
                base_item_name = "cocoa"
            if obj_data is not None and not IsUniqueCrop(base_item_name) and "Wildflour.AtelierGoods_" in ingredient[0] and ("wildflour_pantry_item" not in obj_data["ContextTags"] or IsDuplicatePantry(base_item_name)):
                should_write = True
                sprite_index = int(obj_data["SpriteIndex"])
                x = sprite_index % 16 * 16
                y = sprite_index // 16 * 16
                recipestr += (f'        "Type": "ContextTag",\n')
                recipestr += (f'        "Value": "{base_item_name}_item",\n')
                #output.write(f'        "ContextTagsRequireAll": true,\n')
                recipestr += (f'        "OverrideText": "{{{{i18n: item.{base_item_name}.name}}}}",\n')
                recipestr += (f'        "OverrideTexturePath": "Mods/Wildflour.AtelierGoods/Objects",\n')
                recipestr += (f'        "OverrideTextureRect": {{"X": {x}, "Y": {y}, "Width": 16, "Height": 16}},\n')
            elif ingredient[0] == "-5":
                recipestr += (f'        "Type": "ContextTag",\n')
                recipestr += (f'        "Value": "category_egg",\n')
                recipestr += (f'        "OverrideText": "[LocalizedText Strings/StringsFromCSFiles:CraftingRecipe.cs.572]",\n')
                recipestr += (f'        "OverrideTexturePath": "Maps/springobjects",\n')
                recipestr += (f'        "OverrideTextureRect": {{"X": 128, "Y": 112, "Width": 16, "Height": 16}},\n')
            elif ingredient[0] == "-6":
                recipestr += (f'        "Type": "ContextTag",\n')
                recipestr += (f'        "Value": "category_milk",\n')
                #output.write(f'        "ContextTagsRequireAll": true,\n')
                recipestr += (f'        "OverrideText": "[LocalizedText Strings/StringsFromCSFiles:CraftingRecipe.cs.573]",\n')
                recipestr += (f'        "OverrideTexturePath": "Maps/springobjects",\n')
                recipestr += (f'        "OverrideTextureRect": {{"X": 256, "Y": 112, "Width": 16, "Height": 16}},\n')
            else:
                recipestr += (f'        "Type": "Item",\n')
                recipestr += (f'        "Value": "(O){ingredient[0]}",\n')
            recipestr += (f'        "Amount": {ingredient[1]},\n')
            recipestr += (f'      }},\n')

        recipestr += (f'    ],\n') # ingredients
        recipestr += (f'  }},\n')
        if should_write:
            output.write(recipestr)
    output.write("}")
