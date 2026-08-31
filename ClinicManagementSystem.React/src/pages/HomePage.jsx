import { Link } from "react-router-dom";

export default function HomePage() {
  return (
    <section className="home-grid">
      <article className="card home-card">
        <h2>Patients Management</h2>
        <p>Create, update, search, sort, and paginate patient records.</p>
        <Link className="tab" to="/patients">
          Open Patients Page
        </Link>
      </article>

      <article className="card home-card">
        <h2>Staff Management</h2>
        <p>Create, update, sort, and paginate staff records.</p>
        <Link className="tab" to="/staff">
          Open Staff Page
        </Link>
      </article>
    </section>
  );
}
