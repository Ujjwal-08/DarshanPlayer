# Privacy Policy — Darshan Player

**Last updated: 9 October 2026**

Darshan Player is a media player for Windows published by Ujjwal Dadhich.

**Short version: Darshan Player does not collect, transmit, or sell your personal
information.** There are no accounts, no analytics, no advertising, and no tracking of
any kind. Everything the app remembers stays on your own computer.

---

## Information the app stores on your computer

To work as a media player, Darshan Player saves the following **locally on your device
only**. None of it is sent anywhere.

| What | Where | Why |
|---|---|---|
| Settings — volume, playback speed, equalizer, subtitle appearance, language, window size | `%AppData%\DarshanPlayer\settings.json` | So your preferences persist between sessions |
| Recently opened files and folders (file paths and names) | `%AppData%\DarshanPlayer\settings.json` | For the Recent list and the taskbar jump list |
| Your current playlist (file paths) | `%AppData%\DarshanPlayer\session.m3u8` | So your playlist returns when you reopen the app |
| Resume positions — file path, how far you watched, and when | `%AppData%\DarshanPlayer\settings.json` | So you can continue where you left off |
| Diagnostic logs — app events, window states, and the **names** of files you open | `%LocalAppData%\DarshanPlayer\logs\` | To diagnose crashes and playback problems. Kept for 7 days, then deleted automatically |
| Screenshots you choose to take | Your Pictures folder | Because you asked the app to save them |
| Windows file-type associations | Your user registry (`HKEY_CURRENT_USER`) | So Windows can offer Darshan Player in "Open with" and Default apps |

**The app never uploads, reads, or analyses the content of your media files.** It plays
them. File names appear in the local log only; the logs stay on your machine unless you
choose to send one to us when reporting a bug.

### Removing this data

Uninstall Darshan Player from **Settings ▸ Apps ▸ Installed apps**. To also remove your
saved settings and logs, delete these folders:

- `%AppData%\DarshanPlayer`
- `%LocalAppData%\DarshanPlayer`

Uninstalling removes the file-type associations the app added.

---

## When the app connects to the internet

Darshan Player makes network connections in only two situations:

1. **Checking for updates.** The app periodically asks GitHub whether a newer version has
   been released, and downloads it if so. As with visiting any website, GitHub receives
   your IP address and standard request information. Darshan Player sends no identifier,
   no account, and no information about you or your files. GitHub's handling of that
   request is covered by the
   [GitHub Privacy Statement](https://docs.github.com/en/site-policy/privacy-policies/github-privacy-statement).

2. **Links you click.** Buttons such as "Check for updates", or the support links, open a
   page in your own web browser. Those sites (GitHub, Patreon, PayPal) have their own
   privacy policies, and your use of them is between you and those services. Darshan
   Player shares nothing with them.

The app works fully offline. No feature other than updating requires a connection.

---

## What Darshan Player does not do

- No user accounts, sign-in, or registration
- No analytics, telemetry, usage statistics, or crash reporting sent to us
- No advertising and no advertising identifiers
- No selling, renting, or sharing of personal information — we do not receive any to share
- No location data, contacts, camera, or microphone access
- No reading or scanning of your media library in the background

---

## Children

Darshan Player is a general-purpose media player. It is not directed at children, and
because it collects no personal information, it collects none from children either.

---

## Your rights

Because none of your information ever leaves your device, there is nothing for us to
access, correct, export, or delete on your behalf — you remain in full control, and
deleting the folders listed above removes everything the app has stored.

---

## Changes to this policy

If this policy changes, the updated version will be published at this address with a new
"Last updated" date. Significant changes will also be noted in the release notes.

---

## Contact

Questions about this policy or about privacy in Darshan Player:

- **Issues:** https://github.com/Ujjwal-08/DarshanPlayer/issues
- **Email:** ujjwaldadhich08@gmail.com
