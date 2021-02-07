# Architecture

This is a high level overview of the code base. Authenticator Pro is written with Xamarin Android (not Xamarin Forms!), a translation layer between the .NET framework and the underlying Java APIs. The code is similar to a traditional Android application, but rather that using Java or Kotlin, everything is in C#.
The project is a mono-repo with all available versions in the same solution.

Building requires Visual Studio (not Code) with the Xamarin workload installed.

## Code map

### `AuthenticatorPro.Droid`

This is the primary Android application. All data is stored in a (sometimes encrypted) SQLite database.

### `AuthenticatorPro.WearOS`

This is the companion WearOS application.

### `AuthenticatorPro.Droid.Shared`

This contains code shared between both Android versions (primary and companion). In addition, Android specific drawables for icons are stored here.

#### Notable classes

- `Data/IconResolver.cs` Resolves icon IDs (eg: github) to compile-time Android resource IDs. Newly added icons must be referenced here.

### `AuthenticatorPro.Shared`

Globally shared code. Contains functionality for generating codes, creating backups, etc...

#### Notable namespaces

- `Data` Contains all database entities and miscellaneous data classes.

- `Data/Backup` Code related to creating and decoding backups. Contains `BackupConverter` implementations which convert alternative formats to the internal format.

- `Data/Generator` Implementations of different authenticator types (TOTP, HOTP, etc...). Specification (eg: min max digits) of each is housed in `AuthenticatorType.cs`.

- `Data/Source` Abstraction around database access, handles inserts, deletes, updates. Allows for filtering based on name, category and type.

### `AuthenticatorPro.Test`

Unit tests for `AuthenticatorPro.Shared` project.

### `AuthenticatorPro.UWP`

Universal Windows Platform version of the application. Work in progress.
