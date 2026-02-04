# Testing in Sandbox | API Solutions

When you create an App in the Developer Console, it is automatically granted access to make API calls to the **API Sandbox Environment in Preview**. This enables you to test and understand the APIs before coding them into your application, without requiring any action from athenahealth.

To test API calls in this sandbox environment, you can use:

1. **Your own code or a local application** (e.g., curl, Bruno) pointing to the Preview environment URL for **Practice ID 195900**.
2. The **Sandbox functionality** in the portal to make calls directly from your browser.

**Prerequisites:**

- You need your app's credentials: **Client ID** (API Key) and **Secret**.
- *Note:* If lost, credentials can be reset from the Credentials tab in the App settings within the Developer Console.

## 5 Steps to Start Testing From Your Local Application

1. **Create your Developer Portal Account.**
2. **Get App Credentials:** If you haven't created an App, create a new one and save the credentials. If you have an existing app, retrieve its credentials from your secret manager.
3. **Set up Authentication:** Configure your client's authentication using the **OAuth 2.0** protocol.
4. **Review Documentation:**
   - Check workflow documentation to model/adapt application functionality.
   - Use Developer documentation to understand inputs, call types, and outputs.
5. **Make API Calls:** Target the **Preview environment URL** using **Practice ID 195900**.

## 5 Steps for Testing Directly From the Developer Portal - Sandbox

1. **Create your Developer Portal Account.**
2. **Get App Credentials:** Create a new app or retrieve existing credentials.
3. **Navigate to Sandbox:** Go to the Sandbox page to test APIs directly in the browser.
4. **Authorize:**
   - Click the **Authorize** button.
   - Enter your app's credentials and click **Authorize**.
   - *Note:* Re-authorization is required if the token expires.
5. **Execute Tests:**
   - Use the **Sandbox Filter** to find the API you want to test.
   - Enter valid input values and click **Execute**.
   - Review the provided API response and response code.

## Test Patients

The following test patients are available in **Preview Practice 195900**.

| **Patient ID** | **FHIR R4 Logical ID** | **First Name** | **Last Name** |
| -------------- | ---------------------- | -------------- | ------------- |
| 60178          | a-195900.E-60178       | Donna          | Sandboxtest   |
| 60179          | a-195900.E-60179       | Eleana         | Sandboxtest   |
| 60180          | a-195900.E-60180       | Frankie        | Sandboxtest   |
| 60181          | a-195900.E-60181       | Anna           | Sandbox-Test  |
| 60182          | a-195900.E-60182       | Rebecca        | Sandbox-Test  |
| 60183          | a-195900.E-60183       | Gary           | Sandboxtest   |
| 60184          | a-195900.E-60184       | Dorrie         | Sandboxtest   |

### Locating Patients via API

You can also locate these patients using the following calls:

**FHIR R4:**

HTTP

```
GET https://api.preview.platform.athenahealth.com/fhir/r4/Patient?ah-practice=Organization/a-1.Practice-195900&name=Sandboxtest
```

**athenaOne:**

HTTP

```
GET https://api.preview.platform.athenahealth.com/v1/195900/patients?lastname=Sandboxtest
GET https://api.preview.platform.athenahealth.com/v1/195900/patients?lastname=Sandbox-Test
```