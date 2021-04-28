#!/bin/bash

# Installs Xamarin Android on Ubuntu (tested with 20.10)
# Xamarin Android requires the official Android SDK to build
# Required components: 
# - platform-tools
# - build-tools
# - ndk version ~21 will not work with >= 22
# - sdk platform depending on the apps you want to build

# For build usage see: build.py


# Install prerequisites

sudo apt -y install jq gdebi openjdk-8-jdk


# Install Mono
https://www.mono-project.com/download/stable

sudo apt install gnupg ca-certificates
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb https://download.mono-project.com/repo/ubuntu stable-focal main" | sudo tee /etc/apt/sources.list.d/mono-official-stable.list
sudo apt update

sudo apt -y install mono-complete


# Install Android SDK

echo -n "Install Android SDK? (y/n)? "
read answer

if [ "$answer" != "${answer#[Yy]}" ] ;then

    curl "https://dl.google.com/android/repository/commandlinetools-linux-6858069_latest.zip" > /tmp/cmdline-tools.zip
    sudo mkdir /opt/android-sdk
    sudo chown $USER:$USER /opt/android-sdk

    unzip /tmp/cmdline-tools.zip -d /tmp
    mkdir -p /opt/android-sdk/cmdline-tools/latest
    mv /tmp/cmdline-tools/* /opt/android-sdk/cmdline-tools/latest

    rm /tmp/cmdline-tools.zip
    rmdir /tmp/cmdline-tools

    # Install Android SDK components
    sh /opt/android-sdk/cmdline-tools/latest/bin/sdkmanager --sdk_root="/opt/android-sdk/" "platform-tools" "build-tools;30.0.3" "platforms;android-30" "ndk;21.4.7075529"
fi


# Download Xamarin Android OSS 11.2.2.0
# Build used: https://dev.azure.com/xamarin/public/_build/results?buildId=37684

artifact_json=$(curl "https://dev.azure.com/xamarin/public/_apis/build/builds/37684/artifacts")
download_url=$(echo $artifact_json | jq -r ".value[] | select(.name == \"Installers - Linux\") | .resource.downloadUrl")

curl "$download_url" > /tmp/xamarin-android.zip
unzip /tmp/xamarin-android.zip -d /tmp/xamarin-android
rm /tmp/xamarin-android.zip


# Install Xamarin Android and dependencies

sudo gdebi --non-interactive "/tmp/xamarin-android/Installers - Linux/xamarin.android-oss_11.2.2.0_amd64.deb"
rm -rf /tmp/xamarin-android


# Check nofile limit

if [ $(ulimit -n) -le 1024 ] ;then
    echo "Nofile limit is low. You may encounter issues when building projects, consider increasing the limit.";
fi


echo "Install completed"
