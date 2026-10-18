# Authentication decision

**Status:** Selected for the first local release  
**Scope:** One owner account for a Windows-hosted app available on the user's private network

## Decision

Use ASP.NET Core cookie authentication with a single credential row in the local SQLite database. The app does not use public registration, usernames, or password reset email.

The password is hashed with ASP.NET Core's `PasswordHasher<AppCredential>`. The database stores the hash and a random security stamp, never the original password. The sign-in cookie is HTTP-only, Secure, SameSite=Strict, host-only (`__Host-` prefix), and expires after 12 hours. Five consecutive failures cause a 15-minute lockout. Changing the password rotates the security stamp and ends sessions on other devices.

All Razor Pages require authentication by default. Only first-run setup, login, and the error page allow anonymous requests. First-run setup additionally checks that the request came from the host's loopback interface; Caddy provides host-only HTTPS at `https://127.0.0.1` for this flow. Caddy's LAN HTTPS site proxies to Kestrel, which listens only on `127.0.0.1:5080`.

## Recovery

There is no remote password reset. Stop Caddy and the app, then run `dotnet run --project src/PersonalFinance.Web -- --reset-authentication` from the project root. This removes the one credential row and leaves financial data intact. Restart the app and Caddy, then create a new password from the host at `https://127.0.0.1/Setup`.

## Operational assumptions

- The app is for one owner on a trusted private network and is not exposed to the public internet.
- Caddy's internal CA must be trusted by the host browser and any client browser.
- The host's Windows account and database file access are part of the security boundary.
- Cookie validation checks the current security stamp against SQLite, so a password change or local credential reset invalidates existing sessions.

## Verification still required

Automated tests should cover setup-once behavior, password hashing, good/bad login, lockout, logout, session invalidation, and authorization of protected pages. Manual browser checks should confirm HTTPS setup on the host and sign-in from Windows and Android through Caddy.
