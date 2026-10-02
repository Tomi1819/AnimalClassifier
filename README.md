# AnimalClassifier 🐾

**AnimalClassifier** is a machine learning-powered web application designed to recognize animals from uploaded images and videos. Built with .NET 10 and ML.NET, it offers a secure, scalable, and developer-friendly API for classification tasks.

## 🔍 Overview

This application enables users to upload images of animals and receive classification results powered by a trained machine learning model. It includes user authentication, image history tracking, and a RESTful API for seamless integration with external applications.

## 🧠 Features

- ✅ Upload images and receive AI-based classification
- 🔐 Secure user authentication and registration using JWT
- 🗝️ Passkey sign-in, alongside the password
- 🔑 Password reset over email
- 🔄 Password changes for a signed-in user
- 🚪 Signing out of every other device
- ✏️ Changing the name an account goes by
- 📦 Exporting an account's data, uploads included
- 🗑️ Deleting an account, along with everything it uploaded
- 🕓 History tracking of recognized images
- 🔗 RESTful API for integration with other applications

## 🛠️ Technologies Used

- **Backend**: .NET 10, ASP.NET Core Web API
- **Machine Learning**: ML.NET
- **Database**: Entity Framework Core
- **Authentication**: ASP.NET Identity, JWT
- **Frontend**: [AnimalClassifier.Frontend](https://github.com/Tomi1819/AnimalClassifier.Frontend)

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or another supported database

### First administrator

Register an account, set its email as `Admin:Email`, and restart the backend. On startup that account is made an administrator; sign in again for the role to take effect.

```bash
cd AnimalClassifier
dotnet user-secrets set "Admin:Email" "you@example.com"
```

### Sign-in attempts

Five wrong passwords in a row lock an account for five minutes, which is
Identity's default. That guards the account they were tried on, but not against
one address trying a few passwords on every account, or locking the same
account again and again. `POST /api/auth/login` is therefore limited per
address as well, and answers `429 Too Many Requests` beyond the limit. Signing
in with a passkey is not limited, as there is nothing to guess.

| Setting | Meaning |
| ------- | ------- |
| `RateLimiting:LoginPermitLimit`, `RateLimiting:LoginWindowMinutes` | How many sign-in attempts one address may make per window, 10 every 15 minutes unless set. |

The address is the one the connection comes from. Behind a reverse proxy that
is the proxy's, and every caller would share one allowance, so forwarded headers
have to be set up before the app is deployed that way. The same goes for the
password reset limit below.

The limit slows an address down rather than stopping it: one address can still
lock an account for part of each window, and many addresses are not slowed at
all.

### Passkeys

A passkey signs a user in with their device instead of their password. Passwords
stay, because an account whose only passkey was on a lost phone would otherwise
have no way back in, and the reset email remains that way back.

A passkey is bound to the domain the browser shows the user, which is the
frontend's rather than this API's. The relying party id is therefore taken from
the host in `Frontend:BaseUrl`, and `Passkey:ServerDomain` overrides it.

| Setting | Meaning |
| ------- | ------- |
| `Passkey:ServerDomain` | The domain passkeys are bound to. Left empty, the host from `Frontend:BaseUrl`, which is right whenever the frontend is served from one domain. |

The two have to sit under one domain. A frontend on `app.example.com` and an API
on `api.example.com` share `example.com`, which is then the setting; unrelated
domains share nothing, and passkeys cannot be used at all. Credentials do not
carry across, so one registered against a development domain will not work
against a deployed one.

Browsers only offer passkeys in a secure context. `localhost` counts as one, so
development needs nothing; everywhere else means HTTPS.

Adding a passkey asks for the account's password. `POST /api/passkey/options`
takes `{ password }`, and the state it answers with is what `POST /api/passkey`
needs to register the passkey. A passkey outlasts the session that adds it, and
a password change leaves it in place, so a session alone is not enough to add
one. A wrong password counts towards a lockout, like any other check of it.

The schema keeps passkeys from version 3 onwards, which the `AddPasskeys`
migration moves to. An existing database needs it applied:

```bash
dotnet ef database update -p AnimalClassifier.Infrastructure -s AnimalClassifier
```

It also narrows `AspNetUsers.PhoneNumber` to 256 characters, a column nothing
here writes to.

### The profile and the name

`GET /api/account` answers with the account's name, email and registration
date. The token carries only the email, so this is where a client reads the
rest.

`PUT /api/account/name` takes `{ fullName }` and answers with the profile as it
now stands. The name is kept as typed apart from its spacing, so a name such as
"McDonald" can be put right; registration still capitalises each word. A blank
name, or one over 100 characters, is a bad request. Registration refuses a name
over 100 characters as well, so an account can always save the name it has.
Sessions carry on, since the name plays no part in signing in.

Every date the API sends is UTC and ends in `Z`, including the registration
date here.

Passkeys registered before the change keep the old name on the user's device,
where it was stored when they were created.

### Changing a password

`POST /api/account/change-password` takes the current password and the new one.
A wrong current password counts towards a lockout, the same as a failed sign-in,
so an unattended session cannot be used to guess it.

The change ends every session the account had open, the caller's included, so
the answer carries a new token in the same shape as a sign-in. A client that
keeps it stays signed in; everywhere else has to sign in again.

### Signing out other devices

`POST /api/account/sign-out-other-sessions` changes the account's security
stamp, which every request's token is checked against, so every session ends on
its next request. That covers tokens a user cannot reach any other way, such as
one copied off a lost device; signing out in a browser only forgets its own
copy. As with a password change, the answer carries a new token so the caller
stays signed in. Unlike one, it asks for no password, so that token runs out
when the caller's old one would have. With a full lifetime, a token could be
kept alive for good by trading it in before it expired.

Passkeys are left alone, since they are ways in rather than sessions. Password
reset links still waiting to be used stop working, because Identity ties them to
the stamp too.

### Exporting an account's data

`GET /api/account/export` answers with a ZIP archive of everything the account
holds, named after the day it was made, such as
`animal-classifier-data-2026-09-30.zip`:

| Entry | Contents |
| ----- | -------- |
| `account.json` | The name, email and registration date, and each passkey's name and the date it was added |
| `recognitions.json` | Every recognition, most recent first, with the animal, score, date, frames for a video, and the file it was made from |
| `uploads/` | The images and videos as they were uploaded |

Recognitions cleared from the history are included with `isCleared` set, since
the statistics still count them. What only serves signing in, such as the
password hash and the passkeys' credentials, is left out.

The archive is built in a temporary file rather than in memory, as uploaded
videos can make it large, and the file is deleted once it has been sent. Each
export reads every file the account uploaded, so one account may make only a
few in a while; beyond that it is answered `429 Too Many Requests`.

| Setting | Meaning |
| ------- | ------- |
| `RateLimiting:DataExportPermitLimit`, `RateLimiting:DataExportWindowMinutes` | How many exports one account may make per window, 3 every 15 minutes unless set. |

### Deleting an account

`DELETE /api/account` takes the account's password, which counts towards a
lockout like any other check of it, and answers `204 No Content`. The account
goes at once, with nothing to undo, and every session goes with it.

Its recognitions are removed, cleared ones included, so they leave the
statistics and search pages too; its uploaded files and passkeys go as well.
The admin audit log keeps its entries, showing `Deleted user` where the account
was named, so the log still covers everything that was done.

An administrator cannot delete their account, which keeps someone able to
manage the site. Another administrator has to revoke the role first.

Leaving an entry's account empty needs the `AllowDeletedAccountsInAuditLog`
migration. An existing database needs it applied, with the same
`dotnet ef database update` as above.

### Security alerts

The account's owner is emailed whenever its password is changed or reset, a
passkey is added or removed, or its other devices are signed out, so that
someone else doing it does not go unnoticed. Each email says what happened and
how to take the account back if it was not them. None of them carries a link,
so a genuine alert never looks like the phishing it warns about.

An alert goes out once the change has been made. A mail server that fails to
send it is logged rather than reported, since an error would tell the user that
a change which went through had not. The alerts use the same email settings as
password resets, below.

### Password reset emails

The messages go out over SMTP wherever one is configured. Development may leave it unconfigured, and then writes them to the log instead, so the link is in the console and no mail server is needed. Everywhere else the app refuses to start until `Email:Host` and `Email:SenderEmail` are set.

| Setting | Meaning |
| ------- | ------- |
| `Email:Host`, `Email:Port` | The SMTP server. Port 465 is treated as implicit TLS, anything else upgrades with STARTTLS. |
| `Email:UserName`, `Email:Password` | Credentials, left empty for a server that wants none. |
| `Email:SenderEmail`, `Email:SenderName` | Who the messages come from. Providers deliver reliably only for a domain they have been given permission to send for. |
| `Frontend:BaseUrl` | Where the frontend is served from. The emailed links are built from this rather than from the request, whose host header is chosen by its caller. |
| `Frontend:ResetPasswordPath` | The page that receives the token, which has to match the frontend. |
| `RateLimiting:PasswordResetPermitLimit`, `RateLimiting:PasswordResetWindowMinutes` | How often one address may ask for a reset. |

The password is a secret, so it belongs in an environment variable rather than in `appsettings.json`:

```bash
export Email__Password="..."
```

To watch the real messages while developing, run a local mail catcher such as [Mailpit](https://mailpit.axllent.org/) or [smtp4dev](https://github.com/rnwood/smtp4dev), which accept everything and deliver nothing, and point the app at it. The settings go in user secrets, so that a clone without them still runs:

```bash
cd AnimalClassifier
dotnet user-secrets set "Email:Host" "localhost"
dotnet user-secrets set "Email:Port" "1025"
dotnet user-secrets set "Email:SenderEmail" "no-reply@animalclassifier.test"
```

Removing them again, with `dotnet user-secrets remove "Email:Host"`, puts the links back in the log.

A link stays valid for an hour, is spent once it is used, and changing a password ends every session that account had open.

### Running the tests

The integration tests need [SQL Server LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb). Each run creates its own database and drops it afterwards.

```bash
dotnet test
```
