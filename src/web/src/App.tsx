import { Routes, Route } from 'react-router-dom';

function HomePage() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100">
      <div className="text-center">
        <h1 className="text-5xl font-bold text-gray-900 mb-4">
          Koinon RMS
        </h1>
        <p className="text-xl text-gray-600 mb-8">
          Modern Church Management System
        </p>
        <div className="space-x-4">
          <a
            href="/people"
            className="inline-block px-6 py-3 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 transition-colors touch-target"
          >
            People
          </a>
          <a
            href="/checkin"
            className="inline-block px-6 py-3 bg-indigo-600 text-white font-medium rounded-lg hover:bg-indigo-700 transition-colors touch-target"
          >
            Check-in
          </a>
        </div>
      </div>
    </div>
  );
}

function PeoplePage() {
  return (
    <div className="p-8">
      <h1 className="text-3xl font-bold mb-4">People</h1>
      <p className="text-gray-600">People management coming soon...</p>
    </div>
  );
}

function CheckinPage() {
  return (
    <div className="p-8">
      <h1 className="text-3xl font-bold mb-4">Check-in</h1>
      <p className="text-gray-600">Check-in kiosk coming soon...</p>
    </div>
  );
}

function NotFoundPage() {
  return (
    <div className="min-h-screen flex items-center justify-center">
      <div className="text-center">
        <h1 className="text-6xl font-bold text-gray-900 mb-4">404</h1>
        <p className="text-xl text-gray-600 mb-8">Page not found</p>
        <a
          href="/"
          className="inline-block px-6 py-3 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 transition-colors"
        >
          Go Home
        </a>
      </div>
    </div>
  );
}

function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/people" element={<PeoplePage />} />
      <Route path="/checkin" element={<CheckinPage />} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}

export default App;
