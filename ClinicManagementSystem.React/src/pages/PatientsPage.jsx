import { useEffect, useState } from "react";
import { apiRequest, queryCollection } from "../api/client";
import PatientsForm from "../components/PatientsForm";

export default function PatientsPage() {
  const [patients, setPatients] = useState([]);
  const [selected, setSelected] = useState(null);
  const [q, setQ] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);
  const [sortBy, setSortBy] = useState("lastName");
  const [sortDir, setSortDir] = useState("asc");
  const [busy, setBusy] = useState(false);
  const [status, setStatus] = useState("");

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  async function loadPatients(search = q, nextPage = page) {
    try {
      const data = await queryCollection("/api/patients/query", {
        q: search,
        page: nextPage,
        pageSize,
        sortBy,
        sortDir
      });

      setPatients(data.items || []);
      setTotalCount(data.totalCount || 0);
      setPage(data.page || nextPage);
      setStatus(`Loaded ${(data.items || []).length} of ${data.totalCount || 0} patient records.`);
    } catch (error) {
      setStatus(`Failed to load patients: ${error.message}`);
    }
  }

  useEffect(() => {
    loadPatients(q, page);
  }, [pageSize, sortBy, sortDir]);

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
      await loadPatients(q, page);
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
      const fallbackPage = patients.length === 1 && page > 1 ? page - 1 : page;
      await loadPatients(q, fallbackPage);
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
          <button type="button" onClick={() => { setPage(1); loadPatients(q, 1); }}>
            Search
          </button>
          <button type="button" className="muted" onClick={() => { setQ(""); setPage(1); loadPatients("", 1); }}>
            Reset
          </button>
        </div>

        <div className="inline-row">
          <label htmlFor="patientsSortBy">Sort by</label>
          <select id="patientsSortBy" value={sortBy} onChange={(e) => setSortBy(e.target.value)}>
            <option value="lastName">Last Name</option>
            <option value="firstName">First Name</option>
            <option value="dateOfBirth">Date of Birth</option>
            <option value="email">Email</option>
            <option value="createdAt">Created At</option>
          </select>

          <label htmlFor="patientsSortDir">Direction</label>
          <select id="patientsSortDir" value={sortDir} onChange={(e) => setSortDir(e.target.value)}>
            <option value="asc">Ascending</option>
            <option value="desc">Descending</option>
          </select>

          <label htmlFor="patientsPageSize">Page size</label>
          <select id="patientsPageSize" value={pageSize} onChange={(e) => { setPage(1); setPageSize(Number(e.target.value)); }}>
            <option value={5}>5</option>
            <option value={10}>10</option>
            <option value={20}>20</option>
            <option value={50}>50</option>
          </select>
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

        <div className="inline-row">
          <button type="button" className="muted" disabled={page <= 1} onClick={() => { const p = page - 1; setPage(p); loadPatients(q, p); }}>
            Previous
          </button>
          <span>Page {page} of {totalPages}</span>
          <button type="button" className="muted" disabled={page >= totalPages} onClick={() => { const p = page + 1; setPage(p); loadPatients(q, p); }}>
            Next
          </button>
        </div>

        <p className="status">{status}</p>
      </article>

      <article className="card">
        <PatientsForm selected={selected} onSubmit={handleSave} onCancel={() => setSelected(null)} busy={busy} />
      </article>
    </section>
  );
}
