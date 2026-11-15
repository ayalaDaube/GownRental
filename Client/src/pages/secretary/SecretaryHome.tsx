import { Link, useNavigate } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { logout } from '../../redux/Auth/AuthSlice';
import '../home/Home.css';

function SecretaryHome() {
  const cards = [
    {
      title: 'Quick Overview',
      description: 'Upcoming rentals, scheduled returns and gown inventory in one place.',
    },
    {
      title: 'Calm Professional Design',
      description: 'Soft colors, readable typography and an interface tailored for busy workdays.',
    },
    {
      title: 'Easy Access',
      description: "Ideal for a secretary managing appointments, customers and the gown stock.",
    },
  ];

  const gallery = [
    {
      label: 'Elegant Dress',
      description: 'A subtle combination of lace and calm tones.',
      src: 'https://images.unsplash.com/photo-1495121605193-b116b509b81?auto=format&fit=crop&w=800&q=80',
    },
    {
      label: 'Modest Tailored Dress',
      description: 'Clean lines, high collar and a professional feel.',
      src: 'https://images.unsplash.com/photo-1520975929932-2e007c6de34?auto=format&fit=crop&w=800&q=80',
    },
    {
      label: 'Festive Dress',
      description: 'A feminine and elegant look with a dignified structure.',
      src: 'https://images.unsplash.com/photo-1533106418985-7203c6f5d35?auto=format&fit=crop&w=800&q=80',
    },
  ];

  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const username = useAppSelector((state) => state.auth.username);

  return (
    <main className="home-page">
      <div className="secretary-entry">
        <button
          type="button"
          className="button button-outline"
          onClick={() => {
            dispatch(logout());
            navigate('/');
          }}
        >
          Sign out
        </button>
      </div>
      <section className="home-hero">
        <div className="hero-copy">
          <span className="eyebrow">Gown Rental System</span>
          <h1>Welcome{username ? `, ${username}` : ''}</h1>
          <p>
            This dashboard is designed for clarity and efficiency.
            All tools you need to manage gown rentals are available here.
          </p>
          <div className="hero-actions">
            <Link to="/rentals" className="button button-primary">Manage Rentals</Link>
            <Link to="/gowns" className="button button-secondary">Gown Inventory</Link>
            <Link to="/employee/appointments" className="button button-secondary">Appointments</Link>
          </div>
        </div>

        <div className="hero-visual">
          <div className="dress-outline dress-outline-1" />
          <div className="dress-outline dress-outline-2" />
          <div className="dress-outline dress-outline-3" />
        </div>
      </section>

      <section className="home-cards">
        {cards.map((card) => (
          <article key={card.title} className="info-card">
            <h2>{card.title}</h2>
            <p>{card.description}</p>
          </article>
        ))}
      </section>

      <section className="home-gallery">
        <div className="gallery-header">
          <h2>Modest Gown Samples</h2>
          <p>Inspiration collection for a clean and refined professional display.</p>
        </div>
        <div className="gallery-grid">
          {gallery.map((item) => (
            <article key={item.label} className="gallery-card">
              <div className="gallery-image">
                <img src={item.src} alt={item.label} />
              </div>
              <div>
                <strong>{item.label}</strong>
                <p>{item.description}</p>
              </div>
            </article>
          ))}
        </div>
      </section>
    </main>
  );
}

export default SecretaryHome;
