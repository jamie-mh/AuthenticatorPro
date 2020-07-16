# Contribution Guide 

## Icons

If you wish to contribute more icons to the application, the procedure is as follows:

* Fork the repo.

* Find a high-quality icon for the service you want to add. Try searching online for '{service_name} brand' - generally, many companies offer high-res versions of their logos and icons for press and media. Avoid icons made by 3rd parties with different styles from the original. Prefer flat icons instead of complex ones.

* Use the [Android Asset Studio](https://romannurik.github.io/AndroidAssetStudio/index.html) to generate the appropriate asset sizes for the icon. Use the "Generic Icon Generator" with Trim enabled, 0% padding, 32dp Asset Size, 0dp Asset Padding and Color transparent.

* Name the icon "auth_xxxxx", with xxxxx being the name of the service in lowercase with spaces and special characters removed. Eg: Authenticator Pro -> authenticatorpro.

* Copy the icons into the Resources directory of the AuthenticatorPro.Shared project.

* Update AuthenticatorPro.Shared/Source/Data/Icon.cs by adding the icon into the Service dictionary in alphabetical order. If the icon is barely visible on a dark background. Create an alternative icon as before with the name auth_xxxxx_dark and place it into the ServiceDark dictionary.

* Test the changes if possible.

* Create a pull request.

## Translations

If you don't speak a language good enough, you may add a comment to show which strings need translation. English is preferred over bad grammar.

## Code / Features

Before submitting any code, please open an issue beforehand to discuss if the feature is relevant.
