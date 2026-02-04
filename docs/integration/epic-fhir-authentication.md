### Architecture Overview

Unlike your user-facing dashboard which uses the **Authorization Code Flow** (redirecting a human to log in), your backend must use the **Client Credentials Flow** with a **Private Key JWT**. This is a machine-to-machine exchange where your application authenticates itself using a signed assertion rather than a shared secret.

Official Standard: [HL7 SMART App Launch - Backend Services](https://hl7.org/fhir/smart-app-launch/backend-services.html)

------

### Phase 1: Security & Registration

Before your application can "talk" to Epic, you must establish trust via asymmetric cryptography.

1. **Generate an RSA Key Pair:**
   - Create a public/private key pair (RSA 2048-bit or higher).
   - **Action:** Export the public key as an X.509 certificate (`.cer` or `.pem`).
   - **Action:** Store the private key securely in your backend environment (e.g., Azure Key Vault).
2. **Register Application in Epic Sandbox:**
   - Log in to [fhir.epic.com](https://fhir.epic.com).
   - Create a new application and select "Backend System" as the application type.
   - **Action:** Upload your **Public Key** to the application registration. This allows Epic to verify the signatures your backend sends.
   - **Note:** Record your `ClientID` (Non-Production) and the `Token Endpoint` URL.

### Phase 2: Constructing the Client Assertion (JWT)

Instead of sending a password over the wire, your .NET app must construct and sign a JSON Web Token (JWT) for every token request. This JWT is known as the `client_assertion`.

1. **Define JWT Header:**
   - Must specify the signing algorithm. Epic generally recommends `RS384` (RSA Signature with SHA-384), though `RS256` is often accepted in Sandbox.
   - Reference: [RFC 7515 - JSON Web Signature (JWS)](https://datatracker.ietf.org/doc/html/rfc7515)
2. **Define JWT Payload (Claims):**
   - `iss` (Issuer): Must match your **Client ID** from the Epic portal.
   - `sub` (Subject): Must also match your **Client ID**.
   - `aud` (Audience): Must **exactly** match the Epic OAuth2 Token Endpoint URL (no trailing slashes unless specified).
   - `jti` (JWT ID): A unique GUID for this specific request to prevent replay attacks.
   - `exp` (Expiration): Must be less than 5 minutes from the time of generation. Epic is strict about this to minimize security windows.
3. **Sign the JWT:**
   - Use your stored **Private Key** to sign the payload.
   - **Result:** A Compact Serialization string (e.g., `eyJ...`).

### Phase 3: The Token Exchange

Your backend performs a `POST` request to the Epic Token Endpoint to exchange your self-signed JWT for a valid Epic Access Token.

1. **Endpoint:** `https://fhir.epic.com/interconnect-fhir-oauth/oauth2/token`
2. **Body Content (Form-Url-Encoded):**
   - `grant_type`: Must be `client_credentials`.
   - `client_assertion_type`: Must be `urn:ietf:params:oauth:client-assertion-type:jwt-bearer`.
   - `client_assertion`: The signed JWT string from Phase 2.
3. **Processing the Response:**
   - If successful, you receive a JSON response containing the `access_token` and `expires_in`.
   - **Implementation Note:** Cache this token until it is near expiration (typically 59 minutes) to avoid rate-limiting issues or unnecessary crypto overhead.

### Phase 4: FHIR Resource Access

Once you have the Bearer token, you interact with the FHIR R4 endpoint.

1. **Configure Headers:**
   - `Authorization`: `Bearer [Your_Access_Token]`
   - `Accept`: `application/fhir+json`
2. **Target the Correct Base URL:**
   - Sandbox R4 URL: `https://fhir.epic.com/interconnect-fhir-oauth/api/FHIR/R4/`
3. **Execute Requests:**
   - Unlike Azure's generic server, you must query valid **US Core** profiles.
   - **Critical Constraint:** You must use specific "Test Patients" provided in the Epic documentation. Searching for random names will return empty results or errors.
   - Reference: [Epic FHIR Resources Documentation](https://fhir.epic.com/Specifications) (Navigate to specific resources like "Patient" or "Observation" to see required query parameters).

### Documentation References

- **Authentication Guide:** On [fhir.epic.com](https://fhir.epic.com), refer to the "Documentation" tab, specifically looking for the "Backend Services" or "SMART on FHIR" tutorials.
- **FHIR R4 Specification:** [HL7 FHIR Release 4](http://hl7.org/fhir/R4/) (For understanding the structure of the JSON objects you will receive).
- **US Core Implementation Guide:** [HL7 US Core IG](https://www.hl7.org/fhir/us/core/) (Epic's API implementation is heavily based on these constraints).