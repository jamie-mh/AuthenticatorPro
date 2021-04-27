#!/bin/bash
/usr/bin/env python3 build.py android apk --output android_apk "$@"
/usr/bin/env python3 build.py android aab --output android_aab "$@"
/usr/bin/env python3 build.py wearos apk --output wearos_apk "$@"
/usr/bin/env python3 build.py wearos aab --output wearos_aab "$@"
