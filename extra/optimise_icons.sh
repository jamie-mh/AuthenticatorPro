#!/usr/bin/env bash

set -e

SCRIPT_PATH=`dirname "$0"`
PROJECT_PATH=`realpath "$SCRIPT_PATH/../AuthenticatorPro.Droid.Shared"`

oxipng -o 4 -i 0 --strip safe $PROJECT_PATH/Resources/**/*.png
