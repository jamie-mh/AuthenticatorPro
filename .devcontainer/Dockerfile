ARG DOTNET_VERSION="7.0"
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS dotnet-maui-android

ARG MAINTAINER
ARG TZ="UTC"

LABEL maintainer=${MAINTAINER}
ENV TZ=${TZ}
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# NET SDK
ENV \
    # Unset ASPNETCORE_URLS from aspnet base image
    ASPNETCORE_URLS= \
    # Do not generate certificate
    DOTNET_GENERATE_ASPNET_CERTIFICATE=false \
    # Do not show first run text
    DOTNET_NOLOGO=true \
    # Enable correct mode for dotnet watch (only mode supported in a container)
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    # Skip extraction of XML docs - generally not useful within an image/container - helps performance
    NUGET_XMLDOC_MODE=skip

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        git \
        wget \
    && rm -rf /var/lib/apt/lists/*

# JAVA SDK
ARG JAVA_VERSION="11"
RUN <<EOF
  set -e
  echo "deb http://deb.debian.org/debian bullseye-backports main contrib" > /etc/apt/sources.list.d/bullseye-backports.list
  echo "deb-src http://deb.debian.org/debian bullseye-backports main contrib" >> /etc/apt/sources.list.d/bullseye-backports.list
  apt-get update
  apt-get install -t bullseye-backports -y openjdk-${JAVA_VERSION}-jdk-headless
  rm -rf /var/lib/apt/lists/*
EOF

ENV JAVA_HOME=/usr/lib/jvm/java-${JAVA_VERSION}-openjdk-amd64/

# Android SDK Manager
RUN <<EOF
  set -e
  echo "deb http://deb.debian.org/debian bullseye-backports main contrib" > /etc/apt/sources.list.d/bullseye-backports.list
  echo "deb-src http://deb.debian.org/debian bullseye-backports main contrib" >> /etc/apt/sources.list.d/bullseye-backports.list
  apt-get update
  apt-get install -t bullseye-backports -y sdkmanager
  rm -rf /var/lib/apt/lists/*
EOF

ENV ANDROID_SDK_ROOT=/usr/lib/android-sdk

# Android toolchain
ARG ANDROID_API="33"
ARG BUILD_TOOLS_VERSION="33.0.2"
RUN <<EOF
  set -e
  sdkmanager "platform-tools" "build-tools;${BUILD_TOOLS_VERSION}" "platforms;android-${ANDROID_API}"
EOF

ARG MAUI_VERSION
# MAUI (We can only install the latest version, the ARG is to cache bust if that version changed)
RUN <<EOF
  set -e
  dotnet workload install maui-android
EOF
