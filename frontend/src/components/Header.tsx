import { useAuth0 } from "@auth0/auth0-react"

export function Header() {
    const { isAuthenticated, user, logout } = useAuth0();
    
    return(
        <header style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '0 24px',
            height: '52px',
            borderBottom: '1px solid rgba(255,255,255,0.06)',
            background: 'rgba(10,10,10,0.8)',
            backdropFilter: 'blur(12px)',
            position: 'sticky',
            top: 0,
            zIndex: 10,
            flexShrink: 0,
        }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                <div style={{
                    width: '22px', height: '22px',
                    background: 'linear-gradient(135deg, #3b82f6, #8b5cf6)',
                    borderRadius: '6px',
                    flexShrink: 0,
                }} />
                <span style={{ fontWeight: 600, fontSize: '15px', letterSpacing: '-0.02em', color: '#fff' }}>
                    AskDotNet
                </span>
                    <span style={{
                    fontSize: '11px', fontWeight: 500, color: '#525252',
                    background: 'rgba(255,255,255,0.04)',
                    border: '1px solid rgba(255,255,255,0.08)',
                    borderRadius: '4px', padding: '1px 6px', letterSpacing: '0.02em',
                    }}>
                    C# docs
                </span>
            </div>
            
            {isAuthenticated && user && (
                <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                    <span style={{ fontSize: '13px', color: '#737373', display: 'none' }}>{user.email}</span>
                    <button
                        onClick={() => logout({ logoutParams: { returnTo: window.location.origin } })}
                        style={{
                            fontSize: '12px', color: '#737373', background: 'none',
                            border: '1px solid rgba(255,255,255,0.08)', borderRadius: '6px',
                            padding: '4px 10px', cursor: 'pointer',
                        }}
                    >
                        Sign out
                    </button>
                </div>
            )}
        </header>
    );
}