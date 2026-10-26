[![License](https://img.shields.io/badge/license-view-orange)](LICENSE.md)

# MultiFactor.IIS.Adapter

_Also available in other languages: [Русский](README.ru.md)_

MultiFactor.IIS.Adapter is a software component, an extension for Microsoft Exchange Server. It allows to quickly enable multifactor user authentication for Outlook Web Access (OWA) and Exchange ActiveSync (EAS). 

The component is a part of <a href="https://multifactor.pro/">MultiFactor</a> 2FA hybrid solution.

* <a href="https://github.com/MultifactorLab/MultiFactor.IIS.Adapter">Source code</a>
* <a href="https://github.com/MultifactorLab/MultiFactor.IIS.Adapter/releases">Build</a>

See documentation at https://multifactor.pro/docs/outlook-web-access-2fa/ for additional guidance on integrating 2FA into Outlook Web Access (OWA), and at https://multifactor.ru/docs/baza-znanij/exchange-activesync-iis-adapter/ (in Russian) for Exchange ActiveSync.

## Table of Contents

- [General Information](#general-information)
  - [Component Features](#component-features)
  - [Workflow](#workflow)
- [Prerequisites](#prerequisites)
- [Configuration](#configuration)
  - [Multifactor Configuration](#multifactor-configuration)
  - [OWA Configuration](#owa-configuration)
  - [ActiveSync Configuration](#activesync-configuration)
- [Owa download domains requirements](#owa-download-domains-requirements)
- [ActiveSync mobile clients](#activesync-mobile-clients)
- [More Info](#more-info)
- [License](#license)

## General Information

Microsoft Exchange Server is a mail and calendaring server for mail processing and collaboration.

Outlook Web Access (OWA) is an Exchange web-client that uses IIS capabilities.

Exchange ActiveSync (EAS) is a protocol that synchronizes mail, calendars and contacts between Exchange and mobile devices.

The component contains two independent modules. Each one is enabled in the ``web.config`` of its own IIS application:

| Module     | Protects                    | IIS application               | Module type                                    |
|------------|-----------------------------|-------------------------------|------------------------------------------------|
| OWA        | Browser access to the mail  | `owa`                         | `MultiFactor.IIS.Adapter.Owa.Module`           |
| ActiveSync | Mobile mail clients         | `Microsoft-Server-ActiveSync` | `MultiFactor.IIS.Adapter.ActiveSync.Module`    |

### Component Features

**OWA**

1. Second authentication factor at each OWA login attempt or at configurable time interval;
2. User's 2FA self-enrollment at the first OWA logon;
3. Selective 2FA activation on the user's Active Directory group membership basis;
4. Access log.

**ActiveSync**

1. Second authentication factor when an account is set up on a mobile device;
2. Separate 2FA sessions for each device of the user;
3. Configurable lifetime of a successful 2FA session;
4. Delay before a new second factor request after a denied attempt;
5. Selective 2FA activation on the user's Active Directory group membership basis;
6. Access log in the Windows event log.

### Workflow

**OWA**

1. User logs in to the Outlook Web Access (OWA);
2. OWA requests the first authentication factor (login and password), checks if provided credentials are correct, and creates a user session;
3. MultiFactor.IIS.Adapter component checks that the session is authorized and requests the second authentication factor;
4. User passes second-factor verification request and completes authentication.

**ActiveSync**

1. The mobile client connects to Exchange ActiveSync;
2. Exchange checks the user's login and password;
3. MultiFactor.IIS.Adapter component sends a second factor request to Multifactor, and the user confirms it;
4. After a successful confirmation the user gets access to the mailbox;
5. The result is saved for this user and device for the configured time.

## Prerequisites

1. The component needs direct access to ``api.multifactor.ru`` via TCP port 443 (TLS) or via HTTP proxy;
2. Outlook Web Access must have a valid SSL certificate;
3. The server must be set to the correct time;
4. Net Framework version 4.8 must be installed on the server.

For ActiveSync, additionally:

5. The ``Microsoft-Server-ActiveSync`` application must run in IIS;
6. Protected users must already have a second factor configured in Multifactor: a mobile mail client cannot show the enrollment page.

## Configuration

### Multifactor Configuration

1. Create an account and log in to the Multifactor <a href="https://admin.multifactor.ru">administrative panel</a>;
2. Under <a href="https://admin.multifactor.ru/resources">resources</a>, click **Add Resource**. Then select **Outlook Web Access** under **Site** category for OWA, or **Microsoft ActiveSync** for ActiveSync. Each module needs its own resource;
3. Create and save the resource. You will need **NAS Identifier** and **Shared Secret** parameters to complete OWA configuration, and **API Key** and **API Secret** to complete ActiveSync configuration.

### OWA Configuration

1. Copy the ``Bin\MultiFactor.IIS.Adapter.dll`` file into the directory<br/>``C:\Program Files\Microsoft\Exchange Server\V15\ClientAccess\Owa\Bin``;
2. Edit the file<br/>``C:\Program Files\Microsoft\Exchange Server\V15\ClientAccess\Owa\web.config``:
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

3. To selectively enable access based on Active Directory group membership, add the following parameters to the configuration:

   ```xml
   <add key="multifactor:active-directory-2fa-group" value="owa-2fa;Users" />
   <add key="multifactor:active-directory-2fa-group-membership-cache-timeout" value="15"/>
   ```
   * The first parameter ``multifactor:active-directory-2fa-group`` &mdash; semicolon-separated AD group names without spaces. A user requires 2FA when they belong to at least one configured group. Groups can be nested;
   * The second parameter ``multifactor:active-directory-2fa-group-membership-cache-timeout`` &mdash; the time interval (in minutes) at which the user's group information is updated. For optimal performance, the default value is 15 minutes (but you can set 0 if needed as well).

4. To work with the Multifactor API through HTTP Proxy, add the following parameter to the configuration:

   ```xml
   <add key="multifactor:api-proxy" value="http://proxy:3128" />
   ```

5. After the second factor the user is returned to the external address of OWA &mdash; the one they open in the browser. The adapter takes it from the ``msExchProxyUri`` header that Exchange adds when it proxies the request to the back end, so a standard deployment needs no configuration.

   Set the address manually if the user ends up at the wrong address after the second factor, or a configuration error appears in the event log. That happens with a non-standard OWA publishing, and when users must be returned elsewhere:

   ```xml
   <add key="multifactor:public-url" value="https://mail.example.com/owa/" />
   ```

   The value must be an absolute HTTPS URL that includes the application path, without a query string or a fragment. It takes precedence over the detected address.


6. To limit the personal data sent to the Multifactor API, add the privacy mode parameter:

   ```xml
   <add key="multifactor:privacy-mode" value="Full" />
   ```

   Supported values are `None` (send all personal data, default), `Full` (send no personal data), and
   `Partial` with a comma-separated list of allowed fields (`Name`, `Email`, `Phone`).

   The adapter reads the user name (`displayName`), email (`mail`) and phone from Active Directory
   and sends them to the Multifactor API. Privacy mode controls which of them leave the adapter: `Full`
   sends none, `Partial` sends only the listed fields (e.g. `Partial:Email,Phone`), `None` sends all.
   The user identity is always sent regardless of the mode. The phone attribute is configurable via
   `multifactor:phone-attribute`; without it the phone is not read (and therefore not sent) in any mode.

   An unexpected value falls back to `None`.

### ActiveSync Configuration

Install the component on every Exchange server that serves ActiveSync.

1. Copy the ``Bin\MultiFactor.IIS.Adapter.dll`` file into the directory<br/>``C:\Program Files\Microsoft\Exchange Server\V15\ClientAccess\Sync\Bin``;
2. Edit the file<br/>``C:\Program Files\Microsoft\Exchange Server\V15\ClientAccess\Sync\web.config``:
   - First, create a backup copy of the file
   - Under ```<system.webServer><modules>``` add:<br/><br/>
     ```xml
     <add type="MultiFactor.IIS.Adapter.ActiveSync.Module, MultiFactor.IIS.Adapter" name="ActiveSync" />
     ```
   - Under ```<appSettings>``` add the module's parameters:<br/><br/>
     ```xml
     <add key="multifactor:api-url" value="https://api.multifactor.ru" />
     <add key="multifactor:api-key" value="API Key from Multifactor settings" />
     <add key="multifactor:api-secret" value="API Secret from Multifactor settings" />
     ```
   - Save changes and close the file;
   - Restart the IIS application pool that serves ``Microsoft-Server-ActiveSync``.

   You can check the actual application path in IIS Manager: **Sites** &rarr; **Exchange Back End** &rarr; **Microsoft-Server-ActiveSync** &rarr; **Basic Settings** &rarr; **Physical path**.

3. To set how long a 2FA session lasts, add the following parameters to the configuration:

   ```xml
   <add key="multifactor:session-life-time" value="8" />
   <add key="multifactor:second-factor-re-request-delay" value="5" />
   ```
   * ``multifactor:session-life-time`` &mdash; lifetime of a successful 2FA session in hours, from 1 to 24. The default value is 1 hour;
   * ``multifactor:second-factor-re-request-delay`` &mdash; delay in minutes before the second factor is requested again after a denied attempt, from 1 to 60. The default value is 5 minutes.

   The session is stored separately for each combination of the user and ``DeviceId``.

4. To selectively enable access based on Active Directory group membership, add the following parameters to the configuration:

   ```xml
   <add key="multifactor:active-directory-2fa-group" value="eas-2fa" />
   <add key="multifactor:active-directory-cache-timeout" value="15" />
   ```
   * ``multifactor:active-directory-2fa-group`` &mdash; semicolon-separated AD group names without spaces. A user requires 2FA when they belong to at least one configured group. Groups can be nested. Without the parameter the second factor is requested from all users;
   * ``multifactor:active-directory-cache-timeout`` &mdash; the time interval (in minutes) for which the user's profile and group membership are cached. The default value is 15 minutes.

   If the server serves several trusted domains, list them separated by semicolons. Without the parameter the domain of the Exchange server is used:

   ```xml
   <add key="multifactor:active-directory-domain" value="contoso.local;fabrikam.local" />
   ```

5. By default the user name from Active Directory is sent to Multifactor. To use another attribute as the identity, and to read the phone number, add:

   ```xml
   <add key="multifactor:use-attribute-as-identity" value="userPrincipalName" />
   <add key="multifactor:phone-attribute" value="mobile;otherTelephone" />
   ```
   * ``multifactor:use-attribute-as-identity`` &mdash; the Active Directory attribute used as the user identity;
   * ``multifactor:phone-attribute`` &mdash; semicolon-separated attributes that contain the phone number. Without the parameter the phone is not read.

6. To limit the personal data sent to the Multifactor API, add the privacy mode parameter:

   ```xml
   <add key="multifactor:privacy-mode" value="Partial:Phone" />
   ```

   Supported values are `None` (send all personal data, default), `Full` (send no personal data), and
   `Partial` with a comma-separated list of allowed fields (`Email`, `Phone`). The ActiveSync module does not send
   the user name. The user identity is always sent regardless of the mode.

7. To work with the Multifactor API through HTTP Proxy, add the following parameter to the configuration:

   ```xml
   <add key="multifactor:api-proxy" value="http://proxy:3128" />
   ```

8. To set the behavior when the Multifactor API is unreachable, add the following parameters to the configuration:

   ```xml
   <add key="multifactor:bypass-second-factor-when-api-unreachable" value="false" />
   <add key="multifactor:api-life-check-interval" value="15" />
   ```
   * ``multifactor:bypass-second-factor-when-api-unreachable`` &mdash; with `true` the user temporarily gets access without the second factor if the Multifactor API is unreachable over the network; with `false` access is denied. The default value is `true`. For systems with high security requirements set `false` explicitly;
   * ``multifactor:api-life-check-interval`` &mdash; how long such a bypass lasts, in minutes. The default value is 15 minutes.

   API error responses, including rate limiting, are not treated as network unavailability and do not bypass the second factor.

## Owa download domains requirements

The download domain must be a subdomain of the owa domain. [(Source)](https://learn.microsoft.com/ru-ru/exchange/plan-and-deploy/post-installation-tasks/security-best-practices/exchange-download-domains?view=exchserver-2019)

Example:

|       ---       |                         --- |
|:---------------:|----------------------------:|
|   owa domain    |             owa.example.com |
| download domain | attachments.owa.example.com |

## ActiveSync mobile clients

Clients that send a separate ``DeviceId`` for each connection are fully supported, including Gmail, Blue Mail, Aqua Mail and the built-in iOS mail client.

Outlook for iOS and Android uses one shared virtual device for the user's connections. Therefore the first connection requires the second factor, and the following connections can use the same 2FA session. Take this limitation into account when choosing the allowed mobile clients.

## More Info

The component:
* Can operate in a cluster configuration if it is installed on all servers;
* Works equally well with direct access to the IIS server and through proxies such as Nginx;
* Does not affect existing logic behind login and password authentication.
* The OWA module works directly with OWA. Your existing ECP and MAPI integrations remain unchanged; ActiveSync is protected only when the ActiveSync module is enabled;
* Re-requests the second factor in OWA after a configurable time interval and closes abandoned sessions automatically. The time interval can be configured in MultiFactor management system's <a href="https://admin.multifactor.ru/groups">Group Policy</a> section.
* In ActiveSync, handles the initial ``Provision`` request only; regular synchronization does not trigger a new second factor request;
* Does not apply to Exchange system mailboxes;
* Writes diagnostic events to the Windows **Application** log with the sources ``Multifactor OWA`` and ``Multifactor ActiveSync``;
* May require the ``web.config`` changes to be made again after an Exchange Cumulative Update or Security Update.

## License

Please note, the [license](LICENSE.md) does not entitle you to modify the source code of the Component or create derivative products based on it. The source code is provided as-is for evaluation purposes.
