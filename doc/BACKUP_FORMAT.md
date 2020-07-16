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
            "Algorithm": 0,
            "Digits": 6,
            "Period": 30,
            "Counter": 0,
            "Ranking": 0
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

* The authenticator secret key must be an **uppercase base-32 string with no spaces**. The secret may also contain '=' as a padding character.

* Type: 1 = HOTP, 2 = TOTP

* Algorithm: 0 = SHA-1, 1 = SHA-256, 2 = SHA-512

* Authenticators are ordered by their ranking, unless they're placed into categories where they will be ordered by the AuthenticatorCategory ranking instead.

* Digits must be between 6 and 10

* Period must be >= 0

* The issuer must not be null or blank.

#### Category

* The category Id is the first 8 characters of the SHA-1 hash of the name.

#### AuthenticatorCategory

* An AuthenticatorCategory simply binds Authenticators into Categories using both their primary keys (AuthenticatorSecret and CategoryId).

#### CustomIcon

* If the icon field of an authenticator starts with '@' then it is an id of a custom icon.

* Custom icons have an id (8 characters of SHA1 hash) and some data (bitmap encoded in base64).

### Encrypted Backups

If your backup file is encrypted. The JSON data is encrypted with the AES_CBC_PKCS7 algorithm. You can decrypt a backup file using OpenSSL like this:

First generate a key pair using your backup passphrase:

```openssl enc -nosalt -aes-256-cbc -k [PASSPHRASE] -P```

This command will generate a pair like this:

```
key=0682EC6F5B7CB1E5F5BCCBBF83C551F9FDDE85BD012BB0583C27E2A0A53BB245
iv =15C782384C896CBCFC78333B5ADAC16F
```

Use the following command to decrypt your backup file using the key pair generated previously:

```
openssl enc -nosalt -d -aes-256-cbc -in backup.authpro -K [KEY] -iv [IV] -out backup_decrypted.json
```

This will output the JSON content of your backup file (`backup.authpro`) to `backup_decrypted.json`.
