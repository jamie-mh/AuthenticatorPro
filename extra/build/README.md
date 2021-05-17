# Build tools

## Building locally (Ubuntu)

Run `install_xamarin_ubuntu.sh` then run `build.py`

## Building in a Docker container

### Build image

`docker build -t authpro_build .`

### Run build script in container

`docker run -v [LOCAL OUTPUT DIR]:/out -v [LOCAL KEYSTORE DIR]:/keystore -t authpro_build android apk --keystore /keystore/[KEYSTORE NAME].keystore --keystore-alias [ALIAS] --keystore-pass [PASS] --keystore-key-pass [KEY_PASS] --output /out`