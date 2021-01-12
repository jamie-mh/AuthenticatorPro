# Importing from Steam

Authenticator Pro can generate the 5 character codes used by Steam. However, is does not support confirming trades.

*This guide may require some advanced technical knowledge*

The procedure is as follows:

- Setup a new authenticator using [Steam Desktop Authenticator](https://github.com/Jessecar96/SteamDesktopAuthenticator) (SDA). This will handle the conversion of the Steam protocol to a standard ``otpauth://`` URI. Do not enable encryption just yet.
- Once up and running, in the directory where you are running SDA you will find a ``maFiles`` directory. This contains your account secrets.
- Open the file for your account called ``{STEAM_ID}.maFile``, where ``STEAM_ID`` is the unique identifier for your Steam account, using a text editor. If you are setting this up for the first time, there should only be one file.
- Find the ``uri`` json property it will have a URI in this format ``otpauth://totp/Steam:username?secret=MYSECREYKEY&issuer=Steam``.
- Create a QR code using this string using the method of your choice or enter the secret part of the URI in Authenticator Pro using the Steam type.
- (Optional) Enable encryption in SDA. You should keep using SDA because it's the only way for you to accept trades.

