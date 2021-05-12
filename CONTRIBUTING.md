# Contribution Guide 

## Translations

Translations are now managed on Crowdin. Go to the [Authenticator Pro Crowdin project](https://crwd.in/authenticator-pro) to contribute. If your language is not available, please contact me and I will add it.

## Icons

If you'd like to contribute some icons, check the [missing icons list](./extra/missing_icons.txt) for some that might need adding. There may be duplicates, so check first!

To add an icon to the project the procedure is as follows:

* Fork the repo

* Find a high-quality icon for the service you want to add. Try searching online for '{service_name} brand' - generally, many services offer high-res versions of their logos and icons for press and media.
  
    * Avoid icons made by 3rd parties with different styles from the original
    * Prefer flat icons instead of complex ones
    * Avoid text
    * Avoid frames and backgrounds
  
* Save the icon as a square 128x128 png file

* Name the icon as lowercase with spaces and special characters removed. Eg: Authenticator Pro -> authenticatorpro
  
* Place the file in the "icons" directory

* If the icon requires a dark theme variant, repeat the process and append "_dark" to the name.

* Optional: To complete the process and to build / test the project with the new icons, run the `generate_icons.py` script in the extras directory. This will generate the DPI variants, reference them in the csproj file and icon map.

* Remove the entry from the [missing icons list](./extra/missing_icons.txt) if it exists.

* Commit changes (if the above script was run, commit the changes to the `AuthenticatorPro.Droid.Shared.csproj` and `IconMap.cs` files and add the generated drawables)

* Create a pull request with your changes

## Code / Features

Before submitting any code, please open an issue to discuss if the feature is relevant.
