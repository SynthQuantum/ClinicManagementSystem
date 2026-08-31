import { useEffect, useState } from "react";

const initialForm = {
  firstName: "",
  lastName: "",
  dateOfBirth: "",
  gender: 0,
  phoneNumber: "",
  email: "",
  address: "",
  bloodType: "",
  insuranceProvider: "",
  insurancePolicyNumber: "",
  insuranceExpiryDate: "",
  emergencyContactName: "",
  emergencyContactPhone: "",
  emergencyContactRelationship: "",
  notes: ""
};

const genders = ["Male", "Female", "Other", "PreferNotToSay"];
const phoneRegex = /^\+?[0-9\-\s()]{7,20}$/;

function validatePatient(form) {
  const errors = {};

  if (!form.firstName.trim()) {
    errors.firstName = "First name is required.";
  } else if (form.firstName.length > 100) {
    errors.firstName = "First name must be 100 characters or fewer.";
  }

  if (!form.lastName.trim()) {
    errors.lastName = "Last name is required.";
  } else if (form.lastName.length > 100) {
    errors.lastName = "Last name must be 100 characters or fewer.";
  }

  if (!form.dateOfBirth) {
    errors.dateOfBirth = "Date of birth is required.";
  } else if (new Date(form.dateOfBirth) > new Date()) {
    errors.dateOfBirth = "Date of birth cannot be in the future.";
  }

  if (form.phoneNumber && (!phoneRegex.test(form.phoneNumber) || form.phoneNumber.length > 20)) {
    errors.phoneNumber = "Phone number format is invalid.";
  }

  if (form.email) {
    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (form.email.length > 256 || !emailPattern.test(form.email)) {
      errors.email = "Email format is invalid.";
    }
  }

  if (form.address.length > 500) {
    errors.address = "Address must be 500 characters or fewer.";
  }

  if (form.bloodType.length > 10) {
    errors.bloodType = "Blood type must be 10 characters or fewer.";
  }

  if (form.insuranceProvider.length > 200) {
    errors.insuranceProvider = "Insurance provider must be 200 characters or fewer.";
  }

  if (form.insurancePolicyNumber.length > 100) {
    errors.insurancePolicyNumber = "Insurance policy number must be 100 characters or fewer.";
  }

  if (form.insuranceExpiryDate) {
    const oneYearAgo = new Date();
    oneYearAgo.setFullYear(oneYearAgo.getFullYear() - 1);
    if (new Date(form.insuranceExpiryDate) < oneYearAgo) {
      errors.insuranceExpiryDate = "Insurance expiry date appears invalid.";
    }
  }

  if (form.insurancePolicyNumber.trim() && !form.insuranceProvider.trim()) {
    errors.insuranceProvider = "Insurance provider is required when a policy number is provided.";
  }

  if (form.emergencyContactName.length > 200) {
    errors.emergencyContactName = "Emergency contact name must be 200 characters or fewer.";
  }

  if (form.emergencyContactPhone && (!phoneRegex.test(form.emergencyContactPhone) || form.emergencyContactPhone.length > 20)) {
    errors.emergencyContactPhone = "Emergency contact phone format is invalid.";
  }

  if (form.emergencyContactRelationship.length > 100) {
    errors.emergencyContactRelationship = "Emergency contact relationship must be 100 characters or fewer.";
  }

  if (form.notes.length > 2000) {
    errors.notes = "Notes must be 2000 characters or fewer.";
  }

  return errors;
}

export default function PatientsForm({ selected, onSubmit, onCancel, busy }) {
  const [form, setForm] = useState(initialForm);
  const [errors, setErrors] = useState({});

  useEffect(() => {
    if (!selected) {
      setForm(initialForm);
      setErrors({});
      return;
    }

    setForm({
      firstName: selected.firstName || "",
      lastName: selected.lastName || "",
      dateOfBirth: (selected.dateOfBirth || "").slice(0, 10),
      gender: Number(selected.gender ?? 0),
      phoneNumber: selected.phoneNumber || "",
      email: selected.email || "",
      address: selected.address || "",
      bloodType: selected.bloodType || "",
      insuranceProvider: selected.insuranceProvider || "",
      insurancePolicyNumber: selected.insurancePolicyNumber || "",
      insuranceExpiryDate: selected.insuranceExpiryDate ? String(selected.insuranceExpiryDate).slice(0, 10) : "",
      emergencyContactName: selected.emergencyContactName || "",
      emergencyContactPhone: selected.emergencyContactPhone || "",
      emergencyContactRelationship: selected.emergencyContactRelationship || "",
      notes: selected.notes || ""
    });
    setErrors({});
  }, [selected]);

  function update(name, value) {
    setForm((prev) => ({ ...prev, [name]: value }));
    setErrors((prev) => {
      if (!prev[name]) {
        return prev;
      }

      const next = { ...prev };
      delete next[name];
      return next;
    });
  }

  function submit(event) {
    event.preventDefault();

    const validationErrors = validatePatient(form);
    setErrors(validationErrors);

    if (Object.keys(validationErrors).length > 0) {
      return;
    }

    onSubmit({
      ...form,
      gender: Number(form.gender),
      insuranceExpiryDate: form.insuranceExpiryDate || null,
      phoneNumber: form.phoneNumber || null,
      email: form.email || null,
      address: form.address || null,
      bloodType: form.bloodType || null,
      insuranceProvider: form.insuranceProvider || null,
      insurancePolicyNumber: form.insurancePolicyNumber || null,
      emergencyContactName: form.emergencyContactName || null,
      emergencyContactPhone: form.emergencyContactPhone || null,
      emergencyContactRelationship: form.emergencyContactRelationship || null,
      notes: form.notes || null
    });
  }

  return (
    <form className="form-grid" onSubmit={submit}>
      <h3>{selected ? "Edit Patient" : "Create Patient"}</h3>
      <input value={form.firstName} maxLength={100} onChange={(e) => update("firstName", e.target.value)} placeholder="First name" required />
      {errors.firstName ? <span className="field-error">{errors.firstName}</span> : null}
      <input value={form.lastName} maxLength={100} onChange={(e) => update("lastName", e.target.value)} placeholder="Last name" required />
      {errors.lastName ? <span className="field-error">{errors.lastName}</span> : null}
      <input type="date" value={form.dateOfBirth} onChange={(e) => update("dateOfBirth", e.target.value)} required />
      {errors.dateOfBirth ? <span className="field-error">{errors.dateOfBirth}</span> : null}
      <select value={form.gender} onChange={(e) => update("gender", e.target.value)}>
        {genders.map((g, i) => (
          <option key={g} value={i}>
            {g}
          </option>
        ))}
      </select>
      <input value={form.phoneNumber} maxLength={20} onChange={(e) => update("phoneNumber", e.target.value)} placeholder="Phone" />
      {errors.phoneNumber ? <span className="field-error">{errors.phoneNumber}</span> : null}
      <input type="email" value={form.email} maxLength={256} onChange={(e) => update("email", e.target.value)} placeholder="Email" />
      {errors.email ? <span className="field-error">{errors.email}</span> : null}
      <input value={form.address} maxLength={500} onChange={(e) => update("address", e.target.value)} placeholder="Address" />
      {errors.address ? <span className="field-error">{errors.address}</span> : null}
      <input value={form.bloodType} maxLength={10} onChange={(e) => update("bloodType", e.target.value)} placeholder="Blood type" />
      {errors.bloodType ? <span className="field-error">{errors.bloodType}</span> : null}
      <input value={form.insuranceProvider} maxLength={200} onChange={(e) => update("insuranceProvider", e.target.value)} placeholder="Insurance provider" />
      {errors.insuranceProvider ? <span className="field-error">{errors.insuranceProvider}</span> : null}
      <input value={form.insurancePolicyNumber} maxLength={100} onChange={(e) => update("insurancePolicyNumber", e.target.value)} placeholder="Insurance policy number" />
      {errors.insurancePolicyNumber ? <span className="field-error">{errors.insurancePolicyNumber}</span> : null}
      <input type="date" value={form.insuranceExpiryDate} onChange={(e) => update("insuranceExpiryDate", e.target.value)} />
      {errors.insuranceExpiryDate ? <span className="field-error">{errors.insuranceExpiryDate}</span> : null}
      <input value={form.emergencyContactName} maxLength={200} onChange={(e) => update("emergencyContactName", e.target.value)} placeholder="Emergency contact name" />
      {errors.emergencyContactName ? <span className="field-error">{errors.emergencyContactName}</span> : null}
      <input value={form.emergencyContactPhone} maxLength={20} onChange={(e) => update("emergencyContactPhone", e.target.value)} placeholder="Emergency contact phone" />
      {errors.emergencyContactPhone ? <span className="field-error">{errors.emergencyContactPhone}</span> : null}
      <input value={form.emergencyContactRelationship} maxLength={100} onChange={(e) => update("emergencyContactRelationship", e.target.value)} placeholder="Emergency relationship" />
      {errors.emergencyContactRelationship ? <span className="field-error">{errors.emergencyContactRelationship}</span> : null}
      <textarea value={form.notes} maxLength={2000} onChange={(e) => update("notes", e.target.value)} placeholder="Notes" rows={3} />
      {errors.notes ? <span className="field-error">{errors.notes}</span> : null}
      <div className="actions">
        <button disabled={busy} type="submit">
          {busy ? "Saving..." : selected ? "Update" : "Create"}
        </button>
        {selected ? (
          <button type="button" className="muted" onClick={onCancel}>
            Cancel edit
          </button>
        ) : null}
      </div>
    </form>
  );
}
