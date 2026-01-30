### 1. **Write Access (POST) in Certified APIs**

- **Source:** *athenahealth Developer Portal: Certified API Access*
- **Validation:** The documentation confirms that "Certified APIs" are those required for ONC Health IT Certification (USCDI standards). USCDI v1+ requires the ability to receive (write) Clinical Notes.
- **Specific Endpoint:** The **DocumentReference** profile page explicitly lists the "Create Operation" (`POST`) as a supported interaction for uploading documents.
- **URL:** [`https://docs.athenahealth.com/api/resources/certified-api-access`](https://www.google.com/search?q=[https://docs.athenahealth.com/api/resources/certified-api-access](https://docs.athenahealth.com/api/resources/certified-api-access)) and [`https://docs.athenahealth.com/api/fhir-r4/document-reference`](https://www.google.com/search?q=[https://docs.athenahealth.com/api/fhir-r4/document-reference](https://docs.athenahealth.com/api/fhir-r4/document-reference))

### 2. **Base64 Encoding Requirement (The "Trick")**

- **Source:** *athenahealth Developer Portal: Best Practices*
- **Validation:** Under the "Call Volume & Filtering" section, the docs explicitly state: *"When uploading documents, we suggest you follow our best practices: Base 64 encoded... attachmentcontents should be no larger than 20MB."*
- **URL:** [`https://docs.athenahealth.com/api/guides/best-practices`](https://www.google.com/search?q=[https://docs.athenahealth.com/api/guides/best-practices](https://docs.athenahealth.com/api/guides/best-practices))

### 3. **Rate Limits (15 QPS)**

- **Source:** *athenahealth Developer Portal: Best Practices (API Call Limitations)*
- **Validation:** The policy table explicitly lists the limits for **Preview Environments** (where your pilot will live) as **15 calls/second** (QPS) and 50,000 calls/day. This confirms the constraints for your polling engine.
- **URL:** [`https://docs.athenahealth.com/api/guides/best-practices`](https://www.google.com/search?q=[https://docs.athenahealth.com/api/guides/best-practices](https://docs.athenahealth.com/api/guides/best-practices))

### 4. **Private/Preview Environment Access**

- **Source:** *athenahealth Developer Portal: Onboarding Overview*
- **Validation:** The guide states that access to a specific customer's Preview environment *"requires the signed authorization and consent of the healthcare organization."* This validates your strategy of needing the doctor to grant you access, rather than athenahealth corporate.
- **URL:** [`https://docs.athenahealth.com/api/guides/onboarding-overview`](https://www.google.com/search?q=[https://docs.athenahealth.com/api/guides/onboarding-overview](https://docs.athenahealth.com/api/guides/onboarding-overview))