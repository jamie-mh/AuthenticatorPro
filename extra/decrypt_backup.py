#!/usr/bin/env python3

# Authenticator Pro Backup Decryption Tool
# View https://github.com/jamie-mh/AuthenticatorPro/blob/master/doc/BACKUP_FORMAT.md#encrypted-backups for details

import sys
import hashlib
import json
import argparse

from getpass import getpass
from Crypto.Cipher import AES

HEADER = "AuthenticatorPro"
HASH_MODE = "sha1"
AES_MODE = AES.MODE_CBC
ITERATIONS = 64000
SALT_LENGTH = 20
IV_LENGTH = 16
DERV_KEY_LENGTH = 32


def get_cli_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Decrypt Authenticator Pro backup")
    parser.add_argument("path", metavar="p", type=str, help="Path to file to decrypt")
    parser.add_argument("--password", type=str, help="Backup password")

    return parser.parse_args()


def decrypt(data: bytes, password: str) -> str:
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

    args = get_cli_args()

    try:
        with open(args.path, "rb") as f:
            data = f.read()
    except FileNotFoundError:
        print("error: File cannot be read")
        sys.exit(1)

    try:
        header = data[:len(HEADER)]

        if header.decode("utf-8") != HEADER:
            raise ValueError
    except ValueError:
        print("error: File is not a valid backup file or uses an older format")
        sys.exit(1)

    password = args.password if args.password is not None else getpass("Password: ")

    try:
        result = decrypt(data, password)
    except ValueError:
        print("error: Invalid password")
        sys.exit(1)

    backup = json.loads(result)
    sys.stdout.write(json.dumps(backup, indent=4))


if __name__ == "__main__":
    main()
