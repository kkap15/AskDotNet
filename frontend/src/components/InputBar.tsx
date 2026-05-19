import { useAuth0 } from '@auth0/auth0-react';

type Props = {
  currentInput: string;
  setCurrentInput: (v: string) => void;
  isThinking: boolean;
  handleSubmit: () => void;
};

export function InputBar({ currentInput, setCurrentInput, isThinking, handleSubmit }: Props) {
  const { isAuthenticated, loginWithRedirect } = useAuth0();

  return (
    <div style={{
      borderTop: '1px solid rgba(255,255,255,0.06)',
      padding: '16px 24px 20px',
      background: 'rgba(10,10,10,0.8)',
      backdropFilter: 'blur(12px)',
      flexShrink: 0,
    }}>
      <div style={{ maxWidth: '680px', margin: '0 auto' }}>
        {!isAuthenticated ? (
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '8px', padding: '8px 0' }}>
            <p style={{ fontSize: '13px', color: '#525252', margin: 0 }}>
              Sign in to start asking questions
            </p>
            <button
              onClick={() => loginWithRedirect()}
              style={{
                padding: '8px 20px', background: '#fff', color: '#0a0a0a',
                border: 'none', borderRadius: '8px', fontSize: '13px',
                fontWeight: 600, cursor: 'pointer', letterSpacing: '-0.01em',
              }}
            >
              Sign in
            </button>
          </div>
        ) : (
          <div style={{
            display: 'flex', gap: '8px', alignItems: 'center',
            background: 'rgba(255,255,255,0.04)',
            border: '1px solid rgba(255,255,255,0.08)',
            borderRadius: '12px', padding: '6px 6px 6px 14px',
          }}>
            <input
              type="text"
              value={currentInput}
              onChange={e => setCurrentInput(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && void handleSubmit()}
              disabled={isThinking}
              placeholder="Ask about C# docs..."
              style={{
                flex: 1, background: 'none', border: 'none', outline: 'none',
                color: '#e5e5e5', fontSize: '14px', caretColor: '#3b82f6',
              }}
            />
            <button
              disabled={!currentInput.trim() || isThinking}
              onClick={handleSubmit}
              style={{
                padding: '7px 14px',
                background: currentInput.trim() && !isThinking ? '#3b82f6' : 'rgba(255,255,255,0.06)',
                color: currentInput.trim() && !isThinking ? '#fff' : '#525252',
                border: 'none', borderRadius: '8px', fontSize: '13px',
                fontWeight: 500, cursor: currentInput.trim() && !isThinking ? 'pointer' : 'not-allowed',
              }}
            >
              {isThinking ? 'Thinking...' : 'Send'}
            </button>
          </div>
        )}
        <p style={{ fontSize: '11px', color: '#3d3d3d', textAlign: 'center', marginTop: '10px', marginBottom: 0 }}>
          Answers grounded in Microsoft Learn C# documentation
        </p>
      </div>
    </div>
  );
}