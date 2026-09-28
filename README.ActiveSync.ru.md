[![Лицензия](https://img.shields.io/badge/license-view-orange)](LICENSE.ru.md)

# MultiFactor.IIS.Adapter для Exchange ActiveSync

MultiFactor.IIS.Adapter — программный компонент для подключения двухфакторной аутентификации к Microsoft Exchange ActiveSync (EAS).

Компонент является частью гибридного 2FA-решения сервиса [MULTIFACTOR](https://multifactor.ru/).

Дополнительные сведения доступны в [документации MULTIFACTOR](https://multifactor.ru/docs/baza-znanij/exchange-activesync-iis-adapter/).

## Содержание

- [Общие сведения](#общие-сведения)
  - [Функции компонента](#функции-компонента)
  - [Схема работы](#схема-работы)
- [Требования для установки компонента](#требования-для-установки-компонента)
- [Конфигурация](#конфигурация)
  - [Настройка MULTIFACTOR](#настройка-multifactor)
  - [Настройка Exchange ActiveSync](#настройка-exchange-activesync)
  - [Дополнительные параметры](#дополнительные-параметры)
- [Особенности мобильных клиентов](#особенности-мобильных-клиентов)
- [Дополнительная информация](#дополнительная-информация)
- [Лицензия](#лицензия)

## Общие сведения

Exchange ActiveSync — протокол синхронизации электронной почты, календарей и контактов между Microsoft Exchange Server и мобильными устройствами.

Компонент подключается к приложению `Microsoft-Server-ActiveSync` в IIS и запрашивает второй фактор при первоначальной настройке учётной записи на устройстве.

### Функции компонента

1. Защита подключения к Exchange ActiveSync вторым фактором аутентификации;
2. Раздельные 2FA-сессии для устройств пользователя;
3. Настраиваемый срок действия успешной 2FA-сессии;
4. Задержка перед повторным запросом после неуспешной попытки;
5. Избирательное включение второго фактора на основе принадлежности пользователя к группе Active Directory;
6. Журналирование обращений в журнал событий Windows.

### Схема работы

1. Мобильный клиент подключается к Exchange ActiveSync;
2. Exchange проверяет логин и пароль пользователя;
3. Компонент MultiFactor.IIS.Adapter отправляет запрос второго фактора в MULTIFACTOR;
4. После успешного подтверждения пользователю предоставляется доступ к почтовому ящику;
5. Результат сохраняется для данного пользователя и устройства на настроенный срок.

## Требования для установки компонента

1. Microsoft Exchange Server с приложением `Microsoft-Server-ActiveSync`, работающим в IIS;
2. .NET Framework 4.8;
3. Доступ с сервера Exchange к `api.multifactor.ru` по TCP-порту 443 (TLS) напрямую или через HTTP-прокси;
4. Корректно настроенные дата и время на сервере;
5. Учётная запись с правами на изменение конфигурации IIS и файлов Exchange;
6. Настроенные вторые факторы у защищаемых пользователей MULTIFACTOR.

## Конфигурация

### Настройка MULTIFACTOR

1. Создайте аккаунт и войдите в [систему управления MULTIFACTOR](https://admin.multifactor.ru/);
2. Откройте раздел **Ресурсы** и создайте ресурс **Microsoft ActiveSync**;
3. Сохраните значения **API Key** и **API Secret** — они потребуются при настройке Exchange.

### Настройка Exchange ActiveSync

Компонент необходимо установить на каждый сервер Exchange, обслуживающий ActiveSync.

1. Сделайте резервную копию файла:

   ```text
   C:\Program Files\Microsoft\Exchange Server\V15\ClientAccess\Sync\web.config
   ```

2. Скопируйте готовый файл `MultiFactor.IIS.Adapter.dll` в каталог:

   ```text
   C:\Program Files\Microsoft\Exchange Server\V15\ClientAccess\Sync\Bin
   ```

3. В файле `web.config` добавьте в секцию `<appSettings>` параметры подключения к MULTIFACTOR:

   ```xml
   <add key="multifactor:api-url" value="https://api.multifactor.ru" />
   <add key="multifactor:api-key" value="API Key из настроек MULTIFACTOR" />
   <add key="multifactor:api-secret" value="API Secret из настроек MULTIFACTOR" />
   ```

4. В секцию `<system.webServer><modules>` добавьте модуль:

   ```xml
   <add type="MultiFactor.IIS.Adapter.ActiveSync.Module, MultiFactor.IIS.Adapter"
        name="ActiveSync" />
   ```

5. Сохраните файл и перезапустите пул приложений IIS, обслуживающий `Microsoft-Server-ActiveSync`.

Фактический путь к приложению можно проверить в IIS Manager: **Sites** → **Exchange Back End** → **Microsoft-Server-ActiveSync** → **Basic Settings** → **Physical path**.

### Дополнительные параметры

#### Время действия 2FA-сессии

```xml
<add key="multifactor:session-life-time" value="8" />
<add key="multifactor:second-factor-re-request-delay" value="5" />
```

- `multifactor:session-life-time` — срок действия успешной 2FA-сессии в часах. Допустимый диапазон: от 1 до 24 часов; значение по умолчанию — 1 час;
- `multifactor:second-factor-re-request-delay` — задержка перед повторным запросом 2FA после отказа в минутах. Допустимый диапазон: от 1 до 60 минут; значение по умолчанию — 5 минут.

Сессия сохраняется отдельно для каждого сочетания пользователя и `DeviceId`.

#### Избирательное включение 2FA

Чтобы запрашивать второй фактор только у членов определённой группы Active Directory, добавьте:

```xml
<add key="multifactor:active-directory-2fa-group" value="eas-2fa" />
<add key="multifactor:active-directory-cache-timeout" value="15" />
```

- `multifactor:active-directory-2fa-group` — имя группы Active Directory. Поддерживаются вложенные группы;
- `multifactor:active-directory-cache-timeout` — срок кеширования профиля пользователя и сведений о членстве в группе в минутах. Значение по умолчанию — 15 минут.

Если группа не указана, второй фактор запрашивается у всех пользователей.

Если сервер обслуживает несколько доверенных доменов, их можно указать через точку с запятой:

```xml
<add key="multifactor:active-directory-domain" value="contoso.local;fabrikam.local" />
```

Если параметр отсутствует, используется домен сервера Exchange.

#### Идентификатор и телефон пользователя

По умолчанию в MULTIFACTOR передаётся имя пользователя из Active Directory. Чтобы использовать другой атрибут, добавьте:

```xml
<add key="multifactor:use-attribute-as-identity" value="userPrincipalName" />
```

Дополнительные атрибуты с номером телефона перечисляются через точку с запятой:

```xml
<add key="multifactor:phone-attribute" value="mobile;otherTelephone" />
```

#### Режим конфиденциальности

Ограничивает передачу персональных данных (email и телефона) в API MULTIFACTOR:

```xml
<add key="multifactor:privacy-mode" value="Partial:Phone" />
```

Поддерживаются значения `None` (передавать всё, по умолчанию), `Full` (не передавать персональные данные) и `Partial` со списком разрешённых полей через запятую (`Email`, `Phone`). Идентификатор пользователя передаётся всегда.

#### HTTP-прокси

```xml
<add key="multifactor:api-proxy" value="http://proxy.example.org:3128" />
```

#### Поведение при недоступности API

```xml
<add key="multifactor:bypass-second-factor-when-api-unreachable" value="false" />
<add key="multifactor:api-life-check-interval" value="15" />
```

- При `false` доступ без второго фактора запрещается;
- При `true` пользователь временно пропускается без второго фактора, если API MULTIFACTOR недоступен по сети;
- `multifactor:api-life-check-interval` задаёт срок такого разрешения в минутах. Значение по умолчанию — 15 минут.

По умолчанию используется значение `true`. Для систем с повышенными требованиями к безопасности рекомендуется явно установить `false`.

Ответы API с ошибкой, включая ограничение частоты запросов, не считаются сетевой недоступностью и не разрешают обход второго фактора.

## Особенности мобильных клиентов

Полностью поддерживаются клиенты, которые передают отдельный `DeviceId` для каждого подключения, в том числе Gmail, Blue Mail, Aqua Mail и встроенный почтовый клиент iOS.

Outlook для iOS и Android использует общее виртуальное устройство для подключений пользователя. Поэтому первое подключение требует подтверждения второго фактора, а последующие подключения могут использовать ту же 2FA-сессию. Учитывайте это ограничение при выборе разрешённых мобильных клиентов.

## Дополнительная информация

Компонент:

- Не изменяет проверку логина и пароля в Exchange;
- Обрабатывает первоначальный запрос `Provision`; обычные операции синхронизации не создают новый запрос второго фактора;
- Не применяется к системным почтовым ящикам Exchange;
- Пишет диагностические события в журнал Windows **Application** с источником `Multifactor ActiveSync`;
- Может работать в конфигурации с несколькими серверами, если DLL и параметры `web.config` установлены на каждом сервере;
- Может потребовать повторного внесения изменений в `web.config` после установки Exchange Cumulative Update или Security Update.

## Лицензия

Обратите внимание на [лицензию](LICENSE.ru.md). Она не даёт права вносить изменения в исходный код компонента и создавать производные продукты на его основе. Исходный код предоставляется в ознакомительных целях.