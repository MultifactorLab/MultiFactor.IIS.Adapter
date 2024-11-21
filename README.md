[![License](https://img.shields.io/badge/license-view-orange)](LICENSE.md)

# MultiFactor.IIS.Adapter

_Also available in other languages: [Русский](README.ru.md)_

MultiFactor.IIS.Adapter is a software component, an extension for Microsoft Exchange Server. It allows to quickly enable multifactor user authentication for Outlook Web Access (OWA). 

The component is a part of <a href="https://multifactor.pro/">MultiFactor</a> 2FA hybrid solution.

* <a href="https://github.com/MultifactorLab/MultiFactor.IIS.Adapter">Source code</a>
* <a href="https://github.com/MultifactorLab/MultiFactor.IIS.Adapter/releases">Build</a>

See documentation at https://multifactor.pro/docs/outlook-web-access-2fa/ for additional guidance on integrating 2FA into Outlook Web Access (OWA).

## Table of Contents

- [General Information](#general-information)
  - [Component Features](#component-features)
  - [Workflow](#workflow)
- [Prerequisites](#prerequisites)
- [Configuration](#configuration)
  - [Multifactor Configuration](#multifactor-configuration)
  - [OWA Configuration](#owa-configuration)
- [More Info](#more-info)
- [License](#license)

## General Information

Microsoft Exchange Server is a mail and calendaring server for mail processing and collaboration.

Outlook Web Access (OWA) is an Exchange web-client that uses IIS capabilities.

### Component Features

1. Second authentication factor at each OWA login attempt or at configurable time interval;
2. User's 2FA self-enrollment at the first OWA logon;
3. Selective 2FA activation on the user's Active Directory group membership basis;
4. Access log.

### Workflow

1. User logs in to the Outlook Web Access (OWA);
2. OWA requests the first authentication factor (login and password), checks if provided credentials are correct, and creates a user session;
3. MultiFactor.IIS.Adapter component checks that the session is authorized and requests the second authentication factor;
4. User passes second-factor verification request and completes authentication.

## Prerequisites

1. The component needs direct access to ``api.multifactor.ru`` via TCP port 443 (TLS) or via HTTP proxy;
2. Outlook Web Access must have a valid SSL certificate;
3. The server must be set to the correct time;
4. Net Framework version 4.8 must be installed on the server.

## Configuration

### Multifactor Configuration

1. Create an account and log in to the Multifactor <a href="https://admin.multifactor.ru">administrative panel</a>;
2. Under <a href="https://admin.multifactor.ru/resources">resources</a>, click **Add Resource**. Then select **Outlook Web Access** under **Site** category;
3. Create and save the resource. You will need **NAS Identifier** and **Shared Secret** parameters to complete OWA configuration.

### OWA Configuration

1. Copy the ``Bin\MultiFactor.IIS.Adapter.dll`` file into the directory<br/>``C:\Program Files\Microsoft\Exchange Server\V15\ClientAccess\Owa\Bin``;
2. Copy the ``mfa.aspx`` file into the directory<br/>``C:\Program Files\Microsoft\Exchange Server\V15\ClientAccess\Owa``;
3. Edit the file<br/>``C:\Program Files\Microsoft\Exchange Server\V15\ClientAccess\Owa\web.config``:
   - First, create a backup copy of the file
   - Under ```<modules>```, on the first line add:<br/><br/>
     ```xml
     <add type="MultiFactor.IIS.Adapter.Owa.Module, MultiFactor.IIS.Adapter" name="MFA" />
     ```
   - Under ```<appSettings>``` add the module's parameters:<br/><br/>
     ```xml
     <add key="multifactor:api-url" value="https://api.multifactor.ru" />
     <add key="multifactor:api-key" value="API Key from multifactor settings" />
     <add key="multifactor:api-secret" value="API Secret from Multifactor settings" />
     ```
   - Save changes and close the file.

4. To selectively enable access based on Active Directory group membership, add the following parameters to the configuration:

   ```xml
   <add key="multifactor:active-directory-2fa-group" value="owa-2fa" />
   <add key="multifactor:active-directory-2fa-group-membership-cache-timeout" value="15"/>
   ```
   * The first parameter ``multifactor:active-directory-2fa-group`` &mdash; AD group name. The group can be nested and contain other groups;
   * The second parameter ``multifactor:active-directory-2fa-group-membership-cache-timeout`` &mdash; the time interval (in minutes) at which the user's group information is updated. For optimal performance, the default value is 15 minutes (but you can set 0 if needed as well).

5. To work with the Multifactor API through HTTP Proxy, add the following parameter to the configuration:

   ```xml
   <add key="multifactor:api-proxy" value="http://proxy:3128" />
   ```
   
## Owa download domains requirements

The download domain must be a subdomain of the owa domain.

Example:

|       ---       |                         --- |
|:---------------:|----------------------------:|
|   owa domain    |             owa.example.com |
| download domain | attachments.owa.example.com |

## More Info

The component:
* Can operate in a cluster configuration if it is installed on all servers;
* Works equally well with direct access to the IIS server and through proxies such as Nginx;
* Does not affect existing logic behind login and password authentication.
* Works directly with OWA. Your existing ECP, MAPI, and ActiveSync integrations remain unchanged;
* Re-requests the second factor after a configurable time interval and closes abandoned sessions automatically. The time interval can be configured in MultiFactor management system's <a href="https://admin.multifactor.ru/groups">Group Policy</a> section.

## License

Please note, the [license](LICENSE.md) does not entitle you to modify the source code of the Component or create derivative products based on it. The source code is provided as-is for evaluation purposes.