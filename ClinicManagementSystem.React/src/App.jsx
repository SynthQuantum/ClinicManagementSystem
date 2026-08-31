import { NavLink, Navigate, Route, Routes } from "react-router-dom";
import AuthPanel from "./components/AuthPanel";
import HomePage from "./pages/HomePage";
import PatientsPage from "./pages/PatientsPage";
import StaffPage from "./pages/StaffPage";

export default function App() {
  return (
    <div className="app-shell">
      <header className="topbar">
        <h1>Clinic React Admin</h1>
        <p>Patients and Staff CRUD operations</p>
      </header>

      <AuthPanel />

      <nav className="tabs" aria-label="Data sections">
        <NavLink to="/" end className={({ isActive }) => (isActive ? "tab active" : "tab")}>
          Home
        </NavLink>
        <NavLink to="/patients" className={({ isActive }) => (isActive ? "tab active" : "tab")}>
          Patients
        </NavLink>
        <NavLink to="/staff" className={({ isActive }) => (isActive ? "tab active" : "tab")}>
          Staff
        </NavLink>
      </nav>

      <main>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/patients" element={<PatientsPage />} />
          <Route path="/staff" element={<StaffPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </div>
  );
}
