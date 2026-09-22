# AnimalClassifier 🐾

**AnimalClassifier** is a machine learning-powered web application designed to recognize animals from uploaded images and videos. Built with .NET 10 and ML.NET, it offers a secure, scalable, and developer-friendly API for classification tasks.

## 🔍 Overview

This application enables users to upload images of animals and receive classification results powered by a trained machine learning model. It includes user authentication, image history tracking, and a RESTful API for seamless integration with external applications.

## 🧠 Features

- ✅ Upload images and receive AI-based classification
- 🔐 Secure user authentication and registration using JWT
- 🗝️ Passkey sign-in, alongside the password
- 🔑 Password reset over email
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

The schema keeps passkeys from version 3 onwards, which the `AddPasskeys`
migration moves to. An existing database needs it applied:

```bash
dotnet ef database update -p AnimalClassifier.Infrastructure -s AnimalClassifier
```

It also narrows `AspNetUsers.PhoneNumber` to 256 characters, a column nothing
here writes to.

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
