# Importing from Blizzard Authenticator

Authenticator Pro supports 8 digit codes and as such Blizzard Authenticator accounts are supported. However, Blizzard uses a proprietary algorithm for their account setup, so the accounts must be converted to standard TOTP.

*This guide expects that you are familiar with the Python pip package system*

The procedure is as follows:

- Install the required packages using pip: ``python-bna`` and ``python-qrcode`` by running ``pip3 install bna qrcode``.
- Follow the guide for the ``python-bna`` CLI to setup a new authenticator and generate a QR code: [https://github.com/jleclanche/python-bna/blob/master/README.md](https://github.com/jleclanche/python-bna/blob/master/README.md)
