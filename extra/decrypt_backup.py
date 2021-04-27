# Authenticator Pro Backup Decryption Tool
# View https://github.com/jamie-mh/AuthenticatorPro/blob/master/doc/BACKUP_FORMAT.md#encrypted-backups for details

import sys
import hashlib
import json

from getpass import getpass
from Crypto.Cipher import AES

HEADER = "AuthenticatorPro"
HASH_MODE = "sha1"
AES_MODE = AES.MODE_CBC
ITERATIONS = 64000
SALT_LENGTH = 20
IV_LENGTH = 16
DERV_KEY_LENGTH = 32


def get_file_bytes(path):
    with open(path, "rb") as f:
        data = f.read()

    return data


def decrypt(data, password):
    salt = data[len(HEADER):len(HEADER) + SALT_LENGTH]
    iv = data[len(HEADER) + SALT_LENGTH:len(HEADER) + SALT_LENGTH + IV_LENGTH]
    payload = data[len(HEADER) + SALT_LENGTH + IV_LENGTH:]

    password_bytes = password.encode("utf-8")
    key = hashlib.pbkdf2_hmac(HASH_MODE, password_bytes, salt, ITERATIONS, DERV_KEY_LENGTH)
    aes = AES.new(key, AES_MODE, iv)
    result = aes.decrypt(payload)

    # strip extra invalid chars at end of file
    return result.decode("utf-8").rpartition("}")[0] + "}"


def main():
    if len(sys.argv) < 2:
        print("error: File path expected")
        return

    path = sys.argv[1]

    try:
        data = get_file_bytes(path)
    except:
        print("error: File cannot be read")
        return

    try:
        header = data[:len(HEADER)]

        if header.decode("utf-8") != HEADER:
            raise
    except:
        print("error: File is not a valid backup file or uses an older format")
        return

    password = getpass("Password: ")

    try:
        result = decrypt(data, password)
    except:
        print("error: Invalid password")
        return

    backup = json.loads(result)
    sys.stdout.write(json.dumps(backup, indent=4))


if __name__ == "__main__":
    main()
