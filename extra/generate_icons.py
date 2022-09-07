#!/usr/bin/env python3

# Copyright (C) 2022 jmh
# SPDX-License-Identifier: GPL-3.0-only

import os
from xml.dom import minidom
import xml.etree.ElementTree as ET

from PIL import Image

CURRENT_DIR = os.path.dirname(os.path.realpath(__file__))
MAIN_DIR = os.path.realpath(os.path.join(CURRENT_DIR, os.pardir))

DARK_SUFFIX = "_dark"
RES_PREFIX = "auth_"

DPI_SIZES = {
    "mdpi": 32,
    "hdpi": 48,
    "xhdpi": 64,
    "xxhdpi": 96,
    "xxxhdpi": 128
}


def build_map(files: list):

    # Remove file extension
    files = list(map(lambda f: f[:-4], files))

    standard = []
    dark = []

    for filename in files:
        is_dark = len(filename) > len(DARK_SUFFIX) and filename[-len(DARK_SUFFIX):] == DARK_SUFFIX

        if is_dark:
            dark.append(filename)
        else:
            standard.append(filename)

    map_path = os.path.join(MAIN_DIR, "AuthenticatorPro.Droid.Shared", "src", "Data", "IconMap.cs")
    file = open(map_path, "w")

    file.write("// Copyright (C) 2022 jmh\n")
    file.write("// SPDX-License-Identifier: GPL-3.0-only\n\n")
    file.write("using System.Collections.Generic;\n\n")
    file.write("namespace AuthenticatorPro.Droid.Shared.Data\n")
    file.write("{\n")
    file.write("    // GENERATED CLASS, SHOULD NOT BE EDITED DIRECTLY\n")
    file.write("    public static class IconMap\n")
    file.write("    {\n")
    file.write("        public static readonly Dictionary<string, int> Service = new Dictionary<string, int>\n")
    file.write("        {\n")

    for icon in standard:
        file.write("            { " + f'"{icon}", Resource.Drawable.' + RES_PREFIX + icon + " },\n")

    file.write("        };\n\n")
    file.write("        public static readonly Dictionary<string, int> ServiceDark = new Dictionary<string, int>\n")
    file.write("        {\n")

    for icon in dark:
        file.write("            { " + '"' + icon[:-len(DARK_SUFFIX)] + '", Resource.Drawable.' + RES_PREFIX + icon + " },\n")

    file.write("        };\n")
    file.write("    }\n")
    file.write("}")

    file.truncate()
    file.close()


def write_element_tree(tree: ET.ElementTree, output_path: str):
    contents = ET.tostring(tree.getroot(), encoding="utf-8", xml_declaration=True) 
    formatted = minidom.parseString(contents).toprettyxml(indent="    ")
    formatted = "\n".join([l for l in formatted.splitlines() if l.strip()])

    with open(output_path, "w") as file:
        file.write(formatted)


def build_csproj(icons: list):
    csproj_path = os.path.join(MAIN_DIR, "AuthenticatorPro.Droid.Shared", "AuthenticatorPro.Droid.Shared.csproj")
    csproj = ET.parse(csproj_path)

    namespace = "http://schemas.microsoft.com/developer/msbuild/2003"
    ET.register_namespace("", namespace)

    icons_group = csproj.find(f'{{{namespace}}}ItemGroup[@Label = "icons"]')
    resources = icons_group.findall(f"{{{namespace}}}AndroidResource")

    for resource in resources:
        icons_group.remove(resource)

    for icon in icons:
        for dpi in DPI_SIZES.keys():
            resource = ET.Element("AndroidResource")
            resource.set("Include", f"Resources\\drawable-{dpi}\\{RES_PREFIX}{icon}")
            icons_group.append(resource)

    write_element_tree(csproj, csproj_path)


def generate_for_dpi(dpi: str, filename: str, icon: Image):
    output_path = os.path.join(MAIN_DIR, "AuthenticatorPro.Droid.Shared", "Resources", f"drawable-{dpi}", f"{RES_PREFIX}{filename}")

    if os.path.isfile(output_path):
        return

    with icon.copy() as resized:
        resized.thumbnail((DPI_SIZES[dpi], DPI_SIZES[dpi]), resample=Image.LANCZOS, reducing_gap=3.0)
        resized.save(output_path, optimize=True)


def main():
    icons_dir = os.path.join(MAIN_DIR, "icons")

    files = os.listdir(icons_dir)
    files.sort()

    print(f"Generating {len(files)} icons")

    for filename in files:
        print(f"Processing {filename}")
        path = os.path.join(icons_dir, filename)

        with Image.open(path) as icon:
            for dpi in DPI_SIZES.keys():
                generate_for_dpi(dpi, filename, icon)

    print("Building csproj")
    build_csproj(files)
    print("Building map")
    build_map(files)

    print("Done")


if __name__ == "__main__":
    main()
