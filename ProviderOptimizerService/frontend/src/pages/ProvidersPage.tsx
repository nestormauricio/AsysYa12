import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { providerService, optimizationService, Provider, OptimizationResult } from '../services/api';

export default function ProvidersPage() {
  const navigate = useNavigate();
  const [providers, setProviders] = useState<Provider[]>([]);
  const [results, setResults] = useState<OptimizationResult[]>([]);
  const [loading, setLoading] = useState(true);
  const [optimizing, setOptimizing] = useState(false);
  const [lat, setLat] = useState('-12.046374');
  const [lon, setLon] = useState('-77.042793');
  const [filterType, setFilterType] = useState('');
  const user = JSON.parse(localStorage.getItem('user') || '{}');

  useEffect(() => {
    const token = localStorage.getItem('token');
    if (!token) { navigate('/'); return; }
    loadProviders();
  }, []);

  const loadProviders = async () => {
    setLoading(true);
    try {
      const { data } = await providerService.getAvailable(filterType || undefined);
      setProviders(data);
    } catch {
      navigate('/');
    } finally {
      setLoading(false);
    }
  };

  const handleOptimize = async () => {
    setOptimizing(true);
    setResults([]);
    try {
      const { data } = await optimizationService.optimize(
        parseFloat(lat), parseFloat(lon), filterType || undefined
      );
      setResults(data);
    } catch (e) {
      console.error(e);
    } finally {
      setOptimizing(false);
    }
  };

  const handleLogout = () => {
    localStorage.clear();
    navigate('/');
  };

  const scoreBar = (value: number) => (
    <div style={{ background: '#1e293b', borderRadius: 4, height: 6, width: 80, display: 'inline-block', verticalAlign: 'middle', marginLeft: 8 }}>
      <div style={{ background: '#38bdf8', width: `${Math.min(value * 100, 100)}%`, height: '100%', borderRadius: 4 }} />
    </div>
  );

  return (
    <div style={s.page}>
      {/* Header */}
      <header style={s.header}>
        <span style={s.logo}>🚗 ASISYA — Provider Optimizer</span>
        <span style={s.userBadge}>
          {user.username} ({user.role})
          <button onClick={handleLogout} style={s.logoutBtn}>Salir</button>
        </span>
      </header>

      {/* Optimization Panel */}
      <section style={s.panel}>
        <h2 style={s.sectionTitle}>Optimización de Proveedor</h2>
        <div style={s.row}>
          <div style={s.field}>
            <label style={s.label}>Latitud</label>
            <input style={s.input} value={lat} onChange={e => setLat(e.target.value)} />
          </div>
          <div style={s.field}>
            <label style={s.label}>Longitud</label>
            <input style={s.input} value={lon} onChange={e => setLon(e.target.value)} />
          </div>
          <div style={s.field}>
            <label style={s.label}>Tipo</label>
            <select style={s.input} value={filterType} onChange={e => setFilterType(e.target.value)}>
              <option value="">Todos</option>
              <option value="1">Grúa</option>
              <option value="2">Cerrajería</option>
              <option value="3">Batería</option>
              <option value="4">Neumático</option>
            </select>
          </div>
          <button style={s.btn} onClick={handleOptimize} disabled={optimizing}>
            {optimizing ? '⏳ Calculando...' : '🎯 Optimizar'}
          </button>
          <button style={{ ...s.btn, background: '#334155' }} onClick={loadProviders}>🔄 Actualizar</button>
        </div>

        {results.length > 0 && (
          <div style={{ marginTop: '1.5rem' }}>
            <h3 style={s.tableTitle}>Ranking de Proveedores Óptimos</h3>
            <table style={s.table}>
              <thead>
                <tr style={s.thead}>
                  <th style={s.th}>#</th>
                  <th style={s.th}>Proveedor</th>
                  <th style={s.th}>Score</th>
                  <th style={s.th}>Distancia</th>
                  <th style={s.th}>ETA</th>
                  <th style={s.th}>Llegada Est.</th>
                  <th style={s.th}>Teléfono</th>
                </tr>
              </thead>
              <tbody>
                {results.map((r, i) => (
                  <tr key={r.providerId} style={{ background: i % 2 === 0 ? '#1e293b' : '#0f172a' }}>
                    <td style={s.td}>{i === 0 ? '🥇' : i === 1 ? '🥈' : i === 2 ? '🥉' : i + 1}</td>
                    <td style={s.td}>{r.providerName}</td>
                    <td style={s.td}>
                      {r.score.toFixed(3)} {scoreBar(r.score)}
                    </td>
                    <td style={s.td}>{r.distanceKm.toFixed(1)} km</td>
                    <td style={s.td}>{r.estimatedMinutes.toFixed(0)} min</td>
                    <td style={s.td}>{new Date(r.estimatedArrival).toLocaleTimeString()}</td>
                    <td style={s.td}>{r.providerPhone ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {/* Providers Table */}
      <section style={s.panel}>
        <h2 style={s.sectionTitle}>Proveedores Disponibles</h2>
        {loading ? (
          <p style={{ color: '#94a3b8' }}>Cargando...</p>
        ) : (
          <table style={s.table}>
            <thead>
              <tr style={s.thead}>
                <th style={s.th}>Nombre</th>
                <th style={s.th}>Tipo</th>
                <th style={s.th}>Rating</th>
                <th style={s.th}>Asignaciones Activas</th>
                <th style={s.th}>Total</th>
                <th style={s.th}>Estado</th>
              </tr>
            </thead>
            <tbody>
              {providers.map((p, i) => (
                <tr key={p.id} style={{ background: i % 2 === 0 ? '#1e293b' : '#0f172a' }}>
                  <td style={s.td}>{p.name}</td>
                  <td style={s.td}>{p.type}</td>
                  <td style={s.td}>{'⭐'.repeat(Math.round(p.rating))} ({p.rating})</td>
                  <td style={s.td}>{p.activeAssignments}</td>
                  <td style={s.td}>{p.totalAssignments}</td>
                  <td style={s.td}>
                    <span style={{ color: p.isAvailable ? '#4ade80' : '#f87171', fontWeight: 600 }}>
                      {p.isAvailable ? '✅ Disponible' : '🔴 No disponible'}
                    </span>
                  </td>
                </tr>
              ))}
              {providers.length === 0 && (
                <tr><td colSpan={6} style={{ ...s.td, textAlign: 'center', color: '#94a3b8' }}>No hay proveedores disponibles.</td></tr>
              )}
            </tbody>
          </table>
        )}
      </section>
    </div>
  );
}

const s: Record<string, React.CSSProperties> = {
  page: { minHeight: '100vh', background: '#0f172a', color: '#f1f5f9', fontFamily: 'Inter, system-ui, sans-serif' },
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '1rem 2rem', background: '#1e293b', borderBottom: '1px solid #334155' },
  logo: { fontSize: '1.2rem', fontWeight: 700, color: '#38bdf8' },
  userBadge: { color: '#94a3b8', fontSize: '0.875rem', display: 'flex', alignItems: 'center', gap: 12 },
  logoutBtn: { background: '#ef4444', color: '#fff', border: 'none', borderRadius: 6, padding: '0.3rem 0.8rem', cursor: 'pointer', fontSize: '0.875rem' },
  panel: { margin: '2rem', background: '#1e293b', borderRadius: 12, padding: '1.5rem', boxShadow: '0 4px 20px rgba(0,0,0,0.3)' },
  sectionTitle: { color: '#38bdf8', marginTop: 0, marginBottom: '1rem' },
  tableTitle: { color: '#94a3b8', fontSize: '0.95rem', marginBottom: '0.75rem' },
  row: { display: 'flex', gap: '1rem', flexWrap: 'wrap', alignItems: 'flex-end' },
  field: { display: 'flex', flexDirection: 'column', gap: 4 },
  label: { color: '#94a3b8', fontSize: '0.8rem' },
  input: { padding: '0.5rem 0.75rem', borderRadius: 8, border: '1px solid #334155', background: '#0f172a', color: '#f1f5f9', fontSize: '0.9rem', minWidth: 140 },
  btn: { padding: '0.55rem 1.2rem', background: '#38bdf8', color: '#0f172a', border: 'none', borderRadius: 8, fontWeight: 700, cursor: 'pointer', fontSize: '0.9rem' },
  table: { width: '100%', borderCollapse: 'collapse', fontSize: '0.875rem' },
  thead: { background: '#0f172a' },
  th: { padding: '0.75rem 1rem', textAlign: 'left', color: '#94a3b8', fontWeight: 600, borderBottom: '1px solid #334155' },
  td: { padding: '0.65rem 1rem', color: '#e2e8f0', borderBottom: '1px solid #1e293b' },
};
