**Context:** This is a healthcare "Prior Authorization Hub" dashboard used to track medical cases. The design is clean, enterprise-focused, flat, and uses distinct status colors. Do not include the picture-in-picture speaker from the bottom right—focus only on the UI.

**Layout & Hierarchy:**

**1. Top Navigation Bar (Dark Theme)**

- **Background:** Dark slate/gray (`bg-slate-900`).
- **Top Half:** >     * Left: App Logo (Blue square with "PA"), App Name "Prior Auth Hub", and subtitle "Meridian Health System" in muted text.
  - Right: A search bar ("Search cases, patients, providers..."), a green dot with "System Operational", a "Notifications" button, and a user avatar square with "DR".
- **Bottom Half (Sub-nav):**
  - Left: Navigation links: "Cases" (active/white text), "Analytics", "Providers", "Payer Directory", "Debugger", "Settings" (muted gray text).
  - Right: "Export Report" button (outline/dark) and "+ New Case" primary button (solid blue).

**2. Main Content Area (Light Theme)**

- **Background:** Very light gray (`bg-gray-50`), with all inner panels being crisp white (`bg-white`) with subtle borders and shadows.

**3. Page Header Section**

- **Title:** "Prior Authorization Cases" (Large, bold).
- **Subtitle:** "Manage and track prior authorization requests across all payers and facilities" (Muted gray text).
- **Actions (Right-aligned):** A green "Live" status dot, "Download CSV" button, "Audit Log" button, and a solid blue button saying "Reviewing 36/48...".
- **Filter Bar:** A single row below the title containing text-based filters: "QUEUE: All Facilities", "DATE RANGE: Last 30 days", "ASSIGNED TO: All Coordinators", "AVG TURNAROUND: 2.4 days". Make the labels gray and the values black.

**4. KPI Summary Cards**

- A horizontal grid of 6 bordered white cards, each with a large number and a label. Use specific color accents for the numbers and top borders:
  - Card 1: "50" Total Cases (Teal/Blue)
  - Card 2: "48" New Cases (Purple) - Give this card a thicker purple border to show it is selected.
  - Card 3: "0" Awaiting Payer (Blue)
  - Card 4: "2" Needs Info (Orange)
  - Card 5: "0" Approved (Green)
  - Card 6: "0" Denied (Red)

**5. Table Sub-Navigation (Pills)**

- A row of pill-shaped tabs above the table: "All Cases" (Solid blue background, white text), followed by outlined pills: "New (48)", "Awaiting Payer (0)", "Payer Needs Info (0)", "Agent Needs Info (2)", "Approved (0)", "Denied by Payer (0)", "Denied by Agent (0)".

**6. Data Table**

- **Headers:** PATIENT, STATUS, PAYER, CPT CODE, ICD-10, CREATED, URGENCY. (Small, uppercase, gray text).
- **Row Styling:** White background, but alternate some rows with a very light blue background (e.g., `bg-blue-50`) to indicate they are actively being processed or selected.
- **Sample Data Rows:** Include 4-5 rows of mock data.
  - *Patient:* "John Doe", with "case-001" below it in smaller gray text.
  - *Status:* Use small dots and text. Some should say "Updating..." with a blue dot, others "NEW" with a gray dot.
  - *Payer:* e.g., "NHP", with "payer-001" below it.
  - *Codes:* Provide standard medical codes (e.g., CPT 72148, ICD-10 M54.5).
  - *Urgency:* Small text pills. Mostly gray "ROUTINE", but add one light blue pill for "URGENT".