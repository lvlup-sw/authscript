# Documentation - Epic on FHIR: CDS Hooks

## Introduction

Clinical Decision Support (CDS) Hooks is an HL7 standard for in-workflow decision support integrations between electronic health record (EHR) systems and remote, real-time, provider-facing decision support services.

Epic's support for CDS Hooks is built on a workflow engine and CDS rule engine. Organizations using Epic can configure native CDS alerts—known as **OurPractice Advisory (OPA)**—by defining the workflow point (hook) and the criteria to evaluate. These criteria determine whether a CDS alert appears to the clinician.

**Guidance to CDS Developers:**

- **User Experience:** CDS Hooks interact directly with clinician workflows; developers share responsibility for a positive user experience.
- **Performance:** Services must be fast, avoid alert fatigue, and improve over time.

------

## Supported Hooks

As of May 2021, Epic primarily supports three standard hooks built on pre-existing CDS alert workflow triggers.

### 1. patient-view

The `patient-view` hook is triggered in two primary scenarios:

- **Open Patient Chart:** When a patient's chart is opened. Epic recommends against firing a request every time a chart is opened to prevent poor user experience; it is best used with specific native business criteria.
- **Native Trigger Mapping:** When a native Epic trigger (e.g., "Enter Allergy") calls a CDS Hooks service that is not standardized by CDS Hooks. Since there is no industry standard hook for workflows like "Enter Allergy," Epic reuses the `patient-view` hook. The extension `com.epic.cdshooks.request.bpa-trigger-action` differentiates these triggers.

### 2. order-select

This hook triggers when a clinician enters orders for a patient.

- **Draft Orders:** The `draftOrders` JSON object contains a bundle of both newly selected orders and previously selected, unsigned orders.
- **Selections:** The `selections` array identifies which unsigned orders are *newly* selected. Decision support should focus on these.
- **Resources:** Can contain `MedicationRequest`, `ServiceRequest`, or `ProcedureRequest`.

### 3. order-sign

This hook is triggered as the final step in the ordering process, after the clinician clicks "Sign Orders" but before the system finalizes them.

- **Final Check:** This is the final chance for a clinician to revise an order.
- **Details:** It allows for the collection of all unsigned order details.

------

## CDS Hooks Request Structure

### Prefetch

Prefetch optimizes performance by collecting data needed for the CDS service using the CDS Hooks prefetch model. This is configured in the OPA (LGL) record in Epic.

**Common Prefetch Tokens:**

- `{{context.patientId}}`: Returns the patient ID.
- `{{context.encounterId}}`: Returns the encounter ID.
- `{{context.userId}}`: Returns "PractitionerRole/" if the user has a provider record, or "Practitioner/" if they do not.
- `{{userPractitionerId}}` and `{{userPractitionerRoleId}}`: Return specific resource IDs.

### Input Fields

Epic sends the following fields in the JSON POST body:

| **Field**             | **Description**                                              |
| --------------------- | ------------------------------------------------------------ |
| **hook**              | `order-select`, `order-sign`, or `patient-view`.             |
| **hookInstance**      | A universally unique identifier (UUID) for the call.         |
| **fhirServer**        | The base FHIR URL for the health system.                     |
| **fhirAuthorization** | A structure holding an OAuth 2.0 bearer access token for Epic APIs. |
| **context**           | Contains `patientId` and `userId`. Encounter context sends `encounterId`. Order hooks send `draftOrders`. |

### Epic Extensions

Epic provides specific extensions to identify trigger actions and versions.

| **Extension**                                  | **Description**                                              |
| ---------------------------------------------- | ------------------------------------------------------------ |
| **...request.bpa-trigger-action**              | Maps the hook to a specific Epic action (e.g., 60 for Open Chart, 23 for Sign Orders). |
| **...request.cds-hooks-specification-version** | The specification version used by Epic.                      |
| **...request.fhir-version**                    | Primary FHIR version of the CDS service.                     |
| **...request.criteria-id**                     | The ID of the OPA criteria record in Epic.                   |
| **...request.epic-version**                    | The version of Epic the health system is using.              |

------

## CDS Hooks Response

A CDS Service must respond synchronously. The response determines what content is displayed to the user.

### Response Types

- **Suggestions:** Preferred over app launches. Used if recommendations can be determined with provided information.
- **App Launch:** Enables additional user interaction but is more disruptive.
  - *Auto-launch:* As of August 2023, if a response contains only an app URL (no other cards/native CDS), Epic may auto-launch the app.

### Actions and Overrides

- **Order Overrides:** If creating an unsigned order (e.g., `MedicationRequest`), order overrides are **NOT** set by the service response. Details come from default values in Epic.
- **Preference Lists:** To specify a preference list item, use the system `urn:com.epic.cdshooks.action.code.system.preference-list-item`.

### Response Fields

| **Field**         | **Description**                                              |
| ----------------- | ------------------------------------------------------------ |
| **cards**         | Array of cards containing information, text (Markdown/HTML), suggestions, or links. |
| **systemActions** | Beginning Feb 2024, supports system actions to annotate orders via `ServiceRequest.Update`. |

### Card Attributes

| **Attribute**         | **Description**                                              |
| --------------------- | ------------------------------------------------------------ |
| **indicator**         | `info`, `warning`, `critical` (mapped to Epic-specific values). |
| **uuid**              | Optional, but required if you intend to receive feedback.    |
| **selectionBehavior** | Only `any` is supported.                                     |
| **detail**            | Can be plain text, GitHub flavored markdown, or HTML (via extension). |
| **source.topic.code** | Static alphanumeric identifier for the topic. Required for respecting user overrides on subsequent requests. |

### Codesets for Create APIs

When specifying a coding for a `Create` action, use the following systems:

| **System**                                                   | **Purpose**                         |
| ------------------------------------------------------------ | ----------------------------------- |
| `urn:com.epic.cdshooks.action.code.system.preference-list-item` | Order specific preference list item |
| `urn:com.epic.cdshooks.action.code.system.orderset-item`     | Order SmartSet/OrderSet/Pathway     |
| `urn:com.epic.cdshooks.action.code.system.cms-hcc`           | Suggest Visit Diagnosis (CMS-HCC)   |
| `urn:oid:2.16.840.1.113883.6.90`                             | Suggest ICD-10 codes                |
| `urn:oid:2.16.840.1.113883.6.69`                             | Suggest NDC codes                   |

------

## Security and Authentication

### JWT Tokens

CDS Hooks use digitally signed JSON Web Tokens (JWTs) for authentication. The request header includes a "Bearer" token.

**Important:** Do not confuse the JWT in the "Authorization: Bearer" header (used to authenticate the client) with the access token JWT inside the CDS Hooks request body (used to access Epic APIs).

**Header Fields:**

- `alg`: Default is RSA SHA-384.
- `jku`: URL to the JWK Set containing public keys.

**Payload Fields:**

- `aud`: Your CDS service's endpoint.
- `iss`: The FHIR endpoint of the organization.
- `sub`: CDS hooks client ID.

**Validation:** You must decode the JWT, parse the `jku` claim, and verify the `jku` exists on your trusted allowlist.

------

## Feedback

Services can be configured to receive feedback immediately or in batches.

- **Endpoint:** Defaults to your service endpoint with `/feedback` appended.
- **Requirement:** A UUID must be sent in the initial response card/suggestion to trigger feedback.
- **Override Reasons:** Can be configured in Epic or sent dynamically. If dynamic, the health system must map the values.

------

## Implementation & Best Practices

### Implementation Steps

1. **Client ID:** Register the app to receive production and non-production client IDs.
2. **Configuration:** Provide the CDS service endpoint, prefetch config, and JWT claim expectations (`aud`, `iss`, `sub`).
3. **Workflow:** The organization's analysts configure the native workflows to trigger the service.
4. **Endpoint URI Validation:** The URL used for the CDS service must *exactly* match the Endpoint URI defined on the application/client ID. If it does not match, the hook will trigger but information will be stripped.

### Important Considerations

- **Clinician-Facing Only:** CDS Hooks should only be used for real-time, clinician-facing support. If no content is displayed, do not use CDS Hooks.
- **FHIR Versions:** The default FHIR version specified during registration determines the version of resources in the context and prefetch.
- **SMART App Allowlist:** SMART app link URLs in cards must be pre-registered with the CDS client or they will not appear.
- **Event Notifications:** For event-based notifications, use an event-based interface.
- **Context Sync:** For context synchronization, use FHIRcast.

### Example CDS Hooks

```txt
Condition.Create (Encounter Diagnosis)

Condition.Create (Problems)

MedicationRequest.Create (Unsigned Order)

MedicationRequest.Delete (Unsigned Order)

MedicationRequest.Read (Unsigned Order)

ServiceRequest.Create (Unsigned Order)

ServiceRequest.Delete (Unsigned Order)

ServiceRequest.Read (Unsigned Order)

ServiceRequest.Update (Unsigned Order)
```

These are all available in the sandbox.
