#!/usr/bin/env python3

# Copyright (C) 2023 jmh
# SPDX-License-Identifier: GPL-3.0-only

import argparse

import os
import shutil
import subprocess

CURRENT_DIR = os.path.dirname(os.path.realpath(__file__))
MAIN_DIR = os.path.realpath(os.path.join(CURRENT_DIR, os.pardir))

DARK_SUFFIX = "_dark"
RES_PREFIX = "auth_"

DPI_SIZES = {"mdpi": 32, "hdpi": 48, "xhdpi": 64, "xxhdpi": 96, "xxxhdpi": 128}


def build_map(files: list):

    # Remove file extension
    files = list(map(lambda f: f[:-4], files))

    standard = []
    dark = []

    for filename in files:
        is_dark = (
            len(filename) > len(DARK_SUFFIX)
            and filename[-len(DARK_SUFFIX) :] == DARK_SUFFIX
        )

        if is_dark:
            dark.append(filename)
        else:
            standard.append(filename)

    map_path = os.path.join(
        MAIN_DIR, "AuthenticatorPro.Droid.Shared", "src", "IconMap.cs"
    )
    file = open(map_path, "w")

    # fmt: off
    file.write("// Copyright (C) 2023 jmh\n")
    file.write("// SPDX-License-Identifier: GPL-3.0-only\n\n")
    file.write("using System.Collections.Generic;\n\n")
    file.write("namespace AuthenticatorPro.Droid.Shared\n")
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
    # fmt: on

    file.truncate()
    file.close()


def generate_for_dpi(dpi: str, path: str, overwrite: bool):
    file_name = os.path.basename(path)
    output_path = os.path.join(
        MAIN_DIR,
        "AuthenticatorPro.Droid.Shared",
        "Resources",
        f"drawable-{dpi}",
        f"{RES_PREFIX}{file_name}",
    )

    if not overwrite and os.path.isfile(output_path):
        return

    subprocess.run(
        ["convert", "-resize", f"{DPI_SIZES[dpi]}x{DPI_SIZES[dpi]}", path, output_path],
        check=True,
        capture_output=True,
    )

    subprocess.run(
        [
            "oxipng",
            "-o",
            "3",
            "-i",
            "0",
            "--strip",
            "safe",
            "--threads",
            "1",
            output_path,
        ],
        check=True,
        capture_output=True,
    )


def delete_removed(files: list[str]):
    lookup = {f"auth_{file}": 1 for file in files}

    for dpi in DPI_SIZES.keys():
        resources_path = os.path.join(
            MAIN_DIR, "AuthenticatorPro.Droid.Shared", "Resources", f"drawable-{dpi}"
        )

        resources = os.listdir(resources_path)

        for resource in resources:
            if resource not in lookup:
                print(f"Deleting removed resource {resource}")
                os.remove(os.path.join(resources_path, resource))


def validate_path():
    for program in ["convert", "oxipng"]:
        if shutil.which(program) is None:
            raise ValueError(f"Missing {program} on PATH")


def get_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate icons")
    parser.add_argument(
        "--overwrite",
        action=argparse.BooleanOptionalAction,
        help="Overwrite existing icons",
    )

    return parser.parse_args()


def main():
    args = get_args()
    validate_path()

    icons_dir = os.path.join(MAIN_DIR, "icons")
    files = os.listdir(icons_dir)
    files.sort()

    print(f"Generating {len(files)} icons")
    delete_removed(files)

    for filename in files:
        print(f"Processing {filename}")
        path = os.path.join(icons_dir, filename)

        for dpi in DPI_SIZES.keys():
            generate_for_dpi(dpi, path, args.overwrite)

    print("Building map")
    build_map(files)

    print("Done")


if __name__ == "__main__":
    main()
