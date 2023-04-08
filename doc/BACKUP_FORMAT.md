# Backup File Format

If you are migrating your authenticators from another app, you can create your own Authenticator Pro backup file to quickly import all your data. An unencrypted backup file is written in JSON and has the following format:

```
{
    "Authenticators": [
        {
            "Type": 2,
            "Icon": "google",
            "Issuer": "Google",
            "Username": "google@gmail.com",
            "Secret": "SECRETKEY123ABCD",
            "Pin": null,
            "Algorithm": 0,
            "Digits": 6,
            "Period": 30,
            "Counter": 0,
            "Ranking": 0,
            "CopyCount": 10
        }
    ],
    "Categories": [
        {
            "Id": "a8323a2a",
            "Name": "Web",
            "Ranking": 0
        }
    ],
    "AuthenticatorCategories": [
        {
            "CategoryId": "a8323a2a",
            "AuthenticatorSecret": "SECRETKEY123ABCD",
            "Ranking": 0
        }
    ],
    "CustomIcons": [
        {
            "Id": ".....",
            "Data": "....."
        }
    ]
}
```

#### Authenticator

* For HOTP and TOTP, the authenticator secret key must be an **uppercase base-32 string with no spaces**. It can also contain '=' as a padding character.

* In the case of Mobile-OTP and Yandex codes, the pin field must be set. Otherwise, it can be null.

* Type: 1 = HOTP, 2 = TOTP, 3 = Mobile-Otp, 4 = Steam, 5 = Yandex

* Algorithm (applies to HOTP and TOTP): 0 = SHA-1, 1 = SHA-256, 2 = SHA-512

* Authenticators are ordered by their ranking, unless they're placed into categories where they will be ordered by the AuthenticatorCategory ranking instead.

* Digits must be between 6 and 8 for HOTP, between 6 and 10 for TOTP. This parameter is ignored for Steam, Mobile-Otp and Yandex.

* Period must be > 0

* The issuer must not be null or blank. The username can be null.

#### Category

* The category Id is the first 8 characters of the SHA-1 hash of the name.

#### AuthenticatorCategory

* An AuthenticatorCategory simply binds Authenticators into Categories using both their primary keys (AuthenticatorSecret and CategoryId).

#### CustomIcon

* If the icon field of an authenticator starts with '@' then it is an ID of a custom icon.

* Custom icons have an ID (8 characters of SHA1 hash) and some data (bitmap encoded in base64).

### Encrypted Backups

Authenticator Pro backups are encrypted using AES_CBC_PKCS7. The key is derived using PBKDF2 with SHA1 over 64000 iterations.
The file is structured as follows:

| Section | Size | Value            |
|---------|------|------------------|
| Header  | 16   | AuthenticatorPro |
| Salt    | 20   | .                |
| IV      | 16   | .                |
| Payload | .    | .                |

A Python tool can be used to decrypt your backups.

[Backup Decryption Tool](https://github.com/jamie-mh/AuthenticatorPro/blob/master/extra/decrypt_backup.py)

First, install the required package with ``pip``.

```
pip install pycryptodome
```

Run the Python script with your backup as a parameter. Optionally direct the output to a file.

```
python decrypt_backup.py backup.authpro > backup_decrypted.json
```

You will be prompted for the password, and once decrypted the output will be sent to the ``backup_decrypted.json`` file.
