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
- 💬 Feedback on each image's recognition, reviewed and exported to retrain the model
- 🔗 RESTful API for integration with other applications

## 🛠️ Technologies Used

- **Backend**: .NET 10, ASP.NET Core Web API
- **Machine Learning**: ML.NET
- **Database**: Entity Framework Core
- **Authentication**: ASP.NET Identity, JWT
- **Frontend**: [AnimalClassifier.Frontend](https://github.com/Tomi1819/AnimalClassifier.Frontend)

## 🚀 Getting Started

### Prerequisites

- Windows, 64-bit
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or another supported database

The backend builds wherever .NET does, but runs on Windows alone. OpenCV,
which decodes every uploaded image and video, is referenced by its Windows
runtime only, so anywhere else each upload fails; running elsewhere takes that
platform's OpenCvSharp4 runtime package in its place. The tests need SQL
Server LocalDB, which is Windows' alone as well; see
[Running the tests](#running-the-tests).

### First administrator

Register an account, confirm its email with the link mailed to it, set the email as `Admin:Email`, and restart the backend. On startup that account is made an administrator; sign in again for the role to take effect.

```bash
cd AnimalClassifier
dotnet user-secrets set "Admin:Email" "you@example.com"
```

The setting acts only while there is no administrator, and only on a confirmed
email. Whoever registers the address before its owner is not given the role,
and an administrator whose role another revoked does not get it back on the
next start. Removing the setting demotes no one.

### Registering and confirming the email

`POST /api/auth/register` takes `{ fullName, email, password }`. The email has
to be one, of at most 256 characters. The account is then emailed a link to
the frontend's `Frontend:ConfirmEmailPath` page, carrying the address and a
token, which the page sends to `POST /api/auth/confirm-email` as
`{ email, token }`. Like a password reset link, it lasts an hour.

Signing in does not wait for the confirmation. What does is anything that
trusts the address, such as `Admin:Email`. `GET /api/account` says whether the
email is confirmed, and `POST /api/account/resend-confirmation-email` mails a
signed-in user another link.

Each registration mails an address its caller picks, so one address may
register only so many accounts, and one account may ask for only so many
links. Beyond either, the answer is `429 Too Many Requests`.

| Setting | Meaning |
| ------- | ------- |
| `Frontend:ConfirmEmailPath` | The page that receives the token, which has to match the frontend. |
| `RateLimiting:RegisterPermitLimit`, `RateLimiting:RegisterWindowMinutes` | How many accounts one address may register per window, 10 an hour unless set. |
| `RateLimiting:ConfirmationEmailPermitLimit`, `RateLimiting:ConfirmationEmailWindowMinutes` | How many links one account may ask for per window, 3 every 15 minutes unless set. |

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
is the proxy's, which would have every caller share one allowance, so the
caller's own is taken from the `X-Forwarded-For` header the proxy adds. Only a
proxy on this machine, or one named in `ForwardedHeaders:KnownProxies`, is
believed; see [Deploying](#deploying). The same goes for every other limit
kept per address.

The limit slows an address down rather than stopping it: one address can still
lock an account for part of each window, and many addresses are not slowed at
all. A lockout only stops signing in, though. A session that is already open
carries on, and its owner can still change the password, add a passkey or
delete the account.

### Confirming the password

Changing the password, adding a passkey and deleting the account each ask a
signed-in user for their password. One account's password may be checked only
so often across all three, right or wrong, so an unattended session cannot be
used to guess it. Beyond that the answer is a bad request saying so, until the
window ends.

| Setting | Meaning |
| ------- | ------- |
| `RateLimiting:PasswordConfirmationPermitLimit`, `RateLimiting:PasswordConfirmationWindowMinutes` | How many times one account's password may be checked per window, 10 every 15 minutes unless set. |

This is kept apart from the lockout on signing in, in both directions. Anyone
can lock an account out by getting its password wrong, and that must not keep
its owner from changing it. A wrong guess made inside a session, in turn, must
not keep the owner from signing in.

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
one. The password can be checked only so often, as described under
[Confirming the password](#confirming-the-password).

The schema keeps passkeys from version 3 onwards, which the `AddPasskeys`
migration moves to. An existing database needs it applied:

```bash
dotnet ef database update -p AnimalClassifier.Infrastructure -s AnimalClassifier
```

It also narrows `AspNetUsers.PhoneNumber` to 256 characters, a column nothing
here writes to.

### The profile and the name

`GET /api/account` answers with the account's name, email, whether the email
is confirmed, and its registration date. The token carries only the email, so
this is where a client reads the rest.

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
The current password can be checked only so often, so an unattended session
cannot be used to guess it; see
[Confirming the password](#confirming-the-password).

A password has to be at least 8 characters long and cannot be the account's
email, wherever it is set: registration and a password reset hold to the same
rules. Length is the only rule about what it contains. Changing a password to
the one the account already has is refused too. An account whose password was
set before the minimum was raised keeps signing in with it.

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
| `recognitions.json` | Every recognition, most recent first, with the animal, score, date, frames for a video, the file it was made from, and any feedback given on it |
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

`DELETE /api/account` takes the account's password, which can be checked only
so often like any other confirmation of it, and answers `204 No Content`. The account
goes at once, with nothing to undo, and every session goes with it.

Its recognitions are removed, cleared ones included, so they leave the
statistics too; the feedback given on them, its uploaded files and its
passkeys go as well.
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
| `Email:Host`, `Email:Port` | The SMTP server. Port 465 is treated as implicit TLS, and any other port has to upgrade with STARTTLS, so a server that cannot is refused rather than sent the credentials and links in the clear. A server on this machine, such as a mail catcher, may go without. |
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

### Uploading images and videos

`POST /api/upload/image` takes a JPEG or PNG image in the `formFile` field,
and `POST /api/upload/video` an MP4, MOV or AVI video in the `videoFile` field,
each of at most 5 MB. A file's name and content type are whatever its sender
says they are, so its first bytes are checked as well, and a file that is not
really one is refused before anything reads it.

An image is then decoded and encoded afresh, which is what is classified,
stored and shown. Only the pixels make it across: what the camera wrote beside
them, such as where the photo was taken, is left behind, and the rotation it
recorded is made to the pixels instead. Decoding takes the memory of every
pixel the image's header claims, so one of more than 50 megapixels is refused
before it is decoded, and a video whose frames are larger than 4K before any
frame is read.

A video is classified a frame at a time, one frame for every second of it. A
video long enough to give more than 120 frames is sampled further apart, so
that how long it takes has a bound however long the video is, and one that
cannot be read is refused. An animal counts only when the model scores it at
0.6 or more in at least three frames, since a single frame is often a blur or a
glimpse; a video in which none does is recorded as `Unknown`.

An upload that fails at any step leaves nothing behind: no file, and no
recognition in the history.

A request more than a megabyte larger than the largest file is cut off unread
and answered `413 Payload Too Large`; one just over the limit is still read, so
that it can be told how large a file may be. One account may upload only so
many files a while, and is answered `429 Too Many Requests` beyond that.

Decoding and classifying take a processor for as long as they last, and a video
for a run per frame, so only so many uploads are worked on at once, whoever
sends them. The rest wait their turn, and once too many are waiting, an upload
is answered `503 Service Unavailable` at once rather than kept waiting. A caller
who goes away gives up their turn, and a video stops between frames, as nothing
has been recorded by then.

| Setting | Meaning |
| ------- | ------- |
| `RateLimiting:UploadPermitLimit`, `RateLimiting:UploadWindowMinutes` | How many files one account may upload per window, 20 every 15 minutes unless set. |
| `RateLimiting:ConcurrentClassificationLimit` | How many uploads are worked on at once, one for each processor unless set. |
| `RateLimiting:ClassificationQueueLimit` | How many more may wait for a turn, 20 unless set. |

### Loading uploaded images and videos

Nothing serves the upload folder. Every answer that shows an upload, such as
an upload's own, the history and the search, links to it as
`/api/media/{token}`, relative to the API, and only that link loads it. A
browser loads it from an image or a video tag, which sends no token, so the
link itself is what grants it, without signing in. It is encrypted and
signed, so neither the user nor the file can be read from it and no other link
can be made from it. It works for an hour, and stops at once when its file is
deleted, along with the account. A page is answered with fresh links each time
it asks, so the hour only has to outlast the page being looked at.

The browser may keep a file for as long as its link works, but nothing between
the two may, and a video is answered in ranges, so it can be played from any
point.

Clearing the history keeps the recognitions, which the statistics still count,
but takes them out of the history and the search, so no new link is made to
them. Their files stay, private, in the owner's copy of their data.

### History and search

`GET /api/upload/history?page=1` answers one page of the signed-in user's
recognitions, most recent first, 20 to a page, as
`{ items, page, pageSize, totalCount }`. The page counts from 1, and is the
first unless given; one past the last has no items. An administrator reads any
user's the same way, from `GET /api/admin/users/{id}/history?page=1`.

`GET /api/animal/search?searchTerm=cat` finds the animals whose name contains
the term among every user's images, and answers each with how many images it
was recognised in and links to the 12 most recent. A term such as `a` matches
nearly every animal, so one recognised less than 70% as often as the match
recognised most is left out. Videos are left out, as the page shows images,
and so are recognitions cleared from their owner's history. When nothing
matches, the answer is `404 Not Found`.

### Feedback on a recognition

A user can say of each image's recognition whether the model named the right
animal. `PUT /api/feedback/{recognitionId}` takes:

```json
{ "verdict": "WrongAnimal", "actualAnimal": "coyote", "comment": "", "allowsTraining": true }
```

| Verdict | Means | `actualAnimal` |
| ------- | ----- | -------------- |
| `Correct` | The model was right. | Left out. |
| `WrongAnimal` | The image shows another animal the model knows. | One of `GET /api/feedback/animals`, in any case; it is kept as the model writes it. |
| `UnlistedAnimal` | The image shows an animal the model does not know. | Any other, in letters, with spaces, hyphens or apostrophes between them, of at most 50 characters; it is kept in lower case. |

A comment is optional, of at most 500 characters. The answer is the feedback as
it is kept. A recognition has one feedback at most, so giving it again replaces
it, and `DELETE /api/feedback/{recognitionId}` withdraws it. `GET /api/feedback`
lists one page of what the user has given, most recently given first, and each
history entry carries its feedback as well.

Only an image's recognition takes feedback. A video's names only the animal
seen in it most, so it cannot say which frames were wrong, and has no image of
its own to train on.

The animals the model knows are read from the model itself, so a model trained
on more animals offers them without a change here.

Feedback is used to train the model only if `allowsTraining` says so, and only
once an administrator has accepted it. Without it, the feedback still counts
towards what administrators are told of the model's mistakes, which names no
one. Changing a feedback has it reviewed again, and withdrawing it, or deleting
the account, keeps it out of every export after. A model already trained on it
keeps what it learnt, as there is no taking that back.

Clearing the history leaves the feedback where it is, so that it can still be
withdrawn, and the account's copy of its data includes it.

### Reviewing feedback and retraining the model

An administrator reviews the feedback users allowed training on, under
`/api/admin/feedback`, since a user can be as mistaken about an animal as the
model:

| Endpoint | Does |
| -------- | ---- |
| `GET /api/admin/feedback?status=Pending&page=1` | One page of the feedback in one state of review, `Pending`, `Accepted` or `Rejected`, in the order it was given. Each has its image, what the model said and how sure it was, and the animal it would be trained as, but not whose it is. |
| `POST /api/admin/feedback/{id}/accept`, `.../reject` | Decides one, whatever was decided before. |
| `GET /api/admin/feedback/summary` | How often users agree with the model, the animals it takes for others most often, and the animals it does not know that users named most, counting every feedback. |
| `GET /api/admin/feedback/export` | A ZIP archive of the accepted images, named after the day it was made, such as `animal-classifier-training-2026-10-04.zip`. |

The export reads every accepted image, so it is limited like an account's own
export, by `RateLimiting:DataExportPermitLimit`. It holds:

| Entry | Contents |
| ----- | -------- |
| `dataset/<animal>/<id>.jpg` | An image of an animal the model knows, in a folder named after it |
| `unlisted/<animal>/<id>.jpg` | An image of one it does not |
| `manifest.csv` | Each image's animal, what the model took it for and how sure it was, and what its user said |

The model is not trained here: training takes a processor for many minutes,
and a new model should be checked before it replaces the old one. To retrain
it:

1. Copy the folder the model was trained from, which `MLModel.mbconfig` names,
   and merge the export's `dataset/` into the copy. Each image goes into the
   folder of its animal.
2. Leave `unlisted/` out until an animal has enough images to be learnt, a few
   dozen at least, and then add its folder as a new animal.
3. Open `MLModel.mbconfig` in Visual Studio's Model Builder, point it at the
   copy, and train.
4. Compare the new model with the old on the same images, ones neither was
   trained on, such as a set of exported images kept back for it. Model
   Builder's own score is measured on a part of the folder it picks at random,
   so two of its scores do not compare.
5. Replace `MLModel.mlnet` with the new model, and restart the app, which loads
   it once as it starts.

The accepted feedback stays accepted, so each export holds all of it, and each
model is trained on the original folder and every accepted image together.

### Errors

Every failure is answered with a message for the user, in one shape:

```json
{ "message": "Only JPEG and PNG images can be uploaded." }
```

| Status | When |
| ------ | ---- |
| `400 Bad Request` | The request was understood and refused, such as a wrong password or an unsupported file, or it could not be read, such as a number out of range. |
| `401 Unauthorized` | Signing in failed, or the request's token is missing or no longer valid. |
| `404 Not Found` | What the request names does not exist, or is not the caller's to see. |
| `413 Payload Too Large` | The request was larger than the endpoint reads, and was cut off unread. |
| `429 Too Many Requests` | A rate limit was reached. |
| `500 Internal Server Error` | Something failed inside the app. It is logged, and the message says only that something went wrong. |
| `503 Service Unavailable` | Too many uploads are waiting to be worked on already. Trying again shortly may well succeed. |

### Settings

Each setting is checked when the app starts, and the app refuses to start while
one it needs is missing or wrong, naming it, rather than failing later on the
first request that needs it. A signing key in `Jwt:SecretKey` has to be at
least 32 characters long, for example, and every rate limit at least 1.

### Deploying

Outside development the app refuses to start until each of these is set:

| Setting | Meaning |
| ------- | ------- |
| `ConnectionStrings:DefaultConnection` | The database. |
| `Jwt:SecretKey` | The key tokens are signed with, a secret of at least 32 characters. |
| `Email:Host`, `Email:SenderEmail` | The SMTP server; see [Password reset emails](#password-reset-emails). |
| `Frontend:BaseUrl` | Where the frontend is served from, which the emailed links lead to. |
| `FileUploadSettings:UploadPath` | A folder outside the app's own, which a new deployment may replace whole, along with anything kept in it. |
| `DataProtection:KeysPath` | A folder outside the app's own, for the keys that sign and encrypt the emailed links, the passkey ceremonies' state and the media links. |

The keys are encrypted with the machine's own key, so a copy of the folder is
of no use elsewhere, but the folder should still be readable by the app's
account alone. It has to be kept for as long as the app runs: losing it stops
every link and passkey ceremony in flight, though nothing kept for longer.

Beside those:

| Setting | Meaning |
| ------- | ------- |
| `Cors:AllowedOrigins` | The frontend's origin, when it is served from another than the API's. |
| `AllowedHosts` | The API's host names, such as `api.example.com`, so that a request naming any other is refused. Any host is let through unless set. |
| `ForwardedHeaders:KnownProxies` | The address of a reverse proxy on another machine, so that the caller's address and scheme are taken from the headers it adds. One on this machine, such as IIS, needs nothing. |

The API is meant to be reached over HTTPS: a request over HTTP is redirected,
and a browser that has reached the API over HTTPS is told to keep to it for a
year. Every answer says that it is never to be read as another type than its
own, run as a page, or shown in a frame, as the API serves JSON, images and
videos, and never a page.

### Updating the database

Every change to the schema is a migration, which an existing database needs
applied:

```bash
dotnet ef database update -p AnimalClassifier.Infrastructure -s AnimalClassifier
```

`RemoveAnimalImages` drops the `AnimalImages` table, which nothing ever wrote
to, and `IndexRecognitionDates` indexes when each recognition was made, which
the activity chart reads by. `StoreRecognitionFileNames` keeps each
recognition's file by its name alone, where it kept the path it was served
under, as nothing serves that path any more. `AddRecognitionFeedback` adds the
table of feedback, whose rows go with their recognition.

### Running the tests

The integration tests need [SQL Server LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb). Each run creates its own database and drops it afterwards.

```bash
dotnet test
```

Uploads are recognised by stand-ins for the model and for reading videos,
which a test tells what to see, and the stand-in for the model knows a handful
of animals of its own. `ImageClassifierTests` alone loads the trained
model, to show that the app still reads it correctly, and takes a few seconds
for it.

## 📁 Project Structure

```
AnimalClassifier/                  The API: controllers, and how the app is put together
  Controllers/                     One per area, each only passing a request on and answering
  ErrorHandling/                   How every failure is answered, always as { message }
  Extensions/                      Service registration, a file per area, such as IdentityServiceCollectionExtension
  Cors/ RateLimiting/              Which origins may call the API, and the limits on its endpoints
  Hosting/                         The keys' folder and the proxies, which running deployed needs
AnimalClassifier.Core/             What the app does
  Common/                          What every area uses
    Email/                         Sending email over SMTP, or into the log in development
    Exceptions/                    The refusals a service reports to the caller
    Models/                        Responses shared by every area, such as MessageResponse and PagedResult
    Settings/                      ISettings, which every settings class implements, and the frontend's settings
    Storage/                       Storing uploaded files, a folder per user
  Data/                            The entities, and the repository per table they are read and written through
  Identity/                        Accounts and signing in; AccountName and the roles sit here
    SecurityAlerts/                The emails sent when how an account signs in changes
    EmailConfirmation/             Confirming an account's email with a link mailed to it
    Authentication/                Registering, signing in, and the tokens a session runs on
    Passwords/                     The password rules, confirming one, and resetting a forgotten one
    Passkeys/                      Registering, using and removing passkeys
    Account/                       A signed-in user's own account: profile, name, password, export, deletion
  Recognitions/                    Recognising animals, and reading the recognitions back; MediaFile sits here
    Media/                         The expiring links an uploaded image or video is loaded by
    Classification/                The model, and reading a video's frames for it
    Uploads/                       Checking and storing an upload, and recording what was recognised in it
    Feedback/                      What users say of their recognitions, and checking it against the animals the model knows
    History/                       A user's own recognitions, and clearing them
    Search/                        Finding animals by name
    Statistics/                    The totals, the animals recognised most, and the daily activity
    Training/                      Reviewing the feedback, summing it up, and exporting it to retrain the model on
  Admin/                           Locking users, granting the administrator role, and the audit log
AnimalClassifier.Infrastructure/   The database: its context, the migrations, and each repository's implementation
AnimalClassifier.Tests/            Tests; each class that calls the API has an app and a database of its own
  Common/ Identity/ Recognitions/  The tests of each area, and the stand-ins they use
  Admin/
  ErrorHandling/ Settings/         How failures are answered, and the settings the app refuses to start without
  Hosting/                         The headers every answer carries, and the caller's address behind a proxy
  Support/                         The test app, ApiTest that most test classes start from, and DependencyOrder
```

Core is split by area rather than by kind of file, and each area into parts.
Everything a part needs sits in its folder, and the namespaces follow the
folders:

- the interface beside the service that implements it,
- the requests and responses it takes in `Models/`,
- its settings, which name their own section and mark what they need with
  attributes such as `[Required]`,
- the texts it shows the user, in a `<Part>Messages` class,
- any limit it enforces, on the class that enforces it, as with
  `AccountName.MaxLength`, `PasswordPolicy.MinLength` and
  `UploadValidator.MaxFileSize`.

The entities and the repositories' interfaces are Core's, in `Data/`, and
Infrastructure implements the repositories with Entity Framework Core, so Core
never depends on Infrastructure. Each table has a repository of its own, and
`IUnitOfWork` saves what they were given and runs several changes in one
transaction. A read that serves a request can take the request's cancellation
token, since abandoning one loses nothing; a write never does, so that a caller
who goes away cannot leave a change half made.

### Adding a feature

1. Give it a folder of its own: `Core/<Area>/<Part>/` for a part of an area,
   such as `Core/Recognitions/Uploads/`, or a new area beside the others.
2. Have its services throw `RequestRefusedException`, `NotFoundException` or
   `AuthenticationFailedException` for anything the user should be told.
   `DomainExceptionFilter` answers them with 400, 404 and 401 and the message,
   so a controller has nothing to catch. Any other exception is logged and
   answered as a server error, and its message never reaches the caller.
3. Give its settings a class implementing `ISettings`, and add them with
   `services.AddSettings<TSettings>()`, which checks them as the app starts.
4. Register its services in its area's file in `AnimalClassifier/Extensions`,
   such as `AddApplicationRecognitions`, and call any new file's from
   `Program.cs`.
5. Test it from `AnimalClassifier.Tests/<Area>/`, with a class deriving from
   `ApiTest` to call it as a signed-in user would.

Core's areas depend on each other in one direction, in this order: Common,
Data, Identity, Recognitions, Admin. Each may use those before it and none
after, so Common uses no other area, and nothing uses Admin. The parts within an area are
held to an order of their own in the same way:

| Area | Order of its parts |
| ---- | ------------------ |
| Identity | SecurityAlerts, EmailConfirmation, Authentication, Passwords, Passkeys, Account |
| Recognitions | Media, Classification, Uploads, Feedback, History, Search, Statistics, Training |

What an area's parts share sits in the area's own folder and uses none of
them. `CoreLayoutTests`, `IdentityLayoutTests` and `RecognitionsLayoutTests`
hold these orders and fail, naming the types, when a change breaks one; a new
area or part goes into its list after what it uses.
