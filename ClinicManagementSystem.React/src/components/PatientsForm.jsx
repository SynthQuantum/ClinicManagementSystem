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

export default function PatientsForm({ selected, onSubmit, onCancel, busy }) {
  const [form, setForm] = useState(initialForm);

  useEffect(() => {
    if (!selected) {
      setForm(initialForm);
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
  }, [selected]);

  function update(name, value) {
    setForm((prev) => ({ ...prev, [name]: value }));
  }

  function submit(event) {
    event.preventDefault();

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
      <input value={form.firstName} onChange={(e) => update("firstName", e.target.value)} placeholder="First name" required />
      <input value={form.lastName} onChange={(e) => update("lastName", e.target.value)} placeholder="Last name" required />
      <input type="date" value={form.dateOfBirth} onChange={(e) => update("dateOfBirth", e.target.value)} required />
      <select value={form.gender} onChange={(e) => update("gender", e.target.value)}>
        {genders.map((g, i) => (
          <option key={g} value={i}>
            {g}
          </option>
        ))}
      </select>
      <input value={form.phoneNumber} onChange={(e) => update("phoneNumber", e.target.value)} placeholder="Phone" />
      <input type="email" value={form.email} onChange={(e) => update("email", e.target.value)} placeholder="Email" />
      <input value={form.address} onChange={(e) => update("address", e.target.value)} placeholder="Address" />
      <input value={form.bloodType} onChange={(e) => update("bloodType", e.target.value)} placeholder="Blood type" />
      <input value={form.insuranceProvider} onChange={(e) => update("insuranceProvider", e.target.value)} placeholder="Insurance provider" />
      <input value={form.insurancePolicyNumber} onChange={(e) => update("insurancePolicyNumber", e.target.value)} placeholder="Insurance policy number" />
      <input type="date" value={form.insuranceExpiryDate} onChange={(e) => update("insuranceExpiryDate", e.target.value)} />
      <input value={form.emergencyContactName} onChange={(e) => update("emergencyContactName", e.target.value)} placeholder="Emergency contact name" />
      <input value={form.emergencyContactPhone} onChange={(e) => update("emergencyContactPhone", e.target.value)} placeholder="Emergency contact phone" />
      <input value={form.emergencyContactRelationship} onChange={(e) => update("emergencyContactRelationship", e.target.value)} placeholder="Emergency relationship" />
      <textarea value={form.notes} onChange={(e) => update("notes", e.target.value)} placeholder="Notes" rows={3} />
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
