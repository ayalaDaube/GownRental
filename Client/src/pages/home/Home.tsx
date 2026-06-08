import { Link, useNavigate } from 'react-router-dom';
import { useState } from 'react';
import './Home.css';

function Home() {
  const navigate = useNavigate();
  const [isSecretary, setIsSecretary] = useState(false);
  const [showPasswordBox, setShowPasswordBox] = useState(false);
  const [passwordInput, setPasswordInput] = useState('');
  const [passwordError, setPasswordError] = useState('');

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

  const secretaryCards = [
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
      description: 'Ideal for a secretary managing appointments, customers and the gown stock.',
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
        <button
          type="button"
          className="button button-outline"
          onClick={() => {
            if (isSecretary) {
              localStorage.removeItem('isSecretary');
              setIsSecretary(false);
              setShowPasswordBox(false);
              navigate('/');
            } else {
              setPasswordError('');
              setShowPasswordBox(true);
            }
          }}
        >
          {isSecretary ? 'Exit Secretary' : 'Enter as Secretary'}
        </button>
      </div>

      {showPasswordBox && !isSecretary ? (
        <div
          className="password-overlay"
          onClick={() => {
            setShowPasswordBox(false);
            setPasswordInput('');
            setPasswordError('');
          }}
        >
          <div className="password-card" onClick={(event) => event.stopPropagation()}>
            <label className="password-label" htmlFor="secretary-password">
              Secretary Password
            </label>
            <input
              id="secretary-password"
              type="password"
              className="password-input"
              value={passwordInput}
              onChange={(event) => setPasswordInput(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === 'Enter') {
                  event.preventDefault();
                  if (passwordInput === '1234') {
                    localStorage.setItem('isSecretary', 'true');
                    setIsSecretary(true);
                    setShowPasswordBox(false);
                    setPasswordInput('');
                    navigate('/secretary');
                  } else {
                    setPasswordError('Incorrect password');
                  }
                }
              }}
            />
            <div className="password-actions">
              <button
                type="button"
                className="button button-primary"
                onClick={() => {
                  if (passwordInput === '1234') {
                    localStorage.setItem('isSecretary', 'true');
                    setIsSecretary(true);
                    setShowPasswordBox(false);
                    setPasswordInput('');
                    navigate('/secretary');
                  } else {
                    setPasswordError('Incorrect password');
                  }
                }}
              >
                Submit
              </button>
              <button
                type="button"
                className="button button-outline"
                onClick={() => {
                  setShowPasswordBox(false);
                  setPasswordInput('');
                  setPasswordError('');
                }}
              >
                Cancel
              </button>
            </div>
            {passwordError && <p className="password-error">{passwordError}</p>}
          </div>
        </div>
      ) : null}

      <section className="home-hero">
        <div className="hero-copy">
          <span className="eyebrow">Gown Rental</span>
          <h1>{isSecretary ? 'Welcome' : 'Your Rental Experience Starts Here'}</h1>
          <p>
            {isSecretary
              ? 'This dashboard is designed for clarity and efficiency. All the information and tools needed to manage gown rentals are available here.'
              : 'Browse gowns, book appointments, and enjoy a smooth rental experience for your special occasion.'}
          </p>
          <div className="hero-actions">
            {isSecretary ? (
              <>
                <Link to="/rentals" className="button button-primary">Manage Rentals</Link>
                <Link to="/gowns" className="button button-secondary">Gown Inventory</Link>
                <Link to="/appointments" className="button button-secondary">Appointments</Link>
              </>
            ) : (
              <Link to="/appointments" className="button button-primary">Appointments</Link>
            )}
          </div>
        </div>

        <div className="hero-visual">
          <div className="dress-outline dress-outline-1" />
          <div className="dress-outline dress-outline-2" />
          <div className="dress-outline dress-outline-3" />
        </div>
      </section>

      <section className="home-cards">
        {(isSecretary ? secretaryCards : visitorCards).map((card) => (
          <article key={card.title} className="info-card">
            <h2>{card.title}</h2>
            <p>{card.description}</p>
          </article>
        ))}
      </section>

      <section className="home-gallery">
        <div className="gallery-header">
          <h2>{isSecretary ? 'Modest Gown Samples' : 'Featured Rental Dresses'}</h2>
          <p>
            {isSecretary
              ? 'Inspiration collection for a clean and refined professional display.'
              : 'Explore our curated collection of gowns available for rental and find your perfect match.'}
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
