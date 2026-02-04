### **Legend**

- **🟡 System Actions:** Automated backend processes.
- **🟢 AI Agent Actions:** Intelligent processing, checking, and drafting.
- **🔵 User Actions:** Manual reviews, edits, and decisions by the clinician or support staff.
- **🟣 Decision/Monitor:** End states or monitoring phases.
- **⚪ Context/Notes:** Additional implementation details.

------

### **Phase 1: Intake & Assessment (Automated)**

1. **🟡 System:** Watch for ADT (Admission, Discharge, Transfer) feed for new discharge notifications.
   - *Note: Not visible in UI.*
2. **🟡 System:** Generate work item and pull in available clinical data from the EHR.
   - *Note: Not visible in UI.*
3. **🟢 AI Agent:** Checks to see if the provider has decided to pursue any new treatments for the patient.
   - *Note: Not visible in UI.*
   - **IF NO:**
     - **🟡 System:** Mark as requiring no further action on the back end. *(Terminal State)*
   - **IF YES:** Proceed to next step.
4. **🟢 AI Agent:** Checks if the patient's Insurance requires prior authorization for the treatment.
   - *Note: Not visible in UI.*
   - **IF NO:**
     - **🟡 System:** Mark as requiring no further action on the back end. *(Terminal State)*
   - **IF YES:** Proceed to next step.

### **Phase 2: Drafting & Form Generation (Automated)**

1. **🟡 System:** Generate work item for prior authorization request.
   - *Note: Not visible in UI.*
2. **🟢 AI Agent:** Locate the required prior authorization form the patient's Insurance requires for the specified procedure.
   - *Note: Not visible in UI.*
3. **🟢 AI Agent:** Review all available information within the patient's record in the EHR to draft responses to all of the requested fields in the form.
   - *Note: Not visible in UI.*
   - *Future Enhancement:* Review previously approved prior authorization requests to better draft the request in a way that is likely to get approved (still based on true data from the patient's record).
4. **🟢 AI Agent:** Check the prior auth form for completeness.
   - *Decision: Was all the required details available in the EHR?*
   - *Note: Not visible in UI.*

------

### **Phase 3: Review Loops (User Interaction)**

#### **Path A: If Data Was Complete (YES)**

1. **🟡 System:** Create **Ready for Review** work item for completed prior auth form for user to review and approve in UI.
   - *Note: Agent should include an explanation that can be viewed for where the evidence was located in the form.*
   - *Note: Visible in UI.*
2. **🔵 User:** Review form for accuracy.
   - *Decision: Is the form all set for submission?*
   - *Note: Visible in UI.*
   - **IF NO:**
     - **🔵 User:** Edit the form manually so that it can be submitted. *(Proceeds to Approval)*
   - **IF YES:**
     - **🔵 User:** Approve the form for submission to insurance company. *(Proceeds to Phase 4)*

#### **Path B: If Data Was Incomplete (NO)**

1. **🟡 System:** Create **Missing Data** work item for incomplete prior auth form for user to review and approve in UI.
   - *Future Enhancement:* These could also be items that have a complete form but deemed unlikely to be approved unless strengthened.
   - *Note: Visible in UI.*
2. **🔵 User:** Review form to see if the missing data can be provided by the provider.
   - **IF NO (Cannot provide data):**
     - **🔵 User:** Mark the item as "payers requirements not met."
     - **🟡 System:** Send message back to provider that the payers requirements for prior auth have not been met. *(Terminal State)*
   - **IF YES (Can provide data):**
     - **🔵 User:** Select "update with new data."
     - **🟡 System:** System checks for new data.
     - **🟢 AI Agent:** Updates the form with new data.
     - *Loop:* The workflow loops back to **Step 8 (Check for completeness)**.

------

### **Phase 4: Submission (Post-Approval)**

1. **🔵 User:** Approve the form for submission to insurance company.
2. **🟡 System:** Locate the correct submission method for the insurance and submit the prior auth request.
3. **🟡 System:** Move the work item to the dashboard as a "waiting for payer response" list item.
   - *Note: This may need to be the terminal state for MVP.*
4. **🟣 Monitor:** Monitor for responses and alert the care team accordingly when it arrives.