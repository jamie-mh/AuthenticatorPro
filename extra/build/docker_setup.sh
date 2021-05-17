#!/bin/bash

# Install prerequisites
apt-get update
apt-get -y install jq openjdk-8-jdk gnupg ca-certificates curl unzip git

# Install Mono
apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb https://download.mono-project.com/repo/ubuntu stable-focal main" | tee /etc/apt/sources.list.d/mono-official-stable.list
apt-get update

apt-get -y install mono-devel

# Install Android SDK
curl "https://dl.google.com/android/repository/commandlinetools-linux-6858069_latest.zip" > /tmp/cmdline-tools.zip

mkdir /opt/android-sdk
unzip /tmp/cmdline-tools.zip -d /tmp
mkdir -p /opt/android-sdk/cmdline-tools/latest
mv /tmp/cmdline-tools/* /opt/android-sdk/cmdline-tools/latest

rm /tmp/cmdline-tools.zip
rmdir /tmp/cmdline-tools

yes | sh /opt/android-sdk/cmdline-tools/latest/bin/sdkmanager --licenses --sdk_root="/opt/android-sdk/"
sh /opt/android-sdk/cmdline-tools/latest/bin/sdkmanager --sdk_root="/opt/android-sdk/" "platform-tools" "build-tools;30.0.3" "platforms;android-30" "ndk;21.4.7075529"

# Download Xamarin Android
artifact_json=$(curl "https://dev.azure.com/xamarin/public/_apis/build/builds/37684/artifacts")
download_url=$(echo "$artifact_json" | jq -r ".value[] | select(.name == \"Installers - Linux\") | .resource.downloadUrl")

curl "$download_url" > /tmp/xamarin-android.zip
unzip /tmp/xamarin-android.zip -d /tmp/xamarin-android
rm /tmp/xamarin-android.zip

# Install Xamarin Android and dependencies
apt-get -y install lxd libmonosgen-2.0-1
dpkg -i "/tmp/xamarin-android/Installers - Linux/xamarin.android-oss_11.2.2.0_amd64.deb"
rm -rf /tmp/xamarin-android
