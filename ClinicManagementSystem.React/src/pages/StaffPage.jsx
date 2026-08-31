import { useEffect, useState } from "react";
import { apiRequest } from "../api/client";
import StaffForm from "../components/StaffForm";

const roleNames = ["Admin", "Doctor", "Nurse", "Receptionist", "Patient"];

export default function StaffPage() {
  const [staff, setStaff] = useState([]);
  const [selected, setSelected] = useState(null);
  const [busy, setBusy] = useState(false);
  const [status, setStatus] = useState("");

  async function loadStaff() {
    try {
      const data = await apiRequest("/api/staffmembers");
      setStaff(data);
      setStatus(`Loaded ${data.length} staff records.`);
    } catch (error) {
      setStatus(`Failed to load staff: ${error.message}`);
    }
  }

  useEffect(() => {
    loadStaff();
  }, []);

  async function handleSave(payload) {
    setBusy(true);
    try {
      if (selected?.id) {
        await apiRequest(`/api/staffmembers/${selected.id}`, {
          method: "PUT",
          body: JSON.stringify(payload)
        });
        setStatus("Staff member updated.");
      } else {
        await apiRequest("/api/staffmembers", {
          method: "POST",
          body: JSON.stringify(payload)
        });
        setStatus("Staff member created.");
      }

      setSelected(null);
      await loadStaff();
    } catch (error) {
      setStatus(`Save failed: ${error.message}`);
    } finally {
      setBusy(false);
    }
  }

  async function removeStaff(id) {
    if (!window.confirm("Delete this staff record?")) {
      return;
    }

    try {
      await apiRequest(`/api/staffmembers/${id}`, { method: "DELETE" });
      setStatus("Staff member deleted.");
      if (selected?.id === id) {
        setSelected(null);
      }
      await loadStaff();
    } catch (error) {
      setStatus(`Delete failed: ${error.message}`);
    }
  }

  return (
    <section className="page-grid">
      <article className="card">
        <h2>Staff</h2>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Role</th>
                <th>Available</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {staff.map((s) => (
                <tr key={s.id}>
                  <td>{s.firstName} {s.lastName}</td>
                  <td>{s.email}</td>
                  <td>{roleNames[Number(s.role)] || s.role}</td>
                  <td>{s.isAvailable ? "Yes" : "No"}</td>
                  <td>
                    <button type="button" onClick={() => setSelected(s)}>Edit</button>
                    <button type="button" className="danger" onClick={() => removeStaff(s.id)}>Delete</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <p className="status">{status}</p>
        <p className="hint">Staff endpoints require Admin role in the current API.</p>
      </article>

      <article className="card">
        <StaffForm selected={selected} onSubmit={handleSave} onCancel={() => setSelected(null)} busy={busy} />
      </article>
    </section>
  );
}
