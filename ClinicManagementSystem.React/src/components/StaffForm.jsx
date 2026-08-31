import { useEffect, useState } from "react";

const roles = ["Admin", "Doctor", "Nurse", "Receptionist", "Patient"];

const initialForm = {
  firstName: "",
  lastName: "",
  email: "",
  phoneNumber: "",
  role: 1,
  specialty: "",
  isAvailable: true
};

export default function StaffForm({ selected, onSubmit, onCancel, busy }) {
  const [form, setForm] = useState(initialForm);

  useEffect(() => {
    if (!selected) {
      setForm(initialForm);
      return;
    }

    setForm({
      firstName: selected.firstName || "",
      lastName: selected.lastName || "",
      email: selected.email || "",
      phoneNumber: selected.phoneNumber || "",
      role: Number(selected.role ?? 1),
      specialty: selected.specialty || "",
      isAvailable: Boolean(selected.isAvailable)
    });
  }, [selected]);

  function update(name, value) {
    setForm((prev) => ({ ...prev, [name]: value }));
  }

  function submit(event) {
    event.preventDefault();

    onSubmit({
      ...form,
      role: Number(form.role),
      phoneNumber: form.phoneNumber || null,
      specialty: form.specialty || null,
      isAvailable: Boolean(form.isAvailable)
    });
  }

  return (
    <form className="form-grid" onSubmit={submit}>
      <h3>{selected ? "Edit Staff" : "Create Staff"}</h3>
      <input value={form.firstName} onChange={(e) => update("firstName", e.target.value)} placeholder="First name" required />
      <input value={form.lastName} onChange={(e) => update("lastName", e.target.value)} placeholder="Last name" required />
      <input type="email" value={form.email} onChange={(e) => update("email", e.target.value)} placeholder="Email" required />
      <input value={form.phoneNumber} onChange={(e) => update("phoneNumber", e.target.value)} placeholder="Phone" />
      <select value={form.role} onChange={(e) => update("role", e.target.value)}>
        {roles.map((role, index) => (
          <option key={role} value={index}>
            {role}
          </option>
        ))}
      </select>
      <input value={form.specialty} onChange={(e) => update("specialty", e.target.value)} placeholder="Specialty" />
      <label className="checkbox-row">
        <input
          type="checkbox"
          checked={form.isAvailable}
          onChange={(e) => update("isAvailable", e.target.checked)}
        />
        Available
      </label>
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
