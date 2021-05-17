FROM ubuntu:focal

WORKDIR /build

COPY docker_setup.sh /build/docker_setup.sh
COPY build.py /build/build.py

ARG DEBIAN_FRONTEND=noninteractive
RUN /build/docker_setup.sh

ENV ANDROID_SDK=/opt/android-sdk
ENV ANDROID_NDK=/opt/android-sdk/ndk
ENTRYPOINT [ "/build/build.py" ]