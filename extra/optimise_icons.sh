#!/usr/bin/env bash

set -e

SCRIPT_PATH=`dirname "$0"`
PROJECT_PATH=`realpath "$SCRIPT_PATH/../AuthenticatorPro.Droid.Shared"`

pngquant --skip-if-larger --ext=.png --force --strip -- $PROJECT_PATH/Resources/**/*.png
