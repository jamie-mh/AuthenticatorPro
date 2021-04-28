#!/usr/bin/env python3
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

    with open(csproj_path, "wb") as file:
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


def build_project(args: dict):

    msbuild_args = f'-p:Configuration="Release" -p:AndroidSdkDirectory="{args.sdk}" -p:AndroidNdkDirectory="{args.ndk}"'

    os.system(f'msbuild "{BUILD_DIR}/AuthenticatorPro.sln" {msbuild_args} -t:Restore')
    os.system(f'mkdir -p "{args.output}"')
    os.system(f'msbuild "{BUILD_DIR}/{args.project}/{args.project}.csproj" {msbuild_args} -p:OutputPath="{args.output}" -t:PackageForAndroid')

    signing_args = f'-p:AndroidKeyStore=True -p:AndroidSigningKeyStore="{args.keystore}" -p:AndroidSigningStorePass="{args.keystore_pass}" -p:AndroidSigningKeyAlias="{args.keystore_alias}" -p:AndroidSigningKeyPass="{args.keystore_key_pass}"'
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

    parser.add_argument("--output", type=str, help="Build output path (defaults to 'build')", default="build")
    parser.add_argument("--sdk", type=str, help="Android SDK location (defaults to $ANDROID_SDK)", default=os.getenv("ANDROID_SDK"))
    parser.add_argument("--ndk", type=str, help="Android NDK location (defaults to $ANDROID_NDK)", default=os.getenv("ANDROID_NDK"))

    signing = parser.add_argument_group("build signing")
    signing.add_argument("--keystore", type=str, help="Keystore location (defaults to $ANDROID_KEYSTORE)", default=os.getenv("ANDROID_KEYSTORE"))
    signing.add_argument("--keystore-pass", type=str, help="Keystore password", required=True)
    signing.add_argument("--keystore-alias", type=str, help="Keystore alias", required=True)
    signing.add_argument("--keystore-key-pass", type=str, help="Keystore key password", required=True)

    args = parser.parse_args()

    if args.sdk is None:
        print("error: No Android SDK defined")
        sys.exit(-1)

    if args.ndk is None:
        print("error: No Android NDK defined")
        sys.exit(-1)

    if args.keystore is None:
        print("error: No keystore location provided")
        sys.exit(-1)

    args.output = get_full_path(args.output)
    args.project = PROJECT_NAMES[args.project]
    args.keystore = get_full_path(args.keystore)

    return args


def run(args: dict):
    clean()
    clone()

    adjust_csproj(args.project, args.package)

    if args.project == PROJECT_NAMES["android"] and args.package == "aab":
        adjust_version_code()

    build_project(args)
    clean_build_artifacts(args.output, args.package)


def main():
    args = get_args()
    print(f"Building {args.project} as {args.package}")

    try:
        run(args)
    finally:
        clean()


if __name__ == "__main__":
    main()
