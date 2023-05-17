#!/usr/bin/env bash

/usr/bin/env python3 build.py android apk "$@"
/usr/bin/env python3 build.py android apk --fdroid "$@"
/usr/bin/env python3 build.py android aab "$@"
/usr/bin/env python3 build.py wearos apk "$@"
/usr/bin/env python3 build.py wearos aab "$@"
