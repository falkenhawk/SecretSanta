SecretSanta
===========

### Simple Gift Exchange Organizer

SecretSanta is an easy to use gift exchange organizer that you can deploy to your own webserver, or even to a free Windows Azure webiste.

![Screenshot](https://raw.github.com/falkenhawk/SecretSanta/master/screenshot.png)

## Features

* Add as many family members as you like
* No database needed, all data is stored as JSON in text files
* Users get to click a button to pick a recipient (more fun than just being told who you picked)
* Specify which people each user should NOT pick (e.g. spouses)
* Admin console for managing users
* Responsive Twitter Bootstrap template is mobile friendly
* Wish list functionality with pre-generated preview images for items included
* Configurable gift dollar limit (used for display purposes only)
* Send login links to all users, no passwords or account creation needed
* Emails are sent out to users once everyone has chosen their recipient
* Reminder functionality so users can anonymously remind their recipient to add items to their wish list

## Set Up

1. Add the following appSettings to web.config, or [configure as app settings in Azure](https://azure.microsoft.com/en-us/blog/windows-azure-web-sites-how-application-strings-and-connection-strings-work/):
```xml
    <add key="SecretSanta:AdminEmail" value="my_email@outlook.com" />
    <add key="SecretSanta:MaxImagesToLoad" value="25"/>
    <add key="SecretSanta:DefaultPreviewImage" value="images/photo_not_available.png"/>
    <add key="SecretSanta:DataDirectory" value="App_Data" />
    <add key="SecretSanta:AccountFilePattern" value="*.account.json" />
    <add key="SecretSanta:GiftDollarLimit" value="40" />
    <add key="SecretSanta:SmtpHost" value="smtp.sendgrid.net" />
    <add key="SecretSanta:SmtpPort" value="587" />
    <add key="SecretSanta:SmtpUser" value="my_email_user@azure.com" />
    <add key="SecretSanta:SmtpPass" value="my_email_password" />
```
3. Deploy to any server capable of running ASP.NET Core 1 or later
4. Make sure the folder specified in the `DataDirectory` appSetting is writable


5. Steps to build and save the docker image:
```
docker build -t secretsanta .
docker save -o <absolutepathtofile.tar> secretsanta
```