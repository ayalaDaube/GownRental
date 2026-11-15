import { Link } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { logout } from '../../redux/Auth/AuthSlice';
import './Home.css';

function Home() {
  const dispatch = useAppDispatch();
  const isSignedIn = useAppSelector((state) => Boolean(state.auth.token));

  const visitorCards = [
    {
      title: 'Easy Booking',
      description: 'Schedule your fitting appointment in just a few clicks.',
    },
    {
      title: 'Beautiful Collection',
      description: 'Browse curated gowns for every style and occasion.',
    },
    {
      title: 'Personalized Service',
      description: 'Get support from our team for a smooth rental experience.',
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

  return (
    <main className="home-page">
      <div className="secretary-entry">
        {isSignedIn ? (
          <>
            <Link to="/secretary" className="button button-outline">Dashboard</Link>
            <button type="button" className="button button-outline" onClick={() => dispatch(logout())}>
              Sign out
            </button>
          </>
        ) : (
          <Link to="/login" className="button button-outline">Staff sign-in</Link>
        )}
      </div>

      <section className="home-hero">
        <div className="hero-copy">
          <span className="eyebrow">Gown Rental</span>
          <h1>Your Rental Experience Starts Here</h1>
          <p>
            Browse gowns, book appointments, and enjoy a smooth rental experience for your special occasion.
          </p>
          <div className="hero-actions">
            <Link to="/appointments" className="button button-primary">Appointments</Link>
          </div>
        </div>

        <div className="hero-visual">
          <div className="dress-outline dress-outline-1" />
          <div className="dress-outline dress-outline-2" />
          <div className="dress-outline dress-outline-3" />
        </div>
      </section>

      <section className="home-cards">
        {visitorCards.map((card) => (
          <article key={card.title} className="info-card">
            <h2>{card.title}</h2>
            <p>{card.description}</p>
          </article>
        ))}
      </section>

      <section className="home-gallery">
        <div className="gallery-header">
          <h2>Featured Rental Dresses</h2>
          <p>
            Explore our curated collection of gowns available for rental and find your perfect match.
          </p>
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

export default Home;
