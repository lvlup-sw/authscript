### Epic EHR Access Requirements

1. We need a [FHIR sandbox account](https://fhir.epic.com/Developer/Index) for our "organization" (this is what I originally created)
2. Download the test harness from [this page](https://open.epic.com/Hyperdrive) using that account.
3. Follow attached instructions. This includes sending a request for a user account, per user needing access.

### HyperDrive Test Harness

```markdown
# Download Hyperdrive

**Source:** [https://open.epic.com/Hyperdrive/HyperdriveDownload](https://open.epic.com/Hyperdrive/HyperdriveDownload)

## Hyperdrive Client Testing Harness

The Hyperdrive Client Testing Harness contains the installer, configuration file, and an overview PDF outlining use cases and test views.

### Supported Integrations

Supported integrations currently include:

* E-Signature
* Login
* Voice Recognition
* Scan Acquisition
* Scan Viewing
* Scan Signature Deficiencies
* FHIRcast
* XML
* Encoder
* Web PACS
* SMART on FHIR

---

## Instructions for Access

To gain access to the testing harness, follow these steps:

### 1. Download the client package

First, download the client package, which contains the client, configuration files, and an overview of supported capabilities.

* **Note:** You may review the Terms of Use for this client at any time.
* Once the download is complete, navigate to your computer's file download location and unzip the file.
* Within that file, you will find an "INSTALL INSTRUCTIONS" text file containing setup instructions.

### 2. Create an Epic on FHIR account

We will provision your Hyperdrive login and password via your Epic on FHIR account.

* If you do not already have an account, you will need to create one using the **Epic on FHIR** button, **NOT** the UserWeb button.
* If your organization is already enrolled in Epic on FHIR, you can contact your Epic on FHIR admin to have an account created.
* If you are unsure who your current admin is, contact us with information about your organization and we can help you identify your organization's admin.

### 3. Request a Hyperdrive user account and client identifier

To log into Hyperdrive, first submit a user account request.

* **Individual Accounts:** Accounts are provisioned on an individual basis and require multi-factor authentication (MFA) for login. Request an account for each individual at your organization who will perform testing.
* **Client ID:**
* To connect your integration to our server, if you are only using FHIR APIs or do not need specific APIs/scopes, create a client ID in Epic on FHIR.
* If you plan to use Epic Public APIs or subspace scopes, submit a client ID request and we will create one on your behalf.


* **Additional Build:** Depending on your integration type, you may need additional build to test.
* Existing integrations transitioning to Hyperdrive have been assigned an Epic contact to help with record build and configuration in the test harness.
* Work with your Epic contact to understand what build needs to be performed on Epic's end before testing can begin.



### 4. Epic reviews your account request

We review Hyperdrive account requests each week. Once approved, we will create a Hyperdrive user account for your organization and send you a temporary 48-hour MFA passcode for initial MFA enrollment.

**Viewing Credentials:**
To view your user credentials, log into the 'Organization Details' page of Epic on FHIR, then scroll down to the 'Epic User Account Details' section.

### 5. Launch the testing harness

Launch Hyperdrive and enter your user credentials and MFA passcode when prompted.

* **Important:** Change your password and complete MFA enrollment when prompted. If you do not complete MFA enrollment, your access will expire when your temporary passcode expires.
* **Account Distinction:** Remember, there are two accounts involved:
1. **Epic on FHIR account:** Allows access to the Epic on FHIR site.
2. **Hyperdrive test account:** Used to log into the Hyperdrive client.


* You can find your Hyperdrive testing credentials by logging into Epic on FHIR and navigating to the 'Epic User Account Details' section (as described in Step 4).

---

## Login Reference Guide

The following details illustrate how to locate your credentials and log in.

### A. Locate Credentials (Epic on FHIR Site)

Navigate to **Epic User Account Details** on the Epic on FHIR site.

* **(1) Epic user ID**: Your username.
* **(2) Epic user password**: Your initial password.

### B. Locate Temporary Passcode (Email/Notification)

You will receive a notification stating:

> "Hyperdrive credentials have been provisioned for you to test your integration. For information accessing the Hyperdrive Client Test Harness, see the Hyperdrive page on open.epic. To log in, you will need your username, password, and temporary passcode."

* **(3) Hyperdrive Temporary Passcode**: `<PASSCODE>`
* *Note:* This passcode will expire in 48 hours. If you have not logged into Hyperdrive within 48 hours, the passcode will expire, and you will need to reach out to receive a new one.



### C. Log In (Hyperdrive Client)

1. **Login Screen**: Enter the credentials found in Step A.
* **User ID**: Enter the ID from **(1)**.
* **Password**: Enter the password from **(2)**.


2. **MFA Screen**: Enter your one-time passcode.
* **Passcode**: Enter the code from **(3)**.



---

*Copyright © 2026 Epic Systems Corporation. HL7, CDA, CCD, FHIR, and the FHIR [FLAME DESIGN] are the registered trademarks of Health Level Seven International. ONC CERTIFIED HIT® is a registered trademark of HHS.*

*Would you like me to extract the specific list of supported integrations or summarize the MFA enrollment requirements?*
```

