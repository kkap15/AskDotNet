import ReactMarkdown from "react-markdown";
import type { Message } from "../types/Message";

type Props = { message: Message };

const StreamingCursor = () => (
    <span className="inline-block w-0.5 h-4 bg-current ml-0.5 align-middle"
        style={{ animation: 'blink 1s step-end infinite '}} />
);

export function MessageBubble({message} : Props) {
    const isUser = message.role === 'user';
    
    return (
        <div style={{
            display: 'flex',
            justifyContent: isUser ? 'flex-end' : 'flex-start',
        }}>
            {!isUser && (
                <div style={{
                    width: '26px', height: '26px',
                    background: 'linear-gradient(135deg, #3b82f6, #8b5cf6)',
                    borderRadius: '7px', flexShrink: 0,
                    marginRight: '10px', marginTop: '2px',
                    display: 'flex', alignItems: 'center',
                    justifyContent: 'center', fontSize: '12px',
                }}>
                ✦
                </div>
            )}
            <div style={{
                maxWidth: '560px',
                padding: isUser ? '9px 14px' : '10px 0',
                borderRadius: isUser ? '14px 14px 4px 14px' : '0',
                background: isUser ? 'linear-gradient(135deg, #2563eb, #1d4ed8)' : 'transparent',
                fontSize: '14px', lineHeight: '1.65',
                color: isUser ? '#fff' : '#d4d4d4',
                boxShadow: isUser ? '0 1px 3px rgba(0,0,0,0.3)' : 'none',
            }}>
            {message.status === 'thinking' && message.content === '' ? (
                <div style={{ display: 'flex', gap: '4px', alignItems: 'center', padding: '4px 0' }}>
                    {[0, 1, 2].map(i => (
                        <div key={i} style={{
                            width: '5px', height: '5px', borderRadius: '50%',
                            background: '#525252',
                            animation: `blink 1.2s ease-in-out ${i * 0.2}s infinite`,
                        }} />
                    ))}
                </div>
            ) : (
                <div className={isUser ? 'prose-user' : 'prose-dark'}>
                    <ReactMarkdown>{message.content}</ReactMarkdown>
                    {message.status === 'thinking' && <StreamingCursor />}
                </div>
            )}
        </div>
    </div>
    );
}