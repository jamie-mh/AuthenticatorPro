#!/usr/bin/env python3

# Copyright (C) 2022 jmh
# SPDX-License-Identifier: GPL-3.0-only

import os
import sys
import argparse
import tempfile
import shutil
import xml.etree.ElementTree as ET

REPO = "https://github.com/jamie-mh/AuthenticatorPro.git"

PROJECT_NAMES = {
    "android": "AuthenticatorPro.Droid",
    "wearos": "AuthenticatorPro.WearOS"
}


def get_full_path(path: str) -> str:
    return os.path.abspath(os.path.expanduser(path))


def adjust_csproj(build_dir: str, project: str, package: str):
    csproj_path = os.path.join(build_dir, project, f"{project}.csproj")
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


def adjust_version_code(build_dir: str):
    manifest_path = f"{build_dir}/AuthenticatorPro.Droid/Properties/AndroidManifest.xml"
    manifest = ET.parse(manifest_path)

    android_namespace = "http://schemas.android.com/apk/res/android"
    ET.register_namespace("android", android_namespace)

    tools_namespace = "http://schemas.android.com/tools"
    ET.register_namespace("tools", tools_namespace)

    # add 2 zeroes to version code when building aab
    version_code_path = f"{{{android_namespace}}}versionCode"
    version_code = manifest.getroot().get(version_code_path)
    manifest.getroot().set(version_code_path, "100" + version_code[1:])

    manifest.write(manifest_path, xml_declaration=True, encoding="utf-8")


def build_project(build_dir: str, args: argparse.Namespace):
    solution_path = os.path.join(build_dir, "AuthenticatorPro.sln")
    os.system(f'msbuild "{solution_path}" -t:Restore')

    os.makedirs(args.output, exist_ok=True)

    msbuild_args = f'-p:Configuration="Release" -p:AndroidSdkDirectory="{args.sdk}" -p:AndroidNdkDirectory="{args.ndk}" -p:JavaSdkDirectory="{args.jdk}"'

    project_file_path = os.path.join(build_dir, args.project, f"{args.project}.csproj")
    os.system(f'msbuild "{project_file_path}" {msbuild_args} -p:OutputPath="{args.output}" -t:PackageForAndroid')

    if args.keystore is not None:
        signing_args = f'-p:AndroidKeyStore=True -p:AndroidSigningKeyStore="{args.keystore}" -p:AndroidSigningStorePass="{args.keystore_pass}" -p:AndroidSigningKeyAlias="{args.keystore_alias}" -p:AndroidSigningKeyPass="{args.keystore_key_pass}"'
    else:
        signing_args = "-p:AndroidKeyStore=False"

    os.system(f'msbuild "{project_file_path}" {msbuild_args} {signing_args} -p:OutputPath="{args.output}" -t:SignAndroidPackage')


def clean_build_artifacts(output_dir: str, package: str):
    files = os.listdir(output_dir)

    for file in files:
        if file[-3:] != package or "Signed" not in file:
            path = os.path.join(output_dir, file)
            os.remove(path)

    files = os.listdir(output_dir)

    for file in files:
        old_path = os.path.join(output_dir, file)
        new_path = os.path.join(output_dir, file.replace("-Signed", ""))
        os.rename(old_path, new_path)


def get_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Build Authenticator Pro")
    parser.add_argument("project", metavar="P", type=str, choices=PROJECT_NAMES.keys(), help="Project to build")
    parser.add_argument("package", metavar="T", type=str, choices=["apk", "aab"], help="Package type to build")

    parser.add_argument("--output", type=str, help="Build output path (defaults to 'out')", default="out")
    parser.add_argument("--sdk", type=str, help="Android SDK location (defaults to $ANDROID_SDK)", default=os.getenv("ANDROID_SDK"))
    parser.add_argument("--ndk", type=str, help="Android NDK location (defaults to $ANDROID_NDK)", default=os.getenv("ANDROID_NDK"))
    parser.add_argument("--jdk", type=str, help="JDK location (defaults to $JAVA_HOME)", default=os.getenv("JAVA_HOME"))

    signing = parser.add_argument_group("build signing")
    signing.add_argument("--keystore", type=str, help="Keystore location (if not set, output is signed with debug keystore)")
    signing.add_argument("--keystore-pass", type=str, help="Keystore password")
    signing.add_argument("--keystore-alias", type=str, help="Keystore alias")
    signing.add_argument("--keystore-key-pass", type=str, help="Keystore key password")

    args = parser.parse_args()

    if args.sdk is None:
        raise ValueError("No Android SDK defined")

    if args.ndk is None:
        raise ValueError("No Android NDK defined")

    if args.jdk is None:
        raise ValueError("No JDK defined")

    if args.keystore is not None:
        if args.keystore_pass is None or args.keystore_alias is None or args.keystore_key_pass is None:
            raise ValueError("Keystore provided but not all signing arguments are present")

        args.keystore = get_full_path(args.keystore)

    args.output = get_full_path(args.output)
    args.project = PROJECT_NAMES[args.project]

    return args


def validate_path():
    for program in ["git", "msbuild"]:
        if shutil.which(program) is None:
            raise ValueError(f"Missing {program} on PATH")


def run(args: argparse.Namespace):
    with tempfile.TemporaryDirectory() as build_dir:
        os.system(f'git clone "{REPO}" "{build_dir}"')

        adjust_csproj(build_dir, args.project, args.package)

        if args.project == PROJECT_NAMES["android"] and args.package == "aab":
            adjust_version_code(build_dir)

        build_project(build_dir, args)
        clean_build_artifacts(args.output, args.package)


def main():
    try:
        args = get_args()
    except ValueError as err:
        print(err)
        return 2

    try:
        validate_path()
    except ValueError as err:
        print(err)
        return 1

    print(f"Building {args.project} as {args.package}")
    run(args)
    return 0


if __name__ == "__main__":
    sys.exit(main())
