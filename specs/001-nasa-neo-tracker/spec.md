# Feature Specification: NASA NEO Date Tracker

**Feature Branch**: `001-nasa-neo-tracker`

**Created**: 2026-10-27

**Status**: Draft

## Clarifications

### Session 2026-10-27

- Q: Если для выбранной даты нет результатов, какой сохранённый день показывать вместо неё? → A: Непустой кэшированный результат, полученный последним, независимо от его даты относительно выбранной.
- Q: Когда сеть или NASA API недоступны, приложение должно сразу показывать кэшированные данные или ждать подтверждения пользователя? → A: Сначала сообщить о сбое, показать кэш только после выбора пользователя.

**Input**: User description: "Веб-приложение NASA NEO Tracker с графическим интерфейсом для мониторинга астероидов, пролетающих близко к Земле в выбранную дату. Пользователь вводит дату в формате ГГГГ-ММ-ДД и нажимает кнопку "Получить данные"; кнопка неактивна, пока дата пустая. Приложение показывает список астероидов: название, диаметр (мин–макс, м), скорость (км/ч), расстояние до Земли (км), флаг потенциальной опасности (визуально выделен). Под списком отображается источник данных (NASA NeoWs API) и метка времени ответа. Валидация: неверный формат, несуществующие даты (31 февраля, 30.02), пустое поле — сообщение об ошибке, запрос не выполняется. Если на дату нет данных или API вернул пустой список — показать последний успешно закэшированный день и явно предупредить, за какую он дату. Нет сети или API недоступен — приложение не падает, сообщает об этом и предлагает данные из кэша с указанием их даты. Кэш переживает перезапуск приложения."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Asteroids for a Selected Date (Priority: P1)

As a person monitoring near-Earth objects, I want to choose a date and see the asteroids
recorded for it, so I can quickly understand their size, speed, distance, and potential
hazard.

**Why this priority**: Retrieving and understanding a chosen day's asteroid data is the
main purpose of the application.

**Independent Test**: Enter a valid date for which NASA NeoWs has results, request the
data, and verify that each asteroid's required details and the response source and time
are shown.

**Acceptance Scenarios**:

1. **Given** a valid date with asteroid results, **When** the user requests data,
   **Then** the application shows each asteroid's name, minimum and maximum diameter in
   meters, speed in kilometers per hour, distance from Earth in kilometers, and a
   visually distinct potential-hazard flag.
2. **Given** a successful non-empty response, **When** the result is displayed,
   **Then** the source is identified as NASA NeoWs API and the response time is shown.
3. **Given** the date field is empty, **When** the user views the form,
   **Then** the request button is disabled and the required-field validation message is
   available without making a request.
4. **Given** an invalidly formatted or impossible calendar date, **When** the user
   attempts to request data, **Then** a clear validation message is shown and no request
   is made.

---

### User Story 2 - Use Cached Data When Current Data Is Unavailable (Priority: P1)

As a person who needs to monitor asteroid information reliably, I want the application
to show useful saved results when a date has no results or the service is unreachable,
so I can still consult data and understand how current it is.

**Why this priority**: Resilient access to information is essential when the service
returns no results or connectivity is interrupted.

**Independent Test**: Seed a previously saved non-empty day, then exercise an empty
response and a service/network failure; verify that an empty response displays the
saved day automatically, while a service/network failure offers it and displays it only
after the user chooses it.

**Acceptance Scenarios**:

1. **Given** the selected date has no asteroid results or the service returns an empty
   list, **When** a prior non-empty day is cached, **Then** the application displays the
   non-empty day most recently received and clearly identifies its date and why it is
   being shown, even if that date is later than the selected date.
2. **Given** the network is unavailable or the service cannot be reached, **When** the
   user requests a valid date and cached non-empty data exists, **Then** the application
   does not crash, explains the failure, and offers the cached results with their date;
   it displays those results only after the user chooses to view them.
3. **Given** no cached non-empty day is available, **When** the selected date has no
   results or the service is unavailable, **Then** the application shows a clear
   explanation that no cached results are available and does not present an empty or
   failed response as successful asteroid data.
4. **Given** a successful response for a previously uncached date, **When** the
   application is restarted, **Then** that date's successfully received response remains
   available locally.

---

### User Story 3 - Understand and Recover from Invalid Input (Priority: P2)

As a user entering a date, I want clear validation feedback and no unnecessary request
for invalid input, so I can correct the date before retrieving data.

**Why this priority**: Preventing invalid requests and explaining what to correct avoids
confusing failures and unnecessary service calls.

**Independent Test**: Enter an empty value, a malformed date, and an impossible calendar
date; verify that each is rejected with a clear message and that no request is sent.

**Acceptance Scenarios**:

1. **Given** the date field is empty, **When** validation is shown, **Then** the user
   sees a required-date message and cannot submit the form.
2. **Given** the date does not follow `YYYY-MM-DD`, **When** the user attempts to
   retrieve data, **Then** the user sees a format error and no request is made.
3. **Given** the date follows the required format but is not a real calendar date,
   **When** the user attempts to retrieve data, **Then** the user sees an invalid-date
   message and no request is made.
4. **Given** the user corrects an invalid date to a valid calendar date, **When** the
   user requests data, **Then** normal retrieval proceeds.

### Edge Cases

- The date is empty on initial load: keep the request button disabled and provide
  required-field feedback without submitting a request.
- The date contains a valid-looking but impossible day/month combination, including
  February 30 or 31, or a non-leap-year February 29: reject it before requesting data.
- The service successfully returns an empty asteroid list: treat it as no results and
  use the non-empty day most recently received if one exists, even if it is later than
  the selected date.
- The service returns an error, an unreadable response, or no response because of a
  network failure: show a clear message, do not crash, and offer cached data if
  available without displaying it until the user chooses to view it.
- A prior cache contains only empty days: report that no cached asteroid results are
  available rather than showing an empty day as asteroid data.
- A successful result is saved, then the application is closed and reopened: the
  result remains available.
- A displayed fallback day differs from the requested day: label the fallback date
  prominently so the user cannot mistake it for the requested date.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application MUST provide a graphical form for selecting a date in
  `YYYY-MM-DD` format and requesting near-Earth asteroid data for that date.
- **FR-002**: The request button MUST remain disabled while the date field is empty.
- **FR-003**: The application MUST validate that the date is present, follows
  `YYYY-MM-DD`, and represents a real calendar date before making a data request.
- **FR-004**: For an empty, malformed, or impossible date, the application MUST show a
  clear validation message and MUST NOT make a data request.
- **FR-005**: For a valid selected date, the application MUST display each returned
  asteroid's name, minimum and maximum diameter in meters, speed in kilometers per hour,
  distance from Earth in kilometers, and potential-hazard status.
- **FR-006**: Potential-hazard status MUST be visually distinguishable from the other
  asteroid details.
- **FR-007**: The application MUST identify NASA NeoWs API as the source and display
  the response timestamp alongside the results.
- **FR-008**: The application MUST locally retain successfully received external
  responses across application restarts.
- **FR-009**: When the requested date has no data or its response contains an empty
  asteroid list, the application MUST show the non-empty cached result most recently
  received, regardless of whether its date is earlier or later than the requested date,
  if one exists. The application MUST clearly explain the fallback and identify that
  date.
- **FR-010**: When the network is unavailable or the external service fails, the
  application MUST remain running and show an understandable error. When cached
  non-empty results are available, the application MUST offer them with their date and
  MUST display them only after the user chooses to view them.
- **FR-011**: If no cached non-empty results are available for a no-data or service
  failure case, the application MUST clearly state that cached results are unavailable.
- **FR-012**: The application MUST NOT describe stale fallback data as results for the
  currently requested date or imply that it is a fresh response.
- **FR-013**: API response interpretation, input validation, and cache selection MUST
  remain separate from the user interface and MUST be covered by unit tests.
- **FR-014**: The project README MUST document one command that runs the unit tests for
  this business logic.
- **FR-015**: The application MUST NOT include API keys or other secrets in committed
  source files.

### Key Entities

- **Selected date**: The user-requested calendar date for which asteroid data is
  requested.
- **Asteroid observation**: A near-Earth object record for a date, including its name,
  minimum and maximum diameter, speed, distance from Earth, and potential-hazard status.
- **Daily result**: The asteroid observations returned for a date, together with the
  source and response timestamp; it may contain no observations.
- **Cached daily result**: A successfully received daily result retained locally across
  restarts. A cached non-empty daily result is eligible for fallback display.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In all valid-date test cases with results, 100% of displayed asteroid
  records include the name, both diameter bounds with units, speed with units, distance
  with units, and potential-hazard status.
- **SC-002**: In all empty, malformed, or impossible-date test cases, the application
  makes zero data requests and displays a relevant validation message.
- **SC-003**: In all tested no-results cases with a cached non-empty day, the
  application displays the latest eligible cached day and identifies its date and
  fallback reason. In all tested service/network-failure cases, it offers that day and
  displays it only after the user chooses to view it.
- **SC-004**: In all tested no-results and service/network-failure cases without cached
  non-empty data, the application remains available and clearly explains that cached
  results are unavailable.
- **SC-005**: 100% of successfully received test responses remain locally available
  after restarting the application.
- **SC-006**: Users can distinguish a fallback result's date from the requested date
  in every fallback state.
- **SC-007**: Reviewers can run all unit tests for the specified business logic using
  one command documented in the project README.

## Assumptions

- The date format `YYYY-MM-DD` is Gregorian, and validity includes checking the actual
  number of days in each month and leap years.
- “Last successfully cached day” means the non-empty daily result most recently
  received, regardless of its calendar date relative to the selected date. Successfully
  received empty results may be retained, but do not replace the last non-empty day
  eligible for display.
- During an outage, cached data is offered but is displayed only after the user chooses
  to view it; it must not imply that a fresh response was received.
- The user is not required to sign in, and the feature does not require a database.
- The service response timestamp represents when the response was received and is
  displayed with a clear date/time value.
