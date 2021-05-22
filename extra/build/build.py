#!/usr/bin/env python3

# Copyright (C) 2021 jmh
# SPDX-License-Identifier: GPL-3.0-only

import os
import sys
import argparse
import xml.etree.ElementTree as ET

REPO = "https://github.com/jamie-mh/AuthenticatorPro.git"
BUILD_DIR = "/tmp/AuthenticatorPro"

PROJECT_NAMES = {
    "android": "AuthenticatorPro.Droid",
    "wearos": "AuthenticatorPro.WearOS"
}


def get_full_path(path: str) -> str:
    return os.path.abspath(os.path.expanduser(path))


def clone():
    os.system(f'git clone "{REPO}" "{BUILD_DIR}"')


def clean():
    os.system(f'rm -rf "{BUILD_DIR}"')


def adjust_csproj(project: str, package: str):

    csproj_path = f"{BUILD_DIR}/{project}/{project}.csproj"
    csproj = ET.parse(csproj_path)

    namespace = "http://schemas.microsoft.com/developer/msbuild/2003"
    ET.register_namespace("", namespace)

    property_group_path = f"{{{namespace}}}PropertyGroup[@Condition = \" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' \"]"
    property_group = csproj.find(property_group_path)

    package_format = property_group.find(f"{{{namespace}}}AndroidPackageFormat")
    package_format.text = package

    package_per_abi = property_group.find(f"{{{namespace}}}AndroidCreatePackagePerAbi")
    package_per_abi.text = "true" if package == "apk" else "false"

    csproj.write(csproj_path, xml_declaration=True, encoding="utf-8")


def adjust_version_code():

    manifest_path = f"{BUILD_DIR}/AuthenticatorPro.Droid/Properties/AndroidManifest.xml"
    manifest = ET.parse(manifest_path)

    namespace = "http://schemas.android.com/apk/res/android"
    ET.register_namespace("android", namespace)

    # add 2 zeroes to version code when building aab
    version_code_path = f"{{{namespace}}}versionCode"
    version_code = manifest.getroot().get(version_code_path)
    manifest.getroot().set(version_code_path, "100" + version_code[1:])

    with open(manifest_path, "wb") as file:
        manifest.write(file, xml_declaration=True, encoding="utf-8")


def build_project(args: argparse.Namespace):

    os.system(f'msbuild "{BUILD_DIR}/AuthenticatorPro.sln" -t:Restore')
    os.system(f'mkdir -p "{args.output}"')

    msbuild_args = f'-p:Configuration="Release" -p:AndroidSdkDirectory="{args.sdk}" -p:AndroidNdkDirectory="{args.ndk}"'
    os.system(f'msbuild "{BUILD_DIR}/{args.project}/{args.project}.csproj" {msbuild_args} -p:OutputPath="{args.output}" -t:PackageForAndroid')

    if args.keystore is not None:
        signing_args = f'-p:AndroidKeyStore=True -p:AndroidSigningKeyStore="{args.keystore}" -p:AndroidSigningStorePass="{args.keystore_pass}" -p:AndroidSigningKeyAlias="{args.keystore_alias}" -p:AndroidSigningKeyPass="{args.keystore_key_pass}"'
    else:
        signing_args = "-p:AndroidKeyStore=False"

    os.system(f'msbuild "{BUILD_DIR}/{args.project}/{args.project}.csproj" {msbuild_args} {signing_args} -p:OutputPath="{args.output}" -t:SignAndroidPackage')


def clean_build_artifacts(output_dir: str, package: str):

    files = os.listdir(output_dir)

    for file in files:
        if file[-3:] != package:
            os.remove(f"{output_dir}/{file}")


def get_args() -> argparse.Namespace:

    parser = argparse.ArgumentParser(description="Build Authenticator Pro")
    parser.add_argument("project", metavar="P", type=str, choices=PROJECT_NAMES.keys(), help="Project to build")
    parser.add_argument("package", metavar="T", type=str, choices=["apk", "aab"], help="Package type to build")

    parser.add_argument("--output", type=str, help="Build output path (defaults to 'out')", default="out")
    parser.add_argument("--sdk", type=str, help="Android SDK location (defaults to $ANDROID_SDK)", default=os.getenv("ANDROID_SDK"))
    parser.add_argument("--ndk", type=str, help="Android NDK location (defaults to $ANDROID_NDK)", default=os.getenv("ANDROID_NDK"))

    signing = parser.add_argument_group("build signing")
    signing.add_argument("--keystore", type=str, help="Keystore location (if not set, output is signed with debug keystore)")
    signing.add_argument("--keystore-pass", type=str, help="Keystore password")
    signing.add_argument("--keystore-alias", type=str, help="Keystore alias")
    signing.add_argument("--keystore-key-pass", type=str, help="Keystore key password")

    args = parser.parse_args()

    if args.sdk is None:
        raise RuntimeError("error: No Android SDK defined")

    if args.ndk is None:
        raise RuntimeError("error: No Android NDK defined")

    if args.keystore is not None:
        if args.keystore_pass is None or args.keystore_alias is None or args.keystore_key_pass is None:
            raise RuntimeError("error: Keystore provided but not all signing arguments are present")

        args.keystore = get_full_path(args.keystore)

    args.output = get_full_path(args.output)
    args.project = PROJECT_NAMES[args.project]

    return args


def run(args: argparse.Namespace):
    clean()
    clone()

    adjust_csproj(args.project, args.package)

    if args.project == PROJECT_NAMES["android"] and args.package == "aab":
        adjust_version_code()

    build_project(args)
    clean_build_artifacts(args.output, args.package)


def main():

    try:
        args = get_args()
    except RuntimeError as e:
        print(e)
        sys.exit(2)

    print(f"Building {args.project} as {args.package}")

    try:
        run(args)
    finally:
        clean()


if __name__ == "__main__":
    main()
