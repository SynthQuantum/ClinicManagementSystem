import { useEffect, useState } from "react";
import { apiRequest } from "../api/client";
import PatientsForm from "../components/PatientsForm";

export default function PatientsPage() {
  const [patients, setPatients] = useState([]);
  const [selected, setSelected] = useState(null);
  const [q, setQ] = useState("");
  const [busy, setBusy] = useState(false);
  const [status, setStatus] = useState("");

  async function loadPatients(search = "") {
    try {
      const query = search ? `?q=${encodeURIComponent(search)}` : "";
      const data = await apiRequest(`/api/patients${query}`);
      setPatients(data);
      setStatus(`Loaded ${data.length} patient records.`);
    } catch (error) {
      setStatus(`Failed to load patients: ${error.message}`);
    }
  }

  useEffect(() => {
    loadPatients();
  }, []);

  async function handleSave(payload) {
    setBusy(true);
    try {
      if (selected?.id) {
        await apiRequest(`/api/patients/${selected.id}`, {
          method: "PUT",
          body: JSON.stringify(payload)
        });
        setStatus("Patient updated.");
      } else {
        await apiRequest("/api/patients", {
          method: "POST",
          body: JSON.stringify(payload)
        });
        setStatus("Patient created.");
      }

      setSelected(null);
      await loadPatients(q);
    } catch (error) {
      setStatus(`Save failed: ${error.message}`);
    } finally {
      setBusy(false);
    }
  }

  async function removePatient(id) {
    if (!window.confirm("Delete this patient record?")) {
      return;
    }

    try {
      await apiRequest(`/api/patients/${id}`, { method: "DELETE" });
      setStatus("Patient deleted.");
      if (selected?.id === id) {
        setSelected(null);
      }
      await loadPatients(q);
    } catch (error) {
      setStatus(`Delete failed: ${error.message}`);
    }
  }

  return (
    <section className="page-grid">
      <article className="card">
        <h2>Patients</h2>
        <div className="inline-row">
          <input value={q} onChange={(e) => setQ(e.target.value)} placeholder="Search by text" />
          <button type="button" onClick={() => loadPatients(q)}>
            Search
          </button>
          <button type="button" className="muted" onClick={() => { setQ(""); loadPatients(""); }}>
            Reset
          </button>
        </div>

        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>DOB</th>
                <th>Phone</th>
                <th>Email</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {patients.map((p) => (
                <tr key={p.id}>
                  <td>{p.firstName} {p.lastName}</td>
                  <td>{String(p.dateOfBirth || "").slice(0, 10)}</td>
                  <td>{p.phoneNumber || "-"}</td>
                  <td>{p.email || "-"}</td>
                  <td>
                    <button type="button" onClick={() => setSelected(p)}>Edit</button>
                    <button type="button" className="danger" onClick={() => removePatient(p.id)}>Delete</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <p className="status">{status}</p>
      </article>

      <article className="card">
        <PatientsForm selected={selected} onSubmit={handleSave} onCancel={() => setSelected(null)} busy={busy} />
      </article>
    </section>
  );
}
