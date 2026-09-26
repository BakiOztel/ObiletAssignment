# Bus Journey Search

An ASP.NET Core MVC (.NET 8) application for searching intercity bus journeys. Users pick an origin, a
destination and a departure date, and get every journey for that day, sorted by departure time. All data
comes live from an external bus ticketing API.

> [!IMPORTANT]
> **Before running:** the API base URL and client token are intentionally not committed. Set them once with
> the two `dotnet user-secrets` commands below, using the values from the assignment documents. The
> application does not start without them.

## Getting started

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

**1. Configure secrets** (once per machine):

```bash
cd src/BusJourney.Web
dotnet user-secrets set "Provider:BaseUrl" "https://<api-host>/api"
dotnet user-secrets set "Provider:ApiClientToken" "<api-client-token>"
```

- `Provider:BaseUrl`: the API endpoint from the integration document, up to and including `/api`.
- `Provider:ApiClientToken`: `ApiClientToken` from the assignment document, without the `Basic ` prefix.

The values are stored in your user profile, outside the project folder, so they never end up in git. In other
environments, use the environment variables `Provider__BaseUrl` and `Provider__ApiClientToken`.

**2. Run:**

```bash
dotnet run --launch-profile https
```

Open <https://localhost:7039>. If the browser warns about the certificate, run `dotnet dev-certs https --trust` once.

**3. Test** (from the repository root):

```bash
dotnet test
```

## Features

- Origin and destination with live search, a swap button, and the location picked on one side greyed out on the other (choosing it swaps the two fields).
- Date field with previous / next day arrows and a Today / Tomorrow switch. Defaults are the API's first two locations and tomorrow's date.
- The last search is remembered in the browser and restored on the next visit.
- Validation (same location, past date) in the browser and on the server.
- Journeys sorted by departure time. Journeys leaving after midnight are marked with a separate note.
- Responsive layout: a horizontal search bar on desktop, a stacked layout on phones.
- Each visitor gets their own API session. The browser never calls the provider API directly.

## Architecture

```text
src/
  BusJourney.Application/     Models, services, validation, and the ports the other layers implement
  BusJourney.Infrastructure/  Provider API client: typed HttpClient, request/response envelopes, JSON contracts
  BusJourney.Web/             Controllers, Razor views, session store, filters, static assets (CSS/JS)
tests/
  BusJourney.Tests/           xUnit tests
```

Dependencies point inwards (`Web → Application ← Infrastructure`), so the Application layer knows nothing about
HTTP, ASP.NET or the provider's JSON format.

## Design decisions

- **Session per visitor:** the provider session is created on the visitor's first request and kept in
  ASP.NET Core Session.
- **Typed `HttpClient`:** base address, timeout and the auth header are configured once at registration.
- **One exception type:** every provider failure becomes a `ProviderException`, which a filter turns into a
  user-friendly error.
- **Validation lives on the server:** `JourneyQueryValidator` is the source of truth. The JavaScript copy only
  gives instant feedback.
- **"Today" is always Turkish time,** independent of the server's time zone.
- **Framework-free frontend:** Bootstrap for base styles plus small ES modules. The autocomplete is a
  reusable component.
- **Route names travel in the query string** (`originName`, `destinationName`) next to the ids. They are only
  used for the result header. Originally the header was resolved on the server from the location list when a
  search had no journeys, keeping the URL to ids only. That failed because the provider cannot look a location
  up by id and its unfiltered list is short (see below). Missing names fall back to the journeys' own names.
- **Secrets are never in source control,** and missing settings are caught at startup.

Tests run against a stub HTTP handler and a fixed clock, so they never call the real API.

## Notes on the provider API

- The documented `GetSession` body (`type: 7` + `application`) is rejected by the live API. The body from the
  sample Postman collection (`type: 1` + `connection.port` + `browser`) works and is the one used.
- The documentation is inconsistent about some numeric types (`id`, `internet-price`). The client accepts both
  numbers and strings.
- Although the documentation says `GetBusLocations` returns all locations without a keyword, the live API
  returns only a short list of about 20 popular ones. Other locations (e.g. Bodrum) appear only when searched.
- A search for a date also returns journeys leaving after midnight. They are kept at the end of the list.
- The provider applies an IP-based rate limit, so this application also limits requests per client IP.

## Known limitations

- **Expired provider sessions** are not refreshed automatically. The API does not document which status it
  returns for an expired session, so there is no retry logic yet.
- **The location list is not cached.** It is fetched from the API on every load of the search page. The
  documentation does not say whether the list is fixed or can change, so the application always shows the
  provider's current list.
