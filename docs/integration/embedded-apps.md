# Embedded Apps | API Solutions - athenahealth

## Overview

Leveraging SSO, **Embedded Apps** provides access to content within the athenaOne workflow without having to open a separate browser tab. This makes it easier to make connections and leverage third-party information that is not natively accessible in the EHR.

Embedded Apps provide a great deal of flexibility; you can turn on applications for all users or just designated users, departments, and provider groups in your practice. Applications can be launched within the Clinicals workflow, available at the point of care, or within administrative workflows such as registration, scheduling, or billing.

### Requirements

Embedded Apps must meet the following requirements to be embedded into athenaOne:

- **Authentication:** Authenticate via single sign-on using either of the following standards, for which athenaOne acts as the identity provider (IdP):

  - OpenID Connect (OIDC), including support for SMART App Launch

  - SAML 2.0

    These authentication options enable athena to securely pass context via their associated token.

- **Content Security Policy (CSP):** The app must be configured to be able to open in an iFrame via a CSP.

  - **Example:** `Content-Security-Policy: default-src 'self' *.athenahealth.com`

- **Patient Safety:** To minimize potential patient safety issues, we recommend that patient-specific apps include a header that displays the patient's name and date of birth.

------

## Launch Locations

### Apps Tab

Visible from any chart or encounter, designed to launch directly to the associated patient (we pass the `patientID` & `encounterID`).

- **Recommended use:** Patient-specific applications to be launched from a patient chart or during patient encounters.

### Encounter Card

Inline and contextual. Visible within the encounter workflow (not visible in the chart), designed to launch directly to the associated patient or encounter (we pass the `patientID` & `encounterID`).

- **Recommended use:** Patient-specific applications to be launched during a patient encounter.

**Apps can be launched from the following encounter cards:**

| **Location** | **Stage** | **Encounter Card**         |
| ------------ | --------- | -------------------------- |
| Exam         | Review    | Reason for Visit           |
| Exam         | Review    | Care Plan                  |
| Exam         | Review    | Intake                     |
| Exam         | Review    | Screening                  |
| Exam         | Review    | Quality Management         |
| Exam         | Review    | Outstanding Orders         |
| Exam         | Review    | Documents for Discussion   |
| Exam         | Review    | Patient Status             |
| Exam         | HPI       | History of Present Illness |
| Exam         | ROS       | Review of Systems          |
| Exam         | PE        | Physical Exam              |
| Exam         | PE        | Procedure Documentation    |
| Exam         | A&P       | Assessment & Plan          |
| Exam         | A&P       | Patient-Supplied Results   |
| Exam         | A&P       | Patient Goals              |
| Exam         | A&P       | Patient Instructions       |
| Exam         | A&P       | Discussion Notes           |
| Exam         | A&P       | Follow Up                  |
| Exam         | Sign-Off  | Encounter Information      |
| Exam         | Sign-Off  | Letters                    |

### App Dock

The App Dock serves 2 purposes:

1. This is the location where all apps appear from and minimize to, regardless of where they are launched from.
2. The App Dock is also a launch location and is useful for apps that do not need patient context as well as apps that you might want to launch from a patient record outside of Clinicals (e.g., a patient's Quickview or an appointment).

- The App Dock is visible from anywhere in athenaOne; we will pass the `patientID` & `encounterID` if they are on a related page.
- **SMART apps** are launchable from this location, however, they will be disabled until a user navigates to a patient record.
- **Recommended use:** Admin Tools, Worklists, Patient Lists & Queues, Dashboards, Messaging & Telehealth. This launch point is ideal for universally accessible apps in athenaOne that don't require users to navigate to a specific patient.

------

## Sandboxing

Embedded apps will open in an iFrame with the following Sandbox attributes allowed:

| **Attribute Value**                       | **Description**                                              | **Allowed?** |
| ----------------------------------------- | ------------------------------------------------------------ | ------------ |
| `allow-forms`                             | Allows form submission                                       | **Yes**      |
| `allow-modals`                            | Allows to open modal windows                                 | **Yes**      |
| `allow-pointer-lock`                      | Allows to use the Pointer Lock API                           | **Yes**      |
| `allow-popups`                            | Allows popups                                                | **Yes**      |
| `allow-popups-to-escape-sandbox`          | Allows popups to open new windows without inheriting the sandboxing | **Yes**      |
| `allow-same-origin`                       | Allows the iframe content to be treated as being from the same origin | **Yes**      |
| `allow-scripts`                           | Allows to run scripts                                        | **Yes**      |
| `allow-downloads`                         | Allows downloading files through an `<a>` or `<area>` element with the download attribute, as well as through the navigation that leads to a download of a file. | **Yes**      |
| `allow="microphone *; camera *"`          | Allows camera and microphone use                             | **Yes**      |
| `allow-top-navigation`                    | Allows the iframe content to navigate its top-level browsing context | **No**       |
| `allow-top-navigation-by-user-activation` | Allows the iframe content to navigate its top-level browsing context, but only if initiated by user | **No**       |
| `allow-presentation`                      | Allows to start a presentation session                       | **No**       |
| `allow-orientation-lock`                  | Allows to lock the screen orientation                        | **No**       |

------

## Optional Settings

### App Icon

To display your app icon within the app launcher, please provide the URL of a favicon-sized image. The system will resize it to 24 x 24 px.

- **Supported secure URL (https) image formats:** `.ico`, `.png`, `.gif`, `.jpg`, `.jpeg`.
- Apps without an app logo will display the first initial of the app name by default.

### Launch Without Patient Context (yes/no)

Most SMART on FHIR apps require or depend on Patient Context. If your app is not patient-specific, this requirement can be disabled. This feature allows providing two different experiences within the same app:

- When launched from within a patient record, the app shows patient-specific data.
- When launched without patient context, the app can show an alternative view such as a worklist, dashboard, or other general information.

### Relaunch on Context Change (yes/no)

By default, all apps will relaunch when the context changes, unless explicitly specified otherwise.

------

## Embedded App Messaging Framework

### Overview

The Embedded App Messaging Framework enables secure communication between embedded applications and athenaOne using the `postMessage` API (as defined by the MDN Web API). This framework supports a predefined set of API methods to control embedded apps, such as minimizing, resizing, or popping out apps. It includes built-in security, throttling, and logging mechanisms to ensure reliable and secure operation.

### Key Features

- **Secure Messaging:** Validates message origins using a configurable allowlist to prevent unauthorized access.
- **API Method Support:** Provides predefined methods (e.g., `appMinimize`, `appResize`, `appPopout`) with versioned implementations.
- **Request Throttling:** Limits API requests to one per method per app every 10 seconds to prevent abuse.
- **Batched Logging:** Logs up to 5 messages, flushing every 1 second for efficient debugging and monitoring.
- **Popout Window Support:** Relays messages from popout windows to the host application, maintaining app context.
- **Cross-Origin Safety:** Uses `targetOrigin` to ensure messages are sent only to trusted destinations.

### Usage

athenaOne can send outbound messages to an embedded application and manage incoming messages from it. To use this functionality, all embedded apps must provide athenahealth with the app origin (URL) for sending and receiving messages.

### Supported Outbound Messages (from athenaOne to embedded app)

**Method:** `patientContextChanged`

- **Description:** New `patientID`, broadcast after an encounter (or any patient specific page) is finished fully loading.
- **Parameters:** None

After app launch has occurred, we will make the following `postMessage` Call with each context (patient change) for your app to catch and handle accordingly.

**Message Example:**

JavaScript

```
appIFrameWindow.postMessage(
  {
    event: "patientContextChanged",
    updatedPatient: "<new-patient-id>" // can be patientId string or undefined
  },
  "https://preview.athenahealth.com" // Set appropriately for the environment you are in
);
```

### Supported Inbound Messages (from embedded app to athenaOne)

| **Method**                | **Description**                                              | **Parameters**                                               |
| ------------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| `notifyPatientDataChange` | Triggers a reload of the main frame (e.g., to refresh patient data). | None                                                         |
| `appMinimize`             | Minimizes an app by updating its state.                      | None                                                         |
| `appReopen`               | Reopens a minimized app.                                     | None                                                         |
| `appEnterFullscreen`      | Sets an app to fullscreen mode.                              | None                                                         |
| `appExitFullscreen`       | Exits fullscreen mode for an app.                            | None                                                         |
| `appClose`                | Closes an app.                                               | None                                                         |
| `appResize`               | Resizes an app to a specified width.                         | `newWidth`: (valid values: 100, 200, 400, 600, 800, 1000 default) |
| `appPopout`               | Opens an app in a popout window.                             | None                                                         |
| `appPopin`                | Moves a popped-out app back to the main window and focuses the popout. | None                                                         |

### Message Examples

**Trigger a reload of athenaOne:**

JavaScript

```
window.parent.postMessage({
    type: 'embeddedAppAPIMessage',
    method: 'notifyPatientDataChange',
    methodVersion: '1.0.0'
}, 'https://preview.athenahealth.com/'); // Set appropriately for the environment you are in
```

**Resize the embedded app iFrame:**

JavaScript

```
window.parent.postMessage({
    type: 'embeddedAppAPIMessage',
    method: 'appResize',
    methodVersion: '1.0.0',
    newWidth: '600'
}, 'https://preview.athenahealth.com/'); // Set appropriately for the environment you are in
```

------

## Next Steps

All embedded apps must authenticate via one of the following single sign-on options:

- OIDC/SMART Launch
- Outbound athenaOne SSO (SAML)