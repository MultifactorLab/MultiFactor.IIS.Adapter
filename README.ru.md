[![Лицензия](https://img.shields.io/badge/license-view-orange)](LICENSE.ru.md)

# MultiFactor.IIS.Adapter

_Also available in other languages: [English](README.md)_

MultiFactor.IIS.Adapter &mdash; программный компонент, расширение для Microsoft Exchange Server. Позволяет быстро подключить мультифакторную аутентификацию пользователей к Outlook Web Access (OWA). 

Компонент является частью гибридного 2FA решения сервиса <a href="https://multifactor.ru/" target="_blank">MultiFactor</a>.

* <a href="https://github.com/MultifactorLab/MultiFactor.IIS.Adapter" target="_blank">Исходный код</a>
* <a href="https://github.com/MultifactorLab/MultiFactor.IIS.Adapter/releases" target="_blank">Сборка</a>

Дополнительные инструкции по интеграции 2FA в Outlook Web Access (OWA) см. в документации по адресу https://multifactor.ru/docs/outlook-web-access-2fa/.

## Содержание

- [Общие сведения](#общие-сведения)
  - [Функции компонента](#функции-компонента)
  - [Схема работы](#схема-работы)
- [Требования для установки компонента](#требования-для-установки-компонента)
- [Конфигурация](#конфигурация)
  - [Настройка Multifactor](#настройка-multifactor)
  - [Настройка OWA](#настройка-owa)
- [Дополнительная информация](#дополнительная-информация)
- [Лицензия](#лицензия)

## Общие сведения

Microsoft Exchange Server — программный продукт для обработки и пересылки почтовых сообщений, совместной работы (календари, задачи).

Outlook Web Access (OWA) — веб-клиент Exchange, использующий возможности IIS.

### Функции компонента

1. Защита доступа вторым фактором проверки подлинности при каждом входе и через настраиваемый промежуток времени;
2. Самостоятельная настройка второго фактора пользователем при первом входе;
3. Избирательное включение второго фактора на основе принадлежности к группе в Active Directory;
4. Журнал доступа.

### Схема работы

1. Пользователь открывает сайт Outlook Web Access;
2. OWA запрашивает первый фактор аутентификации: логин и пароль, проверяет корректность указанных данных и создает пользовательскую сессию;
3. Компонент MultiFactor.IIS.Adapter проверяет, что сессия авторизована и переадресовывает пользователя на второй фактор аутентификации;
4. После успешного прохождения второго фактора, пользователь возвращается на сайт OWA и продолжает работу.

## Требования для установки компонента

1. Компоненту необходим доступ к хосту ```api.multifactor.ru``` по TCP порту 443 (TLS) напрямую или через HTTP прокси;
2. Outlook Web Access должен работать с валидным SSL сертификатом;
3. На сервере должно быть установлено правильное время;
4. На сервере должен быть установлен Net Framework версии 4.8.

## Конфигурация

### Настройка Multifactor

1. Создайте аккаунт и войдите в <a href="https://admin.multifactor.ru" target="_blank">административную панель</a> Мультифактор;
2. В разделе <a href="https://admin.multifactor.ru/resources" target="_blank">ресурсы</a> кликните **Добавить ресурс**. В появившемся списке выберите **Outlook Web Access** в разделе **Сайт**. Заполните необходимые поля для того, чтобы получить параметры **NAS Identifier** и **Shared Secret**. Эти параметры потребуются для завершения настройки.

### Настройка OWA

1. Скопируйте файл ``Bin\MultiFactor.IIS.Adapter.dll`` в директорию <br/>``C:\Program Files\Microsoft\Exchange Server\V15\ClientAccess\Owa\Bin``;
2. Скопируйте файл ``mfa.aspx`` в директорию <br/>``C:\Program Files\Microsoft\Exchange Server\V15\ClientAccess\Owa``;
3. Отредактируйте файл <br/>``C:\Program Files\Microsoft\Exchange Server\V15\ClientAccess\Owa\web.config``:
   - сделайте резервную копию файла
   - в раздел ```<modules>``` добавьте компонент первой строкой:<br/><br/>
     ```xml
     <add type="MultiFactor.IIS.Adapter.Owa.Module, MultiFactor.IIS.Adapter" name="MFA" />
     ```
   - в раздел ```<appSettings>``` добавьте параметры компонента:<br/><br/>
     ```xml
     <add key="multifactor:api-url" value="https://api.multifactor.ru" />
     <add key="multifactor:api-key" value="API Key из настроек Мультифактора" />
     <add key="multifactor:api-secret" value="API Secret из настроек Мультифактора" />
     ```
   - сохраните изменения и закройте файл.
4. Для избирательного включения доступа на основне принадлежности к группе Active Directory, добавьте в конфигурацию параметры:

   ```xml
   <add key="multifactor:active-directory-2fa-group" value="owa-2fa" />
   <add key="multifactor:active-directory-2fa-group-membership-cache-timeout" value="15"/>
   ```
   * Первый параметр ``multifactor:active-directory-2fa-group`` &mdash; название группы в AD. Группа может быть вложенной, то есть содержать в себе другие группы;
   * Второй параметр ``multifactor:active-directory-2fa-group-membership-cache-timeout`` &mdash; промежуток времени (в минутах) через который обновляется информация о вхождении пользователя в группу. Для оптимизации производительности, значение по-умолчанию составляет 15 минут (но можно поставить 0).

5. Для работы с API Мультифактора через HTTP Proxy, добавьте в конфигурацию параметр:

   ```xml
   <add key="multifactor:api-proxy" value="http://proxy:3128" />
   ```

## Требования к доменам загрузки OWA

Домен загрузки должен быть поддоменом основного домена OWA. [(Источник)](https://learn.microsoft.com/ru-ru/exchange/plan-and-deploy/post-installation-tasks/security-best-practices/exchange-download-domains?view=exchserver-2019)


Пример:

|      ---       |                         --- |
|:--------------:|----------------------------:|
|   Домен OWA    |             owa.example.com |
| Домен загрузки | attachments.owa.example.com |

## Дополнительная информация

Компонент:

* Может работать в кластерной конфигурации, если он установлен на все сервера;
* Одинаково хорошо работает с прямым доступом к серверу IIS и через прокси, например, nginx;
* Не влияет на первый фактор аутентификации, а именно проверку логина и пароля пользователя;
* Подключается к OWA. Работа с ECP, MAPI и ActiveSync остается без изменений;
* Повторно запрашивает второй фактор через настраиваемый промежуток времени и закрывает оставленные пользователями сессии. Интервал времени настраивается в <a href="https://admin.multifactor.ru/groups" target="_blank">групповой политике</a> сервиса Мультифактор.

## Лицензия

Обратите внимание на [лицензию](LICENSE.ru.md). Она не дает вам право вносить изменения в исходный код Компонента и создавать производные продукты на его основе. Исходный код предоставляется в ознакомительных целях.