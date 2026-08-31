import { useEffect, useState } from "react";
import { apiRequest, queryCollection } from "../api/client";
import StaffForm from "../components/StaffForm";

const roleNames = ["Admin", "Doctor", "Nurse", "Receptionist", "Patient"];

export default function StaffPage() {
  const [staff, setStaff] = useState([]);
  const [selected, setSelected] = useState(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);
  const [sortBy, setSortBy] = useState("lastName");
  const [sortDir, setSortDir] = useState("asc");
  const [busy, setBusy] = useState(false);
  const [status, setStatus] = useState("");

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  async function loadStaff(nextPage = page) {
    try {
      const data = await queryCollection("/api/staffmembers/query", {
        page: nextPage,
        pageSize,
        sortBy,
        sortDir
      });

      setStaff(data.items || []);
      setTotalCount(data.totalCount || 0);
      setPage(data.page || nextPage);
      setStatus(`Loaded ${(data.items || []).length} of ${data.totalCount || 0} staff records.`);
    } catch (error) {
      setStatus(`Failed to load staff: ${error.message}`);
    }
  }

  useEffect(() => {
    loadStaff(page);
  }, [pageSize, sortBy, sortDir]);

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
      await loadStaff(page);
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
      const fallbackPage = staff.length === 1 && page > 1 ? page - 1 : page;
      await loadStaff(fallbackPage);
    } catch (error) {
      setStatus(`Delete failed: ${error.message}`);
    }
  }

  return (
    <section className="page-grid">
      <article className="card">
        <h2>Staff</h2>
        <div className="inline-row">
          <label htmlFor="staffSortBy">Sort by</label>
          <select id="staffSortBy" value={sortBy} onChange={(e) => setSortBy(e.target.value)}>
            <option value="lastName">Last Name</option>
            <option value="firstName">First Name</option>
            <option value="email">Email</option>
            <option value="role">Role</option>
            <option value="isAvailable">Availability</option>
          </select>

          <label htmlFor="staffSortDir">Direction</label>
          <select id="staffSortDir" value={sortDir} onChange={(e) => setSortDir(e.target.value)}>
            <option value="asc">Ascending</option>
            <option value="desc">Descending</option>
          </select>

          <label htmlFor="staffPageSize">Page size</label>
          <select id="staffPageSize" value={pageSize} onChange={(e) => { setPage(1); setPageSize(Number(e.target.value)); }}>
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

        <div className="inline-row">
          <button type="button" className="muted" disabled={page <= 1} onClick={() => { const p = page - 1; setPage(p); loadStaff(p); }}>
            Previous
          </button>
          <span>Page {page} of {totalPages}</span>
          <button type="button" className="muted" disabled={page >= totalPages} onClick={() => { const p = page + 1; setPage(p); loadStaff(p); }}>
            Next
          </button>
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
