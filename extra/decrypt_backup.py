#!/usr/bin/env python3

# Copyright (C) 2023 jmh
# SPDX-License-Identifier: GPL-3.0-only

# Stratum Backup Decryption Tool
# View https://github.com/stratumauth/app/blob/master/doc/BACKUP_FORMAT.md#encrypted-backups for details

import argparse
import hashlib
import json
import sys
from getpass import getpass

import argon2
from argon2 import Type
from cryptography.hazmat.primitives import padding
from cryptography.hazmat.primitives.ciphers import Cipher, modes, algorithms
from cryptography.hazmat.primitives.ciphers.aead import AESGCM

KEY_LENGTH = 32

# Default
HEADER = "AUTHENTICATORPRO"
SALT_LENGTH = 16
IV_LENGTH = 12

PARALLELISM = 4
ITERATIONS = 3
MEMORY_SIZE = 65536

# Legacy
LEGACY_HEADER = "AuthenticatorPro"
LEGACY_HASH_MODE = "sha1"
LEGACY_ITERATIONS = 64000
LEGACY_SALT_LENGTH = 20
LEGACY_IV_LENGTH = 16


def get_cli_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Decrypt Stratum backup")
    parser.add_argument("path", metavar="p", type=str, help="Path to file to decrypt")
    parser.add_argument("--password", type=str, help="Backup password")

    return parser.parse_args()


def decrypt(data: bytes, password: str) -> bytes:
    salt = data[len(HEADER) : len(HEADER) + SALT_LENGTH]
    iv = data[len(HEADER) + SALT_LENGTH : len(HEADER) + SALT_LENGTH + IV_LENGTH]
    payload = data[len(HEADER) + SALT_LENGTH + IV_LENGTH :]

    password_bytes = password.encode("utf-8")

    key = argon2.low_level.hash_secret_raw(
        password_bytes,
        salt,
        time_cost=ITERATIONS,
        memory_cost=MEMORY_SIZE,
        parallelism=PARALLELISM,
        hash_len=KEY_LENGTH,
        type=Type.ID,
    )

    aes = AESGCM(key)
    return aes.decrypt(iv, payload, None)


def decrypt_legacy(data: bytes, password: str) -> bytes:
    salt = data[len(LEGACY_HEADER) : len(LEGACY_HEADER) + LEGACY_SALT_LENGTH]
    iv = data[
        len(LEGACY_HEADER)
        + LEGACY_SALT_LENGTH : len(LEGACY_HEADER)
        + LEGACY_SALT_LENGTH
        + LEGACY_IV_LENGTH
    ]
    payload = data[len(LEGACY_HEADER) + LEGACY_SALT_LENGTH + LEGACY_IV_LENGTH :]

    password_bytes = password.encode("utf-8")

    key = hashlib.pbkdf2_hmac(
        LEGACY_HASH_MODE, password_bytes, salt, LEGACY_ITERATIONS, KEY_LENGTH
    )

    cipher = Cipher(algorithms.AES(key), modes.CBC(iv))
    decryptor = cipher.decryptor()

    raw_bytes = decryptor.update(payload) + decryptor.finalize()
    unpadder = padding.PKCS7(LEGACY_IV_LENGTH * 8).unpadder()

    return unpadder.update(raw_bytes) + unpadder.finalize()


def main():
    args = get_cli_args()
    password = args.password if args.password is not None else getpass("Password: ")

    with open(args.path, "rb") as f:
        data = f.read()

    header = data[: len(HEADER)]
    header_str = header.decode("utf-8")

    if header_str == HEADER:
        decrypted = decrypt(data, password)
    elif header_str == LEGACY_HEADER:
        decrypted = decrypt_legacy(data, password)
    else:
        print("error: File is not a valid backup")
        return

    backup = json.loads(decrypted.decode("utf8"))
    sys.stdout.write(json.dumps(backup, indent=4))


if __name__ == "__main__":
    main()
