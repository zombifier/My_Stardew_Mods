#!/usr/bin/python3

import json5
import sys

filename = sys.argv[1]
outputfile =  sys.argv[2]

obj = dict(json5.load(open(filename, "r")))


with open(outputfile, 'w') as output:
    output.write("{\n")
    for rule in obj:
        split = obj[rule].split("/")
        if "Cloth" not in split[0] and "Wool" not in split[0]:
            continue
        rawingredients = split[0].split(" ")
        ingredients = [[rawingredients[i],rawingredients[i+1]] for i in range(0, len(rawingredients)) if i % 2 == 0 ]
        product = split[2]
        output.write(f'  "{rule}": {{\n')
        output.write(f'    "ProductQualifiedId": "(O){product}",\n')
        output.write(f'    "ProductAmount": 1,\n')
        output.write(f'    "Ingredients": [\n')
        for ingredient in ingredients:
            output.write(f'    {{\n')
            if "Cloth" in ingredient[0]:
                color = ingredient[0][len("appleseed.BCP."):ingredient[0].find("Cloth")]
                if color == "Grey":
                    color = "Gray"
                if color == "Violet":
                    color = "Purple"
                output.write(f'      "Type": "ContextTag",\n')
                output.write(f'      "Value": "{{{{ModId}}}}_dyed_cloth_item,color_{color.lower()}",\n')
                output.write(f'      "ContextTagsRequireAll": true,\n')
                output.write(f'      "OverrideText": "{{{{i18n: item.{color}Cloth.name}}}}",\n')
                output.write(f'      "OverrideTexturePath": "Mods/appleseed.BCP/Objects",\n')
                output.write(f'      "OverrideTextureRect": {{"X": "{{{{X{color}}}}}", "Y": 16, "Width": 16, "Height": 16}},\n')
            elif "Wool" in ingredient[0]:
                color = ingredient[0][len("appleseed.BCP."):ingredient[0].find("Wool")]
                output.write(f'      "Type": "ContextTag",\n')
                output.write(f'      "Value": "{{{{ModId}}}}_dyed_yarn_item,color_{color.lower()}",\n')
                output.write(f'      "ContextTagsRequireAll": true,\n')
                output.write(f'      "OverrideText": "{{{{i18n: item.{color}WoolOrYarn.name}}}}",\n')
                output.write(f'      "OverrideTexturePath": "Mods/appleseed.BCP/Objects",\n')
                output.write(f'      "OverrideTextureRect": {{"X": "{{{{X{color}}}}}", "Y": 0, "Width": 16, "Height": 16}},\n')
            else:
                output.write(f'      "Type": "Item",\n')
                output.write(f'      "Value": "(O){ingredient[0]}",\n')
            output.write(f'      "Amount": {ingredient[1]},\n')
            output.write(f'    }},\n')

        output.write(f'    ],\n') # ingredients
        output.write(f'  }},\n')
    output.write("}")

