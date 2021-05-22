#!/usr/bin/env python3

# Copyright (C) 2021 jmh
# SPDX-License-Identifier: GPL-3.0-only

import os
import re
import yaml

TWOFACTORAUTH_REPO = "https://github.com/2factorauth/twofactorauth.git"
TEMP_DIR = "/tmp/twofactorauth"

DARK_SUFFIX = "_dark"
CURRENT_DIR = os.path.dirname(os.path.realpath(__file__))
MAIN_DIR = os.path.realpath(os.path.join(CURRENT_DIR, os.pardir))


def simplify_name(name: str) -> str:
    name = name.lower()
    name = re.sub(r"[^a-z0-9]", "", name)
    name = name.strip()

    return name


def clone_twofactorauth():
    os.system(f"git clone {TWOFACTORAUTH_REPO} {TEMP_DIR}")


def clean():
    os.system(f"rm -rf {TEMP_DIR}")


def get_available_icons() -> list:
    data_dir = f"{TEMP_DIR}/_data"
    files = os.listdir(data_dir)

    result = []

    for category_file in files:

        if category_file[-3:] != "yml":
            continue

        with open(f"{data_dir}/{category_file}", "r") as file:
            obj = yaml.safe_load(file)

            if "websites" not in obj:
                continue

            for site in obj["websites"]:

                if "tfa" not in site or "totp" not in site["tfa"]:
                    continue

                result.append(site["name"])

    return result


def get_existing_icons() -> list:
    result = os.listdir(f"{MAIN_DIR}/icons")
    result = map(lambda i: i[:-4], result)
    result = filter(lambda i: i[-len(DARK_SUFFIX):] != DARK_SUFFIX, result)
    return list(result)


def get_missing_icons() -> list:
    available = get_available_icons()
    existing = get_existing_icons()

    missing = [i for i in available if simplify_name(i) not in existing]
    return missing


def main():
    try:
        clone_twofactorauth()
        missing = get_missing_icons()
    finally:
        clean()

    for i in missing:
        print(i)


if __name__ == "__main__":
    main()
