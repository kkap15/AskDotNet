import { useAuth0 } from '@auth0/auth0-react';

type Props = {
  currentInput: string;
  setCurrentInput: (v: string) => void;
  isThinking: boolean;
  handleSubmit: () => void;
  guestQuestionCount: number;
  guestLimit: number;
};

export function InputBar({ currentInput, setCurrentInput, isThinking, handleSubmit, guestQuestionCount, guestLimit }: Props) {
  const { isAuthenticated, loginWithRedirect } = useAuth0();
  const remainQuestions = guestLimit - guestQuestionCount;
  
  return (
    <div style={{
      borderTop: '1px solid rgba(255,255,255,0.06)',
      padding: '16px 24px 20px',
      paddingBottom: 'max(20px, env(safe-area-inset-bottom))',
      background: 'rgba(10,10,10,0.8)',
      backdropFilter: 'blur(12px)',
      flexShrink: 0,
    }}>
      <div style={{ maxWidth: '680px', margin: '0 auto' }}>
        {!isAuthenticated && (
          <div style={{display: 'flex', justifyContent: 'space-between', alignItems: 'center',
            marginBottom: '8px', padding: '6px 10px',
            background: remainQuestions <= 1 ? 'rgba(239,68,68,0.08)' : 'rgba(59,130,246,0.08)',
            border: `1px solid ${remainQuestions <= 1 ? 'rgba(239,68,68,0.2)' : 'rgba(59,130,246,0.2)'}`,
            borderRadius: '8px',
          }}>
            <span style={{ fontSize: '12px', color: '#737373'}}>
              {remainQuestions > 0 ? `Guest mode - ${remainQuestions} question${remainQuestions !== 1 ? 's' : ''} remaining` : 'Guest limit reached'}
            </span>
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
        )} 
        
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
            disabled={isThinking || (!isAuthenticated && remainQuestions <= 0)}
            placeholder={!isAuthenticated && remainQuestions <= 0 ? 'Please sign in to ask more questions' : "Ask about C# docs..."}
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
        <p style={{ fontSize: '11px', color: '#3d3d3d', textAlign: 'center', marginTop: '10px', marginBottom: 0 }}>
          Answers grounded in Microsoft Learn C# documentation
        </p>
      </div>
    </div>
  );
}