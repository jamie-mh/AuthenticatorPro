# Contribution Guide 

## Translations 💬

Translations are now managed on Crowdin. Go to the [Authenticator Pro Crowdin project](https://crowdin.com/project/authenticator-pro) to contribute. If your language is not available, please contact me and I will add it.
<br></br>

## Icons ⏺️

If you'd like to contribute some icons, first check if there's [any open issues from user requests](https://github.com/jamie-mh/AuthenticatorPro/issues?q=is%3Aopen+is%3Aissue+label%3Aenhancement).

### Icon criteria:
Not every service needs an icon. To prevent the app having hundreds of icons from obscure and rarely used platforms, we limit what icons can be added. If a service doesn't meet the criteria we encourage the use of custom icons from within the app.

- Platforms that use a 'Single Sign-On' should have the icon added for the sign-on account and not for the individual platforms. Eg: instead of a YouTube icon a Google icon would be needed, or instead of a Photoshop icon an Adobe icon should be used.
- Web based platforms should be within [Similarweb's](https://www.similarweb.com) top 200,000 global rank. [Simply search for the site and see for yourself](https://www.similarweb.com).
- Mobile platforms should have at least 100k+ downloads on the [Google Play Store](https://play.google.com/). 
- If the service is not web based or on the Play Store it will have to be reviewed individually, in which case it's best to just [submit a request as a issue.](https://github.com/jamie-mh/AuthenticatorPro/issues/new?assignees=&labels=enhancement&template=icon_request.md&title=)

### How to add an icon:
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

* Commit your changes

* Create a pull request with your changes
<br></br>

## Code / Features ⚙️

Before submitting any code, please open an issue to discuss if the feature is relevant.
