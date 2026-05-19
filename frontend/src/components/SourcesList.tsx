import type { ChunkReference } from '../types/ChunkReference';

type Props = { sources: ChunkReference[] };

export function SourcesList({ sources }: Props) {
  if (sources.length === 0) return null;

  return (
    <div style={{
      marginTop: '16px', marginLeft: '36px',
      padding: '14px 16px',
      background: 'rgba(255,255,255,0.03)',
      border: '1px solid rgba(255,255,255,0.07)',
      borderRadius: '10px',
    }}>
      <p style={{
        fontSize: '11px', fontWeight: 600, color: '#525252',
        letterSpacing: '0.06em', textTransform: 'uppercase',
        marginBottom: '10px', marginTop: 0,
      }}>
        Sources
      </p>
      <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'flex', flexDirection: 'column', gap: '8px' }}>
        {sources.map((source, index) => (
          <li key={index} style={{ display: 'flex', alignItems: 'baseline', gap: '8px' }}>
            <a
              href={source.SourceUrl}
              target="_blank"
              rel="noopener noreferrer"
              style={{ fontSize: '13px', color: '#a3a3a3', textDecoration: 'none', flex: 1 }}
            >
              <span style={{ color: '#737373', fontSize: '12px', marginRight: '6px' }}>↗</span>
              {source.SourceTitle}
              <span style={{ color: '#525252', margin: '0 4px' }}>·</span>
              <span style={{ color: '#737373' }}>{source.SectionHeading}</span>
            </a>
            <span style={{
              fontSize: '11px', fontWeight: 500, fontFamily: 'Geist Mono, monospace',
              color: source.Similarity > 0.65 ? '#34d399' : source.Similarity > 0.5 ? '#fbbf24' : '#737373',
              flexShrink: 0,
            }}>
              {Math.round(source.Similarity * 100)}%
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}