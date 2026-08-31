import { useEffect, useState } from "react";

const roles = ["Admin", "Doctor", "Nurse", "Receptionist", "Patient"];
const phoneRegex = /^\+?[0-9\-\s()]{7,20}$/;

function validateStaff(form) {
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

  if (!form.email.trim()) {
    errors.email = "Email is required.";
  } else {
    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (form.email.length > 256 || !emailPattern.test(form.email)) {
      errors.email = "Email format is invalid.";
    }
  }

  if (form.phoneNumber && (!phoneRegex.test(form.phoneNumber) || form.phoneNumber.length > 20)) {
    errors.phoneNumber = "Phone number format is invalid.";
  }

  if (form.specialty.length > 200) {
    errors.specialty = "Specialty must be 200 characters or fewer.";
  }

  return errors;
}

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
      email: selected.email || "",
      phoneNumber: selected.phoneNumber || "",
      role: Number(selected.role ?? 1),
      specialty: selected.specialty || "",
      isAvailable: Boolean(selected.isAvailable)
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

    const validationErrors = validateStaff(form);
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) {
      return;
    }

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
      <input value={form.firstName} maxLength={100} onChange={(e) => update("firstName", e.target.value)} placeholder="First name" required />
      {errors.firstName ? <span className="field-error">{errors.firstName}</span> : null}
      <input value={form.lastName} maxLength={100} onChange={(e) => update("lastName", e.target.value)} placeholder="Last name" required />
      {errors.lastName ? <span className="field-error">{errors.lastName}</span> : null}
      <input type="email" value={form.email} maxLength={256} onChange={(e) => update("email", e.target.value)} placeholder="Email" required />
      {errors.email ? <span className="field-error">{errors.email}</span> : null}
      <input value={form.phoneNumber} maxLength={20} onChange={(e) => update("phoneNumber", e.target.value)} placeholder="Phone" />
      {errors.phoneNumber ? <span className="field-error">{errors.phoneNumber}</span> : null}
      <select value={form.role} onChange={(e) => update("role", e.target.value)}>
        {roles.map((role, index) => (
          <option key={role} value={index}>
            {role}
          </option>
        ))}
      </select>
      <input value={form.specialty} maxLength={200} onChange={(e) => update("specialty", e.target.value)} placeholder="Specialty" />
      {errors.specialty ? <span className="field-error">{errors.specialty}</span> : null}
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
