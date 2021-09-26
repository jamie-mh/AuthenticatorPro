# Contribution Guide 

## Translations

Translations are now managed on Crowdin. Go to the [Authenticator Pro Crowdin project](https://crwd.in/authenticator-pro) to contribute. If your language is not available, please contact me and I will add it.

## Icons

If you'd like to contribute some icons, check the [missing icons list](./extra/missing_icons.txt) for some that might need adding. They may already exist, so check first!

To add an icon to the project the procedure is as follows:

* Fork the repo

* Find a high-quality icon for the service you want to add. Try searching online for '{service_name} brand' - generally, many services offer high-res versions of their logos and icons for press and media.
  
    * Avoid icons made by 3rd parties with different styles from the original
    * Prefer flat icons instead of complex ones
    * Avoid text
    * Avoid unnecessary frames and backgrounds
  
* Save the icon as a square 128x128 png

  * The icon must fill as much space as possible
  * The background should be transparent

* Name the icon as lowercase with spaces and special characters removed. Eg: Authenticator Pro -> authenticatorpro
  
* Place the file in the "icons" directory

* If the icon requires a dark theme variant, repeat the process and append "_dark" to the name.

* Remove the entry from the [missing icons list](./extra/missing_icons.txt) if it exists.

* Commit your changes

* Create a pull request with your changes

## Code / Features

Before submitting any code, please open an issue to discuss if the feature is relevant.
